namespace FMS.Data
{
    public class Company : BaseEntity
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string TaxCode { get; set; }

        public string Address { get; set; }
        public string Email { get; set; }
        public string InvoiceName { get; set; }


        public string InvoiceTaxCode { get; set; }

        public string InvoiceAddress { get; set; }
    }
}
