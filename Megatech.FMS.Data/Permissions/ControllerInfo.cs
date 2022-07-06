using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Megatech.FMS.Data.Permissions
{
    [Table("Controllers")]
    public class ControllerInfo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ControllerName { get; set; }

        public string DisplayName { get; set; }

        public List<ActionInfo> Actions { get; set; }

    }
}
