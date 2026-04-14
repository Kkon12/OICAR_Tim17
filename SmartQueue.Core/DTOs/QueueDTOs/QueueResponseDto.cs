using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.QueueDTOs
{
    public class QueueResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int DefaultServiceMinutes { get; set; }
        public int TotalWaiting { get; set; }
        public int OpenCounters { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

// QueueResponseDto controls exactly what the API returns — no sensitive internal fields exposed.
// Never return raw model objects directly from controllers.
