using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class Flight : BaseEntity, IAirportBase
    {
        //[Required(ErrorMessage = "Vui lòng nhập dữ liệu.")]
        public string Code { get; set; }
        public DateTime? DepartureScheduledTime { get; set; }
        public DateTime? DepartuteTime { get; set; }
        public DateTime? ArrivalScheduledTime { get; set; }
        public DateTime? ArrivalTime { get; set; }

        public DateTime? FlightTime { get; set; }
        //public Route Route { get; set; }
        public string RouteName { get; set; }
        //public int? DepartureId { get; set; }
        //public Airport Departure { get; set; }
        //public int? ArrivalId { get; set; }
        //public Airport Arrival { get; set; }

        public int? AirlineId { get; set; }
        public Airline Airline { get; set; }

        public int? AircraftId { get; set; }
        public Aircraft Aircraft { get; set; }

        public string AircraftCode { get; set; }
        public string AircraftType { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập dữ liệu.")]
        //[DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        public decimal EstimateAmount { get; set; }
        public DateTime? RefuelScheduledTime { get; set; }
        public DateTime? RefuelScheduledHours { get; set; }
        public DateTime? RefuelTime { get; set; }

        public int? ParkingLotId { get; set; }
        public ParkingLot ParkingLot { get; set; }
        public string Parking { get; set; }

        //public int? RefuelId { get; set; }

        //public virtual  Refuel Refuel { get; set; }

        public List<RefuelItem> RefuelItems { get; set; }

        public string DriverName { get; set; }
        public string TechnicalerName { get; set; }
        public string Shift { get; set; }
        public DateTime? ShiftStartTime { get; set; }
        public DateTime? ShiftEndTime { get; set; }
        public string AirportName { get; set; }
        public string TruckName { get; set; }
        public FlightStatus Status { get; set; }
        public int? SortOrder { get; set; }
        public int? AirportId { get; set; }

        public Airport Airport { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal Price { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Note { get; set; }

        public int? Follow { get; set; }

        public string InvoiceNameCharter { get; set; }


        public FLIGHT_CREATED_LOCATION CreatedLocation { get; set; }

        //public REFUEL_STATUS Status { get; set; }

        //[NotMapped]
        //public Refuel Refuel
        //{
        //    get
        //    {
        //        var refs = Refuels.FirstOrDefault(r => r.FlightId == this.Id);
        //        if (refs != null)
        //            return refs;
        //        else
        //            return null;
        //    }
        //}

        public FLIGHT_TYPE FlightType { get; set; }
        public FlightCarry FlightCarry { get; set; }
        public int? CreateType { get; set; }

        public Guid? UniqueId { get; set; } = new Guid();

        public void RepairDateTime()
        {
            if (ArrivalScheduledTime == null && DepartureScheduledTime == null)
                ImportError = true;
            else if (RefuelScheduledTime == null)
            {
                //if (ArrivalScheduledTime == null)
                //    ArrivalScheduledTime = DepartureScheduledTime.Value.AddHours(-1);
                var temp = DepartureScheduledTime.Value.AddHours(-1);
                RefuelScheduledTime = temp.AddMinutes(5);
            }
        }
        [NotMapped]
        public bool ImportError { get; set; }

        public string LastUpdateDevice { get; set; }

        public string LegNo { get; set; }

        public string LegUpdateNo { get; set; }
        public bool? IsOutRefuel { get; set; }


        public IList<Review> Reviews { get; set; }

    }
    public class AmountReport
    {
        public string AirportName { get; set; }
        public int AirportId { get; set; }
        public int TH_Number { get; set; }
        public int KH_Number { get; set; }
        public decimal TH_Amount { get; set; }
        public decimal KH_Amount { get; set; }

        public int TH_Number2 { get; set; }
        public int KH_Number2 { get; set; }
        public decimal TH_Amount2 { get; set; }
        public decimal KH_Amount2 { get; set; }

        public string Note { get; set; }
    }
    public class TimekeepingReport
    {
        public int FlightId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }

        public int RefuelBT { get; set; }
        public int RefuelCC { get; set; }
        public int Extract { get; set; }

        public int RefuelBT_2 { get; set; }
        public int RefuelCC_2 { get; set; }
        public int Extract_2 { get; set; }

        public int RefuelBT_3 { get; set; }
        public int RefuelCC_3 { get; set; }
        public int Extract_3 { get; set; }

        public int RefuelBT_4 { get; set; }
        public int RefuelCC_4 { get; set; }
        public int Extract_4 { get; set; }
    }
    public enum FlightStatus
    {
        NONE, //Chờ phân công
        ASSIGNED,//Đã phân công
        REFUELING,// Đang tra nạp
        REFUELED,// Đã tra nạp
        CANCELED,// Đã hủy chuyến
        NOTREFUEL// Không lấy dầu
    }

    public enum FlightSortOrder
    {
        SORT_ORDER,
        DATE_CREATED,
        ArrivalScheduledTime,
        DepartureScheduledTime,
        RefuelScheduledTime
    }

    public enum SortDirection
    {
        ASCENDING,
        DESCENDING
    }

    public enum FLIGHT_TYPE
    {
        DOMESTIC, // Nội địa
        OVERSEA // Quốc tế
    }

    public enum FlightCarry
    {
        PAX,
        CCO,
        CGO
    }

    public enum FLIGHT_CREATED_LOCATION
    {
        IMPORT,
        WEB,
        COPY,
        APP, 
        FHS
    }
}
