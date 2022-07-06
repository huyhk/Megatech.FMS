using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FMS.Data
{
    public partial class Airport : BaseEntity
    {
        [Required(ErrorMessage = "Vui lòng nhập dữ liệu")]
        public string Code { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập dữ liệu")]
        public string Name { get; set; }

        public string Address { get; set; }

        public int? SetTime { get; set; }

        public Branch Branch { get; set; }

        public IList<User> Users { get; set; }

        public int? DepotType { get; set; }

        public string TaxCode { get; set; }

        public string InvoiceName { get; set; }
    }
    public enum Branch
    {
        NONE,
        MB,
        MT,
        MN,

    }
}
