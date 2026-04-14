using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.DTOs.StatsDTOs;
using SmartQueue.Core.DTOs.TicketDTOs;

namespace SmartQueueApp.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public OverviewStatsDto? Overview { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AdminQueuesViewModel
    {
        public List<QueueResponseDto> Queues { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class AdminQueueDetailViewModel
    {
        public QueueResponseDto? Queue { get; set; }
        public QueueSummaryStatsDto? Stats { get; set; }
        public List<CounterResponseDto> Counters { get; set; } = new();
        public QueuePeakHoursDto? PeakHours { get; set; }
        public List<CounterStatsDto> CounterStats { get; set; } = new();

        // Available staff pre-loaded so the assign dropdown renders without
        // a separate request per counter row.
        public List<UserResponseDto> AvailableStaff { get; set; } = new();

        public string? ErrorMessage { get; set; }
    }

    public class AdminStaffViewModel
    {
        public List<UserResponseDto> Users { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class AdminStatisticsViewModel
    {
        public OverviewStatsDto? Overview { get; set; }
        public List<QueueSummaryStatsDto> QueueStats { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class CreateQueueViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DefaultServiceMinutes { get; set; } = 5;
        public string? ErrorMessage { get; set; }
    }

    public class CreateCounterViewModel
    {
        public int QueueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; }
        public List<UserResponseDto> AvailableStaff { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    // ── NEW: used by the inline assign form on QueueDetail ────────────────────
    // Keeps counterId + queueId + the chosen userId together so the controller
    // can call the API and redirect back to the right queue detail page.
    public class AssignCounterViewModel
    {
        public int CounterId { get; set; }
        public int QueueId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class CreateStaffViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Djelatnik";
        public string? ErrorMessage { get; set; }
    }
}
