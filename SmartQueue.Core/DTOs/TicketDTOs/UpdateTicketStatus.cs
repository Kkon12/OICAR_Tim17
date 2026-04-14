using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.TicketDTOs
{
    public class UpdateTicketStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int? CounterId { get; set; }
    }
}
