using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.DTOs.StatsDTOs;
using SmartQueue.Core.DTOs.TicketDTOs;

namespace SmartQueueApp.Services
{
    // Generic result wrapper — never throws, always returns success/failure
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }

        public static ApiResult<T> Ok(T data) =>
            new() { Success = true, Data = data, StatusCode = 200 };

        public static ApiResult<T> Fail(string error, int code = 0) =>
            new() { Success = false, ErrorMessage = error, StatusCode = code };
    }

    public interface IApiService
    {
        // ── Auth ──────────────────────────────────────────────────────────────
        Task<ApiResult<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ApiResult<AuthResponseDto>> RefreshTokenAsync(
            string token, string refreshToken);
        Task<ApiResult<bool>> LogoutAsync(string refreshToken);
        Task<ApiResult<UserResponseDto>> GetMeAsync();
        Task<ApiResult<List<UserResponseDto>>> GetUsersAsync();
        Task<ApiResult<bool>> RegisterStaffAsync(RegisterStaffDto dto);
        Task<ApiResult<bool>> DeactivateUserAsync(string userId);
        Task<ApiResult<bool>> ActivateUserAsync(string userId);

        // ── Queue ─────────────────────────────────────────────────────────────
        Task<ApiResult<List<QueueResponseDto>>> GetQueuesAsync();
        Task<ApiResult<QueueResponseDto>> GetQueueAsync(int id);
        Task<ApiResult<QueueResponseDto>> CreateQueueAsync(CreateQueueDto dto);
        Task<ApiResult<bool>> UpdateQueueAsync(int id, UpdateQueueDto dto);
        Task<ApiResult<bool>> UpdateQueueStatusAsync(int id, string status);
        Task<ApiResult<bool>> DeleteQueueAsync(int id);
        Task<ApiResult<QueueStatusDto>> GetQueueEstimateAsync(int id);

        // ── Ticket ────────────────────────────────────────────────────────────
        Task<ApiResult<TicketResponseDto>> TakeTicketAsync(TakeTicketDto dto);
        Task<ApiResult<TicketResponseDto>> GetTicketAsync(int id);
        Task<ApiResult<List<TicketResponseDto>>> GetQueueTicketsAsync(int queueId);
        Task<ApiResult<bool>> CallTicketAsync(int id, UpdateTicketStatusDto dto);
        Task<ApiResult<bool>> CompleteTicketAsync(int id);
        Task<ApiResult<bool>> SkipTicketAsync(int id);
        Task<ApiResult<TicketHistoryResponseDto>> GetTicketHistoryAsync(
            TicketHistoryQueryDto query);

        // ── Counter ───────────────────────────────────────────────────────────
        Task<ApiResult<List<CounterResponseDto>>> GetCountersAsync(int queueId);
        Task<ApiResult<CounterResponseDto>> GetCounterAsync(int id);

        // Single call to find the counter assigned to the current user.
        // Replaces the N+1 loop in DjelatnikController that called GetQueuesAsync()
        // + GetCountersAsync() for every queue until a match was found.
        Task<ApiResult<CounterResponseDto>> GetMyCounterAsync();
        Task<ApiResult<CounterResponseDto>> CreateCounterAsync(CreateCounterDto dto);
        Task<ApiResult<bool>> UpdateCounterAsync(int id, UpdateCounterDto dto);
        Task<ApiResult<bool>> OpenCounterAsync(int id);
        Task<ApiResult<bool>> CloseCounterAsync(int id);
        Task<ApiResult<bool>> AssignUserToCounterAsync(int id, AssignUserDto dto);
        Task<ApiResult<bool>> DeleteCounterAsync(int id);

        // ── Stats ─────────────────────────────────────────────────────────────
        Task<ApiResult<OverviewStatsDto>> GetOverviewStatsAsync();
        Task<ApiResult<QueueSummaryStatsDto>> GetQueueStatsAsync(int queueId);
        Task<ApiResult<QueuePeakHoursDto>> GetPeakHoursAsync(int queueId);
        Task<ApiResult<List<CounterStatsDto>>> GetCounterStatsAsync(int queueId);
    }
}

/*
 *Why ApiResult<T> wrapper: Controllers should never crash because the API returned 404 or 500.
 *This wrapper forces every caller to handle both success and failure paths explicitly.
 *No unhandled exceptions, no yellow error screens.
 

Why an interface: The MVC app depends on IApiService not ApiService
* — this means we can write unit tests by mocking the interface, 
* and swap implementations without changing controllers.*/