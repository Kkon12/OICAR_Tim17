using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.TicketDTOs
{
    public class TicketHistoryResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<TicketResponseDto> Tickets { get; set; } = new();
    }
}

/*Why pagination: Ticket history can grow to thousands of records quickly.
 * Returning everything at once would be slow and waste bandwidth.
 * Page + PageSize allows the frontend to load chunks — 20 at a time is standard.
Why TotalCount and TotalPages in response: 
 * The frontend needs these to render pagination controls — "Page 3 of 47" requires knowing the total. 
 * Always return metadata alongside paginated data*/