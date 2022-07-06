using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FMS.Data
{
    public class ParkingLot : BaseEntity
    {

        public int AirportId { get; set; }

        public Airport Airport { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        //[Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public GeoLocation Location { get; set; }

        public IList<Flight> Flights { get; set; }
    }
}
