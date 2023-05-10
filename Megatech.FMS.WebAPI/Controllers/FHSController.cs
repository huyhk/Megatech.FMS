using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data.Entity;
using Megatech.FMS.WebAPI.App_Start;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Megatech.FMS.WebAPI.Controllers
{

    public class FHSController : ApiController
    {
        [Authorize]
        [Route("api/fhs/report/{date}")]
        public IHttpActionResult GetSummary(DateTime date)
        {
            using (DataContext db = new DataContext())
            {
                var query = db.FHSImports.Where(f => DbFunctions.TruncateTime(f.DateImported) == date);
                var report = new FHSReportModel { Date = date };
                report.Total = query.Count();
                report.Failed = query.Count(f => f.ResultCode == DATA_IMPORT_RESULT.FAILED);
                report.Success = query.Count(f => f.ResultCode == DATA_IMPORT_RESULT.SUCCESS);

                report.FailedItems = query.Where(f => f.ResultCode == DATA_IMPORT_RESULT.FAILED).Select(f => f.UniqueId).ToArray();
                report.SuccessItems = query.Where(f => f.ResultCode == DATA_IMPORT_RESULT.SUCCESS).Select(f => f.UniqueId).ToArray();
                return Ok(report);
            }
        }
        [Authorize]
        [Route("api/fhs/{id}")]
        public IHttpActionResult Get(Guid id)
        {
            using (DataContext db = new DataContext())
            {
                var item = db.FHSImports.Include(f => f.Items).Where(f => f.LocalUniqueId == id).FirstOrDefault();
                var model = JsonConvert.DeserializeObject<DataImportModel>(JsonConvert.SerializeObject(item));
                return Ok(model);
            }
        }
        [Authorize]
        public IHttpActionResult Post()
        {

            try
            {
                var data = HttpContext.Current.Request.Form["RefuelData"];
                if (data == null)
                    return BadRequest();

                var json = data;// JsonConvert.SerializeObject(model);

                Logger.AppendLog("FHS", data, "fhs-data");

                var model = JsonConvert.DeserializeObject<DataImportModel>(json);

                var file = HttpContext.Current.Request.Files.Count > 0 ?
                         HttpContext.Current.Request.Files[0] : null;

                string[] allowExt = new string[] { ".jpg", ".png", ".jpeg" };
                if (file != null && file.ContentLength > 0)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (allowExt.Contains(ext))
                        model.ReceiptImage = file.FileName;

                }

                

                ModelState.Clear();
                this.Validate(model);


                if (ModelState.IsValid)
                {
                    ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

                    var userName = ClaimsPrincipal.Current.Identity.Name;
                    

                    var fs = file.InputStream;
                    System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
                    Byte[] bytes = br.ReadBytes((Int32)fs.Length);

                    using (DataContext db = new DataContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
                        /// insert raw data first
                        /// 
                        /// 
                        var dtImport = JsonConvert.DeserializeObject<FHSImport>(json);

                        if (model.Mode == DATA_MODE.INSERT)
                        {
                            Logger.AppendLog("MODEL", "model is duplicated " + model.UniqueId, "fhs");
                            if (db.FHSImports.Any(f => f.UniqueId == model.UniqueId && f.ResultCode == DATA_IMPORT_RESULT.SUCCESS))
                            {
                                var fhs = db.FHSImports.FirstOrDefault(f => f.UniqueId == model.UniqueId && f.ResultCode == DATA_IMPORT_RESULT.SUCCESS);
                                return Ok(new ImportResult { Result = DATA_IMPORT_RESULT.DUPLICATED, Message=$"Receipt Number {model.ReceiptNumber} duplicated", Link = Request.GetRequestContext().Url.Request.RequestUri + "/" + fhs.LocalUniqueId, ReturnId = fhs.LocalUniqueId });
                            }
                        }
                        else
                        {
                            if (model.UpdateId == null)
                                return Ok(new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = "UpdateId is required " });
                            var updateGuid = model.UpdateId;

                            var oldItem = db.FHSImports.FirstOrDefault(f => f.LocalUniqueId == updateGuid && f.ResultCode == DATA_IMPORT_RESULT.SUCCESS);
                            if (oldItem == null)
                                return Ok(new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = string.Format("UpdateId '{0}' not exists", model.UpdateId) });
                            //else
                            //    dtImport.UpdateId = oldItem.LocalUniqueId;
                        }

                        dtImport.DateImported = DateTime.Now;
                        dtImport.UserImportedId = user.Id;
                        dtImport.UserCreatedId = user.Id;
                        dtImport.UserUpdatedId = user.Id;

                        if (dtImport.RefuelCompany == null || dtImport.RefuelCompany == REFUEL_COMPANY.SKYPEC)
                            dtImport.RefuelCompany = userName == "NAFSC" ? REFUEL_COMPANY.NAFSC : REFUEL_COMPANY.TAPETCO;

                        

                        model.AirportId = user.AirportId;
                        model.LocalUniqueId = dtImport.LocalUniqueId;
                        if (model.RefuelCompany == null || model.RefuelCompany == REFUEL_COMPANY.SKYPEC)
                            model.RefuelCompany = userName == "NAFSC" ? REFUEL_COMPANY.NAFSC : REFUEL_COMPANY.TAPETCO;
                        var result = InsertImportData(model, user, bytes);


                        if (result.Result == DATA_IMPORT_RESULT.SUCCESS)
                        {
                            result.ReturnId = dtImport.LocalUniqueId;
                            dtImport.ImagePath = result.ImagePath;
                        }
                        if (result.ReturnId != null)
                            result.Link = Request.GetRequestContext().Url.Request.RequestUri + "/" + result.ReturnId;

                        dtImport.ResultCode = result.Result;
                        dtImport.Result = result.Message?.ToString();

                        if (dtImport.ResultCode == DATA_IMPORT_RESULT.SUCCESS)
                            db.FHSImports.Add(dtImport);
                        else if (dtImport.ResultCode == DATA_IMPORT_RESULT.FAILED)
                        {
                            var fhs = db.FHSImports.Where(f => f.UniqueId == model.UniqueId && f.ResultCode == DATA_IMPORT_RESULT.FAILED).Select(f=>f.Id).FirstOrDefault();
                            if (fhs <= 0)
                                db.FHSImports.Add(dtImport);
                            else
                            {
                                dtImport.Id = fhs;
                                //db.FHSImports.Attach(dtImport);
                                db.Entry(dtImport).State = EntityState.Modified;
                            }
                        }
                        db.SaveChanges();
                        return Ok(result);

                    }

                    //return Ok(new ImportResult { Result = DATA_IMPORT_RESULT.SUCCESS });
                }

                else
                {
                    Logger.AppendLog("MODEL", "invalid model " + model.UniqueId, "fhs");
                    var values = ModelState.Values.Select(v => v.Errors).ToList();
                    var erros = values.Select(v => v.FirstOrDefault().ErrorMessage).ToList();
                    Logger.AppendLog("VALIDATE", erros.FirstOrDefault(), "fhs");
                    return
                       Ok(new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = erros });
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "fhs");
                return Ok(new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = ex.Message });
            }
        }

        private ImportResult InsertImportData(DataImportModel model, User user, byte[] bytes, bool retry = false)
        {
            var folderPath = HostingEnvironment.MapPath("~/receipts");

            using (var db = new DataContext())
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    //check receipts

                    if (db.Receipts.Any(rc => rc.Number.Equals(model.ReceiptNumber.Trim(), StringComparison.InvariantCultureIgnoreCase)))
                        return new ImportResult { Result = DATA_IMPORT_RESULT.DUPLICATED, Message = string.Format("Receipt number '{0}' already existed", model.ReceiptNumber) };
                    
                  
                    // find airline by code
                    var airline = db.Airlines.FirstOrDefault(f => f.Code.Equals(model.AirlineCode.Trim(), StringComparison.InvariantCultureIgnoreCase));

                    //find airline by pattern, if exists, use pattern first
                    var patterns = db.Airlines.Where(a => !string.IsNullOrEmpty(a.Pattern)).ToList();

                    var pp = patterns.FirstOrDefault(p => Regex.IsMatch(model.FlightCode, p.Pattern,RegexOptions.IgnoreCase) && p.Branch == (model.RefuelCompany ==REFUEL_COMPANY.NAFSC? Branch.MB: Branch.MN)  );
                    if (pp==null)
                        pp = patterns.FirstOrDefault(p => Regex.IsMatch(model.FlightCode, p.Pattern, RegexOptions.IgnoreCase) );
                    if (pp != null)
                        airline = pp;// db.Airlines.FirstOrDefault(a => a.Id == pp.Id);

                    
                    if (airline == null)
                    {

                            return new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = string.Format("Airline code '{0}' not exists", model.AirlineCode) };
                        //airline = new Airline
                        //{
                        //    Code = model.AirlineCode,
                        //    Name = model.AirlineName, 
                        //    TaxCode = model.TaxCode,
                        //    Address = model.Address,
                        //    AirlineType = 1,

                        //    UserCreatedId = user?.Id,

                        //};
                    }
                    var flight = db.Flights
                        .Include(f => f.RefuelItems)
                        //.Include(f=>f.Airline)
                        .FirstOrDefault(f => f.Code.Equals(model.FlightCode, StringComparison.InvariantCultureIgnoreCase)
                          && f.RefuelScheduledTime == model.RefuelScheduledTime);

                    if (flight == null)
                    {
                        flight = new Flight
                        {
                            Code = model.FlightCode,
                            RouteName = model.RouteName,
                            AircraftCode = model.AircraftCode,
                            AircraftType = model.AircraftType,
                            FlightType = model.IsInternational ? FLIGHT_TYPE.OVERSEA : FLIGHT_TYPE.DOMESTIC,
                            RefuelScheduledTime = model.RefuelScheduledTime,

                            ArrivalScheduledTime = model.ArrivalTime,
                            DepartureScheduledTime = model.DepartureTime,
                            Airline = airline,
                            AirportId = model.AirportId,
                            IsOutRefuel = true,
                            CreatedLocation = FLIGHT_CREATED_LOCATION.FHS,
                            RefuelItems = new List<RefuelItem>()
                        };

                        db.Flights.Add(flight);

                    }
                    else
                        flight.IsOutRefuel = true;

                    //save flight OK, create receipt

                    var receipt = new Receipt
                    {

                        FlightCode = model.FlightCode,
                        RouteName = model.RouteName,
                        AircraftCode = model.AircraftCode,
                        AircraftType = model.AircraftType,
                        FlightType = (int)(model.IsInternational ? FLIGHT_TYPE.OVERSEA : FLIGHT_TYPE.DOMESTIC),
                        Number = model.ReceiptNumber,
                        Date = model.ReceiptDate,
                        Flight = flight,
                        CustomerCode = airline.InvoiceCode ?? airline.Code,
                        Customer = airline,
                        CustomerName = airline.Name,
                        CustomerAddress = airline.Address,
                        CustomerType = airline.AirlineType,
                        UserCreatedId = user.Id,
                        RefuelCompany = model.RefuelCompany,

                        //Image = bytes,
                        Gallon = model.TotalGallon,
                        Volume = model.TotalLiter,
                        Weight = model.TotalKg,
                        UniqueId = Guid.NewGuid(),
                        Items = new List<ReceiptItem>()

                    };

                    SaveImage(bytes, receipt.Number + ".jpg", folderPath);
                    receipt.ImagePath = receipt.Number + ".jpg";

                    db.Receipts.Add(receipt);
                   
                    var price = 0.0M;
                    var currency = CURRENCY.VND;
                    var unit = UNIT.GALLON;

                    //var exRate = 1.0M;
                    var exRate = db.ProductPrices.Where(p => p.StartDate <= model.ReceiptDate && p.Currency == CURRENCY.USD)
                        .OrderByDescending(p => p.StartDate).Select(p => p.ExchangeRate)
                        .FirstOrDefault();

                    var airport = db.Airports.FirstOrDefault(a => a.Id == user.AirportId);

                    var refuelCompany = model.RefuelCompany;
                    var depotType = receipt.IsFHS ? 4 : airport.DepotType;
                    var airlineType = db.Airlines.Where(a => a.Id == airline.Id).Select(a => a.AirlineType).FirstOrDefault() ?? 1;
                    var flightType = receipt.FlightType ?? (int)flight.FlightType;

                    var prices = (from p in db.ProductPrices.Include(p => p.Product)
                                  where p.StartDate <= receipt.Date
                                  group p by new { p.CustomerId, p.AirlineType, p.BranchId, p.DepotType, p.Unit }
                 into groups
                                  select groups.OrderByDescending(g => g.StartDate).FirstOrDefault()).ToList();

                    var pPrice = prices.OrderByDescending(p => p.StartDate)
                                                        .FirstOrDefault(p => p.AirlineType == (flightType == (int)FLIGHT_TYPE.OVERSEA || airlineType == 0 ? 1 : 0) && p.CustomerId == airline.Id);
                    if (pPrice == null && airlineType == (int)CUSTOMER_TYPE.LOCAL)
                    {
                        Logger.AppendLog("import", "local customer null price", "fhs");

                        pPrice = new ProductPrice { Currency = CURRENCY.VND };
                    }
                    //Logger.AppendLog("import", string.Format("flight type {0} depotType {1} branch {2}",flightType,depotType, airport.Branch), "fhs");
                    if (pPrice == null)
                        pPrice = prices.OrderByDescending(p => p.StartDate)
                    .FirstOrDefault(p => p.CustomerId == null &&  p.AirlineType == (flightType == (int)FLIGHT_TYPE.OVERSEA ? 1 : 0) && (p.DepotType == depotType && p.BranchId == (int)airport.Branch));

                    if (pPrice != null)
                    {
                        price = pPrice.Price;
                        currency = pPrice.Currency;
                        unit = unit = pPrice.Unit == 0 ? UNIT.GALLON : UNIT.KG;
                    }
                    else
                        Logger.AppendLog("import", "null price", "fhs");

                    var inv = new Invoice
                    {
                        FlightCode = model.FlightCode,
                        RouteName = model.RouteName,
                        AircraftCode = model.AircraftCode,
                        AircraftType = model.AircraftType,
                        FlightType = model.IsInternational ? FLIGHT_TYPE.OVERSEA : FLIGHT_TYPE.DOMESTIC,
                        BillNo = model.ReceiptNumber,
                        BillDate = model.ReceiptDate,
                        Flight = flight,
                        CustomerCode = airline.InvoiceCode ?? airline.Code,
                        Customer = airline,
                        CustomerName = airline.Name,
                        CustomerAddress = airline.Address,
                        TaxCode = airline.TaxCode,
                        CustomerType = (CUSTOMER_TYPE)airline.AirlineType,
                        UserCreatedId = user.Id,
                        RefuelCompany = refuelCompany,
                        Price = price,
                        Unit = unit,
                        Currency = currency,
                        CustomerEmail = airline.Email,
                        
                        ExchangeRate = exRate,
                        InvoiceType = airline.AirlineType == 0 && !model.IsInternational ? INVOICE_TYPE.BILL : INVOICE_TYPE.INVOICE,
                        LoginTaxCode = airport.TaxCode,
                        Gallon = model.TotalGallon,
                        Volume = model.TotalLiter,
                        Weight = model.TotalKg,
                        Receipt = receipt,
                        TaxRate = flightType == (int)FLIGHT_TYPE.DOMESTIC && airlineType == 1 ? 0.1M : 0,
                        SaleAmount = Math.Round((decimal)price * (unit == UNIT.GALLON ? model.TotalGallon : model.TotalKg), currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero),

                        Items = new List<InvoiceItem>()


                    };
                    inv.FHSUniqueId = model.LocalUniqueId;
                    db.Invoices.Add(inv);
                   
                    inv.TotalAmount = inv.SaleAmount + inv.TaxAmount;

                    foreach (var item in model.Items)
                    {
                        //var truckNo = item.RefuelerNo;
                        var truck = db.Trucks.FirstOrDefault(t => t.Code.Equals(item.RefuelerNo, StringComparison.InvariantCultureIgnoreCase));
                        if (truck == null)
                            return new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = string.Format("Refueler No '{0}' not exists", item.RefuelerNo) };
                        var refuelItem = flight.RefuelItems.FirstOrDefault(rf => rf.TruckId == truck.Id && rf.FlightId == flight.Id && rf.Status != REFUEL_ITEM_STATUS.DONE);
                        if (refuelItem == null)
                        {
                            refuelItem = new RefuelItem
                            {
                                StartNumber = item.StartNumber,
                                EndNumber = item.EndNumber,
                                QCNo = item.CertNo,
                                Density = item.Density,
                                ManualTemperature = item.Temperature,
                                Temperature = item.Temperature,
                                Amount = item.Gallon,
                                Gallon = item.Gallon,
                                Volume = item.Liter,
                                Weight = item.Kg,
                                TruckId = truck.Id,
                                UserCreatedId = user.Id,
                                DriverId = user.Id,
                                OperatorId = user.Id,
                                StartTime = item.StartTime,
                                EndTime = item.EndTime,
                                Price = price,
                                Unit = unit,
                                Currency = currency,
                                Status = REFUEL_ITEM_STATUS.DONE,
                                CreatedLocation = ITEM_CREATED_LOCATION.FHS

                            };
                            flight.RefuelItems.Add(refuelItem);
                        }
                        else
                        {
                            Logger.AppendLog("FHS", "refuel item existed", "fhs");
                            refuelItem.StartNumber = item.StartNumber;
                            refuelItem.EndNumber = item.EndNumber;
                            refuelItem.QCNo = item.CertNo;
                            refuelItem.Density = item.Density;
                            refuelItem.ManualTemperature = item.Temperature;
                            refuelItem.Temperature = item.Temperature;
                            refuelItem.Amount = item.Gallon;
                            refuelItem.Gallon = item.Gallon;
                            refuelItem.Volume = item.Liter;
                            refuelItem.Weight = item.Kg;
                            refuelItem.TruckId = truck.Id;
                            refuelItem.UserCreatedId = user.Id;
                            refuelItem.DriverId = user.Id;
                            refuelItem.OperatorId = user.Id;
                            refuelItem.StartTime = item.StartTime;
                            refuelItem.EndTime = item.EndTime;
                            refuelItem.Price = price;
                            refuelItem.Unit = unit;
                            refuelItem.Currency = currency;
                            refuelItem.Status = REFUEL_ITEM_STATUS.DONE;

                        }
                        
                        var receiptItem = new ReceiptItem
                        {
                            StartNumber = item.StartNumber,
                            EndNumber = item.EndNumber,
                            QualityNo = item.CertNo,
                            Density = item.Density,
                            Temperature = item.Temperature,
                            Gallon = item.Gallon,
                            Volume = item.Liter,
                            Weight = item.Kg,
                            Truck = truck,
                            UserCreatedId = user.Id,
                            DriverId = user.Id,
                            OperatorId = user.Id,
                            StartTime = item.StartTime,
                            EndTime = item.EndTime,
                            RefuelItem = refuelItem

                        };

                        var invItem = new InvoiceItem
                        {
                            StartNumber = item.StartNumber,
                            EndNumber = item.EndNumber,
                            QCNo = item.CertNo,
                            Density = item.Density,
                            Temperature = item.Temperature,
                            Gallon = item.Gallon,
                            Volume = item.Liter,
                            Weight = item.Kg,
                            Truck = truck,
                            TruckNo = item.RefuelerNo,
                            UserCreatedId = user.Id,
                            DriverId = user.Id,
                            OperatorId = user.Id,
                            StartTime = item.StartTime,
                            EndTime = item.EndTime,
                            RefuelItem = refuelItem

                        };
                        receipt.Items.Add(receiptItem);
                        
                        inv.Items.Add(invItem);

                    }
                    receipt.StartTime = flight.StartTime = flight.RefuelItems
                        .Where(rf =>  rf.Status == REFUEL_ITEM_STATUS.DONE)
                        .Min(rf => rf.StartTime);
                    receipt.EndTime = flight.EndTime = flight.RefuelItems
                        .Where(rf => rf.EndTime !=null && rf.Status == REFUEL_ITEM_STATUS.DONE)
                        .Max(rf => rf.EndTime.Value);

                    db.Database.Log = s => Logger.AppendLog("FHS", s, "fhs-sql");
                    Logger.AppendLog("RECEIPT", "Start Saving receipt " + receipt.Number, "fhs");
                    db.SaveChanges();
                    trans.Commit();
                    Logger.AppendLog("RECEIPT", "Save receipt OK " + receipt.Number, "fhs");
                    DataExchange.InvoiceExporter.Export(inv.Id);
                    return new ImportResult
                    {
                        Result = DATA_IMPORT_RESULT.SUCCESS,
                        Message = "Success",
                        ImagePath = Request.RequestUri.GetLeftPart(UriPartial.Authority) + "/receipts/" + receipt.ImagePath
                    };

                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "fhs");
                    //trans.Rollback();
                    return new ImportResult { Result = DATA_IMPORT_RESULT.FAILED, Message = ex.Message };
                }
            }
        }
        private void SaveImage(byte[] bytes, string fileName, string folderPath)
        {
            Logger.AppendLog("RECEIPT", "Save " + fileName, "fhs");
            //var fs = new BinaryWriter(new FileStream(Path.Combine(folderPath, fileName), FileMode.Append, FileAccess.Write));
            //fs.Write(bytes);
            //fs.Close();
            var ms = new MemoryStream(bytes);

            Image img = Image.FromStream(ms);
            Size thumbnailSize = GetThumbnailSize(img);
            Image thumbnail = img.GetThumbnailImage(thumbnailSize.Width, thumbnailSize.Height, null, IntPtr.Zero);

            img.Save(Path.Combine(folderPath, fileName), ImageFormat.Jpeg);
           
            ms.Close();

            Logger.AppendLog("RECEIPT", "Save OK " + fileName, "fhs");

        }
        private Size GetThumbnailSize(Image original)
        {
            // Maximum size of any dimension.
            const int maxPixels = 600;

            // Width and height.
            int originalWidth = original.Width;
            int originalHeight = original.Height;

            // Compute best factor to scale entire image based on larger dimension.
            double factor;
            //if (originalWidth > originalHeight)
            //{
            //    factor = (double)maxPixels / originalWidth;
            //}
            //else
            //{
            //    factor = (double)maxPixels / originalHeight;
            //}

            factor = (double)maxPixels / originalWidth;
            if (factor > 1.0)
                factor = 1;
            // Return thumbnail size.
            return new Size((int)(originalWidth * factor), (int)(originalHeight * factor));
        }

    }


}
