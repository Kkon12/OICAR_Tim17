using Microsoft.EntityFrameworkCore.Migrations;

namespace SmartQueue.Core.Models
{
    public enum TicketStatus { Waiting, Called, InService, Done, Skipped }

    public class Ticket
    {
        public int Id { get; set; }
        public int TicketNumber { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Waiting;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CalledAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // ── Wait time estimation fields ───────────────────────────────────────
        public int? EstimatedWaitMinutes { get; set; }
        public int? ActualWaitMinutes { get; set; }
        public int Position { get; set; }

        // Foreign keys
        public int QueueId { get; set; }
        public string? UserId { get; set; }
        public int? CounterId { get; set; }

        // Navigation
        public Queue Queue { get; set; } = null!;
        public ApplicationUser? User { get; set; }
        public Counter? Counter { get; set; }
    }
}
