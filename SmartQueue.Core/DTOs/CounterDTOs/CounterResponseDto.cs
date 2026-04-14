using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.CounterDTOs
{
    public class CounterResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int QueueId { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public int? CurrentTicketNumber { get; set; }
    }
}

/*Why AssignedUserName in response: Frontend needs to display "Šalter 1 — Ivan Horvat" 
 * not just a UserId GUID. We resolve the name in the controller so the client never needs a second request.
Why CurrentTicketNumber: Shows which ticket is currently being served at this counter 
 * — useful for the display board showing "Now serving: 042 at Šalter 1".*/
