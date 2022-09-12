using System;
using System.Collections.Generic;

namespace FMS.Data
{
    public class FHSImport : BaseEntity
    {
        // airline information
        public string AirlineCode { get; set; }
        public string AirlineName { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }
        public string Email { get; set; }

        // flight information

        public string FlightCode { get; set; }

        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }

        public string RouteName { get; set; }
        public string AircraftCode { get; set; }

        public string AircraftType { get; set; }

        public bool IsInternational { get; set; }

        public string ReceiptNumber { get; set; }

        public ICollection<FHSImportItem> Items { get; set; }

        
        public decimal? TotalGallon { get; set; }
        
        public decimal? TotalLiter { get; set; }
        
        public decimal? TotalKg { get; set; }

        public int UserImportedId { get; set; }

        public DateTime DateImported { get; set; }

        public DATA_IMPORT_RESULT ResultCode { get; set; } = DATA_IMPORT_RESULT.FAILED;

        public string Result { get; set; }

        public string UniqueId { get; set; }

        public Guid LocalUniqueId { get; set; } = Guid.NewGuid();
        public Guid? UpdateId { get; set; }

        public DATA_MODE Mode { get; set; }
        public string ImagePath { get; set; }

        public REFUEL_COMPANY? RefuelCompany { get; set; }
    }

    public enum DATA_IMPORT_RESULT
    {
        SUCCESS = 0,
        FAILED = 1,
        DUPLICATED = 2,
        REFUELER_NOT_EXISTS,
        AIRLINE_NOT_EXISTS,
        KEY_NOT_EXISTS
    }
    public enum DATA_MODE
    {
        INSERT,
        REPLACE
    }
    public class FHSImportItem : BaseEntity
    {
        public string RefuelerNo { get; set; }
        public decimal Gallon { get; set; } = 0;

        public decimal Liter { get; set; } = 0;

        public decimal Kg { get; set; } = 0;

        public decimal Temperature { get; set; } = 0;

        public decimal Density { get; set; } = 0;

        public string CertNo { get; set; }

        public decimal Techlog { get; set; } = 0;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal StartNumber { get; set; } = 0;

        public decimal EndNumber { get; set; } = 0;
    }
}
