using System;

namespace FMS.Data
{
    public class BM2505 : BaseEntity
    {
        public REPORT_TYPE? ReportType { get; set; } = 0;

      

        public int TruckId { get; set; }
        public Truck Truck { get; set; }

        public DateTime Time { get; set; }

        public int? FlightId { get; set; }

        public Flight Flight { get; set; }

        public string TankNo { get; set; }

      

        public string AircraftCode
        {
            get
            {
                return Flight == null ? null : Flight.AircraftCode;
            }
        }

        public int? ContainerId { get; set; }

        public BM2505Container Container { get; set; }

        public string ContainerName
        {
            get
            {
                if (Container != null)
                    return Container.Name;
                else
                    return null;
            }
        }
        public string Depot { get; set; }

        public string RTCNo { get; set; }

        public decimal Temperature { get; set; }

        public decimal Density { get; set; }

        public decimal Density15 { get; set; }

        public decimal? DensityDiff { get; set; }

        public bool DensityCheck { get; set; }

        public string AppearanceCheck { get; set; }

        public bool WaterCheck { get; set; }

        public string PressureDiff { get; set; }

        public string HosePressure { get; set; }

        public int OperatorId { get; set; }

        public User Operator { get; set; }

        public string  Note { get; set; }

    }

    public enum REPORT_TYPE
    {
        FLIGHT,
        OTHER
    }
}
