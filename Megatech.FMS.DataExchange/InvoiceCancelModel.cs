using FMS.Data;

namespace Megatech.FMS.DataExchange
{
    public class InvoiceCancelModel
    {
        public string hoadon68_id { get; set; }
        public string ldo { get; set; }
        public string benA { get; set; }

        public string chucvuA { get; set; }

        public string benB { get; set; }

        public string chucvuB { get; set; }

        public string sobienban { get; set; }

        public InvoiceCancelModel(Invoice inv)
        {
            hoadon68_id = inv.UniqueId.ToString();
            ldo = inv.CancelReason;
        }
    }
}
