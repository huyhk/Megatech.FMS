using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS.Data
{
    public class TruckLog: BaseEntity
    {
        public int TruckId { get; set; }
        

        public int UserId { get; set; }
        
        public User User { get; set; }

        public DateTime LogTime { get; set; }

        public string LogType { get; set; }

        public string LogText { get; set; }

        public string TabletId { get; set; }

    }
}
