using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.StatsDTOs
{
    public class OverviewStatsDto
    {
        public int TotalQueues { get; set; }
        public int ActiveQueues { get; set; }
        public int TotalTicketsToday { get; set; }
        public int TotalWaitingNow { get; set; }
        public int TotalServedToday { get; set; }
        public int TotalSkippedToday { get; set; }
        public double OverallAvgWaitToday { get; set; }
        public int TotalOpenCounters { get; set; }
        public int TotalDjelatsnici { get; set; }
        public List<QueueSummaryStatsDto> QueueBreakdown { get; set; } = new();
    }
}
