using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FMS.Data
{
    public class Property : BaseEntity
    {
        public int SortOrder { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập dữ liệu")]
        public string Title { get; set; }
        public string Code { get; set; }
        public ENTITY_TYPE EntityType { get; set; }
        public REPORT_TYPE ReportType { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập dữ liệu")]
        public string Value1 { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập dữ liệu")]
        public string Value2 { get; set; }
        public string Note { get; set; }
        public ICollection<Flight> Flights { get; set; }
    }

    public enum ENTITY_TYPE
    {
        FLIGHT //Chuyến bay
    }
}

