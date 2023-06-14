using System;

namespace FMS.Data
{
    public class TruckFuel : BaseEntity
    {
        public int TruckId { get; set; }

        public Truck Truck { get; set; }

        public decimal Amount { get; set; }



        public string QCNo { get; set; }

        public decimal? AccumulateRefuelAmount { get; set; }

        public DateTime Time { get; set; } = DateTime.Now;
        public DateTime? StartTime { get; set; } = DateTime.Now;

        public DateTime? EndTime
        {
            get { return Time; }
            set { Time = value.Value; }
        }

        public string TankNo { get; set; }

        public string TicketNo { get; set; }

        public decimal TruckCapacity
        {
            get
            {
                return Truck == null ? 0 : Truck.MaxAmount;
            }
        }

        public int? OperatorId { get; set; }

        public User Operator { get; set; }

        public string MaintenanceStaff { get; set; }

        public DateTime TestStartTime { get; set; }

        public DateTime TestEndTime { get; set; }

        // TestResult true: P, 1: F
        public bool? TestResult { get; set; } 
    }
}