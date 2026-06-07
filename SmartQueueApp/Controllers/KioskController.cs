using Microsoft.AspNetCore.Mvc;
using SmartQueue.Core.DTOs.TicketDTOs;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Controllers
{
    
    public class KioskController : Controller
    {
        private readonly IApiService _api;

        public KioskController(IApiService api)
        {
            _api = api;
        }

       
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
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeTicket(int queueId)
        {
            var result = await _api.TakeTicketAsync(new TakeTicketDto
            {
                QueueId = queueId,
                UserId = null  
            });

            if (!result.Success)
            {
               
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

