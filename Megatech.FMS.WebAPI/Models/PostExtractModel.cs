using Newtonsoft.Json;
using System;

namespace Megatech.FMS.WebAPI.Models
{
    public class PostExtractModel
    {

        public string KeyId { get; set; } = "TEST";

        public string VoucherNo { get; set; } = "TEST";

        public string VoucherDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string AircraftNumber { get; set; } = "TEST";

        public string FlightNumber { get; set; } = "TEST";

        public string AircraftType { get; set; }

        public string ObjectId { get; set; } = "TEST";

        public string RequirementMan { get; set; }

        public string Title { get; set; }

        public string Reason { get; set; }

        public string AirportId { get; set; } = "TEST";

        public string Flight { get; set; }

        public int IsKind { get; set; } = 1;


        [JsonIgnore]
        public bool Exported { get; set; } = false;
        [JsonIgnore]
        public int Id { get; set; }

        public string BeginDate01 { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string EndDate01 { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");



        public string FuelTruckId { get; set; } = "TEST";
        //public string DriverName { get; set; } = "TEST";

        //public string PumpMan { get; set; } = "TEST";

        public decimal GallonQuantity { get; set; } = 0;
        public decimal ActualQuantity { get; set; } = 0;

        public decimal Temperature { get; set; } = 0;
        public decimal Density { get; set; } = 0;
        public decimal Kg { get; set; } = 0;

        public string UserId { get; set; }
    }
}