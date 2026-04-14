using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.TicketDTOs;

namespace SmartQueueApp.Models.ViewModels
{
    public class DjelatnikDashboardViewModel
    {
        public CounterResponseDto? MyCounter { get; set; }
        public List<TicketResponseDto> WaitingTickets { get; set; } = new();
        public TicketResponseDto? CurrentTicket { get; set; }
        public List<CounterResponseDto> AllCounters { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public class KioskViewModel
    {
        public List<SmartQueue.Core.DTOs.QueueDTOs.QueueResponseDto>
            ActiveQueues
        { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class TicketTakenViewModel
    {
        public TicketResponseDto? Ticket { get; set; }
        public string? ErrorMessage { get; set; }
    }
}