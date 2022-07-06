using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FMS.Data
{
    public partial class Report : BaseEntity
    {
        public Report()
        {
            this.ReportDetail = new List<ReportDetail>();
        }
        public REPORT_TYPE ReportType { get; set; }

        public string Url { get; set; }
        public int? AirportId { get; set; }
        public DateTime FromTime { get; set; }
        public DateTime ToTime { get; set; }

        public User UserCreated { get; set; }

        public bool? IsGroup { get; set; }

        public bool IsActive { get; set; }
        public int? UserActivedId { get; set; }
        public User UserActived { get; set; }

        public bool? IsActive2 { get; set; }
        public int? UserActived2Id { get; set; }
        public User UserActived2 { get; set; }

        public bool? IsActive3 { get; set; }
        public int? UserActived3Id { get; set; }
        public User UserActived3 { get; set; }

        public bool? IsActive4 { get; set; }
        public int? UserActived4Id { get; set; }
        public User UserActived4 { get; set; }

        public bool? IsActive5 { get; set; }
        public int? UserActived5Id { get; set; }
        public User UserActived5 { get; set; }

        public int? User1Id { get; set; }
        public User User1 { get; set; }

        public int? User2Id { get; set; }
        public User User2 { get; set; }

        public int? FlightId { get; set; }
        public Flight Flight { get; set; }

        public DateTime? DateActived { get; set; }
        public DateTime? DateActived2 { get; set; }
        public DateTime? DateActived3 { get; set; }
        public DateTime? DateActived4 { get; set; }
        public DateTime? DateActived5 { get; set; }

        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
        public string Value4 { get; set; }
        public string Value5 { get; set; }
        public string Value6 { get; set; }
        public string Value7 { get; set; }
        public string Value8 { get; set; }
        public string Value9 { get; set; }
        public string Value10 { get; set; }
        public string Value11 { get; set; }
        public string Value12 { get; set; }
        public string Value13 { get; set; }
        public string Value14 { get; set; }
        public string Value15 { get; set; }
        public string Value16 { get; set; }
        public string Value17 { get; set; }
        public string Value18 { get; set; }
        public string Value19 { get; set; }
        public string Value20 { get; set; }
        public string Value21 { get; set; }
        public string Value22 { get; set; }
        public string Value23 { get; set; }
        public string Value24 { get; set; }
        public string Value25 { get; set; }
        public string Value26 { get; set; }
        public string Value27 { get; set; }

        public bool? BValue1 { get; set; }
        public bool? BValue2 { get; set; }
        public bool? BValue3 { get; set; }
        public bool? BValue4 { get; set; }
        public bool? BValue5 { get; set; }
        public bool? BValue6 { get; set; }
        public bool? BValue7 { get; set; }
        public bool? BValue8 { get; set; }
        public bool? BValue9 { get; set; }
        public bool? BValue10 { get; set; }
        public bool? BValue11 { get; set; }
        public bool? BValue12 { get; set; }
        public bool? BValue13 { get; set; }
        public bool? BValue14 { get; set; }
        public bool? BValue15 { get; set; }
        public bool? BValue16 { get; set; }
        public bool? BValue17 { get; set; }
        public bool? BValue18 { get; set; }
        public bool? BValue19 { get; set; }
        public bool? BValue20 { get; set; }
        public string Description { get; set; }

        public ICollection<ReportDetail> ReportDetail { get; set; }
    }

    [Table("ReportDetails")]
    public partial class ReportDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ReportId { get; set; }
        [ForeignKey("ReportId")]
        public Report Report { get; set; }
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
        public string Value4 { get; set; }
        public REPORT_DETAIL_TYPE? Type { get; set; }
    }
    public enum REPORT_DETAIL_TYPE
    {
        TRUCK,
        DRIVER,
        OPERATOR
    }
    public enum REPORT_TYPE
    {
        BM2501,
        BM7009,
        BM10002,
        BM10103,
        BM2507,
        BM2502,
        BM2505
    }
}
