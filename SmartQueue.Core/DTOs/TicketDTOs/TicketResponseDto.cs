using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.TicketDTOs
{
    public class TicketResponseDto
    {
        public int Id { get; set; }
        public int TicketNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public int QueueId { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public int Position { get; set; }
        public int? EstimatedWaitMinutes { get; set; }
        public int? ActualWaitMinutes { get; set; }
        public int? CounterId { get; set; }
        public string? CounterName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CalledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

/*Why CounterName in response: The customer needs to know which 
 * physical counter to go to — "Please proceed to Šalter 2" — not just a CounterId number.*/
