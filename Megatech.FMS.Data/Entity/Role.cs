using Megatech.FMS.Data.Permissions;
using System.Collections.Generic;

namespace FMS.Data
{
    public class Role
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public ICollection<ActionInfo> Actions { get; set; }

        public ICollection<User> Users { get; set; }


    }
}
