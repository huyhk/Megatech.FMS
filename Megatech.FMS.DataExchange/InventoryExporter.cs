using EntityFramework.DynamicFilters;
using FMS.Data;
using Megatech.FMS.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Megatech.FMS.DataExchange
{
    public class InventoryExporter
    {
        private static string EXPORT_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";

        private class LoginModel
        {
            public string UserName { get; set; }

            public string Token { get; set; }
        }

        private class PostResultModel
        {
            public int Done { get; set; }

            public string Message { get; set; }
        }

        private static string API_BASE_URL = ConfigurationManager.AppSettings["OMEGA_BASE_URL"];// "http://203.128.244.66:10001/api/";
        private static string LIMIT_FHS_DATE = ConfigurationManager.AppSettings["LIMIT_FHS_DATE"];
        private static string Login()
        {
            //Logger.AppendLog("EXPORT", "Start Login ", "export");
            string token = null;
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(API_BASE_URL);

                string url = "Authentication.asmx/Login";
                HttpContent content = new StringContent("{\"UserName\":\"Admin\",\"Password\":\"123456\"}", Encoding.UTF8, "application/json");
                var result = client.PostAsync(url, content);

                if (result.Result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var model = JsonConvert.DeserializeObject<LoginModel>(result.Result.Content.ReadAsStringAsync().Result.ToString());
                    if (model != null)
                    {
                        token = model.Token;
                    }
                }
            }
            catch (Exception ex)
            {


                Logger.AppendLog("OMEGA", "Login error : " + ex.Message, "omega");
            }
            // Logger.AppendLog("EXPORT", "End Login ", "export");
            return token;
        }

        private static ExportResultModel ExportInventory(List<InventoryExportModel> model, string token)
        {
            try
            {
                var url = API_BASE_URL + "Skypec.asmx/PushData";

                var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.Method = "POST";

                httpRequest.ContentType = "application/json";
                httpRequest.Headers["Authorization"] = token;

                var jsonData = new { Items = model };
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.DateFormatString = EXPORT_DATE_FORMAT;
                var data = JsonConvert.SerializeObject(jsonData, jsonSettings);
                Logger.AppendLog("ExportInventory", data, "omega-data");

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);

                }

                using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {

                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var result = streamReader.ReadToEnd();
                            try
                            {
                                var resultModel = JsonConvert.DeserializeObject<PostResultModel>(result);

                                if (resultModel == null || resultModel.Done != 1)
                                {
                                    return new ExportResultModel { api = "OMEGA", message = resultModel.Message, success = false };
                                }
                            }
                            catch (Exception ex)
                            {
                                return new ExportResultModel { api = "OMEGA", message = ex.Message, success = false };
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    if ((ex as WebException).Response != null)
                    {
                        var stream = (ex as WebException).Response.GetResponseStream();
                        string response = new StreamReader(stream).ReadToEnd();
                        var resultModel = JsonConvert.DeserializeObject<PostResultModel>(response);
                        Logger.AppendLog("ExportInventory", response, "omega");
                        if (resultModel != null && resultModel.Message.StartsWith("400-"))
                        {
                            Logger.AppendLog("ExportInventory", "duplicated", "omega");
                            return new ExportResultModel { api = "OMEGA", message = "duplicate", success = true };
                        }
                        else if (resultModel != null)
                        {

                            return new ExportResultModel { api = "OMEGA", code = resultModel.Done.ToString(), message = resultModel.Message, success = false };
                        }
                    }
                }
                Logger.AppendLog("ExportInventory", ex.Message, "omega");
                return new ExportResultModel { api = "OMEGA", message = ex.Message, success = false };
            }

            return new ExportResultModel { api = "OMEGA", message = "", success = true };
        }

        //private static DataContext db = new DataContext();
        private static bool running = false;
        public static ExportResultModel Export(int id)
        {
            if (running)
                return new ExportResultModel { code = "999", message = "busy" };
            using (DataContext db = new DataContext())
            {
                running = true;

                try
                {
                    //db.Database.ExecuteNonQuery("exec usp_fix_invoices");
                    db.DisableFilter("IsNotDeleted");
                    var inv = db.Invoices.Include(i => i.Items.Select(iit => iit.Operator))
                        .Include(i => i.Receipt)
                        .Include(i => i.Items.Select(iit => iit.Driver))
                        .Include(i => i.Flight.Airport).FirstOrDefault(i => i.Id == id );
                    if (inv == null )
                        return new ExportResultModel { success = false, code = "404", message = "null invoice" };
                    var limitDate = new DateTime(2022, 04, 05);
                    if (LIMIT_FHS_DATE != null)
                        DateTime.TryParseExact(LIMIT_FHS_DATE, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                               DateTimeStyles.None, out limitDate);

                    if (inv.IsFHS && inv.Date < limitDate)
                        return new ExportResultModel { success = false, code = "401", message = "FHS invoice temporary disable" };

                    Logger.AppendLog("Export", "invoice id: " + id.ToString() + " invoice # " + inv.InvoiceNumber + " bill #: " + inv.BillNo, "omega");

                    if (string.IsNullOrEmpty(inv.InvoiceNumber))
                    {
                        /// looking for invoice number from refuelitems
                        /// 
                            //return new ExportResultModel { success = false, code = "405", message = "Invoice number empty"};
                        var itemIds = inv.Items.Select(it => it.RefuelItemId).ToArray();
                        inv.InvoiceNumber = db.RefuelItems.Where(r => itemIds.Contains(r.Id) && r.InvoiceNumber != null).Select(r => r.InvoiceNumber).FirstOrDefault();
                    }

                    //check null driver/operator

                    //if (inv.Items.Any(it => it.DriverId == null || it.OperatorId == null))                    
                    //{
                    //    var nullDO = inv.Items.Where(it => it.DriverId == null || it.OperatorId == null).ToList();
                    //    foreach (var item in nullDO)
                    //    {
                    //        var doNull = db.RefuelItems.Where(r => r.Id == item.RefuelItemId).Select(r => new { Driver = r.Driver, Operator = r.Operator }).FirstOrDefault();
                    //        if (doNull != null)
                    //        {
                    //            item.Driver = doNull.Driver;
                    //            item.Operator = doNull.Operator;
                    //        }
                    //    }                       
                    //}
                    if (inv.Items.Any(it => it.DriverId == null || it.Operator == null))
                    {
                        foreach (var item in inv.Items.Where(it => it.Operator == null || it.DriverId == null))
                        {
                            var mi = db.RefuelItems.FirstOrDefault(r => r.FlightId == inv.FlightId &&
                                r.TruckId == item.TruckId &&
                                r.StartNumber == item.StartNumber &&
                                r.EndNumber == item.EndNumber &&
                                r.Gallon == item.Gallon &&
                                r.Status == REFUEL_ITEM_STATUS.DONE);
                            if (mi != null)
                            {
                                item.RefuelItemId = mi.Id;
                                item.DriverId = mi.DriverId;
                                item.OperatorId = mi.OperatorId;
                                
                            }
                        }
                        
                    }
                    var exportResponse = Export(inv);
                    Logger.AppendLog("Export", "invoice id: " + id.ToString() + " done", "omega");
                    if (exportResponse.success)
                    {
                        Logger.AppendLog("Export", "invoice id: " + id.ToString() + " success", "omega");
                        inv.Exported_OMEGA = true;
                        inv.Exported_OMEGA_Date = DateTime.Now;
                        var ids = inv.Items.Select(x => x.RefuelItemId ).ToArray();
                        var guids = inv.Items.Select(x => x.RefuelUniqueId).ToArray();
                        if (db.SaveChanges() > 0)
                        {
                            var refuelItems = db.RefuelItems.Where(r => ids.Contains(r.Id) || guids.Contains(r.UniqueId)).ToList();
                            refuelItems.ForEach(r =>
                            {
                                var invItem = inv.Items.FirstOrDefault(it => it.RefuelItemId == r.Id);
                                r.Exported = true; r.InvoiceNumber = inv.InvoiceNumber; r.Printed = true;
                                if (r.Status != REFUEL_ITEM_STATUS.DONE && invItem!=null)
                                {
                                    //reverse update from invoice to refuel data
                                    r.Status = REFUEL_ITEM_STATUS.DONE;
                                    r.StartTime = invItem.StartTime;
                                    r.EndTime = invItem.EndTime;
                                    r.StartNumber = invItem.StartNumber;
                                    r.EndNumber = invItem.EndNumber;
                                    r.ManualTemperature = invItem.Temperature;
                                    r.Density = invItem.Density;
                                    r.Gallon = r.Amount = invItem.Gallon;
                                    r.Volume = invItem.Volume;
                                    r.Weight = invItem.Weight;
                                    r.TechLog = inv.TechLog;

                                }
                                r.DateUpdated = DateTime.Now;
                            });

                            //scan for null refuelItemId 
                            var nullRefuels = inv.Items.Where(x => x.RefuelItemId == null).ToList();
                            if (nullRefuels.Count > 0)
                            {
                                foreach (var item in nullRefuels)
                                {
                                    var refItem = db.RefuelItems.FirstOrDefault(r => r.FlightId == inv.FlightId 
                                    && r.TruckId == item.TruckId 
                                   
                                    && r.StartNumber == item.StartNumber );
                                    if (refItem != null)
                                    {
                                        item.RefuelItemId = refItem.Id;
                                        refItem.Exported = true;
                                        refItem.Printed = true;
                                        refItem.InvoiceNumber = inv.InvoiceNumber;
                                    }    
                                }
                            }
                            db.SaveChanges();
                        }
                        else
                        {

                            Logger.AppendLog("OMEGA", "Save changes failed", "omega");

                        }

                        //Logger.AppendLog("Export", "result code: " + exportResponse.code, "omega");
                    }

                    else
                        Logger.AppendLog("Export", "result failed: " + exportResponse.message, "omega");
                    return exportResponse;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "omega");
                    return new ExportResultModel { api = "OMEGA", code = "-1", message = ex.Message };
                }
                finally
                {
                    running = false;
                }
            }
        }
        public static ExportResultModel Export(Invoice inv)
        {
            try
            {
                if (string.IsNullOrEmpty(inv.InvoiceNumber))
                    return new ExportResultModel { api = "OMEGA", code = "0", message = "Empty invoice number", success = true };
                var token = Login();
                if (!string.IsNullOrEmpty(token))
                {
                    if (inv.InvoiceType == INVOICE_TYPE.RETURN)
                    {
                        Logger.AppendLog("Extract", "Export extract id:" + inv.InvoiceNumber, "omega");
                        var postItems = CreateExtractItems(inv);
                        return ExportExtract(postItems, token);
                    }
                    else
                    {
                        var postItems = CreatePostItems(inv);
                        if (postItems != null)
                            return ExportInventory(postItems, token);
                        else
                            return new ExportResultModel { api = "OMEGA", code = "-2", message = "create export item error" };
                    }
                }
                else
                    return new ExportResultModel { api = "OMEGA", code = "-1", message = "login failed" };
            }
            catch (Exception ex)
            {
                Logger.AppendLog("Export(Invoice inv)", ex.Message, "omega");
                return new ExportResultModel { api = "OMEGA", code = "-2", message = ex.Message };
            }
        }

        private static ExportResultModel ExportExtract(List<ExtractExportModel> postItems, string token)
        {
            if (token != null)
            {


                try
                {


                    var url = API_BASE_URL + "Skypec.asmx/PopData";

                    var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpRequest.Method = "POST";

                    httpRequest.ContentType = "application/json";
                    httpRequest.Headers["Authorization"] = token;

                    var jsonData = new { Items = postItems };
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.DateFormatString = EXPORT_DATE_FORMAT;
                    var data = JsonConvert.SerializeObject(jsonData, jsonSettings);

                    Logger.AppendLog("ExportExtract", data, "omega-data");

                    using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                    {
                        streamWriter.Write(data);

                    }

                    using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {

                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                var result = streamReader.ReadToEnd();
                                try
                                {
                                    var resultModel = JsonConvert.DeserializeObject<PostResultModel>(result);

                                    if (resultModel == null || resultModel.Done != 1)
                                    {
                                        return new ExportResultModel { api = "OMEGA", message = resultModel.Message, success = false };
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return new ExportResultModel { api = "OMEGA", message = ex.Message, success = false };
                                }


                            }
                        }
                    }

                }
                catch (Exception ex)
                {


                    if (ex is WebException)
                    {
                        if ((ex as WebException).Response != null)
                        {
                            var stream = (ex as WebException).Response.GetResponseStream();
                            string response = new StreamReader(stream).ReadToEnd();
                            var resultModel = JsonConvert.DeserializeObject<PostResultModel>(response);
                            if (resultModel != null && resultModel.Message.StartsWith("400-"))
                            {
                                return new ExportResultModel { api = "OMEGA", message = "duplicate", success = true };
                            }
                            return new ExportResultModel { api = "OMEGA", message = response, success = true };
                        }
                    }
                    return new ExportResultModel { api = "OMEGA", message = ex.Message, success = false };
                }
                finally
                {

                }

            }


            return new ExportResultModel { api = "OMEGA", message = "Hut hoan tra", success = true };
        }

        private static List<InventoryExportModel> CreatePostItems(Invoice inv)
        {
            try
            {
                DateTime startTime = inv.Items.Min(it => it.StartTime);
                DateTime endTime = inv.Items.Max(it => it.EndTime);
                var qcNo = inv.Items.Select(item => item.QCNo).FirstOrDefault();
                var list = inv.Items.Select(item => new InventoryExportModel
                {
                    Id = inv.Id,
                    KeyId = inv.FlightId.ToString() + inv.Id.ToString(),
                    VoucherDate = inv.Date?.ToString(EXPORT_DATE_FORMAT),
                    VoucherNo = inv.InvoiceNumber,
                    FormNo = string.IsNullOrEmpty(inv.FormNo) ? (inv.Flight.Airport.Code + "-" + inv.SignNo) : inv.FormNo,
                    Sign = inv.SignNo,
                    UserId = inv.UserCreatedId.ToString(),
                    FlightNumber = inv.Flight.Code,
                    AircraftNumber = (inv.AircraftType ?? inv.Flight.AircraftType) + "-" + (inv.AircraftCode ?? inv.Flight.AircraftCode),
                    ObjectId = inv.CustomerCode,
                    CurrencyId = inv.Currency.ToString(),
                    UnitId = inv.Unit == UNIT.GALLON ? "Gal" : "Kg",
                    Date01 = inv.Flight.RefuelScheduledTime.Value.ToString(EXPORT_DATE_FORMAT),
                    PumpLocation = string.IsNullOrEmpty(inv.Flight.Parking) ? "N/A" : inv.Flight.Parking,
                    TestResult = qcNo,
                    FuelTruckId = item.TruckNo,
                    GallonQuantity = item.Gallon,

                    Temperature = item.Temperature,
                    Density = item.Density,
                    BeginDate01 = startTime.ToString(EXPORT_DATE_FORMAT),
                    EndDate01 = endTime.ToString(EXPORT_DATE_FORMAT),
                    BeforceNum = item.StartNumber,
                    AfterNum = item.EndNumber,
                    AircraftKg = inv.TechLog.HasValue?  inv.TechLog.ToString(): ( string.IsNullOrEmpty(item.WeightNote) ? "0" : item.WeightNote),
                    DriverName = item.Driver.FullName,
                    PumpMan = item.Operator.FullName,
                    VATGroupID = string.Format("{0:0}", inv.TaxRate * 100),
                    TotalUnitPrice = inv.InvoiceType == INVOICE_TYPE.INVOICE ? (decimal)inv.Price : 0,
                    TaxRate = inv.InvoiceType == INVOICE_TYPE.INVOICE ? inv.TaxRate : 0,
                    Unit = inv.Unit,
                    EnvironmentPrice = inv.GreenTax??0,
                    EnvironmentAmount = (decimal)inv.GreenTaxAmount,
                    //TotalOriginalAmount = item.Price * (r.Unit == UNIT.GALLON ? item.Amount : item.Weight),
                    //TotalVATOriginalAmount = item.TaxRate * item.Price * (r.Unit == UNIT.GALLON ? item.Amount : item.Weight),

                    TotalActualQuantity = inv.Volume,
                    TotalGallonQuantity = inv.Gallon,
                    TotalKg = inv.Weight,
                    ActualQuantity = item.Volume,
                    Kg = item.Weight,
                    ExchangeRate = (decimal)inv.ExchangeRate,
                    Receipt = inv.BillNo


                }).ToList();

                foreach (var item in list)
                {
                    item.FromAirportId = inv.IsFHS ? inv.RefuelCompany.ToString().Substring(0, 3) : inv.Flight.Airport.Code;
                    item.ToAirportId = inv.RouteName ?? inv.Flight.RouteName;
                    var routes = Regex.Split( item.ToAirportId.Trim(), @"[\t\s\-=\+_]+");
                    if (routes.Length > 1)
                        item.ToAirportId = routes[1];
                    else item.ToAirportId = routes[0];
                }
                return list;
            }
            catch (Exception ex)
            {
                
                Logger.AppendLog("CreatePostItems(Invoice inv)", ex.Message, "omega");
                Logger.LogException(ex, "omega");
                return null;
            }
        }

        private static List<ExtractExportModel> CreateExtractItems(Invoice inv)
        {
            var list = inv.Items.Select(item => new ExtractExportModel
            {
                Id = inv.Id,
                KeyId = inv.FlightId.ToString() + inv.Id.ToString(),
                VoucherDate = item.EndTime.Date.ToString(EXPORT_DATE_FORMAT),
                VoucherNo = item.Invoice.InvoiceNumber,
                UserId = inv.UserCreatedId.ToString(),
                FlightNumber = inv.Flight.Code,
                AircraftNumber = inv.Flight.AircraftCode,
                ObjectId = inv.CustomerCode,
                AircraftType = inv.Flight.AircraftType,
                Flight = inv.Flight.RouteName.Trim(),


                FuelTruckId = item.TruckNo,
                GallonQuantity = item.Gallon,
                ActualQuantity = item.Volume,
                Kg = item.Weight,
                Temperature = item.Temperature,
                Density = item.Density,
                BeginDate01 = item.StartTime.ToString(EXPORT_DATE_FORMAT),
                EndDate01 = item.EndTime.ToString(EXPORT_DATE_FORMAT),


                AirportId = inv.Flight.Airport.Code,
                IsKind = 2,
                Reason = "",
                RequirementMan = "",
                Title = "",

            }).ToList();
            return list;
        }
    }
}
