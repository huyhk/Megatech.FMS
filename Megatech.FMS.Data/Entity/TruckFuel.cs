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
    }
}