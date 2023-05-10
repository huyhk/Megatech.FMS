using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class Receipt : BaseEntity
    {
        public string Number { get; set; }

        public DateTime Date { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }

        public decimal Weight { get; set; }

        public int CustomerId { get; set; }

        public Airline Customer { get; set; }

        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string TaxCode { get; set; }


        public int FlightId { get; set; }
        public Flight Flight { get; set; }

        public string RouteName { get; set; }

        public string AircraftCode { get; set; }


        public decimal? ReturnAmount { get; set; }

        public string DefuelingNo { get; set; }

        public bool InvoiceSplit { get; set; }

        public decimal? SplitAmount { get; set; }

        //public string PdfImageString { get; set; }
        //public string SignImageString { get; set; }

        [Column(TypeName = "image")]
        public byte[] Image { get; set; }

        [Column(TypeName = "image")]
        public byte[] Signature { get; set; }

        [Column(TypeName = "image")]
        public byte[] SellerImage { get; set; }

        public bool? IsReturn { get; set; }

        public List<ReceiptItem> Items { get; set; }

        public List<Invoice> Invoices { get; set; } = new List<Invoice>();

        public REFUEL_COMPANY? RefuelCompany { get; set; } = REFUEL_COMPANY.SKYPEC;

        public bool IsFHS
        {
            get
            {
                return RefuelCompany == REFUEL_COMPANY.NAFSC || RefuelCompany == REFUEL_COMPANY.TAPETCO;
            }

        }

        public int? FlightType { get; set; }
        public string FlightCode { get; set; }
        public string AircraftType { get; set; }
        public bool? Manual { get; set; }
        public int? CustomerType { get; set; }
        public string ImagePath { get; set; }
        public string SignaturePath { get; set; }
        public string SellerPath { get; set; }
        public decimal? TechLog { get; set; }   
        public string ReplaceNumber { get; set; }

        public bool? IsReuse { get; set; }
        public Guid? UniqueId { get; set; }
        public string ReplacedId { get; set; }


        public bool? IsThermal { get; set; }

        public List<Invoice> CreateInvoices()
        {
            try
            {

                var inv = new Invoice
                {
                    BillDate = Date,
                    BillNo = Number,
                    FlightId = FlightId,
                    CustomerId = CustomerId,
                    CustomerName = CustomerName,
                    CustomerAddress = CustomerAddress,
                    CustomerCode =  CustomerCode,
                    CustomerType = CustomerType == null ? (Flight.Airline.AirlineType == 0 ? CUSTOMER_TYPE.LOCAL : CUSTOMER_TYPE.INTERNATIONAL) : (CUSTOMER_TYPE)CustomerType,
                    AircraftCode = AircraftCode,
                    AircraftType = AircraftType,
                    RouteName = RouteName,
                    FlightCode = FlightCode,

                    TaxCode = TaxCode,
                    Items = new List<InvoiceItem>(),
                    Manual = Manual,
                    TechLog = TechLog,
               
                    LoginTaxCode = Flight.Airport.TaxCode,
                    UserCreatedId = UserCreatedId

                };

                inv.FlightType = (FLIGHT_TYPE)Flight.FlightType;

                if ((bool)IsReturn)
                    inv.InvoiceType = INVOICE_TYPE.RETURN;
                else if (Flight.Airline.DomesticInvoice??false)
                    inv.InvoiceType = INVOICE_TYPE.INVOICE;
                else if (Flight.FlightType == FLIGHT_TYPE.DOMESTIC && Flight.Airline.AirlineType == 0)
                    inv.InvoiceType = INVOICE_TYPE.BILL;
                else
                    inv.InvoiceType = INVOICE_TYPE.INVOICE;

                

                var list = new List<Invoice>();
                list.Add(inv);

                foreach (var item in Items)
                {

                    var invoiceItem = new InvoiceItem
                    {
                        TruckId = item.TruckId,
                        TruckNo = item.Truck.Code,
                        StartNumber = item.StartNumber,
                        EndNumber = item.EndNumber,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Temperature = item.Temperature,
                        Density = item.Density,
                        DriverId = item.DriverId,
                        OperatorId = item.OperatorId,
                        QCNo = item.QualityNo,
                        RefuelItemId = item.RefuelId,
                        RefuelUniqueId = item.RefuelUniqueId,
                        Gallon = item.Gallon,
                        Volume = item.Volume,
                        Weight = item.Weight,
                        UserCreatedId = UserCreatedId

                    };

                    if (invoiceItem.Gallon > 0)
                    {


                        inv.Items.Add(invoiceItem);
                    }
                }
                //check if receipt has return amount, create return bill
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

    }

    public enum PRINTER_TYPE
    {
        THERMAL
    }

    public class ReceiptItem : BaseEntity
    {
        public int? TruckId { get; set; }
        public Truck Truck { get; set; }

        public Guid? RefuelUniqueId { get; set; }
        public int? RefuelId { get; set; }
        [ForeignKey("RefuelId")]
        public RefuelItem RefuelItem { get; set; }
        public int ReceiptId { get; set; }
        [ForeignKey("ReceiptId")]
        public Receipt Receipt { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal StartNumber { get; set; }
        public decimal EndNumber { get; set; }

        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }
        public decimal Weight { get; set; }


        public decimal Temperature { get; set; }
        public decimal Density { get; set; }

        public string QualityNo { get; set; }

        public int? DriverId { get; set; }

        public int? OperatorId { get; set; }


    }
}
