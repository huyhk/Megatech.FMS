using System;

namespace FMS.Data
{
    public class TruckAssign : BaseEntity
    {
        public int TruckId { get; set; }
        public Truck Truck { get; set; }

        public int DriverId { get; set; }
        public User Driver { get; set; }

        public int TechnicalerId { get; set; }
        public User Technicaler { get; set; }

        public int ShiftId { get; set; }
        public Shift Shift { get; set; }

        public DateTime StartDate { get; set; }

        public int AirportId { get; set; }
        public Airport Airport { get; set; }

        //public int UserId { get; set; }
        //public User User { get; set; }
    }
}
