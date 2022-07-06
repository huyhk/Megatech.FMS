using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS.Data
{
    [Table("RefuelItem_Logs")]
    public class RefuelItem_Log
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Ids { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? UserId { get; set; }
    }
}
