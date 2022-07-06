using FMS.Data;
using Newtonsoft.Json;
using System;

namespace Megatech.FMS.WebAPI.Models
{
    public class BM2505Model : BaseViewModel
    {
        public int TruckId { get; set; }


        public DateTime Time { get; set; }

        public int? FlightId { get; set; }

        public string FlightCode { get; set; }

        public string TankNo { get; set; }

        public string AircraftCode
        {
            get; set;
        }

        public string RTCNo { get; set; }

        public decimal Temperature { get; set; }

        public decimal Density { get; set; }

        public decimal Density15 { get; set; }

        public bool DensityCheck { get; set; }

        public string AppearanceCheck { get; set; }

        public bool WaterCheck { get; set; }



        public string PressureDiff { get; set; }

        public string HosePressure { get; set; }

        public int OperatorId { get; set; }

        public string OperatorName { get; set; }

        internal BM2505 toEntity()
        {
            return JsonConvert.DeserializeObject<BM2505>(JsonConvert.SerializeObject(this));
        }
    }
}