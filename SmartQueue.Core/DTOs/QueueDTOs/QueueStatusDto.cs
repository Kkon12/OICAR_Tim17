namespace SmartQueue.Core.DTOs.QueueDTOs
{
    public class QueueStatusDto
    {
        public int QueueId { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public int CurrentlyServingNumber { get; set; }
        public int TotalWaiting { get; set; }
        public int OpenCounters { get; set; }
        public double AverageServiceMinutes { get; set; }
        public List<TicketPositionDto> WaitingTickets { get; set; } = new();
    }

    public class TicketPositionDto
    {
        public int TicketNumber { get; set; }
        public int Position { get; set; }
        public int EstimatedWaitMinutes { get; set; }
    }
}

/*Why QueueStatusDto: This is the object pushed via SignalR to ALL waiting customers every time the queue moves. 
 * It contains everything the customer's screen needs — current number, their position,
 * and their updated estimate — in one single payload.
 * 
 * ---
 * 
 * 
Why TicketPositionDto: Each waiting ticket gets its own position and estimate.
When a ticket is called, everyone behind moves up one position and their estimate drops accordingly.*/