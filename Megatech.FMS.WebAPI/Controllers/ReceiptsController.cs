using FMS.Data;
using Megatech.FMS.DataExchange;
using Megatech.FMS.WebAPI.App_Start;
using Megatech.FMS.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class ReceiptsController : ApiController
    {
        private DataContext db = new DataContext();

        // GET: api/Receipts
        [Route("api/receipts/{truckId}/{date}")]
        public IQueryable<Receipt> GetReceipts(int truckId, DateTime? date)
        {
            return db.Receipts;
        }
        [HttpGet]
        [Route("api/receipt/image/{id}")]
        public IHttpActionResult CreateImage(int id) {
            var folderPath = HostingEnvironment.MapPath("/receipts");

            var model = db.Receipts.Include(re =>re.Items.Select(it=>it.Truck)).Include(re =>re.Flight.Airline).Include(re =>re.Customer)
                .FirstOrDefault(re =>re.Id == id);
            try
            {
                var receipt = new ReceiptModel
                {
                    Number = model.Number,
                    Date = model.Date,
                    FlightCode = model.FlightCode,
                    AircraftCode = model.AircraftCode,
                    AircraftType = string.IsNullOrEmpty(model.AircraftType)? model.Flight.AircraftType: model.AircraftType,
                    RouteName = model.RouteName,
                    FlightType = model.FlightType,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    CustomerName = model.CustomerName,
                    CustomerCode = model.CustomerCode,
                    CustomerAddress = model.CustomerAddress,
                    TaxCode = model.TaxCode,
                    Gallon = model.Gallon,
                    Volume = model.Volume,
                    Weight = model.Weight,
                    IsFHS = model.IsFHS,
                    IsThermal = model.IsThermal,
                    TechLog = model.TechLog,

                    Items = new List<ReceiptItemModel>()
                };

                ///process flighttype
                var routes = model.RouteName.Split(new char[] { '-', '_', ' ' });
                if (routes.Length > 1)
                {
                    var toAirport = routes[1];
                    
                }

                foreach (var item in model.Items)
                {
                    receipt.Items.Add(new ReceiptItemModel
                    {
                        TruckNo = item.Truck.Code,
                        StartNumber = item.StartNumber,
                        EndNumber = item.EndNumber,
                        StartTime = item.StartTime,
                        EndTime = model.EndTime,
                        Density = item.Density,
                        Temperature = item.Temperature,
                        Gallon = item.Gallon,
                        Volume = item.Volume,
                        Weight = item.Weight,
                        QualityNo = item.QualityNo
                    });
                }
                if (model.SignaturePath != null)
                {
                    receipt.Signature = File.ReadAllBytes(Path.Combine(folderPath, model.SignaturePath));
                }
                else if (model.Signature != null)
                    receipt.Signature = model.Signature;
                if (model.SellerPath != null)
                {
                    receipt.SellerSignature = File.ReadAllBytes(Path.Combine(folderPath, model.SellerPath));
                }
                else if (model.SellerImage != null)
                    receipt.SellerSignature = model.SellerImage;
                Image img = receipt.CreateReceiptImage();
                img.Save(Path.Combine(folderPath, receipt.Number + ".jpg"));
                return Ok(receipt);
            }
            catch (Exception ex){
                return Ok(ex);
            }
        }

        [HttpGet]
        [Route("api/receipt/image")]
        public IHttpActionResult CreateImage()
        {
            var folderPath = HostingEnvironment.MapPath("/receipts");
            var lst = db.Receipts
                .Include(re => re.Items.Select(it => it.Truck)).Include(re => re.Flight.Airline).Include(re => re.Customer)
                .Where(re => re.RefuelCompany == REFUEL_COMPANY.NAFSC && re.Date >= new DateTime(2022, 07, 31)
                && re.AircraftType == null).ToList();
            foreach (var model in lst)
            {

                var receipt = new ReceiptModel
                {
                    Number = model.Number,
                    Date = model.Date,
                    FlightCode = model.FlightCode,
                    AircraftCode = model.AircraftCode,
                    AircraftType = model.Flight.AircraftType,
                    RouteName = model.RouteName,
                    FlightType = model.FlightType,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    CustomerName = model.CustomerName,
                    CustomerCode = model.CustomerCode,
                    CustomerAddress = model.CustomerAddress,
                    TaxCode = model.TaxCode,
                    Gallon = model.Gallon,
                    Volume = model.Volume,
                    Weight = model.Weight,
                    IsFHS = model.IsFHS,
                    IsThermal = model.IsThermal,
                    TechLog = model.TechLog,
                    Items = new List<ReceiptItemModel>()
                };
                foreach (var item in model.Items)
                {
                    receipt.Items.Add(new ReceiptItemModel
                    {
                        TruckNo = item.Truck.Code,
                        StartNumber = item.StartNumber,
                        EndNumber = item.EndNumber,
                        StartTime = item.StartTime,
                        EndTime = model.EndTime,
                        Density = item.Density,
                        Temperature = item.Temperature,
                        Gallon = item.Gallon,
                        Volume = item.Volume,
                        Weight = item.Weight,
                        QualityNo = item.QualityNo
                    });
                }
                if (model.SignaturePath != null)
                {
                    receipt.Signature = File.ReadAllBytes(Path.Combine(folderPath, model.SignaturePath));
                }
                else receipt.Signature = model.Signature;
                if (model.SellerPath != null)
                {
                    receipt.SellerSignature = File.ReadAllBytes(Path.Combine(folderPath, model.SellerPath));
                }
                else receipt.SellerSignature = model.SellerImage;
                Image img = receipt.CreateReceiptImage();
                img.Save(Path.Combine(folderPath, receipt.Number + ".jpg"));
                model.ImagePath = receipt.Number + ".jpg";
            }

            db.SaveChanges();

            return Ok(lst);
        }

        [HttpGet]
        [Route("api/receipt/updateImage/{id?}")]
        public IHttpActionResult UpdateImage(int? id = null)
        {
            var ret = InvoiceExporter.UpdateImage(id);
            return Ok(ret);
        }

        [HttpGet]
        [Route("api/receipt/json/{id}")]
        public IHttpActionResult CreateJson(int id, int r = 0  )
        {
            try
            {
                var file = HttpContext.Current.Request.Files.Count > 0 ?
                        HttpContext.Current.Request.Files[0] : null;

                var flight = db.Flights.Include(f => f.Airline).Include(f => f.Airport).Include("RefuelItems").Include("RefuelItems.Truck")
                    .Include("RefuelItems.Operator")
                    .Include("RefuelItems.Driver").FirstOrDefault(f => f.Id == id);
                Logger.AppendLog("FLIGHT", flight.Code, "receipt-json");
                var list = flight.RefuelItems.Where(re=>re.Status == REFUEL_ITEM_STATUS.DONE).ToList();
                if (r > 0)
                    list = flight.RefuelItems.Where(re => re.Id == r).ToList();

                if (flight == null || list.Count == 0)
                    return NotFound();

                var refuel = list.FirstOrDefault();


                var num = refuel.Truck.Code.Substring(0, 3) + refuel.Truck.Code.Substring(refuel.Truck.Code.Length - 2, 2) + refuel.EndTime?.ToString("yyMMddHHmm") + "1";

                var price = 0.0M;
                var currency = CURRENCY.VND;
                var unit = UNIT.GALLON;

                //var exRate = 1.0M;
                var exRate = db.ProductPrices.Where(p => p.StartDate <= flight.EndTime && p.Currency == CURRENCY.USD)
                    .OrderByDescending(p => p.StartDate).Select(p => p.ExchangeRate)
                    .FirstOrDefault();

                var airport = db.Airports.FirstOrDefault(a => a.Id == flight.AirportId);


                var depotType = airport.DepotType;
                var airlineType = db.Airlines.Where(a => a.Id == flight.AirlineId).Select(a => a.AirlineType).FirstOrDefault() ?? 0;
                var flightType = (int)flight.FlightType;
                var airline = flight.Airline;


                var prices = (from p in db.ProductPrices.Include(p => p.Product)
                              where p.StartDate <= flight.EndTime
                              //&& p.BranchId == (int) airport.Branch// && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch
                              group p by new { p.CustomerId, p.AirlineType, p.BranchId, p.DepotType, p.Unit }
                     into groups
                              select groups.OrderByDescending(g => g.StartDate).FirstOrDefault()).ToList();

                var pPrice = prices.OrderByDescending(p => p.StartDate)
                                                    .FirstOrDefault(p => p.AirlineType == (flightType == (int)FLIGHT_TYPE.OVERSEA || airlineType == 0 ? 1 : 0) && p.CustomerId == flight.AirlineId);
                if (pPrice == null && airlineType == (int)CUSTOMER_TYPE.LOCAL)
                {
                    pPrice = new ProductPrice { Currency = CURRENCY.VND };
                }
                if (pPrice == null)
                    pPrice = prices.OrderByDescending(p => p.StartDate)
                .FirstOrDefault(p => p.AirlineType == (flightType == (int)FLIGHT_TYPE.OVERSEA ? 1 : 0) && (p.Unit == (int)0 && p.DepotType == depotType && p.BranchId == (int)airport.Branch));

                if (pPrice != null)
                {
                    price = pPrice.Price;
                    currency = pPrice.Currency;
                    unit = unit = pPrice.Unit == 0 ? UNIT.GALLON : UNIT.KG;
                }

                decimal greenTax = 0M;

                //Logger.AppendLog("RECEIPT", string.Format("greentax :{0} airlineTypev:{1} flightType:{2}",greenTax,airlineType, flightType), "receipt-create");

                if ((airline.DomesticInvoice ?? false) && airlineType == 0 && flightType == 0)
                {
                    greenTax = db.GreenTaxes.OrderByDescending(gr => gr.StartDate).Where(gr => gr.StartDate <= flight.EndTime)
                        .Select(gr => gr.TaxAmount).FirstOrDefault();


                }
                Logger.AppendLog("RECEIPT", string.Format("greentax :{0} airlineTyp:{1} flightType:{2}", greenTax, airlineType, flightType), "receipt-json");
                var inv = new Invoice
                {
                    Flight = flight,
                    Customer = flight.Airline,
                    FlightCode = flight.Code,
                    FlightType = flight.FlightType,
                    CustomerCode = string.IsNullOrEmpty(flight.Airline.InvoiceCode)? flight.Airline.Code: flight.Airline.InvoiceCode,
                    CustomerName = flight.Airline.Name,
                    CustomerAddress = flight.Airline.Address,
                    TaxCode = flight.Airline.TaxCode,
                    CustomerType = (CUSTOMER_TYPE)flight.Airline.AirlineType,
                    AircraftCode = flight.AircraftCode,
                    AircraftType = flight.AircraftType,
                    RouteName = flight.RouteName,
                    BillNo = num,
                    BillDate = flight.EndTime.Date,
                    InvoiceType = (airline.DomesticInvoice??false)  || flight.Airline.AirlineType == 1 ? INVOICE_TYPE.INVOICE : INVOICE_TYPE.BILL,
                    Price = price,
                    Unit = unit,
                    
                    Currency = currency,
                    Items = new List<InvoiceItem>(),
                    

                };
                Logger.AppendLog("INVOICE", "INVOICE ok", "receipt-json");
                foreach (var item in list)
                {
                    inv.Items.Add(new InvoiceItem
                    {
                        TruckId = item.Truck.Id,
                        TruckNo = item.Truck.Code,
                        Gallon = item.Gallon,
                        Volume = item.Volume,
                        Weight = item.Weight,
                        Density = item.Density,
                        Temperature = item.ManualTemperature,
                        QCNo = item.QCNo,
                        DriverId = item.DriverId,
                        Driver = item.Driver,
                        Operator = item.Operator,
                        StartNumber = item.StartNumber,
                        EndNumber = item.EndNumber,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime.Value,
                        Invoice = inv,


                    });
                    inv.RefuelCompany = item.Truck.RefuelCompany;
                }
                Logger.AppendLog("ITEM", "ITEM ok", "receipt-json");

                inv.Gallon = inv.Items.Sum(it => it.Gallon);
                inv.Volume = (decimal)inv.Items.Sum(it => it.Volume);
                inv.Weight = (decimal)inv.Items.Sum(it => it.Weight);
                inv.TaxRate = flightType == (int) FLIGHT_TYPE.DOMESTIC ? 0.1M : 0M;
                inv.SaleAmount = (decimal)((inv.Unit == UNIT.GALLON ? inv.Gallon : inv.Weight) * inv.Price);
                
                inv.GreenTax = inv.BillDate >= new DateTime(2022, 08, 01) ? greenTax : 0;
                inv.TotalAmount = inv.SaleAmount + inv.TaxAmount + inv.GreenTaxAmount;

                Logger.AppendLog("RECEIPT", string.Format("greentax :{0} airlineTyp:{1} flightType:{2}, taxRate: {3}", greenTax, airlineType, flightType, inv.TaxRate), "receipt-json");


                DataExchange.InvoiceExportModel model = new DataExchange.InvoiceExportModel(inv);


                return Ok(model);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "receipt-json");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/receipts/create/{id}")]
        public IHttpActionResult Create(int id, int r = 0)
        {
            
            try
            {
                var file = HttpContext.Current.Request.Files.Count > 0 ?
                        HttpContext.Current.Request.Files[0] : null;

                var num = HttpContext.Current.Request.Params["receiptNumber"];
                var invoiceNumber = HttpContext.Current.Request.Params["invoiceNo"];
                var ccid = HttpContext.Current.Request.Params["ccid"];
                var signNo = HttpContext.Current.Request.Params["sign"];
                var date = HttpContext.Current.Request.Params["date"] ==null? (DateTime?)null :  DateTime.ParseExact(HttpContext.Current.Request.Params["date"],"yyyy-MM-dd", CultureInfo.InvariantCulture) ;
                var signType = HttpContext.Current.Request.Params["sign_type"] == null ? (int?)null : int.Parse(HttpContext.Current.Request.Params["sign_type"]);
                var flight = db.Flights.Include(f => f.Airline).Include(f => f.RefuelItems.Select(re => re.Truck)).FirstOrDefault(f => f.Id == id);

                var list = flight.RefuelItems.Where(re=>re.Status == REFUEL_ITEM_STATUS.DONE && (r==0 || re.Id == r)).ToList();

                if (flight == null || list.Count == 0)
                    return NotFound();

                var refuel = list.FirstOrDefault();

                if (string.IsNullOrEmpty(num))
                    num = refuel.Truck.Code.Substring(0, 3) + refuel.Truck.Code.Substring(refuel.Truck.Code.Length - 2, 2) + refuel.EndTime?.ToString("yyMMddHHmm") + "1";


                ReceiptModel model = new ReceiptModel
                {
                    Number = num,
                    Date = refuel.EndTime.Value.Date,
                    CustomerName = flight.Airline.Name,
                    CustomerAddress = flight.Airline.Address,
                    CustomerCode = flight.Airline.InvoiceCode?? flight.Airline.Code,
                    TaxCode = flight.Airline.TaxCode,
                    CustomerId = flight.Airline.Id,
                    AircraftCode = flight.AircraftCode,
                    AircraftType = flight.AircraftType,
                    FlightCode = flight.Code,
                    FlightId = flight.Id,
                    FlightType = (int)flight.FlightType,
                    RouteName = flight.RouteName,


                    Gallon = list.Sum(re =>re.Gallon),
                    Volume = (decimal)list.Sum(re =>re.Volume),
                    Weight = (decimal)list.Sum(re =>re.Weight),
                    StartTime = list.Min(re =>re.StartTime),
                    EndTime = list.Min(re =>re.EndTime.Value),

                    CCID = ccid,
                    SignNo =signNo,
                    InvoiceDate=date, 
                    InvoiceNumber= invoiceNumber,
                    SignType = signType,
                    Items = new List<ReceiptItemModel>()
                    


                };
                foreach (var rf in list)
                {
                    model.Items.Add(new ReceiptItemModel
                    {
                        Gallon = rf.Gallon,
                        Volume = (decimal)rf.Volume,
                        Weight = (decimal)rf.Weight,
                        StartTime = rf.StartTime,
                        EndTime = rf.EndTime.Value,
                        QualityNo = rf.QCNo,
                        RefuelItemId = rf.UniqueId.ToString(),
                        Density = rf.Density,
                        Temperature = rf.ManualTemperature,
                        EndNumber = rf.EndNumber,
                        StartNumber = rf.StartNumber,
                        TruckId = rf.TruckId,
                        TruckNo = rf.Truck.Code
                    });
                }


                if (file != null && file.ContentLength > 0)
                {
                    var fs = file.InputStream;
                    System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
                    Byte[] bytes = br.ReadBytes((Int32)fs.Length);
                    model.PdfImageString = Convert.ToBase64String(bytes, 0, bytes.Length);
                }
                Logger.AppendLog("RECEIPT", "OK", "receipt-create");
                var receipt = ReceiptModel.SaveReceipt(model, null, null);
                Logger.AppendLog("RECEIPT", "Save OK", "receipt-create");


                return Ok(receipt);
            }
            catch (Exception ex)
            {
                throw ex;
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Receipts/5
        [ResponseType(typeof(Receipt))]
        public IHttpActionResult GetReceipt(int id)
        {
            var receipt = db.Receipts.Include(re =>re.Items).Where(re =>re.Id == id)
            .Select(r => new ReceiptModel
            {
                Id = r.Id,
                CustomerName = r.CustomerName,
                Number = r.Number,
                Date = r.Date,
                FlightCode = r.Flight.Code,
                AircraftCode = r.AircraftCode,
                AircraftType = r.Flight.AircraftType,
                RouteName = r.RouteName,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Signature = r.Signature,
                Gallon = r.Gallon,
                Weight = r.Weight,
                Volume = r.Volume,
                SellerSignature = r.SellerImage,
                Items = r.Items.Select(ri => new ReceiptItemModel
                {
                    TruckId = ri.TruckId,
                    TruckNo = ri.Truck.Code,
                    Gallon = ri.Gallon,
                    Weight = ri.Weight,
                    Volume = ri.Volume,
                    StartNumber = ri.StartNumber,
                    EndNumber = ri.EndNumber,
                    Temperature = ri.Temperature,
                    Density = ri.Density
                }).ToList()
            }).FirstOrDefault();


            return Ok(receipt);
        }

        // PUT: api/Receipts/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutReceipt(int id, Receipt receipt)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != receipt.Id)
            {
                return BadRequest();
            }

            db.Entry(receipt).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReceiptExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]

        [Route("api/receipts/multipart")]
        public IHttpActionResult PostMultipart()
        {
            var folderPath = HostingEnvironment.MapPath("/receipts");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (HttpContext.Current.Request.Files.Count>0)
            {
                var imageFile = HttpContext.Current.Request.Files["Receipt-Image"];
                var buyerFile = HttpContext.Current.Request.Files["Buyer-Signature"];
                var sellerFile = HttpContext.Current.Request.Files["Seller-Signature"];
                var json = HttpContext.Current.Request["Receipt-Data"];

                ReceiptModel receipt = JsonConvert.DeserializeObject<ReceiptModel>(json);

                if (imageFile != null)
                {
                    imageFile.SaveAs(Path.Combine(folderPath, receipt.Number + ".jpg"));
                    receipt.ImagePath = receipt.Number + ".jpg";

                }

                if (buyerFile != null)
                {
                    buyerFile.SaveAs(Path.Combine(folderPath, receipt.Number + "_BUYER.jpg"));
                    receipt.SignaturePath = receipt.Number + "_BUYER.jpg";
                }
                if (sellerFile != null)
                {
                    sellerFile.SaveAs(Path.Combine(folderPath, receipt.Number + "_SELLER.jpg"));
                    receipt.SellerPath = receipt.Number + "_SELLER.jpg";
                }
                
                ReceiptModel.SaveReceipt(receipt); 
                return Ok(receipt);


            }
            return BadRequest();
        }
        // POST: api/Receipts
        [ResponseType(typeof(Receipt))]
        public IHttpActionResult PostReceipt(ReceiptModel receipt)
        {
            if (receipt == null)
                return BadRequest();

            var ticks = DateTime.Now.Ticks.ToString();

            Logger.AppendLog("RECEIPT", "Receive Request  " + ticks, "receipt");

            var json = JsonConvert.SerializeObject(receipt);
            Logger.AppendLog("RECEIPT", json, "receipt-data");
            if (!ModelState.IsValid)
            {
                Logger.AppendLog("ERROR", receipt.Number, "receipt");
                var values = ModelState.Values.Select(v => v.Errors).ToList();
                var erros = values.Select(v => v.FirstOrDefault().ErrorMessage).ToList();

       

                foreach (var item in erros)
                {
                    Logger.AppendLog("ERROR", item, "receipt");
                }
                return BadRequest(ModelState);
            }
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var userId = user != null ? user.Id : 0;
            //var json = JsonConvert.SerializeObject(receipt);
            if (receipt.EndTime < DateTime.Today.AddDays(-5))
            {
                Logger.AppendLog(ticks, "Error Old receipt " + receipt.Number, "receipt");
                receipt.Id = int.MaxValue;
                return Ok(receipt);
            }

            var folderPath = HostingEnvironment.MapPath("/receipts");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            db.Database.BeginTransaction();
            try
            {
                Logger.AppendLog(ticks, "Save Receipt # " + receipt.Number, "receipt");
                var model = db.Receipts.FirstOrDefault(re =>re.Number == receipt.Number && re.IsReuse == receipt.IsReuse );
                if (model != null)
                {
                    Logger.AppendLog(ticks, $"Receipt # {receipt.Number} existed", "receipt");
                    Logger.AppendLog(ticks, $"Model.StartTime {model.StartTime} Receipt.StartTime {receipt.StartTime} Model.Gallon {model.Gallon} Receipt.Gallon {receipt.Gallon}", "receipt");
                    receipt.Id = model.Id;
                }
               

                //process route and flight type
                var routes = receipt.RouteName.Split(new char[] { '-', ' ' });
                if (routes.Length > 1)
                {
                    var desc = routes[1];
                    
                }

                if (model != null && receipt.ReplacedId == null)
                    receipt.Id = model.Id;
                else 
                    receipt = ReceiptModel.SaveReceipt(receipt,ticks, userId);
                //if (receipt.IsCancelled && model != null)
                //{
                //    Logger.AppendLog("INV", "Cancel invoice Reason:" + receipt.CancelReason, "invoice");
                //    var invoices = db.Invoices.Where(inv => inv.ReceiptId == model.Id).ToList();
                //    foreach (var item in invoices)
                //    {
                //        Logger.AppendLog("INV", "Invoice Id : " + item.Id.ToString(), "invoice");
                //        item.CancelReason = receipt.CancelReason;
                //        item.RequestCancel = true;
                //        db.SaveChanges();
                //        //DataExchange.InvoiceExporter.Cancel(item.Id);
                //    }
                //    Logger.AppendLog("INV", "END cancel invoice", "invoice");
                //}
                //HostingEnvironment.QueueBackgroundWorkItem(ct => DataExchange.Exporter.ExportInvoice());
                if (receipt.Id>0)
                {
                    //receipt.Id = model.Id;
                    //receipt.PdfImageString = null;
                    //receipt.SignImageString = null;
                    //receipt.SellerImageString = null;
                    return Ok(receipt);
                }
                return NotFound();
            }
            catch (Exception ex)
            {

                Logger.AppendLog("ERROR", receipt.Number, "receipt");
                Logger.LogException(ex, "receipt");

                db.Database.CurrentTransaction.Rollback();
                return BadRequest();
            }

        }

        private void SaveImage(string base64String, string fileName, string folderPath)
        {
            SaveImage(Convert.FromBase64String(base64String), fileName, folderPath);
        }
        private void SaveImage(byte[] bytes, string fileName, string folderPath)
        {
            //Logger.AppendLog("RECEIPT", "Save " + fileName, "receipt");
            //var fs = new BinaryWriter(new FileStream(Path.Combine(folderPath, fileName), FileMode.Append, FileAccess.Write));
            //fs.Write(bytes);
            //fs.Close();
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    Image img = Image.FromStream(ms);
                    img.Save(Path.Combine(folderPath, fileName), ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                Logger.AppendLog("IMAGE", "Save image failed " + Path.Combine(folderPath, fileName), "receipt");
                Logger.LogException(ex, "receipt");
            }
        }


        // DELETE: api/Receipts/5
        [ResponseType(typeof(Receipt))]
        public IHttpActionResult DeleteReceipt(int id)
        {
            Receipt receipt = db.Receipts.Find(id);
            if (receipt == null)
            {
                return NotFound();
            }

            db.Receipts.Remove(receipt);
            db.SaveChanges();

            return Ok(receipt);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ReceiptExists(int id)
        {
            return db.Receipts.Count(e => e.Id == id) > 0;
        }
    }
}