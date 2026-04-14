using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartQueue.Core.DTOs.StatsDTOs
{
    public class PeakHourDto
    {
        public int HourOfDay { get; set; }
        public string HourLabel { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public double AvgWaitMinutes { get; set; }
    }
}
/*
Why HourLabel alongside HourOfDay: 
The frontend needs both — HourOfDay: 9 for charting and 
HourLabel: "09:00-10:00" for display. 
Pre-formatting in the API means less work in every frontend. */