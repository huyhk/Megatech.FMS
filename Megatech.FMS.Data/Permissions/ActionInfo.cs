using FMS.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Megatech.FMS.Data.Permissions
{
    [Table("Actions")]
    public class ActionInfo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ActionName { get; set; }

        public string DisplayName { get; set; }

        public int ControllerId { get; set; }
        public ControllerInfo Controller { get; set; }

        public ICollection<Role> Roles { get; set; }
    }
}
