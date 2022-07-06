using System.Collections.Generic;

namespace FMS.Data
{
    public class Airline : Company
    {
        public IList<Aircraft> Aircrafts { get; set; }
        public IList<Flight> Flights { get; set; }
        public string Pattern { get; set; }
        public bool? IsCharter { get; set; }
        public int? AirlineType { get; set; }
        public UNIT? Unit { get; set; }
        public string InvoiceCode { get; set; }

        public bool? DomesticInvoice { get; set; }

        public PAYMENT_METHOD? PaymentMethod { get; set; } = PAYMENT_METHOD.BANK;
    }

    public enum PAYMENT_METHOD
    {
        BANK,
        CASH
    }
}
