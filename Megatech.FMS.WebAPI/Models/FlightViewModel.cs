using System.Collections.Generic;
using System.Linq;

namespace Megatech.FMS.WebAPI.Models
{
    public class FlightViewModel
    {
        public int Id { get; set; }

        public string FlightCode { get; set; }

        public string AircraftCode { get; set; }

        public string AircraftType { get; set; }

        public string ParkingLot { get; set; }

        public string RouteName { get; set; }



        public string CustomerName { get; set; }

        public string TaxCode { get; set; }

        public string Address { get; set; }

        public IList<RefuelViewModel> RefuelItems { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return RefuelItems.Sum(r => r.RealAmount);
            }
        }

        public decimal TotalVolume { get; set; }
    }
}