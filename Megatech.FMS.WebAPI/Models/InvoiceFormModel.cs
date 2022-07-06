using FMS.Data;

namespace Megatech.FMS.WebAPI.Models
{
    public class InvoiceFormModel
    {
        public int Id { get; set; }
        public int AirportId { get; set; }
        public INVOICE_TYPE InvoiceType { get; set; }
        public string FormNo { get; set; }
        public string Sign { get; set; }
        public bool IsDefault { get; set; }
    }
}