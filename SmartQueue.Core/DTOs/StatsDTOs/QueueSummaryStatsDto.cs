using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.StatsDTOs
{
    public class QueueSummaryStatsDto
    {
        public int QueueId { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalTicketsToday { get; set; }
        public int TotalTicketsThisWeek { get; set; }
        public int TotalTicketsThisMonth { get; set; }
        public int CurrentlyWaiting { get; set; }
        public int CurrentlyServing { get; set; }
        public int CompletedToday { get; set; }
        public int SkippedToday { get; set; }
        public double AvgWaitMinutesToday { get; set; }
        public double AvgServiceMinutesToday { get; set; }
        public double SkipRateToday { get; set; }
        public int OpenCounters { get; set; }
        public int TotalCounters { get; set; }
    }
}

/*Why so many separate DTOs: Each endpoint returns a different shape of data 
— the overview dashboard needs different fields than the peak hours analysis. 
Separate DTOs keep each response lean and purposeful — no null fields, no wasted bandwidth.*/

/*Why SkipRateToday: Skip rate is a key operational metric — high skip rate means customers are giving up.
Admins need this to identify problems: too few counters, too slow service, or queue status issues.*/