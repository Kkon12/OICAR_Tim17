using Microsoft.AspNetCore.Mvc;
using SmartQueue.Core.DTOs.TicketDTOs;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Controllers
{
    // No [Authorize] — the kiosk is a public-facing screen.
    // Customers walk up and take a number without any account.
    public class KioskController : Controller
    {
        private readonly IApiService _api;

        public KioskController(IApiService api)
        {
            _api = api;
        }

        // ── GET /kiosk ────────────────────────────────────────────────────────
        // Queue selection screen — loads all active queues for the customer
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetQueuesAsync();

            return View(new KioskViewModel
            {
                ActiveQueues = result.Data?
                    .Where(q => q.Status == "Active")
                    .ToList() ?? new(),
                ErrorMessage = result.Success ? null : result.ErrorMessage
            });
        }

        // ── POST /kiosk/take ──────────────────────────────────────────────────
        // Called when customer taps a queue card.
        // On success  → redirect to the ticket confirmation screen.
        // On failure  → return to Index with the error visible (not a silent
        //               redirect that swallows the message).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeTicket(int queueId)
        {
            var result = await _api.TakeTicketAsync(new TakeTicketDto
            {
                QueueId = queueId,
                UserId = null   // anonymous — kiosk tickets are not linked to accounts
            });

            if (!result.Success)
            {
                // FIX: previously this did RedirectToAction("Index") which
                // dropped the error message entirely — the customer saw the
                // queue list again with no explanation. Now we re-load the
                // queues and render Index directly so the alert is visible.
                var queues = await _api.GetQueuesAsync();
                return View("Index", new KioskViewModel
                {
                    ActiveQueues = queues.Data?
                        .Where(q => q.Status == "Active")
                        .ToList() ?? new(),
                    ErrorMessage = result.ErrorMessage
                });
            }

            return RedirectToAction("Ticket", new { id = result.Data!.Id });
        }

        // ── GET /kiosk/ticket/{id} ────────────────────────────────────────────
        // Ticket confirmation screen shown after a successful take.
        public async Task<IActionResult> Ticket(int id)
        {
            var result = await _api.GetTicketAsync(id);

            return View(new TicketTakenViewModel
            {
                Ticket = result.Data,
                ErrorMessage = result.Success ? null : result.ErrorMessage
            });
        }
    }
}

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * TakeTicket failure path: was RedirectToAction("Index") which silently
 * discarded result.ErrorMessage. Replaced with a direct View("Index", model)
 * call that re-loads the queue list and passes the error into the view model
 * so the alert renders correctly on the kiosk screen.
 *
 * WHY NO [Authorize]
 * ───────────────────
 * The kiosk is intentionally public. Any [Authorize] attribute would redirect
 * the customer to the login page, which makes no sense for a walk-up terminal.
 * The API itself controls whether anonymous ticket-taking is permitted.
 *
 * WHY UserId = null
 * ──────────────────
 * Kiosk tickets are anonymous — the customer gets a printed/displayed number
 * and watches the board. There is no account to link the ticket to. Mobile app
 * users would pass their UserId here to get push notifications, but that is a
 * separate flow outside the kiosk.
 *
 * WHY re-load queues on failure instead of TempData + redirect
 * ─────────────────────────────────────────────────────────────
 * TempData survives one redirect, so it would technically work. However,
 * the kiosk screen is meant to be completely stateless and self-contained —
 * a failed ticket attempt should leave the screen in the exact same ready
 * state with a clear explanation, not depend on session storage.
 */
