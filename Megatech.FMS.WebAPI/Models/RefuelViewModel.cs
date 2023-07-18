using FMS.Data;
using System;
using System.Collections.Generic;

namespace Megatech.FMS.WebAPI.Models
{
    public class RefuelViewModel : BaseViewModel
    {
        private readonly decimal GALLON_TO_LITTER = 3.7854M;

        public RefuelViewModel()
        {
            StartTime = EndTime = DateTime.Now;
            RefuelItemType = REFUEL_ITEM_TYPE.REFUEL;

        }
        public RefuelViewModel(RefuelItem r)
        {

            FlightStatus = r.Flight.Status;
            FlightId = r.FlightId;
            FlightCode = r.Flight.Code;
            FlightType = r.Flight.FlightType;
            EstimateAmount = r.Flight.EstimateAmount;

            Id = r.Id;
            AircraftType = r.Flight.AircraftType;
            AircraftCode = r.Flight.AircraftCode;
            ParkingLot = r.Flight.Parking;
            RouteName = r.Flight.RouteName;
            Status = r.Status;
            ArrivalTime = r.Flight.ArrivalScheduledTime == null || r.Flight.ArrivalScheduledTime.Value.Year == 9999 ? r.Flight.RefuelScheduledTime.Value.AddMinutes(-60) : r.Flight.ArrivalScheduledTime.Value;
            DepartureTime = r.Flight.DepartureScheduledTime == null || r.Flight.DepartureScheduledTime.Value.Year == 9999 ? r.Flight.RefuelScheduledTime.Value.AddMinutes(60) : r.Flight.DepartureScheduledTime.Value;
            RefuelTime = r.Flight.RefuelScheduledTime;
            RealAmount = r.Amount;
            StartTime = r.Status != REFUEL_ITEM_STATUS.NONE ? r.StartTime : DateTime.Now;
            EndTime = r.EndTime ?? DateTime.Now;
            StartNumber = r.StartNumber;
            EndNumber = r.EndNumber;
            DeviceEndTime = r.DeviceEndTime;
            DeviceStartTime = r.DeviceStartTime;
            Density = r.Density;
            ManualTemperature = r.ManualTemperature;
            Temperature = r.Temperature;
            QualityNo = r.QCNo;
            TaxRate = r.TaxRate;
            Price = r.Price;
            Currency = r.Currency;
            Unit = r.Status == REFUEL_ITEM_STATUS.DONE ? (r.Unit ?? 0) : r.Flight.Airline.Unit;
            TruckId = r.TruckId;
            TruckNo = r.Truck.Code;
            Gallon = r.Gallon;
            AirlineId = r.Flight.AirlineId ?? 1;
            AirlineType = r.Flight.Airline == null ? 0 : r.Flight.Airline.AirlineType;

            AirportId = r.Flight.AirportId;

            RefuelItemType = r.RefuelItemType;

            ReturnAmount = r.ReturnAmount;
            WeightNote = r.WeightNote;
            InvoiceNumber = r.InvoiceNumber;

            DriverId = r.DriverId ?? 0;
            DriverName = r.DriverId == null ? "" : r.Driver.FullName;
            OperatorId = r.OperatorId ?? 0;
            OperatorName = r.OperatorId == null ? "" : r.Operator.FullName;

            IsInternational = r.Flight.FlightType == FLIGHT_TYPE.OVERSEA;
            Completed = r.Completed;
            Printed = r.Printed;
            InvoiceNameCharter = r.Flight.InvoiceNameCharter.Trim();
            InvoiceFormId = r.InvoiceFormId;
            PrintTemplate = !r.Printed ? (r.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && r.Flight.Airline.AirlineType == 0 ? PRINT_TEMPLATE.BILL : PRINT_TEMPLATE.INVOICE) : r.PrintTemplate;



        }
        //public int Id { get; set; }
        public int LocalId { get; set; }
        public int FlightId { get; set; }
        public string FlightCode { get; set; }
        public FLIGHT_TYPE FlightType { get; set; }
        public decimal EstimateAmount { get; set; }
        public string AircraftCode { get; set; }
        public string AircraftType { get; set; }
        public string ParkingLot { get; set; }
        public string RouteName { get; set; }
        public string TruckNo { get; set; }
        public int TruckId { get; set; }
        public decimal RealAmount { get; set; }



        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime? DeviceStartTime { get; set; }

        public DateTime? DeviceEndTime { get; set; }

        public REFUEL_ITEM_STATUS Status { get; set; }

        public DateTime ArrivalTime { get; set; }

        public DateTime DepartureTime { get; set; }

        public DateTime? RefuelTime { get; set; }

        public decimal StartNumber { get; set; }
        public decimal EndNumber { get; set; }
        public decimal OriginalEndMeter { get; set; }

        public decimal Temperature { get; set; }
        public decimal ManualTemperature { get; set; }
        public decimal Density { get; set; }

        public decimal Price { get; set; }

        public CURRENCY Currency { get; set; }

        public UNIT? Unit { get; set; }

        public decimal TaxRate { get; set; }

        //public decimal Weight { get; set; }

        //public decimal Volume { get; set; }

        public decimal Volume
        {
            get;
            set;

        }

        public decimal Weight
        {
            get { return Math.Round(Volume * Density, 0, MidpointRounding.AwayFromZero); }

        }

        public int AirlineId { get; set; }
        public int? AirlineType { get; set; }

        public int? AirportId { get; set; }
        public string QualityNo { get; set; }

        public List<RefuelViewModel> Others { get; set; }

        public FlightStatus FlightStatus { get; set; }
        public decimal Gallon { get; set; }

        public REFUEL_ITEM_TYPE RefuelItemType { get; set; }


        //added 20201228

        public decimal? ReturnAmount { get; set; }

        public string WeightNote { get; set; }

        public decimal TechLog { get; set; }
        public string InvoiceNumber { get; set; }

        public string ReturnInvoiceNumber { get; set; }

        public int DriverId { get; set; }


        public string DriverName { get; set; }

        public int OperatorId { get; set; }

        public string OperatorName { get; set; }

        public bool IsAlert { get; set; }

        public bool IsInternational { get; set; }

        public bool Completed { get; set; }

        public bool Printed { get; set; }

        public string InvoiceNameCharter { get; set; }

        public PRINT_TEMPLATE? PrintTemplate { get; set; } = PRINT_TEMPLATE.INVOICE;

        public CHANGE_FLAG ChangeFlag { get; set; }

        public int? InvoiceFormId { get; set; }
        public InvoiceForm InvoiceForm { get; set; }
        public bool? Exported { get; set; } = false;
        public enum CHANGE_FLAG
        {
            NONE = 0,
            PRICE = 1,
            GROSS_QTY = 2,
            END_METER = 4,
            INVOICE_NUMBER = 8
        }

        public BM2508_RESULT? BM2508Result { get; set; }

        public bool BondingCable
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.BONDING_CABLE);
            }
        }
        public bool FuelingHose
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.FUELING_HOSE);
            }
        }
        public bool FuelingCap
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.FUELING_CAP);
            }
        }
        public bool Ladder
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.LADDER);
            }
        }

        public InvoiceModel Invoice { get; set; }
        public int? ReceiptCount { get; set; }

        public string ReceiptNumber { get; set; }

        public Guid? ReceiptUniqueId { get; set; }

        public RETURN_UNIT? ReturnUnit { get;  set; }
        public AirlineViewModel AirlineModel { get; internal set; }


        public bool HasReview { get; set; }
        public string FlightUniqueId { get; set; } = Guid.NewGuid().ToString();
        public decimal? WaterSensor { get; internal set; }
    }
}