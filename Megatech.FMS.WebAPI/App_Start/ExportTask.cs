using EntityFramework.DynamicFilters;
using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Hosting;
using Z.EntityFramework.Plus;

namespace Megatech.FMS.WebAPI.App_Start
{
    public class ExportTask
    {
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
        private static DateTime lastRequestDate;
        private static bool processing = false;
        private static string EXPORT_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private static decimal GALLON_TO_LITTER = 3.7854M;

        private static string API_BASE_URL = ConfigurationManager.AppSettings["EXPORT_BASE_URL"];
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
                //Logger.AppendLog("EXPORT", "Login error : " + ex.Message, "export");
            }
            // Logger.AppendLog("EXPORT", "End Login ", "export");
            return token;
        }



        private static bool Export(List<PostModel> model, string token)
        {
            var ret = true;


#if DEBUG
            TEST_EXPORT = true;
#endif
            if (TEST_EXPORT)
                return true;

            if (token != null)
            {


                try
                {
                    Logger.AppendLog("EXPORT", "Start export ", "export");


                    var url = API_BASE_URL + "Skypec.asmx/PushData";

                    var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpRequest.Method = "POST";

                    httpRequest.ContentType = "application/json";
                    httpRequest.Headers["Authorization"] = token;

                    var jsonData = new { Items = model };
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.DateFormatString = EXPORT_DATE_FORMAT;
                    var data = JsonConvert.SerializeObject(jsonData, jsonSettings);
                    Logger.AppendLog("EXPORT", data, "export-data");

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
                                        Logger.AppendLog("EXPORT", "Failed " + data, "export");

                                        ret = false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.AppendLog("EXPORT", "Failed " + data, "export");
                                    ret = false;
                                }

                                Logger.AppendLog("EXPORT", "Status OK ", "export");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    ret = false;

                    Logger.AppendLog("EXPORT", "Error :" + ex.Message, "export");
                    if (ex is WebException)
                    {
                        if ((ex as WebException).Response != null)
                        {
                            var stream = (ex as WebException).Response.GetResponseStream();
                            string response = new StreamReader(stream).ReadToEnd();
                            var resultModel = JsonConvert.DeserializeObject<PostResultModel>(response);
                            if (resultModel != null && resultModel.Message.StartsWith("400-"))
                            {
                                ret = true;
                                Logger.AppendLog("EXPORT", "Duplicate Error:" + response, "export");
                            }
                            else
                                Logger.AppendLog("EXPORT", "Error Response :" + response, "export");
                        }
                    }

                }
                finally
                {

                }

            }
            else ret = false;

            return ret;
        }

        private static bool ExportExtract(List<PostExtractModel> model, string token)
        {
            var ret = true;


#if DEBUG
            TEST_EXPORT = true;
#endif
            if (TEST_EXPORT)
                return true;


            if (token != null)
            {


                try
                {
                    Logger.AppendLog("EXTRACT", "Start export ", "extract");


                    var url = API_BASE_URL + "Skypec.asmx/PopData";

                    var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpRequest.Method = "POST";

                    httpRequest.ContentType = "application/json";
                    httpRequest.Headers["Authorization"] = token;

                    var jsonData = new { Items = model };
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.DateFormatString = EXPORT_DATE_FORMAT;
                    var data = JsonConvert.SerializeObject(jsonData, jsonSettings);
                    Logger.AppendLog("EXTRACT", data, "export-data");

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
                                        Logger.AppendLog("EXTRACT", "Failed " + data, "extract");

                                        ret = false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.AppendLog("EXTRACT", "Failed " + data, "extract");
                                    ret = false;
                                }

                                Logger.AppendLog("EXTRACT", "Status OK ", "extract");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {

                    Logger.AppendLog("EXTRACT", "Error :" + ex.Message, "extract");
                    if (ex is WebException)
                    {
                        if ((ex as WebException).Response != null)
                        {
                            var stream = (ex as WebException).Response.GetResponseStream();
                            Logger.AppendLog("EXTRACT", "Error Response :" + new StreamReader(stream).ReadToEnd(), "extract");
                        }
                    }
                    ret = false;
                }
                finally
                {

                }

            }
            else ret = false;

            return ret;
        }

        //running the export task every 1 hour
        private static DateTime lastRun = DateTime.MinValue;

        private static bool TEST_EXPORT = ConfigurationManager.AppSettings["TEST_EXPORT"] != null && ConfigurationManager.AppSettings["TEST_EXPORT"].Equals("1");
        public static void Execute()
        {
#if DEBUG
            TEST_EXPORT = true;
#endif
            if (TEST_EXPORT)
                return;
            if (DateTime.Today > new DateTime(2021, 07, 9))
            {
                var diff = DateTime.Now - lastRun;
                if (diff.TotalMinutes > 15)
                {
                    lastRun = DateTime.Now;
                    Execute(null, REQUIRED_FIELD.INVOICE_NUMBER);
                    Extract(null);
                }
            }
        }
        public static void Execute(DateTime? date, REQUIRED_FIELD required = REQUIRED_FIELD.INVOICE_NUMBER)
        {



            HostingEnvironment.QueueBackgroundWorkItem(cToken =>
            {

                DoExport(0, date, required: required);

            });



        }

        public static void Execute(int flightId = 0)
        {
            Logger.AppendLog("EXPORT", "Export FlightID :" + flightId.ToString(), "export");

            HostingEnvironment.QueueBackgroundWorkItem(cToken =>
            {

                DoExport(flightId);

            });


            Logger.AppendLog("EXPORT", "END FlightID :" + flightId.ToString(), "export");
        }

        public static void Extract(DateTime? date)
        {



            HostingEnvironment.QueueBackgroundWorkItem(cToken =>
            {

                DoExportExtract(0, date);

            });



        }

        public static void Extract(int flightId = 0)
        {
            Logger.AppendLog("EXPORT", "Export FlightID :" + flightId.ToString(), "export");

            HostingEnvironment.QueueBackgroundWorkItem(cToken =>
            {

                DoExportExtract(flightId);

            });


            Logger.AppendLog("EXPORT", "END FlightID :" + flightId.ToString(), "export");
        }
        private static bool exporting = false;
        private static bool extracting = false;

        [Flags]
        public enum REQUIRED_FIELD
        {
            NONE = 0,
            INVOICE_NUMBER = 1,
            INVOICE_FORM = 2

        }

        private static DateTime MIN_DATE = new DateTime(2021, 8, 1);

        private static void DoExport(int flightId = 0, DateTime? date = null, REFUEL_ITEM_TYPE itemType = REFUEL_ITEM_TYPE.REFUEL, REQUIRED_FIELD required = REQUIRED_FIELD.INVOICE_NUMBER)
        {

            try
            {
                processing = true;
                Logger.AppendLog("EXPORT", string.Format("========================= Start {0:yyyy-MM-dd} ========  ", date), "export");


                if (exporting) return;

                exporting = true;


                string token = Login();
                if (!string.IsNullOrEmpty(token))
                {
                    using (DataContext db = new DataContext())
                    {
                        db.DisableFilter("IsNotDeleted");
                        var rQuery = db.RefuelItems.Where(r => (flightId == 0 || r.FlightId == flightId)
                        && r.Status == REFUEL_ITEM_STATUS.DONE
                        && r.RefuelItemType == itemType
                        && !(r.Exported ?? false)
                        && r.Density > 0
                        && !r.IsDeleted
                        //&& r.Printed
                        && r.Driver != null
                        && r.Operator != null
                        && r.Flight != null
                        && r.Amount > 0
                        && r.StartTime <= r.EndTime
                        //&& (r.Printed || r.Flight.Status == FlightStatus.REFUELED)
                        );

                        //if (required.HasFlag(REQUIRED_FIELD.INVOICE_NUMBER))
                        rQuery = rQuery.Where(r => !string.IsNullOrEmpty(r.InvoiceNumber));

                        //if (required.HasFlag(REQUIRED_FIELD.INVOICE_FORM))
                        rQuery = rQuery.Where(r => (r.InvoiceFormId ?? 0) > 0);

                        if (date == null || date < MIN_DATE) date = DateTime.Today.AddDays(-3);

                        var threshold = DateTime.Now.AddMinutes(flightId > 0 ? 0 : -30);
                        if (date != null)
                            rQuery = rQuery.Where(r => DbFunctions.TruncateTime(r.EndTime.Value) >= date.Value);


                        var ids = rQuery.Select(r => r.Id).ToArray();
                        var refuels = db.RefuelItems
                            .Include(r => r.InvoiceForm)
                            .Include(r => r.Flight.Airline).Include(r => r.Flight.Airport).Include(r => r.Driver).Include(r => r.Operator).Include(r => r.Truck)
                            .Where(r => ids.Contains(r.Id))
                            .ToList();

                        //Logger.AppendLog("EXPORT", rQuery.ToString(), "export-SQL");
                        //Logger.AppendLog("EXPORT", rQuery.Count().ToString(), "export-SQL");

                        Logger.AppendLog("EXPORT", string.Format("Refuel item count {0}", refuels.Count), "export");


                        var postItems = refuels.OrderBy(r => r.EndTime).GroupBy(r => new { r.InvoiceNumber, r.FlightId }).Where(model => model.Max(r => r.DateUpdated) < threshold)
                        .Select(g => new
                        {
                            Airline = g.LastOrDefault().Flight.Airline,
                            g.Key.FlightId,
                            g.Key.InvoiceNumber,
                            AircraftNumber = g.LastOrDefault(r => r.Flight != null) == null ? "N/A" : (g.LastOrDefault(r => r.Flight != null).Flight.AircraftType + "-" + g.LastOrDefault(r => r.Flight != null).Flight.AircraftCode),
                            CountInv = g.Count(),
                            InvoiceForm = g.LastOrDefault(r => r.InvoiceForm != null) != null ? g.LastOrDefault(r => r.InvoiceForm != null).InvoiceForm : null,
                            FormNo = g.LastOrDefault(r => r.InvoiceForm != null) == null ? "N/A" : g.LastOrDefault(r => r.InvoiceForm != null).InvoiceForm.FormNo,
                            Sign = g.LastOrDefault(r => r.InvoiceForm != null) == null ? "N/A" : g.LastOrDefault(r => r.InvoiceForm != null).InvoiceForm.Sign,
                            StartTime = g.Min(r => r.StartTime),
                            EndTime = g.Max(r => r.EndTime),
                            TotalAmount = g.Sum(r => r.Amount),// - (decimal)Math.Round(Math.Round((double)(r.ReturnAmount??0 / (r.Density == 0 ? 1 : r.Density)), 0, MidpointRounding.AwayFromZero) / (double)GALLON_TO_LITTER,0, MidpointRounding.AwayFromZero)),
                            //Gallon = g.Sum(r => r.Amount),
                            //Volume = g.Sum(r => r.Volume - (decimal)Math.Round((double)(r.ReturnAmount / (r.Density == 0 ? 1 : r.Density)),0, MidpointRounding.AwayFromZero)),
                            ReturnKg = g.Sum(r => r.ReturnUnit == RETURN_UNIT.GALLON ? (decimal)Math.Round((double)r.Density * Math.Round((double)(r.ReturnAmount * GALLON_TO_LITTER), 0, MidpointRounding.AwayFromZero), 0, MidpointRounding.AwayFromZero)
                                : r.ReturnAmount ?? 0),
                            ReturnVolume = g.Sum(r => r.ReturnUnit == RETURN_UNIT.GALLON ? (decimal)Math.Round((double)(r.ReturnAmount * GALLON_TO_LITTER), 0, MidpointRounding.AwayFromZero)
                                : (decimal)Math.Round((double)(r.ReturnAmount / (r.Density == 0 ? 1 : r.Density)), 0, MidpointRounding.AwayFromZero)),
                            Volume = g.Sum(r => r.Volume),
                            //Weight = g.Sum(r => r.Weight - r.ReturnAmount ?? 0),// Math.Round(g.Last().Density * g.Sum(r => r.Volume), 0, MidpointRounding.AwayFromZero),
                            Price = g.LastOrDefault(gl => gl.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && gl.Flight.Airline.AirlineType == 0) != null ? 0 : g.Max(r => r.Price),

                            Density = g.Last().Density,
                            Temperature = g.Last().ManualTemperature,

                            TaxRate = g.Max(r => r.TaxRate),
                            FlightCode = g.Last().Flight.Code,
                            FromAirport = g.Last().Flight.Airport.Code,
                            RouteName = g.Last().Flight.RouteName,
                            Unit = g.Last().Unit,
                            QCNo = g.LastOrDefault(gl => !string.IsNullOrEmpty(gl.QCNo)) != null ? g.LastOrDefault(gl => !string.IsNullOrEmpty(gl.QCNo)).QCNo : "N/A",
                            AircraftKg = g.LastOrDefault(gl => !string.IsNullOrEmpty(gl.WeightNote)) != null ? g.LastOrDefault(gl => !string.IsNullOrEmpty(gl.WeightNote)).WeightNote : "0",
                            UserId = g.LastOrDefault(gl => gl.UserUpdated != null) != null ? g.LastOrDefault(gl => gl.UserUpdated != null).UserUpdated.UserName : null
                        }).OrderBy(g => g.FlightId).ToList();

                        Logger.AppendLog("EXPORT", string.Format("Invoice item count {0}", postItems.Count), "export");

                        var postList = new List<PostModel>();
                        var lastFlight = 0;
                        var flightCount = 1;
                        foreach (var item in postItems.Where(g => g.TotalAmount > 0))
                        {
                            try
                            {
                                Logger.AppendLog("EXPORT", string.Format("Flight Id: {0} Code: {1} Invoice#:{2} ", item.FlightId, item.FlightCode, item.InvoiceNumber), "export");

                                if (lastFlight == item.FlightId)
                                    flightCount++;
                                else
                                {
                                    lastFlight = item.FlightId;
                                    flightCount = 1;
                                }

                                var keyID = item.FlightId.ToString() + flightCount.ToString();

                                decimal totalGallon = 0;
                                decimal totalVolume = 0;
                                decimal totalWeight = 0;
                                var items = refuels.Where(r => r.FlightId == item.FlightId && r.InvoiceNumber == item.InvoiceNumber)
                                .Select(r => new PostModel
                                {
                                    Id = r.Id,
                                    KeyId = keyID,
                                    VoucherDate = item.EndTime.Value.Date.ToString(EXPORT_DATE_FORMAT),
                                    VoucherNo = item.InvoiceNumber,
                                    FormNo = item.FormNo,
                                    Sign = item.Sign,
                                    UserId = item.UserId ?? r.Operator.UserName,
                                    FlightNumber = r.Flight.Code,
                                    AircraftNumber = item.AircraftNumber,
                                    ObjectId = r.Flight.Airline.Code,
                                    CurrencyId = r.Currency.ToString(),
                                    UnitId = r.Unit == UNIT.GALLON ? "Gal" : "Kg",
                                    Date01 = r.Flight.RefuelScheduledTime.Value.ToString(EXPORT_DATE_FORMAT),
                                    PumpLocation = string.IsNullOrEmpty(r.Flight.Parking) ? "N/A" : r.Flight.Parking,
                                    TestResult = item.QCNo,
                                    FuelTruckId = r.Truck.Code,
                                    GallonQuantity = r.Amount - (r.ReturnUnit == RETURN_UNIT.GALLON ? (decimal)r.ReturnAmount : (decimal)Math.Round(Math.Round((double)(r.ReturnAmount / (r.Density == 0 ? 1 : r.Density)), 0, MidpointRounding.AwayFromZero) / (double)GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero)),

                                    Temperature = r.Temperature,
                                    Density = r.Density,
                                    BeginDate01 = item.StartTime.ToString(EXPORT_DATE_FORMAT),
                                    EndDate01 = item.EndTime.Value.ToString(EXPORT_DATE_FORMAT),
                                    BeforceNum = r.StartNumber,
                                    AfterNum = r.EndNumber,
                                    AircraftKg = item.AircraftKg,
                                    DriverName = r.Driver.FullName,
                                    PumpMan = r.Operator.FullName,
                                    VATGroupID = string.Format("0", item.TaxRate * 100),
                                    TotalUnitPrice = item.Price,
                                    TaxRate = item.TaxRate,
                                    Unit = item.Unit,
                                    //TotalOriginalAmount = item.Price * (r.Unit == UNIT.GALLON ? item.Amount : item.Weight),
                                    //TotalVATOriginalAmount = item.TaxRate * item.Price * (r.Unit == UNIT.GALLON ? item.Amount : item.Weight),
                                    FromAirportId = item.FromAirport,
                                    ToAirportId = r.Flight.RouteName,


                                }).ToList();

                                var updatedList = items.Select(r => r.Id).ToArray();

                                foreach (var postModel in items)
                                {
                                    postModel.ActualQuantity = Math.Round(postModel.GallonQuantity * GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero);
                                    postModel.Kg = Math.Round(postModel.ActualQuantity * postModel.Density, 0, MidpointRounding.AwayFromZero);
                                    postModel.BeforceNum = postModel.AfterNum - postModel.GallonQuantity;

                                    totalGallon += postModel.GallonQuantity;
                                    totalVolume += postModel.ActualQuantity;
                                    totalWeight += postModel.Kg;

                                    var routes = postModel.ToAirportId.Split(new char[] { '-', '_' });
                                    if (routes.Length > 1)
                                    {
                                        //postModel.FromAirportId = routes[0].Trim();
                                        postModel.ToAirportId = routes[1].Trim();
                                    }
                                    else
                                        postModel.ToAirportId = postModel.ToAirportId.Trim();

                                    if (postModel.ToAirportId.Length > 3)
                                        postModel.ToAirportId = postModel.ToAirportId.Substring(0, 3);

                                    if (string.IsNullOrEmpty(postModel.ToAirportId))
                                        postModel.ToAirportId = "N/A";

                                }
                                /* var calculatedVolume = Math.Round(totalGallon * GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero);
                                 var calculatedWeight = Math.Round(calculatedVolume * item.Density, 0, MidpointRounding.AwayFromZero);
                                 if (totalVolume != calculatedVolume)
                                 {
                                     var biggestItem = items.OrderByDescending(it => it.ActualQuantity).FirstOrDefault();
                                     biggestItem.ActualQuantity = biggestItem.ActualQuantity + calculatedVolume - totalVolume  ;

                                 }
                                 if (totalWeight != calculatedWeight)
                                 {
                                     var biggestItem = items.OrderByDescending(it => it.Kg).FirstOrDefault();
                                     biggestItem.Kg = biggestItem.Kg + calculatedWeight - totalWeight  ;

                                 }

                                 items.ForEach(it => { it.TotalGallonQuantity = totalGallon; it.TotalActualQuantity = calculatedVolume; it.TotalKg = calculatedWeight; });
                                 */
                                items.ForEach(it => { it.TotalGallonQuantity = totalGallon; it.TotalActualQuantity = totalVolume; it.TotalKg = totalWeight; });
                                if (Export(items, token))
                                {
                                    //db.RefuelItems.Where(r => updatedList.Contains(r.Id)).Update(r => new RefuelItem { Exported = true, DateUpdated = DateTime.Now });
                                    foreach (var postedItem in items)
                                    {
                                        db.RefuelItems.Where(r => r.Id == postedItem.Id).Update(r => new RefuelItem
                                        {
                                            Exported = true,
                                            DateUpdated = DateTime.Now,
                                            /*Amount = postedItem.GallonQuantity,
                                            Gallon = postedItem.GallonQuantity,
                                            Volume = postedItem.ActualQuantity,
                                            Weight = postedItem.Kg,
                                            ManualTemperature = postedItem.Temperature,
                                            Density = postedItem.Density*/
                                        });

                                    }
                                    db.SaveChanges();
                                    Logger.AppendLog("EXPORT", string.Format("End Item Flight Id: {0} Code: {1} Invoice#:{2} ", item.FlightId, item.FlightCode, item.InvoiceNumber), "export");
                                }
                                else
                                {
                                    Logger.AppendLog("EXPORT", "Data Error: " + JsonConvert.SerializeObject(items), "export");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.AppendLog("EXPORT", string.Format("Error Item Flight Id {0} Code {1} Invoice# {2} : {3}", item.FlightId, item.FlightCode, item.InvoiceNumber, ex.Message), "export");
                                Logger.AppendLog("EXPORT", ex.StackTrace, "export");
                            }

                        }

                    }

                }
                else
                    Logger.AppendLog("EXPORT", "Login failed", "export");


                Logger.AppendLog("EXPORT", string.Format("========================= END  {0:yyyy-MM-dd} ========  ", date), "export");


                exporting = false;
            }
            catch (Exception ex)
            {
                Logger.AppendLog("EXPORT", "Error " + ex.Message, "export");
                if (ex.InnerException != null)
                    Logger.AppendLog("EXPORT", "Error " + ex.InnerException.Message, "export");
                exporting = false;
            }
        }



        private static void DoExportExtract(int flightId = 0, DateTime? date = null)
        {
#if DEBUG
            return;
#endif
            try
            {
                Logger.AppendLog("EXTRACT", string.Format("========================= Start {0:yyyy-MM-dd} ========  ", date), "extract");


                if (extracting) return;

                extracting = true;


                string token = Login();
                if (!string.IsNullOrEmpty(token))
                {
                    using (DataContext db = new DataContext())
                    {
                        var rQuery = db.RefuelItems.Where(r => (flightId == 0 || r.FlightId == flightId)
                        && r.Status == REFUEL_ITEM_STATUS.DONE && (r.ReturnAmount > 0 || r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT));

                        if (date == null || date < MIN_DATE) date = DateTime.Today.AddDays(-5);

                        var threshold = DateTime.Now.AddMinutes(-30);
                        if (date != null)
                            rQuery = rQuery.Where(r => DbFunctions.TruncateTime(r.EndTime.Value) >= date.Value && r.EndTime < threshold);


                        var query = rQuery.Where(r => !(bool)r.ExportExtract && r.Density > 0);

                        var refuels = query.Include(r => r.Flight.Airline).Include(r => r.Flight.Airport).Include(r => r.Driver).Include(r => r.Operator).Include(r => r.Truck).ToList();


                        Logger.AppendLog("EXTRACT", string.Format("Refuel item count {0}", refuels.Count), "extract");

                        var postItems = refuels.OrderBy(r => r.EndTime).GroupBy(r => new { InvoiceNumber = r.ReturnInvoiceNumber, r.FlightId, r.RefuelItemType })
                        .Select(g => new
                        {
                            g.Key.FlightId,
                            g.Key.InvoiceNumber,
                            g.Key.RefuelItemType,
                            CountInv = g.Count(),
                            StartTime = g.Min(r => r.StartTime),
                            EndTime = g.Max(r => r.EndTime),

                            Density = g.Last().Density,
                            Weight = Math.Round(g.Last().Density * g.Sum(r => r.Volume ?? 0), 0),
                            TaxRate = g.Max(r => r.TaxRate),
                            FlightCode = g.Last().Flight.Code,
                            AircraftType = g.Last().Flight.AircraftType,
                            FromAirport = g.Last().Flight.Airport.Code,
                            RouteName = g.Last().Flight.RouteName,
                            QCNo = g.LastOrDefault(gl => !string.IsNullOrEmpty(gl.QCNo)) != null ? g.LastOrDefault(gl => !string.IsNullOrEmpty(gl.QCNo)).QCNo : "N/A",

                            UserId = g.LastOrDefault(gl => gl.UserUpdated != null) != null ? g.LastOrDefault(gl => gl.UserUpdated != null).UserUpdated.UserName : null
                        }).OrderBy(g => g.FlightId).ToList();



                        var postList = new List<PostExtractModel>();
                        var lastFlight = 0;
                        var flightCount = 1;
                        foreach (var item in postItems)
                        {
                            try
                            {
                                Logger.AppendLog("EXTRACT", string.Format("Flight Id: {0} Code: {1} Invoice#:{2} ", item.FlightId, item.FlightCode, item.InvoiceNumber), "extract");



                                if (lastFlight == item.FlightId)
                                    flightCount++;
                                else
                                {
                                    lastFlight = item.FlightId;
                                    flightCount = 1;
                                }

                                var keyID = item.FlightId.ToString() + flightCount.ToString();


                                var items = refuels.Where(r => r.FlightId == item.FlightId && r.ReturnInvoiceNumber == item.InvoiceNumber)
                                .Select(r => new PostExtractModel
                                {
                                    KeyId = keyID,
                                    VoucherDate = item.EndTime.Value.Date.ToString(EXPORT_DATE_FORMAT),
                                    VoucherNo = string.IsNullOrEmpty(item.InvoiceNumber) ? keyID : item.InvoiceNumber,
                                    UserId = item.UserId ?? r.Operator.UserName,
                                    FlightNumber = r.Flight.Code,
                                    AircraftNumber = string.IsNullOrEmpty(r.Flight.AircraftCode) ? "N/A" : r.Flight.AircraftCode,
                                    AircraftType = string.IsNullOrEmpty(r.Flight.AircraftType) ? "N/A" : r.Flight.AircraftType,
                                    Flight = item.RouteName.Trim(),
                                    ObjectId = r.Flight.Airline.Code,

                                    FuelTruckId = r.Truck.Code,
                                    GallonQuantity = r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT ? r.Amount : (decimal)Math.Round((decimal)Math.Round((double)(r.ReturnAmount / r.Density)) / GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero),
                                    ActualQuantity = r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT ? r.Volume ?? 0 : (decimal)Math.Round((double)(r.ReturnAmount / r.Density), 0, MidpointRounding.AwayFromZero),
                                    Kg = r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT ? (decimal)r.Weight : (decimal)r.ReturnAmount,
                                    Temperature = r.ManualTemperature,
                                    Density = item.Density,
                                    BeginDate01 = item.StartTime.ToString(EXPORT_DATE_FORMAT),
                                    EndDate01 = item.EndTime.Value.ToString(EXPORT_DATE_FORMAT),


                                    AirportId = item.FromAirport,
                                    IsKind = r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT ? 1 : 2,
                                    Reason = "",
                                    RequirementMan = "",
                                    Title = "",
                                    Id = r.Id


                                }).ToList();

                                var updatedList = items.Select(r => r.Id).ToArray();

                                if (ExportExtract(items, token))
                                {
                                    db.RefuelItems.Where(r => updatedList.Contains(r.Id)).Update(r => new RefuelItem { ExportExtract = true, DateUpdated = DateTime.Now });
                                    db.SaveChanges();
                                    Logger.AppendLog("EXTRACT", string.Format("End Item Flight Id: {0} Code: {1} Invoice#:{2} ", item.FlightId, item.FlightCode, item.InvoiceNumber), "extract");
                                }
                                else
                                {
                                    Logger.AppendLog("EXTRACT", "Data Error: " + JsonConvert.SerializeObject(items), "extract");
                                }

                            }
                            catch (Exception ex)
                            {
                                Logger.AppendLog("EXTRACT", string.Format("Error Item Flight Id {0} Code {1} Invoice# {2} : {3}", item.FlightId, item.FlightCode, item.InvoiceNumber, ex.Message), "extract");
                            }

                        }

                    }

                }
                else
                    Logger.AppendLog("EXTRACT", "Login failed", "extract");





                extracting = false;
            }
            catch (Exception ex)
            {
                Logger.AppendLog("EXTRACT", "Error " + ex.Message, "extract");
            }
            finally
            {
                Logger.AppendLog("EXTRACT", string.Format("========================= END  {0:yyyy-MM-dd} ========  ", date), "extract");
            }
        }


    }
}