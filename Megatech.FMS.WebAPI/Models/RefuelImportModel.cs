using FMS.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Megatech.FMS.WebAPI.Models
{
    public class DataImportModel
    {
        public string UniqueId { get; set; }

        // airline information
        [Required]
        public string AirlineCode { get; set; }
        public string AirlineName { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }
        public string Email { get; set; }

        // flight information
        [Required]
        public string FlightCode { get; set; }
        
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime? RefuelScheduledTime { get; set; }

        [Required]
        public string AircraftCode { get; set; }
        [Required]
        public string AircraftType { get; set; }
        [Required]
        public string RouteName { get; set; }

        public bool IsInternational { get; set; }
        [Required]

        public string ReceiptNumber { get; set; }

        [Required]
        [Range(typeof(DateTime),"2022/04/01", "2032/04/01", ErrorMessage = "Value of {0} must be greater than {1:dd/MM/yyyy}")]
        public DateTime ReceiptDate { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage ="Value of '{0}' must be greater than 0")]
        public decimal TotalGallon { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]

        public decimal TotalLiter { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]

        public decimal TotalKg { get; set; }
        [JsonIgnore]
        [Required (ErrorMessage ="Invalid Receipt Image Format (png, jpg)")]
        public string ReceiptImage { get; set; }

        public List<DataImportItemModel> Items { get; set; }
        
        [JsonIgnore]
        public int? AirportId { get; internal set; }

        public DATA_MODE Mode { get; set; } = DATA_MODE.INSERT;

        public Guid? UpdateId { get; internal set; }

        [JsonIgnore]

        public Guid? LocalUniqueId { get; internal set; }

       
        public REFUEL_COMPANY? RefuelCompany { get; set; } = REFUEL_COMPANY.TAPETCO;
    }



    public class DataImportItemModel
    {
        [Required]
        public string RefuelerNo { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]
        public decimal Gallon { get; set; } = 0;
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]
        public decimal Liter { get; set; } = 0;
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]
        public decimal Kg { get; set; } = 0;
        [Required]
        public decimal Temperature { get; set; } = 0;
        [Required]
        [Range(0.72, 0.86, ErrorMessage = "Value for {0} must be between {1} and {2}")]
        public decimal Density { get; set; } = 0;
        [Required]
        public string CertNo { get; set; }

        public decimal Techlog { get; set; } = 0;
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        [Required]
        //[Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]
        public decimal StartNumber { get; set; } = 0;
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Value of '{0}' must be greater than 0")]
        public decimal EndNumber { get; set; } = 0;
    }

    public class ImportResult
    {
        public DATA_IMPORT_RESULT Result { get; set; }
        public string ResultCode { get { return Result.ToString(); }   }
        public object Message { get; set; }
        public object ReturnId { get; internal set; }
        public string Link { get; internal set; }

        [JsonIgnore]
        public string  ImagePath { get; set; }
    }
}