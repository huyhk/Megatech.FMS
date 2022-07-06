using FMS.Data;
using System.ComponentModel.DataAnnotations;

namespace Megatech.FMS.WebAPI.Models
{
    public class AirlineViewModel
    {
        public int Id { get; set; }
        [Required]

        public string Code { get; set; }
        [Required]

        public string Name { get; set; }

        public string TaxCode { get; set; }

        public string Address { get; set; }

        public decimal Price { get; set; }

        public int? Unit { get; set; }

        public CURRENCY Currency { get; set; }

        public string ProductName { get; set; }

        public string InvoiceName { get; set; }

        public string InvoiceTaxCode { get; set; }

        public string InvoiceAddress { get; set; }

        public bool IsInternational { get; set; }

        public int PriceId { get; set; }

        public string UserName { get; set; }

        public int AirportId { get; set; }

        public decimal Price01 { get; set; }

        public int Price01Id { get; set; }


    }
}