using System;

namespace Megatech.FMS.WebAPI.Models
{
    public class TruckFuelViewModel
    {
        public int Id { get; set; }
        public int TruckId { get; set; }

        public decimal Amount { get; set; }

        public string QCNo { get; set; }

        public DateTime Time { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int? OperatorId { get; set; }
        public string OperatorName { get; set; }

        public string MaintenanceStaff { get; set; }

        public string TankNo { get; set; }

        public string TicketNo { get; set; }

        public decimal? Accumulate { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? TestStartTime { get; set; }
        public DateTime? TestEndTime { get; set; }
        public bool? TestResult { get; set; }
    }
}