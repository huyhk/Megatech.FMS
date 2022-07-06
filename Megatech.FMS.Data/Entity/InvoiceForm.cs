using System;

namespace FMS.Data
{
    public class InvoiceForm : BaseEntity, IAirportBase
    {
        public int AirportId { get; set; }

        public Airport Airport { get; set; }

        public INVOICE_TYPE InvoiceType { get; set; }

        public string FormNo { get; set; }

        public string Sign { get; set; }

        public DateTime? StartDate { get; set; }

        public bool IsDefault { get; set; }
    }
}
