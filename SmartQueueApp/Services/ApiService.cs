using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.DTOs.StatsDTOs;
using SmartQueue.Core.DTOs.TicketDTOs;

namespace SmartQueueApp.Services
{
    // ── Croatian DateTime Converters ──────────────────────────────────────────
    public class CroatianDateTimeConverter : JsonConverter<DateTime>
    {
        private const string Format = "dd/MM/yyyy HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParseExact(str, Format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
            return DateTime.Parse(str ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(
                value.ToString(Format, CultureInfo.InvariantCulture));
    }

    public class CroatianNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        private const string Format = "dd/MM/yyyy HH:mm:ss";

        public override DateTime? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str)) return null;
            if (DateTime.TryParseExact(str, Format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
            return DateTime.Parse(str);
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(
                    value.Value.ToString(Format, CultureInfo.InvariantCulture));
            else
                writer.WriteNullValue();
        }
    }

    // ── ApiService ────────────────────────────────────────────────────────────
    public class ApiService : IApiService
    {
        private readonly IHttpClientFactory _factory;
        private readonly TokenService _tokenService;
        private readonly ILogger<ApiService> _logger;

        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new CroatianDateTimeConverter(),
                new CroatianNullableDateTimeConverter()
            }
        };

        public ApiService(
            IHttpClientFactory factory,
            TokenService tokenService,
            ILogger<ApiService> logger)
        {
            _factory = factory;
            _tokenService = tokenService;
            _logger = logger;
        }

        // ── Core HTTP helpers ─────────────────────────────────────────────────

        private HttpClient CreateClient()
        {
            var client = _factory.CreateClient("SmartQueueAPI");
            var jwt = _tokenService.GetJwt();
            if (!string.IsNullOrEmpty(jwt))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            return client;
        }

        private async Task<ApiResult<T>> SendAsync<T>(
            HttpMethod method, string url, object? body = null)
        {
            try
            {
                // KEY FIX: only attempt refresh when a token actually exists.
                // If hasToken is false the request is intentionally anonymous
                // (e.g. kiosk loading queues, kiosk taking a ticket).
                // The API itself is the authoritative gate for those endpoints.
                //
                // BEFORE this fix: IsJwtExpired() returned true for anonymous
                // requests (no cookie = "expired"), causing TryRefreshTokenAsync
                // to fail and short-circuit with "Session expired" before the
                // request ever left the app — breaking the entire kiosk flow.
                var hasToken = !string.IsNullOrEmpty(_tokenService.GetJwt());
                if (hasToken && _tokenService.IsJwtExpired() && !url.Contains("auth"))
                {
                    var refreshed = await TryRefreshTokenAsync();
                    if (!refreshed)
                        return ApiResult<T>.Fail(
                            "Session expired. Please login again.", 401);
                }

                var client = CreateClient();
                var request = new HttpRequestMessage(method, url);

                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(
                        json, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request);
                return await ParseResponse<T>(response);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Request to {Url} timed out", url);
                return ApiResult<T>.Fail("Request timed out. Please try again.", 408);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling {Url}", url);
                return ApiResult<T>.Fail(
                    "Cannot connect to server. Is the API running?", 503);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling {Url}", url);
                return ApiResult<T>.Fail("An unexpected error occurred.", 500);
            }
        }

        private async Task<ApiResult<T>> ParseResponse<T>(
            HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    if (typeof(T) == typeof(bool))
                        return ApiResult<T>.Ok((T)(object)true);

                    if (string.IsNullOrWhiteSpace(content))
                        return ApiResult<T>.Ok(default!);

                    var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return data != null
                        ? ApiResult<T>.Ok(data)
                        : ApiResult<T>.Fail("Empty response from server");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex,
                        "Failed to deserialize response: {Content}", content);
                    return ApiResult<T>.Fail("Invalid response format from server");
                }
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "You are not authorized. Please login again.",
                HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
                HttpStatusCode.NotFound => "The requested resource was not found.",
                HttpStatusCode.BadRequest => ExtractErrorMessage(content),
                HttpStatusCode.Conflict => ExtractErrorMessage(content),
                HttpStatusCode.ServiceUnavailable => "Service temporarily unavailable.",
                _ => $"Server error ({(int)response.StatusCode})"
            };

            return ApiResult<T>.Fail(errorMessage, (int)response.StatusCode);
        }

        private static string ExtractErrorMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "Bad request.";
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? "Bad request.";
                if (doc.RootElement.TryGetProperty("title", out var title))
                    return title.GetString() ?? "Bad request.";
            }
            catch { }
            return content.Length > 200 ? content[..200] : content;
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                // Re-check inside the lock — another thread may have refreshed
                // already while this one was waiting.
                if (!_tokenService.IsJwtExpired()) return true;

                var jwt = _tokenService.GetJwt();
                var refreshToken = _tokenService.GetRefreshToken();

                if (string.IsNullOrEmpty(jwt) || string.IsNullOrEmpty(refreshToken))
                    return false;

                var client = _factory.CreateClient("SmartQueueAPI");
                var body = JsonSerializer.Serialize(
                    new { token = jwt, refreshToken });

                var resp = await client.PostAsync("api/auth/refresh-token",
                    new StringContent(body, Encoding.UTF8, "application/json"));

                if (!resp.IsSuccessStatusCode) return false;

                var responseContent = await resp.Content.ReadAsStringAsync();
                var authResp = JsonSerializer.Deserialize<AuthResponseDto>(
                    responseContent, _jsonOptions);

                if (authResp == null) return false;

                // FIX: StoreTokens no longer takes a userId parameter.
                // The user ID is decoded from the new JWT itself via
                // TokenService.GetUserId() — always in sync with the API.
                _tokenService.StoreTokens(
                    authResp.Token,
                    authResp.RefreshToken,
                    authResp.Role,
                    authResp.FirstName,
                    authResp.LastName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        // ── AUTH ──────────────────────────────────────────────────────────────
        public async Task<ApiResult<AuthResponseDto>> LoginAsync(LoginDto dto)
            => await SendAsync<AuthResponseDto>(
                HttpMethod.Post, "api/auth/login", dto);

        public async Task<ApiResult<AuthResponseDto>> RefreshTokenAsync(
            string token, string refreshToken)
            => await SendAsync<AuthResponseDto>(
                HttpMethod.Post, "api/auth/refresh-token",
                new { token, refreshToken });

        public async Task<ApiResult<bool>> LogoutAsync(string refreshToken)
            => await SendAsync<bool>(
                HttpMethod.Post, "api/auth/logout", refreshToken);

        public async Task<ApiResult<UserResponseDto>> GetMeAsync()
            => await SendAsync<UserResponseDto>(
                HttpMethod.Get, "api/auth/me");

        public async Task<ApiResult<List<UserResponseDto>>> GetUsersAsync()
            => await SendAsync<List<UserResponseDto>>(
                HttpMethod.Get, "api/auth/users");

        public async Task<ApiResult<bool>> RegisterStaffAsync(RegisterStaffDto dto)
            => await SendAsync<bool>(
                HttpMethod.Post, "api/auth/register-staff", dto);

        public async Task<ApiResult<bool>> DeactivateUserAsync(string userId)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/auth/users/{userId}/deactivate");

        public async Task<ApiResult<bool>> ActivateUserAsync(string userId)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/auth/users/{userId}/activate");

        // ── QUEUE ─────────────────────────────────────────────────────────────
        public async Task<ApiResult<List<QueueResponseDto>>> GetQueuesAsync()
            => await SendAsync<List<QueueResponseDto>>(
                HttpMethod.Get, "api/queue");

        public async Task<ApiResult<QueueResponseDto>> GetQueueAsync(int id)
            => await SendAsync<QueueResponseDto>(
                HttpMethod.Get, $"api/queue/{id}");

        public async Task<ApiResult<QueueResponseDto>> CreateQueueAsync(
            CreateQueueDto dto)
            => await SendAsync<QueueResponseDto>(
                HttpMethod.Post, "api/queue", dto);

        public async Task<ApiResult<bool>> UpdateQueueAsync(
            int id, UpdateQueueDto dto)
            => await SendAsync<bool>(
                HttpMethod.Put, $"api/queue/{id}", dto);

        public async Task<ApiResult<bool>> UpdateQueueStatusAsync(
            int id, string status)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/queue/{id}/status", status);

        public async Task<ApiResult<bool>> DeleteQueueAsync(int id)
            => await SendAsync<bool>(
                HttpMethod.Delete, $"api/queue/{id}");

        public async Task<ApiResult<QueueStatusDto>> GetQueueEstimateAsync(int id)
            => await SendAsync<QueueStatusDto>(
                HttpMethod.Get, $"api/queue/{id}/estimate");

        // ── TICKET ────────────────────────────────────────────────────────────
        public async Task<ApiResult<TicketResponseDto>> TakeTicketAsync(
            TakeTicketDto dto)
            => await SendAsync<TicketResponseDto>(
                HttpMethod.Post, "api/ticket/take", dto);

        public async Task<ApiResult<TicketResponseDto>> GetTicketAsync(int id)
            => await SendAsync<TicketResponseDto>(
                HttpMethod.Get, $"api/ticket/{id}");

        public async Task<ApiResult<List<TicketResponseDto>>> GetQueueTicketsAsync(
            int queueId)
            => await SendAsync<List<TicketResponseDto>>(
                HttpMethod.Get, $"api/ticket/queue/{queueId}");

        public async Task<ApiResult<bool>> CallTicketAsync(
            int id, UpdateTicketStatusDto dto)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/ticket/{id}/call", dto);

        public async Task<ApiResult<bool>> CompleteTicketAsync(int id)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/ticket/{id}/complete");

        public async Task<ApiResult<bool>> SkipTicketAsync(int id)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/ticket/{id}/skip");

        public async Task<ApiResult<TicketHistoryResponseDto>> GetTicketHistoryAsync(
            TicketHistoryQueryDto query)
        {
            var url = $"api/ticket/history?page={query.Page}&pageSize={query.PageSize}";
            if (query.QueueId.HasValue) url += $"&queueId={query.QueueId}";
            if (!string.IsNullOrEmpty(query.Status)) url += $"&status={query.Status}";
            if (query.DateFrom.HasValue) url += $"&dateFrom={query.DateFrom:yyyy-MM-dd}";
            if (query.DateTo.HasValue) url += $"&dateTo={query.DateTo:yyyy-MM-dd}";
            return await SendAsync<TicketHistoryResponseDto>(HttpMethod.Get, url);
        }

        // ── COUNTER ───────────────────────────────────────────────────────────
        public async Task<ApiResult<List<CounterResponseDto>>> GetCountersAsync(
            int queueId)
            => await SendAsync<List<CounterResponseDto>>(
                HttpMethod.Get, $"api/counter/queue/{queueId}");

        public async Task<ApiResult<CounterResponseDto>> GetCounterAsync(int id)
            => await SendAsync<CounterResponseDto>(
                HttpMethod.Get, $"api/counter/{id}");

        public async Task<ApiResult<CounterResponseDto>> GetMyCounterAsync()
             => await SendAsync<CounterResponseDto>(
                  HttpMethod.Get, "api/counter/mine");

        public async Task<ApiResult<CounterResponseDto>> CreateCounterAsync(
            CreateCounterDto dto)
            => await SendAsync<CounterResponseDto>(
                HttpMethod.Post, "api/counter", dto);

        public async Task<ApiResult<bool>> UpdateCounterAsync(
            int id, UpdateCounterDto dto)
            => await SendAsync<bool>(
                HttpMethod.Put, $"api/counter/{id}", dto);

        public async Task<ApiResult<bool>> OpenCounterAsync(int id)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/counter/{id}/open");

        public async Task<ApiResult<bool>> CloseCounterAsync(int id)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/counter/{id}/close");

        public async Task<ApiResult<bool>> AssignUserToCounterAsync(
            int id, AssignUserDto dto)
            => await SendAsync<bool>(
                HttpMethod.Patch, $"api/counter/{id}/assign", dto);

        public async Task<ApiResult<bool>> DeleteCounterAsync(int id)
            => await SendAsync<bool>(
                HttpMethod.Delete, $"api/counter/{id}");

        // ── STATS ─────────────────────────────────────────────────────────────
        public async Task<ApiResult<OverviewStatsDto>> GetOverviewStatsAsync()
            => await SendAsync<OverviewStatsDto>(
                HttpMethod.Get, "api/stats/overview");

        public async Task<ApiResult<QueueSummaryStatsDto>> GetQueueStatsAsync(
            int queueId)
            => await SendAsync<QueueSummaryStatsDto>(
                HttpMethod.Get, $"api/stats/queue/{queueId}");

        public async Task<ApiResult<QueuePeakHoursDto>> GetPeakHoursAsync(
            int queueId)
            => await SendAsync<QueuePeakHoursDto>(
                HttpMethod.Get, $"api/stats/queue/{queueId}/peakhours");

        public async Task<ApiResult<List<CounterStatsDto>>> GetCounterStatsAsync(
            int queueId)
            => await SendAsync<List<CounterStatsDto>>(
                HttpMethod.Get, $"api/stats/queue/{queueId}/counters");
    }
}

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * 1. SendAsync — added `var hasToken` guard before the JWT expiry check.
 *    Previously: no JWT cookie → IsJwtExpired() returns true → refresh fails
 *    → "Session expired" returned before any network call — kiosk broken.
 *    Now: no JWT cookie → skip the whole block → request goes to API → API
 *    decides (public endpoint passes, protected endpoint returns 401 normally).
 *
 * 2. TryRefreshTokenAsync — removed userId from StoreTokens() call.
 *    AuthResponseDto has no UserId field. The user ID now lives exclusively
 *    in the JWT claims and is read via TokenService.GetUserId().
 *
 * WHY SemaphoreSlim ON REFRESH
 * ─────────────────────────────
 * Without it, multiple concurrent requests with an expired token all race to
 * refresh simultaneously — each call revokes the previous refresh token,
 * causing all but one to fail. The lock serializes this to exactly one refresh.
 *
 * WHY hasToken && NOT just removing the check entirely
 * ─────────────────────────────────────────────────────
 * The expiry check must still fire for authenticated users whose token has
 * genuinely expired mid-session. Removing it entirely would let stale tokens
 * keep hitting authenticated API endpoints until the API rejects them with a
 * 401, which would surface as a confusing error rather than a clean redirect
 * to login. The guard only bypasses the check when there is genuinely no token.
 */
