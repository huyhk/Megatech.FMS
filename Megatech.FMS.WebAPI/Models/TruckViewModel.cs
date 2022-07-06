using FMS.Data;

namespace Megatech.FMS.WebAPI.Models
{
    public class TruckViewModel
    {


        public int Id { get; set; }
        public string Code { get; set; }
        public string TruckNo { get; set; }

        public decimal CurrentAmount { get; set; }

        public decimal Capacity { get; set; }

        public string TabletSerial { get; set; }

        public string DeviceSerial { get; set; }

        public string DeviceIP { get; set; }

        public string PrinterIP { get; set; }

        public int AirportId { get; set; }

        public string AirportCode { get; set; }

        public string TaxCode { get; set; }

        public bool AllowNewRefuel { get; set; }


        public bool IsFHS { get; set; }

        public int ReceiptCount { get; set; }
        public REFUEL_COMPANY? RefuelCompany { get; set; }
        public string ReceiptCode
        {
            get
            {
                if (RefuelCompany == REFUEL_COMPANY.SKYPEC || RefuelCompany == null && TaxCode !=null)

                    return this.TaxCode.Substring(TaxCode.Length -2, 2) + Code.Substring(Code.Length - 2);
                else
                    return RefuelCompany.ToString().Substring(0, 2) + Code.Substring(Code.Length - 2);
            }
        }
    }
}