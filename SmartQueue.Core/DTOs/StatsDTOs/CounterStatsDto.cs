using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.StatsDTOs
{
    public class CounterStatsDto
    {
        public int CounterId { get; set; }
        public string CounterName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedUserName { get; set; }
        public int TicketsServedToday { get; set; }
        public double AvgServiceMinutesToday { get; set; }
    }
}
