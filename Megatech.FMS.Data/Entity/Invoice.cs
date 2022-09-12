using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class Invoice : BaseEntity
    {

        public string InvoiceNumber { get; set; }

        public int? InvoiceFormId { get; set; }

        public string FormNo { get; set; }

        public string SignNo { get; set; }

        public DateTime? Date { get; set; }

        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }

        public decimal Weight { get; set; }

       
        

      

        public decimal? Temperature { get; set; }

        public decimal? Density { get; set; }

        public INVOICE_TYPE InvoiceType { get; set; }

        public bool? Exported { get; set; }

        public CURRENCY Currency { get; set; } = CURRENCY.VND;

        public UNIT Unit { get; set; } = UNIT.GALLON;

        public decimal? Price { get; set; }

        public int FlightId { get; set; }

        public Flight Flight { get; set; }

        public decimal TaxRate { get; set; } = 0;
        public decimal SaleAmount { get; set; } = 0;
        public decimal? TotalAmount { get; set; }

        public decimal? GreenTax { get; set; }

        public decimal GreenTaxAmount {
            get {
                return (GreenTax??0) * Volume;
            
            }
        }

        public decimal TaxAmount
        {
            get
            {
                return Math.Round((SaleAmount + (decimal)GreenTaxAmount)* TaxRate, Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
            }
        }
        public int CustomerId { get; set; }

        public Airline Customer { get; set; }

        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerEmail { get; set; }

        public string TaxCode { get; set; }

        public ICollection<InvoiceItem> Items { get; set; }

        [Column(TypeName = "image")]

        public byte[] Image { get; set; }
        [Column(TypeName = "image")]


        public byte[] Signature { get; set; }


        public CUSTOMER_TYPE CustomerType { get; set; }

        public FLIGHT_TYPE FlightType { get; set; }
        public string BillNo { get; set; }
        public DateTime BillDate { get; set; }

        public bool? Exported_AITS { get; set; }
        public DateTime? Exported_AITS_Date { get; set; }

        public bool? Exported_OMEGA { get; set; }
        public DateTime? Exported_OMEGA_Date { get; set; }


        public string LoginTaxCode { get; set; }
        public string DefuelingNo { get; set; }

        public int? ReceiptId { get; set; }
        public Receipt Receipt { get; set; }

        public Guid? UniqueId { get; set; }
        public decimal? ExchangeRate { get; set; } = 1;


        public bool? Cancelled { get; set; } = false;

        public DateTime? CancelledTime { get; set; }

        public string CancelReason { get; set; }

        public bool? RequestCancel { get; set; }


        public int? ReplaceId { get; set; }
        [ForeignKey("ReplaceId")]
        public Invoice ReplaceInvoice { get; set; }
        public REFUEL_COMPANY? RefuelCompany { get; set; } = REFUEL_COMPANY.SKYPEC;
        public bool IsFHS
        {
            get
            {
                return RefuelCompany == REFUEL_COMPANY.TAPETCO || RefuelCompany == REFUEL_COMPANY.NAFSC;
            }
        }

        public string RouteName { get; set; }
        public string AircraftType { get; set; }
        public string AircraftCode { get; set; }
        public string FlightCode { get; set; }
        public bool? Manual { get; set; }

        public bool? IsElectronic { get; set; } = true;
        public object LocalUniqueId { get; set; } = Guid.NewGuid();
        public Guid? FHSUniqueId { get; set; }
        public decimal? TechLog { get;  set; }
        public string CCID { get; set; }
        public int? SignType { get; set; }
    }

    public enum REFUEL_COMPANY
    {
        SKYPEC,
        NAFSC,
        TAPETCO
    }
    public class InvoiceItem : BaseEntity
    {
        public int InvoiceId { get; set; }

        public Invoice Invoice { get; set; }

        public Guid? RefuelUniqueId { get; set; }

        public int? RefuelItemId { get; set; }

        public RefuelItem RefuelItem { get; set; }

        public int? TruckId { get; set; }

        public Truck Truck { get; set; }
        public string TruckNo { get; set; }

        public string QCNo { get; set; }

        public decimal Gallon { get; set; }

        public decimal? Volume { get; set; }

        public decimal? Weight { get; set; }

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
        public decimal Price { get; set; } = 0;
        public decimal SaleAmount { get; set; } = 0;

    }

    public enum INVOICE_TYPE
    {
        INVOICE,
        BILL,
        RETURN
    }

    public enum CUSTOMER_TYPE
    {
        LOCAL = 0,
        INTERNATIONAL = 1
    }

    //public enum FLIGHT_TYPE
    //{
    //    DOMESTIC = 0,
    //    OVERSEA = 1
    //}

    public class INVOICE_SOURCE
    {
        public static string SKYPEC = "ACE7A63F-D0B4-4CCD-B16D-FBE8761B5C1A";
        public static string NAFSC = "29955C52-8552-4C55-9FB5-085DC67C9752";
        public static string TAPETCO = "5E8FB7E0-B6BC-4B91-B428-C354AAB2814E";

        public static string SKYPEC_DOMESTIC = "1K22TSA";
    }
    public class INVOICE_SIGN
    {
        public static string SKYPEC = "K21TSB";
        public static string NAFSC = "K21TNB";
        public static string TAPETCO = "K21TTB";

        public static string SKYPEC_DOMESTIC = "1K22TSA";
    }
}
