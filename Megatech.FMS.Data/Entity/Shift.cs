using System;

namespace FMS.Data
{
    public class Shift : BaseEntity
    {
        public int AirportId { get; set; }
        public Airport Airport { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
    }
}
