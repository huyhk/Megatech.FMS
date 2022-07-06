using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Megatech.FMS.WebAPI.Models
{
    public class FHSReportModel
    {
        public DateTime Date { get; set; }

        public int Total { get; set; }

        public int Failed { get; set; }

        public int Success { get; set; }

        public string[] FailedItems { get; set; }
        public string[] SuccessItems { get; set; }
    }
}