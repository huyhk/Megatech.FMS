using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS.Data
{
    [Table("GreenTaxes")]
    public class GreenTax:BaseEntity
    {
        public DateTime StartDate { get; set; }

        public decimal TaxAmount { get; set; }
    }
}
