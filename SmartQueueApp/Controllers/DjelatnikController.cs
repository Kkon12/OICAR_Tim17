using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.TicketDTOs;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Controllers
{
    [Authorize(Roles = "Djelatnik,Admin")]
    public class DjelatnikController : Controller
    {
        private readonly IApiService _api;
        private readonly TokenService _tokenService;

        public DjelatnikController(IApiService api, TokenService tokenService)
        {
            _api = api;
            _tokenService = tokenService;
        }

        // ── GET /djelatnik ────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            // FIX: was a N+1 loop — called GetQueuesAsync() then
            // GetCountersAsync(queueId) for every queue until a match was found.
            // Every action (call, complete, skip) redirects here, so this fired
            // on every single button click. With 5 queues = 6 HTTP calls each time.
            // Now: 1 call to GetMyCounterAsync() + 1 call for tickets = 2 total.
            var counterResult = await _api.GetMyCounterAsync();

            // 404 = no counter assigned — valid state, not an error
            if (!counterResult.Success && counterResult.StatusCode != 404)
                return View(new DjelatnikDashboardViewModel
                {
                    ErrorMessage = counterResult.ErrorMessage
                });

            var myCounter = counterResult.Data;
            List<TicketResponseDto> waitingTickets = new();

            if (myCounter != null)
            {
                // Load waiting tickets for the counter's queue
                var tickets = await _api.GetQueueTicketsAsync(myCounter.QueueId);
                if (tickets.Success && tickets.Data != null)
                    waitingTickets = tickets.Data;
            }

            // Separate "currently being served" from the waiting queue.
            // Called / InService tickets shown at top; Waiting tickets in table.
            var currentTicket = waitingTickets
                .FirstOrDefault(t => t.Status == "Called"
                                  || t.Status == "InService");

            return View(new DjelatnikDashboardViewModel
            {
                MyCounter = myCounter,
                WaitingTickets = waitingTickets
                    .Where(t => t.Status == "Waiting")
                    .ToList(),
                CurrentTicket = currentTicket
            });
        }

        // ── POST /djelatnik/counter/open ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenCounter(int id)
        {
            var result = await _api.OpenCounterAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Counter opened. Ready to serve!"
                : result.ErrorMessage;
            return RedirectToAction("Index");
        }

        // ── POST /djelatnik/counter/close ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseCounter(int id)
        {
            var result = await _api.CloseCounterAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Counter closed. Good work today!"
                : result.ErrorMessage;
            return RedirectToAction("Index");
        }

        // ── POST /djelatnik/ticket/call ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallTicket(int ticketId, int counterId)
        {
            var result = await _api.CallTicketAsync(ticketId,
                new UpdateTicketStatusDto
                {
                    CounterId = counterId,
                    Status = "Called"
                });

            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Ticket called!"
                : result.ErrorMessage;
            return RedirectToAction("Index");
        }

        // ── POST /djelatnik/ticket/complete ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTicket(int ticketId)
        {
            var result = await _api.CompleteTicketAsync(ticketId);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Ticket completed. Well done!"
                : result.ErrorMessage;
            return RedirectToAction("Index");
        }

        // ── POST /djelatnik/ticket/skip ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SkipTicket(int ticketId)
        {
            var result = await _api.SkipTicketAsync(ticketId);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Ticket skipped."
                : result.ErrorMessage;
            return RedirectToAction("Index");
        }
    }
}

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * Index() — replaced N+1 loop with GetMyCounterAsync() + GetQueueTicketsAsync().
 *
 * Before:
 *   GetQueuesAsync()                          → 1 call
 *   GetCountersAsync(queue.Id) per queue      → N calls
 *   GetQueueTicketsAsync(queue.Id) on match   → 1 call
 *   Total: N+2 calls on every page load and every action redirect.
 *
 * After:
 *   GetMyCounterAsync()                       → 1 call (GET api/counter/mine)
 *   GetQueueTicketsAsync(counter.QueueId)     → 1 call
 *   Total: 2 calls always, regardless of queue count.
 *
 * WHY 404 IS NOT AN ERROR
 * ────────────────────────
 * GetMyCounterAsync() returns 404 when no counter is assigned to this user.
 * That is a normal, expected state — the Djelatnik simply hasn't been assigned
 * yet. The view handles it with the "No counter assigned" empty state card.
 * Only non-404 failures (503, 401, etc.) show an error message.
 *
 * WHY [Authorize(Roles = "Djelatnik,Admin")]
 * ───────────────────────────────────────────
 * Admins need access to test counter operations and provide oversight.
 * Role-based UI differences are handled in the layout, not by blocking here.
 */