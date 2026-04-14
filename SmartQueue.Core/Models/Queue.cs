namespace SmartQueue.Core.Models
{
    public enum QueueStatus { Active, Paused, Closed }

    public class Queue
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QueueStatus Status { get; set; } = QueueStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Wait time estimation fields ───────────────────────────────────────
        public int DefaultServiceMinutes { get; set; } = 5;   // Admin fallback
        public int MinTicketsForStats { get; set; } = 20;     // when to trust real data

        // Navigation
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<Counter> Counters { get; set; } = new List<Counter>();
        public ICollection<QueueStatSnapshot> StatSnapshots { get; set; } = new List<QueueStatSnapshot>();
    }
}