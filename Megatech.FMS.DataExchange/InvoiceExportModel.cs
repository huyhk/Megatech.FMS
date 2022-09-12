using FMS.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Megatech.FMS.DataExchange
{
    public class InvoiceJsonData
    {

        public InvoiceJsonData()
        { }
        public InvoiceJsonData(Invoice inv)
        {
            data = new List<InvoiceExportModel>();
            data.Add(new InvoiceExportModel(inv));
            sign_type = inv.SignType != null ? (int)inv.SignType : (inv.Price == 0 ? 3 : 1);
        }
        public string tab_masters { get; set; } = "hoadon68";
        public int editmode { get; set; } = 1;
        public List<InvoiceExportModel> data { get; set; }
        public int sign_type { get; set; } = 1;

    }
    public class InvoiceExportModel
    {
        private static string EMAIL_LIST = ConfigurationManager.AppSettings["AITS_EMAIL_LIST"];
        public InvoiceExportModel(Invoice inv)
        {
            try
            {
                cctbao_id = inv.CCID !=null?inv.CCID: inv.InvoiceType == INVOICE_TYPE.INVOICE ? (inv.RefuelCompany == REFUEL_COMPANY.TAPETCO ? INVOICE_UNIQUE_ID.TAPETCO :
                    inv.RefuelCompany == REFUEL_COMPANY.NAFSC ? INVOICE_UNIQUE_ID.NAFSC : INVOICE_UNIQUE_ID.SKYPEC) :
                    inv.InvoiceType == INVOICE_TYPE.BILL ? "4DE25494-3723-40A4-A6B4-851C21EE851F" : "32A417CC-913C-4707-9952-20116383C9D2";
                khieu = inv.SignNo != null ? inv.SignNo :  inv.InvoiceType == INVOICE_TYPE.INVOICE ? (inv.RefuelCompany == REFUEL_COMPANY.TAPETCO ? INVOICE_SIGN_ID.TAPETCO :
                    inv.RefuelCompany == REFUEL_COMPANY.NAFSC ? INVOICE_SIGN_ID.NAFSC : INVOICE_SIGN_ID.SKYPEC) :
                    inv.InvoiceType == INVOICE_TYPE.BILL ? "PX21/08" : "PX21/09";
                if (inv.Manual != null && (bool)inv.Manual && inv.InvoiceType == INVOICE_TYPE.INVOICE)
                {
                    cctbao_id = INVOICE_UNIQUE_ID.MANUAL;
                    khieu = INVOICE_SIGN_ID.MANUAL;
                }
                mnmua = inv.Flight.Airline!= null && inv.Flight.Airline.InvoiceCode !=null? inv.Flight.Airline.InvoiceCode :(inv.CustomerCode ?? "");
                ten = inv.CustomerName;
                dchi = inv.CustomerAddress;
                mst = !string.IsNullOrEmpty(inv.TaxCode) ? inv.TaxCode :( inv.Flight.Airline != null ? inv.Flight.Airline.TaxCode : null ) ;
                if (string.IsNullOrEmpty(EMAIL_LIST))
                    email = inv.Customer.Email ?? "";
                else
                    email = EMAIL_LIST;// "bichnn@aits.vn, datnt@aits.vn, ngocnb@skypec.com.vn, huy.ho@megatech.com.vn,nhi.dang@megatech.com.vn";
                                       //sign_type = 1;
                htttoan = inv.Customer.PaymentMethod == PAYMENT_METHOD.CASH ? "Tiền mặt (Cash)" : "Chuyển khoản (Transfer)";
                tdlap = (inv.Date !=null? (DateTime)inv.Date: DateTime.Today).ToString("yyyy-MM-dd");
                dvtte = inv.Currency.ToString();
                tgia = inv.ExchangeRate ?? 1.0M;
                sbke = inv.BillNo;

                nbke = inv.BillDate.ToString("yyyy-MM-dd");
                tgtcthue = inv.SaleAmount + (decimal)inv.GreenTaxAmount;
                tgtthue = inv.TaxAmount;
                tgtttbso = inv.TotalAmount ?? 0;

                loaihd = inv.InvoiceType == INVOICE_TYPE.INVOICE ? 1 : 4;
                inv_KeyAPI_SHXTN = inv.FlightId + inv.BillNo;// + inv.Id;
                if (inv.InvoiceType == INVOICE_TYPE.INVOICE)
                    inv_Invoice_Type = inv.CustomerType == CUSTOMER_TYPE.INTERNATIONAL && inv.FlightType == FLIGHT_TYPE.OVERSEA ? 1 :
                        inv.CustomerType == CUSTOMER_TYPE.INTERNATIONAL && inv.FlightType == FLIGHT_TYPE.DOMESTIC ? 2 :
                        inv.CustomerType == CUSTOMER_TYPE.LOCAL && inv.FlightType == FLIGHT_TYPE.OVERSEA ? 3 :
                        inv.CustomerType == CUSTOMER_TYPE.LOCAL && inv.FlightType == FLIGHT_TYPE.DOMESTIC ? 4 : 5;
                else if (inv.InvoiceType == INVOICE_TYPE.BILL)
                    inv_Invoice_Type = 10; // phieu xuat kho
                else
                    inv_Invoice_Type = 6; // hut hoan tra

                inv_SoSo = inv.Items.FirstOrDefault().QCNo;

                inv_FromDate = inv.Items.OrderBy(it => it.StartTime).Select(d => d.StartTime).FirstOrDefault();
                inv_ToDate = inv.Items.OrderBy(it => it.EndTime).Select(d => d.EndTime).LastOrDefault();

                inv_Store_Code = inv.Flight.Airport.Code;
                inv_Store_Name = inv.RefuelCompany == REFUEL_COMPANY.TAPETCO || inv.RefuelCompany == REFUEL_COMPANY.NAFSC ? inv.RefuelCompany.ToString() : inv.Flight.Airport.Name;
                inv_Location = inv.Flight.Airport.Code;
                DefuelingNo = inv.DefuelingNo ?? "";
                inv_Refueling_Method = inv.RefuelCompany == REFUEL_COMPANY.TAPETCO || inv.RefuelCompany == REFUEL_COMPANY.NAFSC ? "Tra nạp ngầm (FHS)" : "Tra nạp xe (Refueler)";
                details = new List<InvoiceDetailJsonData>();
                details.Add(new InvoiceDetailJsonData(inv));
            }
            catch (Exception ex)
            {
                Logging.Logger.AppendLog("EXP", "InvoiceExportModel(Invoice inv)", "aits-error");
                Logging.Logger.LogException(ex, "aits-error");

            }
        }
        public string cctbao_id { get; set; }
        public string khieu { get; set; }
        public string mnmua { get; set; }
        public string ten { get; set; }
        public string mst { get; set; }
        public string dchi { get; set; }
        public string email { get; set; }


        public string tdlap { get; set; }
        public string stknmua { get; set; }
        public string dvtte { get; set; }
        public decimal tgia { get; set; }
        public string sbke { get; set; }
        public string nbke { get; set; }
        public decimal tgtcthue { get; set; }
        public decimal tgtthue { get; set; }
        public decimal tgtttbso { get; set; }
        public int loaihd { get; set; }
        public string htttoan { get; set; } = "Chuyển khoản (Transfer)";
        public string inv_Refueling_Method { get; set; } = "Tra nạp xe (Refueler)";
        public int inv_Invoice_Type { get; set; }
        public string inv_SoSo { get; set; }
        public DateTime inv_FromDate { get; set; }
        public DateTime inv_ToDate { get; set; }
        public string inv_Adjustment_Type { get; set; }
        public string inv_KeyAPI_QLHH { get; set; }
        public string inv_KeyAPI_TCKT { get; set; }
        public string inv_Store_Code { get; set; }
        public string inv_Store_Name { get; set; }

        public string inv_Location { get; set; }
        public string DefuelingNo { get; set; }

        public string inv_KeyAPI_SHXTN { get; set; }
        //public int sign_type { get; set; }


        public List<InvoiceDetailJsonData> details { get; set; }


    }

    public struct INVOICE_UNIQUE_ID
    {
        public static string SKYPEC = "ACE7A63F-D0B4-4CCD-B16D-FBE8761B5C1A";
        public static string NAFSC = "29955C52-8552-4C55-9FB5-085DC67C9752";
        public static string TAPETCO = "5E8FB7E0-B6BC-4B91-B428-C354AAB2814E";
        public static string MANUAL = "77F3A17A-7697-4B15-BB18-99B3ABC222BD";

        public static string SKYPEC_DOMESTIC = "D487F53A-5DAF-439C-9214-01D15EB3011B";
    }

    public struct INVOICE_SIGN_ID
    {
        public static string SKYPEC = "1K21TSB";
        public static string NAFSC = "1K21TNB";
        public static string TAPETCO = "1K21TTB";
        public static string MANUAL = "1K22TZZ";
        public static string SKYPEC_DOMESTIC = "1K22TSA";

    }
    public class InvoiceDetailJsonData
    {
        public InvoiceDetailJsonData(Invoice inv)
        {
            data = new List<InvoiceExportDetailModel>();
            int i = 1;
            if (inv.InvoiceType == INVOICE_TYPE.INVOICE)
            {
                var dt = new InvoiceExportDetailModel(inv);
                dt.stt = 1;
                data.Add(dt);
                if (inv.GreenTax > 0)
                {
                    var dtTax = new InvoiceExportDetailModel(inv, true);
                    dtTax.stt = 2;
                    data.Add(dtTax);
                }
            }
            else
                foreach (var item in inv.Items)
                {
                    var dt = new InvoiceExportDetailModel(item);
                    dt.stt = i++;
                    data.Add(dt);
                }
        }
        public string tab_details { get; set; } = "hoadon68_chitiet";
        public List<InvoiceExportDetailModel> data { get; set; }
    }
    public class InvoiceExportDetailModel
    {
        /// <summary>
        /// for invoice type = 0, INVOICE
        /// </summary>
        /// <param name="item"></param>
        public InvoiceExportDetailModel(Invoice item)
        {
            try
            {
                ten = "JET A-1";
                //inv_Refueler_No = item.TruckNo;
                //inv_Start_Meter = item.StartNumber;
                //inv_End_Meter = item.EndNumber;
                dgia = (decimal)item.Price;
                thtien = (decimal)item.SaleAmount;// * (item.Unit == UNIT.GALLON ? item.Gallon : item.Weight);
                dvtinh = item.Unit == UNIT.GALLON ? "USG" : "KG";
                sluong = item.Unit == UNIT.GALLON ? item.Gallon : item.Weight;

                tsuat = string.Format("{0:#0}", item.TaxRate > 0 ? item.TaxRate * 100 : -1);
                ptthue = item.TaxRate * 100;
                tthue = (decimal)Math.Round((double)(item.TaxRate*thtien), item.Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
                tgtien = tthue + thtien;
                inv_AircraftType = item.AircraftType ?? item.Flight.AircraftType;
                inv_RegNo = item.AircraftCode ?? item.Flight.AircraftCode;
                inv_FlightNo = item.FlightCode ?? item.Flight.Code;
                inv_Router = item.RouteName ?? item.Flight.RouteName;
                inv_Actual_Temprature = item.Temperature;
                inv_Actual_Ensity = item.Density;
                inv_Observed_Liters = item.Volume;
                inv_Quantity_Ga = item.Gallon;
                inv_Quantity_Kg = item.Weight;
                is_invoice = 1;
                inv_Number_DeliveryBill = item.BillNo;
                inv_Date_DeliveryBill = item.BillDate.ToString("yyyy-MM-dd");

            }
            catch (Exception ex)
            {
                Logging.Logger.AppendLog("EXP", "InvoiceExportDetailModel(Invoice item)", "aits-error");
                Logging.Logger.LogException(ex, "aits-error");
            }
        }


        /// <summary>
        /// for invoice type = 1, 2 BILL, RETURN
        /// </summary>
        /// <param name="item"></param>
        public InvoiceExportDetailModel(InvoiceItem item)
        {
            ten = "JET A-1";
            inv_Refueler_No = item.TruckNo;
            inv_Start_Meter = item.StartNumber;
            inv_End_Meter = item.EndNumber;
            dgia = (decimal)item.Invoice.Price;

            thtien = (decimal)item.Invoice.Price * (item.Invoice.Unit == UNIT.GALLON ? item.Gallon : item.Weight);
            dvtinh = item.Invoice.Unit == UNIT.GALLON ? "USG" : "KG";
            sluong = item.Invoice.Unit == UNIT.GALLON ? item.Gallon : item.Weight;
            tsuat = string.Format("{0:#0}", item.Invoice.TaxRate > 0 ? item.Invoice.TaxRate * 100 : -1);
            ptthue = item.Invoice.TaxRate * 100;
            tthue = (decimal)Math.Round((double)(item.Invoice.TaxRate * thtien), item.Invoice.Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
            tgtien = tthue + thtien;
            inv_AircraftType = item.Invoice.AircraftType?? item.Invoice.Flight.AircraftType;
            inv_RegNo = item.Invoice.AircraftCode ?? item.Invoice.Flight.AircraftCode;
            inv_FlightNo = item.Invoice.Flight.Code;
            inv_Router = item.Invoice.Flight.RouteName;
            inv_Actual_Temprature = item.Temperature;
            inv_Actual_Ensity = item.Density;
            inv_Observed_Liters = item.Volume;
            inv_Quantity_Ga = item.Gallon;
            inv_Quantity_Kg = item.Weight;
            is_invoice = 1;
            inv_Number_DeliveryBill = item.Invoice.BillNo;
            inv_Date_DeliveryBill = item.Invoice.BillDate.ToString("yyyy-MM-dd");



        }

        public InvoiceExportDetailModel(Invoice item, bool greenTax = false)
        {
            try
            {
                ten = "Thuế BVMT";
                //inv_Refueler_No = item.TruckNo;
                //inv_Start_Meter = item.StartNumber;
                //inv_End_Meter = item.EndNumber;
                dgia = (decimal)item.GreenTax;
                thtien = (decimal)item.GreenTaxAmount;// * (item.Unit == UNIT.GALLON ? item.Gallon : item.Weight);
                dvtinh = "LIT";
                sluong = item.Volume;

                tsuat = string.Format("{0:#0}", item.TaxRate > 0 ? item.TaxRate * 100 : -1);
                ptthue = item.TaxRate * 100;
                tthue = (decimal)Math.Round((double)(item.TaxRate * item.GreenTaxAmount), item.Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
                tgtien = tthue + thtien;
                inv_AircraftType = item.AircraftType ?? item.Flight.AircraftType;
                inv_RegNo = item.AircraftCode ?? item.Flight.AircraftCode;
                inv_FlightNo = item.FlightCode ?? item.Flight.Code;
                inv_Router = item.RouteName ?? item.Flight.RouteName;
                inv_Actual_Temprature = item.Temperature;
                inv_Actual_Ensity = item.Density;
                inv_Observed_Liters = item.Volume;
                inv_Quantity_Ga = item.Gallon;
                inv_Quantity_Kg = item.Weight;
                is_invoice = 1;
                inv_Number_DeliveryBill = item.BillNo;
                inv_Date_DeliveryBill = item.BillDate.ToString("yyyy-MM-dd");

            }
            catch (Exception ex)
            {
                Logging.Logger.AppendLog("EXP", "InvoiceExportDetailModel(Invoice item)", "aits-error");
                Logging.Logger.LogException(ex, "aits-error");
            }
        }
        public int stt { get; set; }

        public string ten { get; set; }
        public string inv_Refueler_No { get; set; }
        public decimal inv_Start_Meter { get; set; }
        public decimal inv_End_Meter { get; set; }

        public decimal dgia { get; set; }

        public decimal? sluong { get; set; }
        public string dvtinh { get; set; }
        public decimal? thtien { get; set; }
        public decimal ptthue { get; set; }
        public string tsuat { get; set; }
        public decimal? tthue { get; set; }
        public decimal? tgtien { get; set; }

        public string inv_AircraftType { get; set; }
        public string inv_RegNo { get; set; }
        public string inv_FlightNo { get; set; }
        public string inv_Router { get; set; }
        public decimal? inv_Actual_Temprature { get; set; }
        public decimal? inv_Actual_Ensity { get; set; }
        public decimal? inv_Observed_Liters { get; set; }
        public decimal? inv_Quantity_Kg { get; set; }
        public decimal? inv_Quantity_Ga { get; set; }

        public string inv_Number_DeliveryBill { get; set; }
        public string inv_Date_DeliveryBill { get; set; }
        public int is_invoice { get; set; }

        public int tchat { get; set; } = 1;

    }

    public class ExportResultModel
    {
        public string code { get; set; }

        public string message { get; set; }

        public InvoiceResultModel data { get; set; }

        public bool success { get; set; }

        public string api { get; set; } = "AITS";
    }

    public class InvoiceResultModel
    {
        public string id { get; set; }

        public string shdon { get; set; }

        public string khieu { get; set; }

        public DateTime? tdlap { get; set; }

        public string hoadon68_id { get; set; }
    }
}
