using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;
using SmartQueue.Core.DTOs.StatsDTOs;

namespace SmartQueueApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IApiService _api;

        public AdminController(IApiService api)
        {
            _api = api;
        }

        // ── GET /admin ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetOverviewStatsAsync();
            return View(new AdminDashboardViewModel
            {
                Overview = result.Data,
                ErrorMessage = result.Success ? null : result.ErrorMessage
            });
        }

        // ── GET /admin/queues ─────────────────────────────────────────────────
        public async Task<IActionResult> Queues()
        {
            var result = await _api.GetQueuesAsync();
            return View(new AdminQueuesViewModel
            {
                Queues = result.Data ?? new(),
                ErrorMessage = result.Success ? null : result.ErrorMessage
            });
        }

        // ── GET /admin/queuedetail/{id} ───────────────────────────────────────
        public async Task<IActionResult> QueueDetail(int id)
        {
            var queueTask = _api.GetQueueAsync(id);
            var statsTask = _api.GetQueueStatsAsync(id);
            var countersTask = _api.GetCountersAsync(id);
            var peakTask = _api.GetPeakHoursAsync(id);
            var cStatsTask = _api.GetCounterStatsAsync(id);
            // FIX: also load staff here so the assign dropdown is available
            // on the counter table without a separate page.
            var staffTask = _api.GetUsersAsync();

            await Task.WhenAll(
                queueTask, statsTask, countersTask,
                peakTask, cStatsTask, staffTask);

            if (!queueTask.Result.Success)
                return RedirectToAction("Queues");

            return View(new AdminQueueDetailViewModel
            {
                Queue = queueTask.Result.Data,
                Stats = statsTask.Result.Data,
                Counters = countersTask.Result.Data ?? new(),
                PeakHours = peakTask.Result.Data,
                CounterStats = cStatsTask.Result.Data ?? new(),
                // Only active Djelatnik accounts — no point assigning deactivated staff
                AvailableStaff = staffTask.Result.Data?
                    .Where(u => u.Role == "Djelatnik" && u.IsActive)
                    .ToList() ?? new()
            });
        }

        // ── GET /admin/queue/create ───────────────────────────────────────────
        public IActionResult CreateQueue()
            => View(new CreateQueueViewModel());

        // ── POST /admin/queue/create ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQueue(CreateQueueViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.CreateQueueAsync(new CreateQueueDto
            {
                Name = model.Name,
                Description = model.Description,
                DefaultServiceMinutes = model.DefaultServiceMinutes
            });

            if (!result.Success)
            {
                model.ErrorMessage = result.ErrorMessage;
                return View(model);
            }

            TempData["Success"] = $"Queue '{model.Name}' created successfully.";
            return RedirectToAction("Queues");
        }

        // ── POST /admin/queue/{id}/status ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQueueStatus(int id, string status)
        {
            var result = await _api.UpdateQueueStatusAsync(id, status);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? $"Queue status updated to {status}."
                : result.ErrorMessage;
            return RedirectToAction("Queues");
        }

        // ── POST /admin/queue/{id}/delete ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQueue(int id)
        {
            var result = await _api.DeleteQueueAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Queue deleted successfully."
                : result.ErrorMessage;
            return RedirectToAction("Queues");
        }

        // ── GET /admin/counter/create?queueId={id} ────────────────────────────
        public async Task<IActionResult> CreateCounter(int queueId)
        {
            var staff = await _api.GetUsersAsync();
            return View(new CreateCounterViewModel
            {
                QueueId = queueId,
                AvailableStaff = staff.Data?
                    .Where(u => u.Role == "Djelatnik" && u.IsActive)
                    .ToList() ?? new()
            });
        }

        // ── POST /admin/counter/create ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCounter(CreateCounterViewModel model)
        {
            var result = await _api.CreateCounterAsync(new CreateCounterDto
            {
                Name = model.Name,
                QueueId = model.QueueId,
                AssignedUserId = string.IsNullOrEmpty(model.AssignedUserId)
                    ? null : model.AssignedUserId
            });

            if (!result.Success)
            {
                var staff = await _api.GetUsersAsync();
                model.AvailableStaff = staff.Data?
                    .Where(u => u.Role == "Djelatnik" && u.IsActive)
                    .ToList() ?? new();
                model.ErrorMessage = result.ErrorMessage;
                return View(model);
            }

            TempData["Success"] = $"Counter '{model.Name}' created successfully.";
            return RedirectToAction("QueueDetail", new { id = model.QueueId });
        }

        // ── POST /admin/counter/{id}/assign ───────────────────────────────────
        // NEW: assign (or reassign) a Djelatnik to an existing counter inline
        // from the QueueDetail page — no separate page needed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCounter(AssignCounterViewModel model)
        {
            var result = await _api.AssignUserToCounterAsync(
                model.CounterId,
                new AssignUserDto { UserId = model.UserId });

            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Staff member assigned to counter."
                : result.ErrorMessage;

            return RedirectToAction("QueueDetail", new { id = model.QueueId });
        }

        // ── POST /admin/counter/{id}/delete ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCounter(int id, int queueId)
        {
            var result = await _api.DeleteCounterAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Counter deleted successfully."
                : result.ErrorMessage;
            return RedirectToAction("QueueDetail", new { id = queueId });
        }

        // ── GET /admin/staff ──────────────────────────────────────────────────
        public async Task<IActionResult> Staff()
        {
            var result = await _api.GetUsersAsync();
            return View(new AdminStaffViewModel
            {
                Users = result.Data ?? new(),
                ErrorMessage = result.Success ? null : result.ErrorMessage
            });
        }

        // ── GET /admin/staff/create ───────────────────────────────────────────
        public IActionResult CreateStaff()
            => View(new CreateStaffViewModel());

        // ── POST /admin/staff/create ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.RegisterStaffAsync(new RegisterStaffDto
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role
            });

            if (!result.Success)
            {
                model.ErrorMessage = result.ErrorMessage;
                return View(model);
            }

            TempData["Success"] =
                $"{model.Role} account created for {model.FirstName} {model.LastName}.";
            return RedirectToAction("Staff");
        }

        // ── POST /admin/staff/{id}/deactivate ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateStaff(string id)
        {
            var result = await _api.DeactivateUserAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Staff member deactivated."
                : result.ErrorMessage;
            return RedirectToAction("Staff");
        }

        // ── POST /admin/staff/{id}/activate ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateStaff(string id)
        {
            var result = await _api.ActivateUserAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Staff member activated."
                : result.ErrorMessage;
            return RedirectToAction("Staff");
        }

        // ── GET /admin/statistics ─────────────────────────────────────────────
        public async Task<IActionResult> Statistics()
        {
            var overviewTask = _api.GetOverviewStatsAsync();
            var queuesTask = _api.GetQueuesAsync();
            await Task.WhenAll(overviewTask, queuesTask);

            var queueStats =
                new List<SmartQueue.Core.DTOs.StatsDTOs.QueueSummaryStatsDto>();

            if (queuesTask.Result.Success && queuesTask.Result.Data != null)
            {
                var statsTasks = queuesTask.Result.Data
                    .Select(q => _api.GetQueueStatsAsync(q.Id))
                    .ToList();

                await Task.WhenAll(statsTasks);

                queueStats = statsTasks
                    .Where(t => t.Result.Success && t.Result.Data != null)
                    .Select(t => t.Result.Data!)
                    .ToList();
            }

            return View(new AdminStatisticsViewModel
            {
                Overview = overviewTask.Result.Data,
                QueueStats = queueStats
            });
        }
    }
}

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * 1. QueueDetail — added staffTask to the Task.WhenAll block so active
 *    Djelatnik accounts are loaded alongside counters. Stored in
 *    AdminQueueDetailViewModel.AvailableStaff for the assign dropdown.
 *
 * 2. AssignCounter (NEW POST action) — calls AssignUserToCounterAsync with
 *    the chosen UserId, then redirects back to QueueDetail. The form lives
 *    inline in the counter table row in QueueDetail.cshtml — no separate page.
 *
 * WHY INLINE ASSIGN INSTEAD OF A SEPARATE PAGE
 * ──────────────────────────────────────────────
 * Assigning a staff member is a one-field operation (pick from a dropdown).
 * A dedicated page would add unnecessary navigation. The inline dropdown in
 * the counter row keeps the admin on the queue detail page and the interaction
 * is immediate — select + submit, done.
 *
 * WHY Task.WhenAll WITH 6 TASKS NOW
 * ───────────────────────────────────
 * Adding staffTask to the parallel block costs nothing — all 6 calls fire
 * simultaneously. The total page load time is still determined by the slowest
 * single call, not the sum of all calls.
 */
