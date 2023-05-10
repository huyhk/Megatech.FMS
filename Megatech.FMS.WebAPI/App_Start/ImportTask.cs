using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Web.Hosting;

namespace Megatech.FMS.WebAPI.App_Start
{
    public class ImportTask
    {
        private static string BASE_URL = ConfigurationManager.AppSettings["IMPORT_BASE_URL"];
        private static string PRICE_BASE_URL = ConfigurationManager.AppSettings["IMPORT_PRICE_BASE_URL"];
        private static int INTERVAL = 60;
        public static void Execute()
        {
            if (BASE_URL == null)
                BASE_URL = "http://203.128.244.70:6125";

            if (PRICE_BASE_URL == null)
                PRICE_BASE_URL = "http://203.128.244.70:6124/api/";


            if (lastRequestDate < DateTime.Now.AddMinutes(0 - INTERVAL))
            {
                Logger.AppendLog("IMPORT", "Last Request " + lastRequestDate.ToString(), "import");
                lastRequestDate = DateTime.Now;

                HostingEnvironment.QueueBackgroundWorkItem(cToken =>
               {
                   ImportAirlines();

               });

                HostingEnvironment.QueueBackgroundWorkItem(cToken =>
                {
                    ImportPrices();

                });
                Logger.AppendLog("IMPORT", "Execute " + lastRequestDate.ToString(), "import");
            }
        }

        private static void ImportPrices()
        {
            DateTime date = DateTime.Today.AddMonths(1);
            date = date.AddDays(1 - date.Day);
            while ((ImportPrices(date) == 0 && date >= DateTime.Today.AddMonths(-10)) || date >= DateTime.Today)
                date = date.AddMonths(-1);


        }

        private static bool processing_airline = false;
        private static DateTime lastRequestDate;
        private static bool processing_price = false;

        private static void ImportAirlines()
        {
            if (!processing_airline)
            {
                try
                {
                    processing_airline = true;
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(PRICE_BASE_URL);
                    var result = client.GetAsync("GetCustomers").Result;

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        HttpContent content = result.Content;

                        var body = content.ReadAsStringAsync().Result;

                        var importList = System.Web.Helpers.Json.Decode<List<AirlineImportModel>>(body);

                        using (DataContext db = new DataContext())
                        {
                            foreach (var item in importList)
                            {


                                var a = db.Airlines.FirstOrDefault(c => c.Code.Equals(item.ShortObjectId, StringComparison.InvariantCultureIgnoreCase));
                                if (a == null)
                                {
                                    a = new Airline
                                    {
                                        Code = item.ShortObjectId,
                                        InvoiceCode = item.ObjectId,
                                        Name = item.ObjectName.CompoundToUnicode(),
                                        InvoiceName = item.ObjectNameVAT.CompoundToUnicode(),

                                        Address = item.Address,
                                        Unit = item.UnitId.ToUpper() == "GAL" ? UNIT.GALLON : UNIT.KG,
                                        AirlineType = item.Type,
                                        Email = item.EmailAddress,
                                        TaxCode = item.VATNo
                                    };
                                    db.Airlines.Add(a);
                                }
                                else
                                {
                                    //Logger.AppendLog("AIRLINE", item.ObjectId + ":" + item.ObjectName, "import");
                                    //if (string.IsNullOrEmpty(a.Name))
                                        a.Name = item.ObjectName.CompoundToUnicode();
                                    a.InvoiceName = item.ObjectNameVAT.CompoundToUnicode();
                                    a.InvoiceCode = item.ObjectId;
                                    a.Address = item.Address;
                                    a.Unit = item.UnitId.ToUpper() == "GAL" ? UNIT.GALLON : UNIT.KG;
                                    a.AirlineType = item.Type;
                                    a.Email = item.EmailAddress;
                                    a.TaxCode = item.VATNo;
                                    a.IsDeleted = item.Disable;

                                }
                            }
                            db.SaveChanges();
                        }
                    }
                    processing_airline = false;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "import");

                }
            }
        }

        private static int ImportPrices(DateTime date)
        {
            if (!processing_price)
            {
                try
                {
                    Logger.AppendLog("PRICE", "Start Import " + date.ToString("yyyy-MM"), "import");
                    processing_price = true;
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(PRICE_BASE_URL);
                    var url = string.Format("getPrices?nam={0}&thang={1}", date.Year, date.Month);

                    var result = client.GetAsync(url).Result;

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        HttpContent content = result.Content;

                        var body = content.ReadAsStringAsync().Result;

                        var importList = System.Web.Helpers.Json.Decode<List<PriceImportModel>>(body);
                        Logger.AppendLog("PRICE", "Price count : " + importList.Count.ToString(), "import");
                        using (DataContext db = new DataContext())
                        {
                            var productId = db.Products.Select(p => p.Id).FirstOrDefault();
                            foreach (var item in importList)
                            {
                                var unit = UNIT.GALLON;
                                Enum.TryParse<UNIT>(item.UnitId.ToUpper(), out unit);
                                var customerCode = item.ShortObjectId;
                                var customer = db.Airlines.FirstOrDefault(aa => customerCode != null && aa.Code.Equals(customerCode, StringComparison.InvariantCultureIgnoreCase));

                                var aq = db.ProductPrices.Where(c => c.StartDate.Equals(date) && c.BranchId == item.BranchId
                                && c.AirlineType == item.IsInternationalType && c.DepotType == item.IsType && c.StartDate == date
                                && c.Unit == (int)unit);
                                if (customer != null)
                                    aq = aq.Where(c => c.CustomerId == customer.Id);
                                var a = aq.FirstOrDefault();
                                if (a == null)
                                {
                                    a = new ProductPrice
                                    {
                                        StartDate = date,
                                        BranchId = item.BranchId,
                                        Currency = (CURRENCY)Enum.Parse(typeof(CURRENCY), item.CurrencyId),
                                        DepotType = item.IsType,
                                        AirlineType = item.IsInternationalType,
                                        Price = item.Price01,
                                        ProductId = productId,
                                        Unit = (int)unit,
                                        CustomerId = customer == null ? null : (int?)customer.Id,
                                        ExchangeRate = (CURRENCY)Enum.Parse(typeof(CURRENCY), item.CurrencyId) == CURRENCY.VND ? 1.0M : item.Ty_Gia
                                    };
                                    db.ProductPrices.Add(a);
                                }
                                else
                                {

                                    a.CustomerId = customer == null ? null : (int?)customer.Id;

                                    a.Currency = (CURRENCY)Enum.Parse(typeof(CURRENCY), item.CurrencyId);

                                    a.Price = item.Price01;
                                    a.ExchangeRate = a.Currency == CURRENCY.VND ? 1.0M : item.Ty_Gia;
                                    a.DateUpdated = DateTime.Now;
                                }
                                db.SaveChanges();
                            }
                            
                        }
                        processing_price = false;
                        Logger.AppendLog("PRICE", "End Import", "import");
                        return importList.Count;
                    }
                    processing_price = false;
                }
                catch (Exception ex)
                {
                    //Logger.AppendLog("PRICE", "Error: " + ex.Message, "import");
                    Logger.LogException(ex, "import");
                }

            }
            return 0;
        }
    }
}