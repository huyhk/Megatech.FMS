using System;

namespace FMS.Data
{
    public class ProductPrice : BaseEntity
    {

        public ProductPrice()
        {
            this.Currency = CURRENCY.VND;
        }

        public int? CustomerId { get; set; }

        public Airline Customer { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public decimal Price { get; set; }

        public CURRENCY Currency { get; set; }

        public int? DepotType { get; set; }

        public int? AirlineType { get; set; }

        public int? BranchId { get; set; }

        public int? Unit { get; set; }


        public decimal? ExchangeRate { get; set; }

    }
    public enum CURRENCY
    {
        VND,
        USD
    }

    public enum UNIT
    {
        GALLON,
        KG
    }
}
