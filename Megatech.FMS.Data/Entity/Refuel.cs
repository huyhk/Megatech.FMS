using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class Refuel : BaseEntity
    {

        public int FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight Flight { get; set; }

        public IList<RefuelItem> Items { get; set; }

        public decimal TotalAmount { get; set; }


        public decimal Price { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public REFUEL_STATUS Status { get; set; }

    }

    public enum REFUEL_STATUS
    {
        NONE,
        PROCESSING,
        DONE,
        ERROR
    }
}
