using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Megatech.FMS.DataExchange
{
    
    public class FlightImportItemModel
    {
        [XmlElement("LEG_NO")]
        public string Leg_No { get; set; }

        [XmlElement("LEG_UPDATE_NO")]
        public string Leg_Update_No { get; set; }

        [XmlElement("FN_CARRIER")]
        public string FN_Carrier { get; set; }
        [XmlElement("FN_NUMBER")] 
        public string FN_Number { get; set; }

        public string FN_Suffix { get; set; }


        [XmlElement("DEP_SCHED_DT")]
        public DateTime DepartureScheduledTime { get; set; }

        [XmlElement("DEP_DT")]
        public DateTime DepartureTime { get; set; }
        [XmlElement("ARR_SCHED_DT")]
        public DateTime ArrivalScheduledTime { get; set; }
        [XmlElement("ARR_DT")]
        public DateTime ArrivalTime { get; set; }
        [XmlElement("AC_REGISTRATION")]
        public string AircraftCode { get; set; }

        [XmlElement("DEP_AP_ACTUAL")]
        public string FromAirport { get; set; }
        [XmlElement("ARR_AP_ACTUAL")]
        public string ToAirport { get; set; }
    }
    [XmlRoot(ElementName = "Get_Flight_SPResult")]
    public class FlightImportModel {

        [XmlElement(ElementName = "RawDataInfo_SP")]
        public List<FlightImportItemModel> Items { get; set; }
    }
}
