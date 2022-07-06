using FMS.Data;
using Newtonsoft.Json;
using System;

namespace Megatech.FMS.WebAPI.Models
{
    public class PostModel
    {
        private static decimal GALLON_TO_LITTER = 3.7854M;



        public string KeyId { get; set; } = "TEST";

        public string FormNo { get; set; } = "TEST";

        public string Sign { get; set; } = "TEST";

        public string VoucherNo { get; set; } = "TEST";

        public string VoucherDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string AircraftNumber { get; set; } = "TEST";

        public string FlightNumber { get; set; } = "TEST";

        public string ObjectId { get; set; } = "TEST";

        public string CurrencyId { get; set; } = "TEST";

        public string Date01 { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string UnitId { get; set; } = "TEST";


        public string FromAirportId { get; set; } = "TEST";
        public string ToAirportId { get; set; } = "TEST";

        public decimal TotalGallonQuantity { get; set; } = 0;

        //public decimal TotalActualQuantity { get; set; } = 0;

        //public decimal TotalKg { get; set; } = 0;

        public decimal TotalActualQuantity
        {
            //get {
            //    return Math.Round(TotalGallonQuantity * GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero);
            //}
            get;
            set;
        }
        public decimal TotalKg
        {
            //get
            //{
            //    return Math.Round(TotalActualQuantity * Density, 0, MidpointRounding.AwayFromZero);
            //}
            get;
            set;
        }
        public decimal TotalUnitPrice { get; set; } = 0;

        public decimal TotalOriginalAmount
        {
            get
            {
                return (Unit == UNIT.KG ? TotalKg : TotalGallonQuantity) * TotalUnitPrice;
            }
        }

        public string VATGroupID { get; set; } = "TEST";

        public decimal TotalVATOriginalAmount
        {
            get
            {
                return TaxRate * TotalUnitPrice * (Unit == UNIT.KG ? TotalKg : TotalGallonQuantity);
            }
        }

        [JsonIgnore]
        public UNIT? Unit { get; set; } = 0;
        [JsonIgnore]
        public decimal TaxRate { get; set; } = 0;
        [JsonIgnore]
        public bool Exported { get; set; } = false;
        [JsonIgnore]
        public int Id { get; set; }


        public string TestResult { get; set; } = "TEST";
        public string BeginDate01 { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string EndDate01 { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string PumpLocation { get; set; } = "TEST";

        public string FuelTruckId { get; set; } = "TEST";
        public string DriverName { get; set; } = "TEST";

        public string PumpMan { get; set; } = "TEST";

        public string AircraftKg { get; set; } = "0";
        public decimal BeforceNum { get; set; } = 0;
        public decimal AfterNum { get; set; } = 0;
        public decimal GallonQuantity { get; set; } = 0;
        public decimal ActualQuantity { get; set; } = 0;

        public decimal Temperature { get; set; } = 0;
        public decimal Density { get; set; } = 0;
        public decimal Kg { get; set; } = 0;

        public string UserId { get; set; }
    }
}