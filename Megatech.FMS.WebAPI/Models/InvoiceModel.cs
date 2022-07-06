using FMS.Data;
using System;
using System.Collections.Generic;

namespace Megatech.FMS.WebAPI.Models
{
    public class InvoiceModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }

        public int InvoiceFormId { get; set; }

        public string FormNo { get; set; }

        public string Sign { get; set; }

        public DateTime Date { get; set; }

        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }

        public decimal Weight { get; set; }


        public decimal TotalGallon { get; set; }

        public decimal TotalVolume { get; set; }

        public decimal TotalWeight { get; set; }

        public decimal TaxRate { get; set; }

        public decimal SaleAmount { get; set; }

        public decimal Temperature { get; set; }

        public decimal Density { get; set; }

        public INVOICE_TYPE InvoiceType { get; set; }

        public bool Exported { get; set; }

        public CURRENCY Currency { get; set; } = CURRENCY.VND;

        public UNIT Unit { get; set; } = UNIT.GALLON;

        public decimal Price { get; set; } = 0;

        public int FlightId { get; set; }

        public string FlightCode { get; set; }

        public string AircraftCode { get; set; }

        public string AircraftType { get; set; }

        public string RouteName { get; set; }


        public decimal SubTotal { get; set; } = 0;

        public decimal VatAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;

        public int CustomerId { get; set; }

        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string TaxCode { get; set; }

        public ICollection<InvoiceItemModel> Items { get; set; } = new List<InvoiceItemModel>();
        public FLIGHT_TYPE FlightType { get; internal set; }

        public decimal? TechLog { get; set; }
        public bool HasReturn { get;  set; }

        public List<InvoiceItemModel> ReturnItems { get; set; } = new List<InvoiceItemModel>();
    }


    public class InvoiceItemModel
    {
        public int RefuelItemId { get; set; }

        public int TruckId { get; set; }

        public string TruckNo { get; set; }

        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }

        public decimal Weight { get; set; }

        public decimal StartNumber { get; set; }

        public decimal EndNumber { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal Temperature { get; set; }

        public decimal Density { get; set; }

        public string WeightNote { get; set; }

        public int? DriverId { get; set; }

        public User Driver { get; set; }

        public int? OperatorId { get; set; }

        public User Operator { get; set; }

        public string QualityNo { get; set; }

        public bool IsReturn { get; set; }
        public string RefuelUniqueId { get; internal set; }


    }

}