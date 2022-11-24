using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMS.Data
{
    public class RefuelItem : BaseEntity
    {
        public RefuelItem()
        {
            RefuelItemType = REFUEL_ITEM_TYPE.REFUEL;
        }
        public int TruckId { get; set; }

        public Truck Truck { get; set; }

        public DateTime StartTime { get; set; }

        public decimal Amount { get; set; }

        public decimal Temperature { get; set; }

        public decimal StartNumber { get; set; }

        public decimal EndNumber { get; set; }

        public decimal? OriginalEndMeter { get; set; }
        public int? CreateType { get; set; }

        //public decimal ManualEndnumber { get; set; }

        //public decimal Volume { get; set; }

        //public decimal Weight { get; set; }

        public bool Completed { get; set; }

        public bool Printed { get; set; }

        public DateTime? EndTime { get; set; }

        public int? DriverId { get; set; }
        public User Driver { get; set; }

        public int? OperatorId { get; set; }
        public User Operator { get; set; }

        public DateTime? DeviceStartTime { get; set; }

        public DateTime? DeviceEndTime { get; set; }

        public decimal Gallon { get; set; }

        public decimal? ReturnAmount { get; set; }

        public string InvoiceNumber { get; set; }



        //public int RefuelId { get; set; }
        //[ForeignKey("RefuelId")]
        //public virtual Refuel Refuel { get; set; }

        public REFUEL_ITEM_STATUS Status { get; set; } = REFUEL_ITEM_STATUS.NONE;

        //public ITEM_PRINT_STATUS PrintStatus { get; set; }

        public decimal ManualTemperature { get; set; }

        public decimal Density { get; set; }

        public int FlightId { get; set; }

        public Flight Flight { get; set; }

        public decimal Price { get; set; }

        public CURRENCY Currency { get; set; } = CURRENCY.VND;

        public UNIT? Unit { get; set; } = UNIT.GALLON;

        public string QCNo { get; set; }

        public decimal TaxRate { get; set; }

        //public decimal SaleAmount { get; set; }

        public int? ApprovalUserId { get; set; }


        public ITEM_APPROVE_STATUS ApprovalStatus { get; set; }


        public string ApprovalNote { get; set; }

        public ITEM_CREATED_LOCATION CreatedLocation { get; set; } = ITEM_CREATED_LOCATION.IMPORT;

        public REFUEL_ITEM_TYPE RefuelItemType { get; set; } = REFUEL_ITEM_TYPE.REFUEL;


        public string WeightNote { get; set; }

        public decimal? TechLog { get; set; }

        public bool? Exported { get; set; } = false;

        public bool? ExportExtract { get; set; } = false;

        public PRINT_TEMPLATE? PrintTemplate { get; set; } = PRINT_TEMPLATE.INVOICE;

        public int? InvoiceFormId { get; set; }
        public InvoiceForm InvoiceForm { get; set; }


        public string ReturnInvoiceNumber { get; set; }



        public const decimal GALLON_TO_LITTER = 3.7854M;


        public decimal? Volume
        {
            get; set;
        }

        public decimal? Weight
        {
            get; set;
        }

        public decimal? OriginalTemperature { get; set; }

        public decimal? OriginalDensity { get; set; }

        public decimal? OriginalGallon { get; set; }

        public decimal OriginalVolume
        {
            get
            {
                return Math.Round(this.OriginalGallon ?? 0 * GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero);
            }
        }


        public decimal OriginalWeight
        {
            get
            {
                return Math.Round(this.OriginalVolume * this.Density, 0, MidpointRounding.AwayFromZero);
            }
        }


        public decimal SalesAmount
        {
            //get;
            get
            {
                return Math.Round((Unit == UNIT.GALLON ? this.Amount : this.Weight ?? 0) * this.Price, Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
            }
        }
        public decimal VATAmount
        {
            get
            {
                return Math.Round(this.SalesAmount * this.TaxRate, Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
            }
        }

        public decimal TotalSalesAmount
        {
            get
            {
                return Math.Round(this.SalesAmount * (1 + this.TaxRate), Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
            }
        }

        [ForeignKey("UserUpdatedId")]
        public User UserUpdated { get; set; }


        public BM2508_RESULT? BM2508Result { get; set; }
        [NotMapped]
        public bool BondingCable
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.BONDING_CABLE);
            }
            set
            {
                if (BM2508Result == null)
                    BM2508Result = 0;

                BM2508Result = (BM2508Result | BM2508_RESULT.BONDING_CABLE) ^ (value ? 0 : BM2508_RESULT.BONDING_CABLE);

            }
        }
        [NotMapped]
        public bool FuelingHose
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.FUELING_HOSE);
            }
            set
            {
                if (BM2508Result == null)
                    BM2508Result = 0;

                BM2508Result = (BM2508Result | BM2508_RESULT.FUELING_HOSE) ^ (value ? 0 : BM2508_RESULT.FUELING_HOSE);

            }
        }
        [NotMapped]
        public bool FuelingCap
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.FUELING_CAP);
            }
            set
            {
                if (BM2508Result == null)
                    BM2508Result = 0;

                BM2508Result = (BM2508Result | BM2508_RESULT.FUELING_CAP) ^ (value ? 0 : BM2508_RESULT.FUELING_CAP);

            }
        }
        [NotMapped]
        public bool Ladder
        {
            get
            {
                return BM2508Result.HasValue && BM2508Result.Value.HasFlag(BM2508_RESULT.LADDER);
            }
            set
            {
                if (BM2508Result == null)
                    BM2508Result = 0;

                BM2508Result = (BM2508Result | BM2508_RESULT.LADDER) ^ (value ? 0 : BM2508_RESULT.LADDER);

            }
        }

        public Guid? UniqueId { get; set; } = Guid.NewGuid();

        public int? ReceiptId { get; set; }
        public Receipt Receipt { get; set; }



        public int? ReceiptCount { get; set; }

        public string ReceiptNumber { get; set; }

        public Guid? ReceiptUniqueId { get; set; }
        public RETURN_UNIT? ReturnUnit { get; set; }


        public string LastUpdateDevice { get; set; }
    }


    public enum RETURN_UNIT
    {
        KG,
        GALLON
    }
    public enum REFUEL_ITEM_STATUS
    {
        NONE,
        PROCESSING,
        PAUSED,
        DONE,
        ERROR
    }

    public enum ITEM_PRINT_STATUS
    {
        NONE,
        SUCCESS,
        ERROR
    }

    public enum ITEM_APPROVE_STATUS
    {
        NONE,
        APPROVED,
        REJECTED
    }

    public enum ITEM_CREATED_LOCATION
    {
        IMPORT,
        WEB,
        COPY,
        APP,
        FHS
    }
    public enum REFUEL_ITEM_TYPE
    {
        REFUEL,
        EXTRACT,
        TEST
    }

    public enum REFUEL_UNIT
    {
        GALLON,
        KG
    }

    public enum REFUEL_CURRENCY
    {
        VND,
        USD
    }

    public enum PRINT_TEMPLATE
    {
        INVOICE,
        BILL
    }
}