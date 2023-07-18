using System;
using System.ComponentModel.DataAnnotations.Schema;

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
            get;
            set;
        } = DateTime.Now;

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


        public DateTime? StartTimeCheck { get; set; }

        public DateTime? EndTimeCheck { get; set; }
        [NotMapped]
        public DateTime? TestStartTime
        {
            get { return StartTimeCheck; }
            set { StartTimeCheck = value; }
        }
        [NotMapped]
        public DateTime? TestEndTime
        {
            get { return EndTimeCheck; }
            set { EndTimeCheck = value; }
        }

        public int? CheckResults { get; set; }

        [NotMapped]
        // TestResult true: P, 1: F
        public bool? TestResult
        {
            get
            {
                return CheckResults == 1;


            }
            set
            {
                CheckResults = value.HasValue? (bool)value ? 1 : 0: 0;
            }
        }
    }
}