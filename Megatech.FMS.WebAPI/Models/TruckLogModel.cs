using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Megatech.FMS.WebAPI.Models
{
    public class TruckLogModel: BaseViewModel
    {
        public DateTime LogTime { get; set; }
        public string LogType { get; set; }

        public string LogText { get; set; }

        public int UserId { get; set; }

        public int TruckId { get; set; }

        public string TabletId { get; set; }


    }
}