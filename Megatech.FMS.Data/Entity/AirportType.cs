using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS.Data
{
    public class AirportType: BaseEntity
    {
        public string Code { get; set; }
        public FlightType Type { get; set; }
    }
}
