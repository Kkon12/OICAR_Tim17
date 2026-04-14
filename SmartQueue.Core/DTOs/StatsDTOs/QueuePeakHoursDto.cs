using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.StatsDTOs
{
    public class QueuePeakHoursDto
    {
        public int QueueId { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public List<PeakHourDto> ByHour { get; set; } = new();
        public List<PeakDayDto> ByDay { get; set; } = new();
        public PeakHourDto? BusiestHour { get; set; }
        public PeakDayDto? BusiestDay { get; set; }
    }
}
