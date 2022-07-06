using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Megatech.FMS.WebAPI.Models
{
    public class ManualReceiptModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string SignNo { get; set; }
        public string CCID { get; set; }

        public bool? Exported { get; set; }
    }
}