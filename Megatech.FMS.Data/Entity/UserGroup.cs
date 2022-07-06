using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS.Data
{
    public class UserGroup
    {
        public int Id { get; set; }
        public int Id_1 { get; set; }
        public int Id_2 { get; set; }
        public string Name_1 { get; set; }
        public string Name_2 { get; set; }
        public int? AirportId { get; set; }
    }
}
