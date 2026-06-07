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
           
            var counterResult = await _api.GetMyCounterAsync();

           
            if (!counterResult.Success && counterResult.StatusCode != 404)
                return View(new DjelatnikDashboardViewModel
                {
                    ErrorMessage = counterResult.ErrorMessage
                });

            var myCounter = counterResult.Data;
            List<TicketResponseDto> waitingTickets = new();
            TicketResponseDto? currentTicket = null;

            if (myCounter != null)
            {
             
                var waitingResult = await _api.GetQueueTicketsAsync(myCounter.QueueId);
                if (waitingResult.Success && waitingResult.Data != null)
                    waitingTickets = waitingResult.Data;

                
                var calledResult = await _api.GetCalledTicketForCounterAsync(myCounter.Id);
                if (calledResult.Success && calledResult.Data != null)
                    currentTicket = calledResult.Data;
            }

            return View(new DjelatnikDashboardViewModel
            {
                MyCounter = myCounter,
                WaitingTickets = waitingTickets,
                CurrentTicket = currentTicket,
                SignalRToken = _tokenService.GetJwt() ?? string.Empty
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

