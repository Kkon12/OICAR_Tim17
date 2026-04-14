using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.TicketDTOs
{
    public class TicketHistoryQueryDto
    {
        public int? QueueId { get; set; }           // filter by queue
        public string? Status { get; set; }         // filter by status
        public DateTime? DateFrom { get; set; }     // filter from date
        public DateTime? DateTo { get; set; }       // filter to date
        public string? UserId { get; set; }         // filter by user
        public int Page { get; set; } = 1;          // pagination
        public int PageSize { get; set; } = 20;     // default 20 per page
    }
}

/*Why all filters are nullable: Every filter is optional — calling with no filters returns all tickets. 
 * The query builds dynamically based on what is provided.*/
