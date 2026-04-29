namespace SmartQueue.Core.Models
{
    public class QueueStatSnapshot
    {
        public int Id { get; set; }
        public int QueueId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int HourOfDay { get; set; }              // 0-23
        public double AvgServiceMinutes { get; set; }
        public int SampleCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Nav
        public Queue Queue { get; set; } = null!;
    }
}