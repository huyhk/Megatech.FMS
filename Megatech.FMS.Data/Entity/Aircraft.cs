using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FMS.Data
{
    public class Aircraft : BaseEntity
    {
        public Aircraft() : base()
        { }
        public Aircraft(DataContext context) : base(context)
        { }

        public string Name { get; set; }
        [Required]
        public string Code { get; set; }
        [Required]
        public string AircraftType { get; set; }

        public int? CustomerId { get; set; }
        public virtual Airline Customer { get; set; }

        public IList<Flight> Flights { get; set; }
    }
}
