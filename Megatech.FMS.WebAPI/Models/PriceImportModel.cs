namespace Megatech.FMS.WebAPI.Models
{
    public class PriceImportModel
    {
        public int IsInternationalType { get; set; }

        public int BranchId { get; set; }

        public string CurrencyId { get; set; }

        public decimal Price01 { get; set; }

        public int IsType { get; set; }

        public string UnitId { get; set; }

        public string ShortObjectId { get; set; }

        public decimal Ty_Gia { get; set; }
    }
}