using System;

namespace Megatech.FMS.WebAPI.Models
{
    public class ShiftViewModel
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime Date { get; set; }

        public int? AiportId { get; set; }

        public DateTime PreStart { get; set; }
        public DateTime NextEnd { get; set; }

        public ShiftViewModel PrevShift { get; set; }
        public ShiftViewModel NextShift { get; set; }
    }
}