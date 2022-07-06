namespace Megatech.FMS.WebAPI.Models
{
    public class AirlineImportModel
    {
        public string UnitId { get; set; }

        public string ShortObjectId { get; set; }

        public string ObjectName { get; set; }

        public string Address { get; set; }
        public int Type { get; set; }
        public string EmailAddress { get; set; }

        public string VATNo { get; set; }

        public bool Disable { get; set; }
        public string ObjectId { get;  set; }
        public string ObjectNameVAT { get;  set; }
    }
}