using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.StatsDTOs
{
    public class PeakDayDto
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public double AvgWaitMinutes { get; set; }
    }
}
