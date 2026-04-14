using System.Collections.Generic;

namespace SmartQueue.Core.Models
{
    public enum CounterStatus { Open, Busy, Closed }

    public class Counter
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;  // npr. "Šalter 1"
        public CounterStatus Status { get; set; } = CounterStatus.Closed;

        // Foreign keys
        public int QueueId { get; set; }
        public string? AssignedUserId { get; set; }  // Djelatnik za ovaj šalter

        // Navigation
        public Queue Queue { get; set; } = null!;
        public ApplicationUser? AssignedUser { get; set; }
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}