using EntityFramework.DynamicFilters;
using FMS.Data;
using Megatech.FMS.WebAPI.App_Start;
using Megatech.FMS.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using System.Data.Entity.SqlServer;
using System.Configuration;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class RefuelsController : ApiController
    {
        public RefuelsController()
        {
            //Logger.SetPath(HostingEnvironment.MapPath("~/logs"));
            //ExportTask.Execute();
            HostingEnvironment.QueueBackgroundWorkItem(ct =>
            {
                //DataExchange.Logger.AppendLog("REFUEL", "Constructor", "aits");
                //Logger.AppendLog("EXPORT", "Auto run", "refuel");

                DataExchange.Exporter.ExportInvoice(); 
            });
        }
        public enum TIME_RANGE
        {
            SHIFT,
            TODAY,
            ALL
        }

        private DataContext db = new DataContext();

        private bool receipt_v2 = ConfigurationManager.AppSettings["receipt_2"] != null;

        [Authorize]
        [Route("api/refuels/modified")]

        public IEnumerable<RefuelViewModel> GetModified(REFUEL_ITEM_TYPE type = REFUEL_ITEM_TYPE.REFUEL, DateTime? lastModified = null)
        {
            string tabletId, appVersion = string.Empty, truckCode = string.Empty;
            if (Request.Headers.Contains("App-Version"))
                appVersion = Request.Headers.GetValues("App-Version").FirstOrDefault();
            if (Request.Headers.Contains("Tablet-Id"))
            {
                tabletId = Request.Headers.GetValues("Tablet-Id").FirstOrDefault();

                var truckId = Request.Headers.GetValues("Truck-Id").FirstOrDefault();
                truckCode = Request.Headers.GetValues("Truck-Code").FirstOrDefault();
                if (!truckCode.IsNullOrEmpty())
                {
                    var fileName = truckCode;
                    Logger.AppendLog("MODIFIED", $"Truck No: {truckCode} Last Modified:  {lastModified:yyyy-MM-dd HH:mm:ss} App-Version: {appVersion}", fileName);
                }
            }
            if (!string.IsNullOrEmpty(appVersion))
            {
                var versionNumber = int.Parse(appVersion.Substring(appVersion.LastIndexOf(".") + 1));
                if (versionNumber < 42)
                    receipt_v2 = false;
            }
            else receipt_v2 = false;
          

            using (var db = new DataContext())
            using (var trans = db.Database.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                
                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

                var userName = ClaimsPrincipal.Current.Identity.Name;

                var user = db.Users.FirstOrDefault(u => u.UserName == userName);

                var airportId = user != null ? user.AirportId : 0;

                var airport = db.Airports.FirstOrDefault(a => a.Id == airportId);
                //if (lastModified >= DateTime.Today)
               
                db.DisableFilter("IsNotDeleted");
                db.Database.CommandTimeout = 180;
                db.Configuration.ProxyCreationEnabled = false;
                var query = db.RefuelItems.AsQueryable();

                if (airportId != 0)
                    query = query.Where(r => r.Flight.AirportId == airportId);

                if (lastModified == null)
                    lastModified = DateTime.Today.AddDays(-1);
                else
                    lastModified = lastModified.Value.AddDays(-1);

                query = query.Where(r => r.DateUpdated > lastModified ||
                                (r.DateDeleted > lastModified) || r.Flight.DateUpdated > lastModified ||
                                (r.Flight.DateDeleted > lastModified)).OrderBy(r => r.DateUpdated).ThenBy(r => r.DateDeleted);

                try
                {
                    var list = query.AsNoTracking().OrderBy(r => r.Flight.RefuelScheduledTime)
                        .Select(r => new RefuelViewModel
                        {
                            FlightStatus = r.Flight.Status,
                            FlightId = r.FlightId,
                            FlightCode = r.Flight.Code,
                            FlightType = r.Flight.FlightType,
                            EstimateAmount = r.Flight.EstimateAmount,

                            Id = r.Id,
                            AircraftType = r.Flight.AircraftType,
                            AircraftCode = r.Flight.AircraftCode,
                            ParkingLot = r.Flight.Parking,
                            RouteName = r.Flight.RouteName,
                            Status = r.Status,
                            ArrivalTime = r.Flight.ArrivalScheduledTime == null || r.Flight.ArrivalScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, -60).Value : r.Flight.ArrivalScheduledTime.Value,
                            DepartureTime = r.Flight.DepartureScheduledTime == null || r.Flight.DepartureScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, 60).Value : r.Flight.DepartureScheduledTime.Value,
                            RefuelTime = r.Flight.RefuelScheduledTime,

                            RealAmount = (r.OriginalGallon ?? 0) > 0 ? (decimal)r.OriginalGallon : r.Amount,
                            Gallon = (r.OriginalGallon ?? 0) > 0 ? (decimal)r.OriginalGallon : r.Amount,
                            Volume = r.Volume ?? 0,

                            StartTime = r.Status != REFUEL_ITEM_STATUS.NONE && r.StartTime.Year < 9999 ? r.StartTime : DateTime.Now,
                            EndTime = r.EndTime ?? DateTime.Now,
                            StartNumber = r.StartNumber,
                            EndNumber = r.EndNumber,
                            DeviceEndTime = r.DeviceEndTime,
                            DeviceStartTime = r.DeviceStartTime,
                            Density = r.Density,
                            ManualTemperature = r.ManualTemperature,
                            Temperature = r.Temperature,
                            QualityNo = r.QCNo,
                            TaxRate = r.TaxRate,
                            Price = r.Price,
                            Currency = r.Currency,
                            Unit = r.Status == REFUEL_ITEM_STATUS.DONE ? (r.Unit ?? 0) : r.Flight.Airline.Unit,
                            TruckId = r.TruckId,
                            TruckNo = r.Truck.Code,

                            AirlineId = r.Flight.AirlineId ?? 1,
                            AirlineType = r.Flight.Airline == null ? 0 : r.Flight.Airline.AirlineType,

                            AirportId = r.Flight.AirportId,

                            RefuelItemType = r.RefuelItemType,

                            ReturnAmount = r.ReturnAmount,
                            ReturnUnit = r.ReturnUnit ?? RETURN_UNIT.KG,
                            WeightNote = r.WeightNote ?? (r.TechLog != null ? SqlFunctions.StringConvert(r.TechLog).Trim() : ""),
                            InvoiceNumber = r.InvoiceNumber,
                            ReturnInvoiceNumber = r.ReturnInvoiceNumber,

                            DriverId = r.DriverId ?? 0,
                            DriverName = r.DriverId == null ? "" : r.Driver.FullName,
                            OperatorId = r.OperatorId ?? 0,
                            OperatorName = r.OperatorId == null ? "" : r.Operator.FullName,

                            IsInternational = r.Flight.FlightType == FLIGHT_TYPE.OVERSEA,
                            Completed = r.Completed,
                            Printed = r.Printed,
                            IsDeleted = r.IsDeleted || r.Flight.IsDeleted,


                            InvoiceNameCharter = (r.Flight.InvoiceNameCharter ?? "").Trim() == "" ? r.Flight.Airline.Name : r.Flight.InvoiceNameCharter.Trim(),
                            InvoiceFormId = r.InvoiceFormId,
                            PrintTemplate = !r.Printed ? (r.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && r.Flight.Airline.AirlineType == 0 ? PRINT_TEMPLATE.BILL : PRINT_TEMPLATE.INVOICE) : r.PrintTemplate,

                            BM2508Result = r.BM2508Result,
                            UniqueId = r.UniqueId.ToString(),
                            ReceiptCount = r.ReceiptCount,
                            ReceiptNumber = r.Receipt != null ? r.Receipt.Number : (r.Printed || receipt_v2) ? r.ReceiptNumber : "",
                            ReceiptUniqueId = r.ReceiptUniqueId,
                            Exported = r.Exported,
                            DateUpdated = r.DateUpdated

                        }).ToList();


                    var prices = db.ProductPrices.Where(p => p.StartDate <= DateTime.Now).OrderByDescending(p => p.StartDate).GroupBy(p => new { p.CustomerId, p.AirlineType, p.DepotType, p.Unit, p.BranchId })
                         .Select(g => g.OrderByDescending(p => p.StartDate).FirstOrDefault()).ToList();

                    foreach (var item in list.Where(r => r.Status != REFUEL_ITEM_STATUS.DONE && !r.IsDeleted))
                    {
                        var price = prices.OrderByDescending(p => p.StartDate).FirstOrDefault(p => p.CustomerId == item.AirlineId);
                        if (price == null)
                            price = prices.OrderByDescending(p => p.StartDate)
                                .FirstOrDefault(p => p.CustomerId == null && p.AirlineType == (int)item.FlightType && p.BranchId == (int)airport.Branch && p.DepotType == airport.DepotType && p.Unit == (int)item.Unit && item.AirlineType == 1);
                        if (price == null)
                            price = prices.OrderByDescending(p => p.StartDate).FirstOrDefault(p => p.StartDate <= DateTime.Now && p.Unit == (int)item.Unit && item.AirlineType == 0);

                        if (price != null)
                        {
                            item.Price = price.Price;
                            item.Currency = price.Currency;
                        }

                    }
                    return list;
                }
                catch (Exception ex)
                {
                    Logger.AppendLog("QUERY", query.ToString(), "sql-modified");
                    Logger.AppendLog($"GetModified({lastModified:yyyyMMdd HH:mm:ss})", $"Truck Code: {truckCode} App-Version: {appVersion} Airport-Id:{airportId}", "refuel");
                    Logger.LogException(ex, "refuel");
                    return null;
                }

            }
        }

        [Authorize]

        // GET: api/Refuels
        public IEnumerable<RefuelViewModel> GetRefuels(string truckNo, int truckId = 0, int o = 1, REFUEL_ITEM_TYPE type = REFUEL_ITEM_TYPE.REFUEL, TIME_RANGE range = TIME_RANGE.SHIFT, bool d = false)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var airportId = user != null ? user.AirportId : 0;

            var now = DateTime.Now.TimeOfDay;// DbFunctions.CreateTime(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);


            var qshift = db.Shifts.Where(s => (s.StartTime < s.EndTime && DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)
                                            || (s.StartTime > s.EndTime && (DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) <= now
                                                                            || DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) >= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)))

                .Where(s => s.AirportId == airportId);
            var shift = qshift.FirstOrDefault();
            var start = DateTime.Today;
            var end = DateTime.Today;
            if (range == TIME_RANGE.SHIFT && shift != null)
            {
                start = start.Add(shift.StartTime.TimeOfDay);
                if (start > DateTime.Now)
                    start = start.AddDays(-1);
                end = end.Add(shift.EndTime.TimeOfDay);
                if (end < start)
                    end = end.AddDays(1);
            }
            if (range == TIME_RANGE.TODAY)
                end = end.AddDays(1);
            if (range == TIME_RANGE.ALL)
            {
                start = DateTime.MinValue;
                end = DateTime.MaxValue;
            }

            if (d)
                db.DisableFilter("IsNotDeleted");
            db.Configuration.ProxyCreationEnabled = false;
            var query = db.RefuelItems.Include(r => r.Flight).Include(r => r.Truck);
            query = query.Where(r => r.Flight.RefuelScheduledTime >= start)
                    .Where(r => r.Flight.RefuelScheduledTime <= end)
                .Where(r => r.RefuelItemType == type);
            if (airportId != 0)
                query = query.Where(r => r.Flight.AirportId == airportId);

            var truck = db.Trucks.FirstOrDefault(t => t.Code == truckNo);
            if (truck != null)
                truckId = truck.Id;
            if (o == 1)
                query = query.Where(r => r.TruckId == truckId);
            else
                query = query.Where(r => r.TruckId != truckId);


            var list = query.OrderBy(r => r.Flight.RefuelScheduledTime)
                .Select(r => new RefuelViewModel
                {
                    FlightStatus = r.Flight.Status,
                    FlightId = r.FlightId,
                    FlightCode = r.Flight.Code.Trim(),
                    EstimateAmount = r.Flight.EstimateAmount,
                    Id = r.Id,
                    AircraftType = r.Flight.AircraftType.Trim(),
                    AircraftCode = r.Flight.AircraftCode.Trim(),
                    ParkingLot = r.Flight.Parking.Trim(),
                    RouteName = r.Flight.RouteName.Trim(),

                    Status = r.Status,
                    ArrivalTime = r.Flight.ArrivalScheduledTime == null || r.Flight.ArrivalScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, -60).Value : r.Flight.ArrivalScheduledTime.Value,
                    DepartureTime = r.Flight.DepartureScheduledTime == null || r.Flight.DepartureScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, 60).Value : r.Flight.DepartureScheduledTime.Value,

                    RefuelTime = r.Flight.RefuelScheduledTime,
                    RealAmount = r.OriginalGallon ?? 0,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime ?? DateTime.MinValue,
                    StartNumber = r.StartNumber,
                    EndNumber = r.EndNumber,
                    DeviceEndTime = r.DeviceEndTime,
                    DeviceStartTime = r.DeviceStartTime,
                    Density = r.Density,
                    ManualTemperature = r.ManualTemperature,
                    Temperature = r.Temperature,
                    Price = r.Price,
                    Currency = r.Currency,
                    Unit = r.Unit ?? 0,
                    QualityNo = r.QCNo,
                    TaxRate = r.TaxRate,
                    TruckNo = r.Truck.Code,
                    TruckId = r.TruckId,
                    Gallon = r.Gallon,
                    AirlineId = r.Flight.AirlineId ?? 1,
                    RefuelItemType = r.RefuelItemType,

                    ReturnAmount = r.ReturnAmount,
                    ReturnUnit = r.ReturnUnit ?? RETURN_UNIT.KG,
                    WeightNote = r.WeightNote ?? (r.TechLog != null ? SqlFunctions.StringConvert(r.TechLog).Trim() : ""),
                    InvoiceNumber = r.InvoiceNumber,

                    DriverId = r.DriverId ?? 0,
                    DriverName = r.Driver == null ? "" : r.Driver.FullName,

                    OperatorId = r.OperatorId ?? 0,
                    OperatorName = r.Operator == null ? "" : r.Operator.FullName,
                    IsDeleted = r.IsDeleted || r.Flight.IsDeleted,
                    IsInternational = r.Flight.FlightType == FLIGHT_TYPE.OVERSEA,

                    Completed = r.Completed,
                    Printed = r.Printed,
                    InvoiceNameCharter = (r.Flight.InvoiceNameCharter ?? "").Trim() == "" ? r.Flight.Airline.Name : r.Flight.InvoiceNameCharter.Trim(),
                    InvoiceFormId = r.InvoiceFormId,
                    PrintTemplate = !r.Printed ? (r.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && r.Flight.Airline.AirlineType == 0 ? PRINT_TEMPLATE.BILL : PRINT_TEMPLATE.INVOICE) : r.PrintTemplate

                }).ToList();//.OrderBy(r => r.Status).ThenByDescending(r => r.RefuelTime);

            var flights = list.Select(item => item.FlightId).ToArray();
            /*
            var items = list.Select(item => item.Id).ToArray();

            var others = db.RefuelItems.Where(r=> flights.Contains(r.FlightId) && !items.Contains(r.Id))
                .Select(r => new RefuelViewModel
                {
                    FlightId = r.FlightId,
                    FlightCode = r.Flight.Code,
                    EstimateAmount = r.Flight.EstimateAmount,
                    Id = r.Id,
                    AircraftType = r.Flight.AircraftType,
                    AircraftCode = r.Flight.AircraftCode,
                    ParkingLot = r.Flight.Parking,
                    RouteName = r.Flight.RouteName,
                    Status = r.Status,
                    ArrivalTime = r.Flight.ArrivalScheduledTime ?? DateTime.MinValue,
                    DepartureTime = r.Flight.DepartureScheduledTime ?? DateTime.MinValue,
                    RefuelTime = r.Flight.RefuelTime ,
                    RealAmount = r.originalGallon,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime ?? DateTime.MinValue,
                    DeviceEndTime = r.DeviceEndTime ,
                    DeviceStartTime = r.DeviceStartTime ,
                    StartNumber = r.StartNumber,
                    EndNumber = r.EndNumber,
                    Density = r.Density,
                    ManualTemperature = r.ManualTemperature,
                    Temperature = r.Temperature,
                    QualityNo = r.QCNo,
                    TaxRate = r.TaxRate,
                    Price = r.Price,
                    TruckNo = r.Truck.Code,
                    Gallon = r.Gallon,
                    AirlineId = r.Flight.AirlineId ?? 0,


                }).ToList().OrderBy(r => r.Status).ThenBy(r => r.DepartureTime);
            foreach (var item in list)
            {
                item.Others = others.Where(r => r.FlightId == item.FlightId).ToList();

            }
            */
            return list;
        }
        [ResponseType(typeof(RefuelViewModel))]
        public IHttpActionResult GetRefuel(string uniqueId)
        {
            var guid = Guid.Parse(uniqueId);
            if (guid != null)
            {
                var id = db.RefuelItems.Where(r => r.UniqueId == guid).Select(r => r.Id).FirstOrDefault();
                if (id > 0)
                    return GetRefuel(id);

            }
            return NotFound();
        }
        // GET: api/Refuels/5
        [ResponseType(typeof(RefuelViewModel))]
        public IHttpActionResult GetRefuel(int id)
        {
            using (var db = new DataContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                Logger.AppendLog("REFUEL", "Start get item", "refuel");
                string tabletId, appVersion = string.Empty, truckCode = string.Empty;
                if (Request.Headers.Contains("App-Version"))
                {
                    appVersion = Request.Headers.GetValues("App-Version").FirstOrDefault();
                }
                if (Request.Headers.Contains("Truck-Code"))
                {
                    truckCode = Request.Headers.GetValues("Truck-Code").FirstOrDefault();

                }
                if (Request.Headers.Contains("Tablet-Id"))
                {
                    tabletId = Request.Headers.GetValues("Tablet-Id").FirstOrDefault();

                }
                if (!string.IsNullOrEmpty(appVersion))
                {
                    var versionNumber = int.Parse(appVersion.Substring(appVersion.LastIndexOf(".") + 1));
                    if (versionNumber < 42)
                        receipt_v2 = false;

                    Logger.AppendLog("REFUEL", $"Refuel Id {id} Receipt V2 {receipt_v2} Version Number {versionNumber}", "refuel");
                }
                else receipt_v2 = false;
                //db.DisableFilter("IsNotDeleted");

                var refuel = db.RefuelItems.AsNoTracking()
                    .Select(r => new RefuelViewModel
                    {
                        AirlineModel = new AirlineViewModel
                        {
                            Id = r.Flight.Airline.Id,
                            Name = r.Flight.Airline.Name ?? "",
                            Code = r.Flight.Airline.Code ?? "",
                            TaxCode = r.Flight.Airline.TaxCode ?? "",
                            Address = r.Flight.Airline.Address ?? "",

                            InvoiceAddress = r.Flight.Airline.InvoiceAddress,
                            InvoiceName = r.Flight.Airline.InvoiceName,
                            InvoiceTaxCode = r.Flight.Airline.InvoiceTaxCode,
                            IsInternational = r.Flight.Airline.AirlineType == 1,
                            Unit = (int)r.Flight.Airline.Unit
                        },
                        FlightStatus = r.Flight.Status,
                        FlightId = r.FlightId,
                        FlightCode = r.Flight.Code,
                        FlightType = r.Flight.FlightType,
                        EstimateAmount = r.Flight.EstimateAmount,

                        Id = r.Id,
                        AircraftType = r.Flight.AircraftType,
                        AircraftCode = r.Flight.AircraftCode,
                        ParkingLot = r.Flight.Parking,
                        RouteName = r.Flight.RouteName,
                        Status = r.Status,
                        ArrivalTime = r.Flight.ArrivalScheduledTime == null || r.Flight.ArrivalScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, -60).Value : r.Flight.ArrivalScheduledTime.Value,
                        DepartureTime = r.Flight.DepartureScheduledTime == null || r.Flight.DepartureScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, 60).Value : r.Flight.DepartureScheduledTime.Value,
                        RefuelTime = r.Flight.RefuelScheduledTime,

                        RealAmount = (r.OriginalGallon ?? 0) > 0 ? (decimal)r.OriginalGallon : r.Amount,

                        Gallon = (r.OriginalGallon ?? 0) > 0 ? (decimal)r.OriginalGallon : r.Amount,

                        Volume = r.Volume ?? 0,

                        StartTime = r.Status != REFUEL_ITEM_STATUS.NONE && r.StartTime.Year < 9999 ? r.StartTime : DateTime.Now,
                        EndTime = r.EndTime ?? DateTime.Now,
                        StartNumber = r.StartNumber,
                        EndNumber = r.EndNumber,
                        DeviceEndTime = r.DeviceEndTime,
                        DeviceStartTime = r.DeviceStartTime,
                        Density = r.Density,
                        ManualTemperature = r.ManualTemperature,
                        Temperature = r.Temperature,
                        QualityNo = r.QCNo,
                        TaxRate = r.TaxRate,
                        Price = r.Price,
                        Currency = r.Currency,
                        Unit = r.Status == REFUEL_ITEM_STATUS.DONE ? (r.Unit ?? 0) : r.Flight.Airline.Unit,
                        TruckId = r.TruckId,
                        TruckNo = r.Truck.Code,

                        AirlineId = r.Flight.AirlineId ?? 1,
                        AirlineType = r.Flight.Airline == null ? 0 : r.Flight.Airline.AirlineType,

                        AirportId = r.Flight.AirportId,

                        RefuelItemType = r.RefuelItemType,

                        ReturnAmount = r.ReturnAmount,
                        ReturnUnit = r.ReturnUnit ?? RETURN_UNIT.KG,
                        WeightNote = r.WeightNote ?? (r.TechLog != null ? SqlFunctions.StringConvert(r.TechLog).Trim() : ""),
                        InvoiceNumber = r.InvoiceNumber,
                        ReturnInvoiceNumber = r.ReturnInvoiceNumber,

                        DriverId = r.DriverId ?? 0,
                        DriverName = r.DriverId == null ? "" : r.Driver.FullName,
                        OperatorId = r.OperatorId ?? 0,
                        OperatorName = r.OperatorId == null ? "" : r.Operator.FullName,

                        IsInternational = r.Flight.FlightType == FLIGHT_TYPE.OVERSEA,
                        Completed = r.Completed,
                        Printed = r.Printed,
                        InvoiceNameCharter = (r.Flight.InvoiceNameCharter ?? "").Trim() == "" ? r.Flight.Airline.Name : r.Flight.InvoiceNameCharter.Trim(),
                        InvoiceFormId = r.InvoiceFormId,
                        PrintTemplate = !r.Printed ? (r.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && r.Flight.Airline.AirlineType == 0 ? PRINT_TEMPLATE.BILL : PRINT_TEMPLATE.INVOICE) : r.PrintTemplate,

                        BM2508Result = r.BM2508Result,
                        UniqueId = r.UniqueId.ToString(),
                        ReceiptCount = r.ReceiptCount,
                        ReceiptNumber = r.Receipt != null ? r.Receipt.Number : (r.Printed || receipt_v2) ? r.ReceiptNumber : "",
                        ReceiptUniqueId = r.ReceiptUniqueId,
                        Exported = r.Exported,
                        DateUpdated = r.DateUpdated

                    }).FirstOrDefault(r => r.Id == id);


                Logger.AppendLog("REFUEL", "End get item", "refuel");
                if (refuel == null)
                {
                    return NotFound();
                }

                var trucks = (from r in db.RefuelItems
                              from t in db.Trucks
                              where t.Id == r.TruckId && r.FlightId == refuel.FlightId
                              select new { t.Id, r.Status, r.Amount, t.CurrentAmount }).ToList();
                var refueledAmount = trucks.Where(t => t.Status == REFUEL_ITEM_STATUS.DONE).Sum(t => t.Amount);
                var availAmount = trucks.Where(t => t.Status != REFUEL_ITEM_STATUS.DONE && t.Id != refuel.TruckId).Sum(t => t.CurrentAmount) + trucks.FirstOrDefault(t => t.Id == refuel.TruckId).CurrentAmount;

                Logger.AppendLog("REFUEL", "End get trucks info", "refuel");
                refuel.IsAlert = availAmount < refuel.EstimateAmount - refueledAmount;

                /// get the price 
                if (refuel.Status != REFUEL_ITEM_STATUS.DONE)
                {
                    var airport = db.Airports.FirstOrDefault(a => a.Id == refuel.AirportId);
                    var price = db.ProductPrices.Where(p => p.StartDate <= DateTime.Now)
                        .OrderByDescending(p => p.StartDate)
                        .FirstOrDefault(p => p.CustomerId == refuel.AirlineId);
                    if (price == null)
                        price = db.ProductPrices.Where(p => p.StartDate <= DateTime.Now && p.BranchId == (int)airport.Branch && p.DepotType == airport.DepotType && p.Unit == (int)refuel.Unit && refuel.AirlineType == 1)
                       .OrderByDescending(p => p.StartDate)
                       .FirstOrDefault(p => p.AirlineType == (refuel.IsInternational ? 1 : 0));

                    if (price == null)
                        price = db.ProductPrices.OrderByDescending(p => p.StartDate)
                            .FirstOrDefault(p => p.StartDate <= DateTime.Now && p.Unit == (int)refuel.Unit && refuel.AirlineType == 0);
                    if (price != null)
                    {
                        refuel.Price = price.Price;
                        refuel.Currency = price.Currency;
                    }
                }

                //if (refuel.FlightStatus == FlightStatus.REFUELED)
                //{
                refuel.Others = db.RefuelItems//.Include(r => r.Truck)
                    .Where(r => r.FlightId == refuel.FlightId && r.Id != refuel.Id && r.RefuelItemType == refuel.RefuelItemType).OrderBy(r => r.StartTime)
                       .Select(r => new RefuelViewModel
                       {
                           FlightStatus = r.Flight.Status,
                           FlightId = r.FlightId,
                           FlightCode = r.Flight.Code,
                           FlightType = r.Flight.FlightType,
                           EstimateAmount = r.Flight.EstimateAmount,

                           Id = r.Id,
                           AircraftType = r.Flight.AircraftType,
                           AircraftCode = r.Flight.AircraftCode,
                           ParkingLot = r.Flight.Parking,
                           RouteName = r.Flight.RouteName,
                           Status = r.Status,
                           ArrivalTime = r.Flight.ArrivalScheduledTime == null || r.Flight.ArrivalScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, -60).Value : r.Flight.ArrivalScheduledTime.Value,
                           DepartureTime = r.Flight.DepartureScheduledTime == null || r.Flight.DepartureScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, 60).Value : r.Flight.DepartureScheduledTime.Value,
                           RefuelTime = r.Flight.RefuelScheduledTime,
                           RealAmount = r.OriginalGallon ?? 0,

                           Gallon = r.OriginalGallon ?? 0,

                           Volume = r.Volume ?? 0,

                           StartTime = r.Status != REFUEL_ITEM_STATUS.NONE && r.StartTime.Year < 9999 ? r.StartTime : DateTime.Now,
                           EndTime = r.EndTime ?? DateTime.Now,
                           StartNumber = r.StartNumber,
                           EndNumber = r.EndNumber,
                           DeviceEndTime = r.DeviceEndTime,
                           DeviceStartTime = r.DeviceStartTime,
                           Density = r.Density,
                           ManualTemperature = r.ManualTemperature,
                           Temperature = r.Temperature,
                           QualityNo = r.QCNo,
                           TaxRate = r.TaxRate,
                           Price = r.Price,
                           Currency = r.Currency,
                           Unit = r.Status == REFUEL_ITEM_STATUS.DONE ? (r.Unit ?? 0) : r.Flight.Airline.Unit,
                           TruckId = r.TruckId,
                           TruckNo = r.Truck.Code,

                           AirlineId = r.Flight.AirlineId ?? 1,
                           AirlineType = r.Flight.Airline == null ? 0 : r.Flight.Airline.AirlineType,

                           AirportId = r.Flight.AirportId,

                           RefuelItemType = r.RefuelItemType,

                           ReturnAmount = r.ReturnAmount,
                           ReturnUnit = r.ReturnUnit ?? RETURN_UNIT.KG,
                           WeightNote = r.WeightNote ?? (r.TechLog != null ? SqlFunctions.StringConvert(r.TechLog).Trim() : ""),
                           InvoiceNumber = r.InvoiceNumber,

                           DriverId = r.DriverId ?? 0,
                           DriverName = r.DriverId == null ? "" : r.Driver.FullName,
                           OperatorId = r.OperatorId ?? 0,
                           OperatorName = r.OperatorId == null ? "" : r.Operator.FullName,

                           IsInternational = r.Flight.FlightType == FLIGHT_TYPE.OVERSEA,
                           Completed = r.Completed,
                           Printed = r.Printed,
                           InvoiceNameCharter = (r.Flight.InvoiceNameCharter ?? "").Trim() == "" ? r.Flight.Airline.Name : r.Flight.InvoiceNameCharter.Trim(),
                           InvoiceFormId = r.InvoiceFormId,
                           PrintTemplate = !r.Printed ? (r.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && r.Flight.Airline.AirlineType == 0 ? PRINT_TEMPLATE.BILL : PRINT_TEMPLATE.INVOICE) : r.PrintTemplate,

                           UniqueId = r.UniqueId.ToString(),
                           ReceiptCount = r.ReceiptCount,
                           ReceiptNumber = r.Receipt != null ? r.Receipt.Number : (r.Printed || receipt_v2) ? r.ReceiptNumber : "",
                           ReceiptUniqueId = r.ReceiptUniqueId,
                           DateUpdated = r.DateUpdated

                       }).ToList();
                //}
                Logger.AppendLog("REFUEL", "End get others", "refuel");

                return Ok(refuel); 
            }
        }

        // PUT: api/Refuels/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutRefuel(int id, Refuel refuel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != refuel.Id)
            {
                return BadRequest();
            }

            db.Entry(refuel).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RefuelExists(id))
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
        [Authorize]
        // POST: api/Refuels
        [ResponseType(typeof(RefuelViewModel))]
        public IHttpActionResult PostRefuel(RefuelViewModel refuel)
        {
            Logger.AppendLog("POST", refuel.TruckNo + " " + refuel.FlightCode,"refuel");

            using (var db = new DataContext())
                using(var dbTransaction = db.Database.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                


                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

                var userName = ClaimsPrincipal.Current.Identity.Name;

                var user = db.Users.FirstOrDefault(u => u.UserName == userName);

                var userId = user != null ? user.Id : 0;

                var fileName = string.IsNullOrEmpty(refuel.TruckNo) ? "post" : refuel.TruckNo;

                var deviceId = string.Empty;

                if (Request.Headers.Contains("Tablet-Id"))
                {
                    var tabletId = Request.Headers.GetValues("Tablet-Id").FirstOrDefault();
                    var appVersion = Request.Headers.GetValues("App-Version").FirstOrDefault();
                    var truckId = Request.Headers.GetValues("Truck-Id").FirstOrDefault();
                    deviceId = tabletId;
                    Logger.AppendLog("POST", String.Format("Truck-Id: {2} Tablet-Id: {0} App-Version:{1}  ", tabletId, appVersion, truckId), fileName);
                }
                Logger.AppendLog("POST", String.Format("Truck No: {0} User Id: {1} User Name: {2}", refuel.TruckNo, userId, user == null ? "" : user.UserName), fileName);
                Logger.AppendLog("POST", "Start post " + refuel.Id.ToString() + " - " + refuel.UniqueId.ToString(), fileName);
                Logger.AppendLog("POST", "Flight code " + refuel.FlightCode, fileName);


                Logger.AppendLog("POST", JsonConvert.SerializeObject(refuel), fileName + "-data");


                var airportId = user != null ? user.AirportId : 0;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var guid = Guid.Empty;
                if (!string.IsNullOrEmpty(refuel.UniqueId))
                    Guid.TryParse(refuel.UniqueId, out guid);
                var count = db.RefuelItems.Where(r => r.UniqueId == guid).Count();

                bool isOK = false; int tryCount = 1;
                while (!isOK && tryCount++<5)
                {
                    try
                    {
                        db.DisableFilter("IsNotDeleted");
                        if (refuel.Id == 0)
                        {
                            var check = db.RefuelItems.FirstOrDefault(r => r.TruckId == refuel.TruckId
                                && (refuel.FlightId == r.FlightId || refuel.FlightCode == r.Flight.Code) && r.UniqueId == guid
                                && r.RefuelItemType == refuel.RefuelItemType
                                && DbFunctions.DiffHours(r.StartTime, refuel.StartTime) < 1
                                && DbFunctions.DiffHours(r.Flight.ArrivalTime, refuel.ArrivalTime) < 1);
                            if (check != null)
                            {
                                refuel.Id = check.Id;
                                refuel.FlightId = check.FlightId;

                            }

                            if (refuel.FlightId == 0)
                            {
                                Flight fl = db.Flights.FirstOrDefault(f => f.Code.Equals(refuel.FlightCode) && f.RefuelScheduledTime == refuel.RefuelTime);
                                if (fl == null)
                                //insert new flight
                                {
                                    fl = new Flight
                                    {
                                        Code = refuel.FlightCode ?? "N/A",
                                        AircraftCode = refuel.AircraftCode,

                                        Parking = refuel.ParkingLot,
                                        RouteName = refuel.RouteName,
                                        AircraftType = refuel.AircraftType,
                                        ArrivalTime = refuel.ArrivalTime,// DateTime.Today.Add(refuel.ArrivalTime.TimeOfDay),
                                        DepartuteTime = refuel.DepartureTime,//DateTime.Today.Add(refuel.DepartureTime.TimeOfDay),
                                        ArrivalScheduledTime = refuel.ArrivalTime, //DateTime.Today.Add(refuel.ArrivalTime.TimeOfDay),
                                        DepartureScheduledTime = refuel.DepartureTime,// DateTime.Today.Add(refuel.DepartureTime.TimeOfDay),
                                        RefuelScheduledTime = refuel.RefuelTime,//DateTime.Today.Add(refuel.RefuelTime.Value.TimeOfDay),
                                        EstimateAmount = refuel.EstimateAmount,
                                        RefuelTime = refuel.RefuelTime,
                                        StartTime = refuel.RefuelTime.Value,
                                        EndTime = refuel.RefuelTime.Value,
                                        AirportId = user.AirportId,
                                        CreatedLocation = FLIGHT_CREATED_LOCATION.APP,
                                        UserCreatedId = user.Id,
                                        //LastUpdateDevice = deviceId

                                    };

                                    var route = refuel.RouteName ?? "";
                                    var routes = route.Split(new char[] { '-', '_' });
                                    if (routes.Length == 2)
                                    {
                                        fl.FlightType = db.Airports.Where(a => routes.Contains(a.Code)).Count() < 2 ? FLIGHT_TYPE.OVERSEA : FLIGHT_TYPE.DOMESTIC;
                                    }
                                    else
                                        fl.FlightType = FLIGHT_TYPE.OVERSEA;

                                    db.Flights.Add(fl);

                                    Logger.AppendLog("POST", "Save new flight", fileName);
                                    db.SaveChanges();
                                }
                                refuel.FlightId = fl.Id;
                            }
                        }

                        var truck = db.Trucks.OrderBy(t => t.IsDeleted).FirstOrDefault(t => t.Id == refuel.TruckId);
                        if (truck == null)
                            db.Trucks.OrderBy(t => t.IsDeleted).FirstOrDefault(t => t.Code.Equals(refuel.TruckNo));

                        var truckId = truck == null ? 0 : truck.Id;

                        
                        // find existing item
                        var model = db.RefuelItems.Include(r => r.Flight).FirstOrDefault(r => r.UniqueId == guid);

                        if (model == null || count > 1)
                            model = db.RefuelItems.Include(r => r.Flight).FirstOrDefault(r => r.Id == refuel.Id);

                        if (model != null)
                        {
                            ///assigned but deleted, recall it.
                            if (model.IsDeleted)
                            {
                                var mModel = db.RefuelItems.Include(r => r.Flight).FirstOrDefault(r => r.TruckId == truckId && r.FlightId == model.FlightId && (r.Status != REFUEL_ITEM_STATUS.DONE || r.StartTime == refuel.StartTime));
                                if (mModel != null)
                                {
                                    model.DateUpdated = DateTime.Now;
                                    db.SaveChanges();
                                    model = mModel;
                                }
                                if (refuel.Status != REFUEL_ITEM_STATUS.NONE)
                                    model.IsDeleted = false;

                            }
                            //wrong truck id
                            if (model.TruckId != truckId)
                            {

                                Logger.AppendLog("POST", "Different Truck DBTruckId: " + model.TruckId.ToString() + " App TruckId: " + refuel.TruckId.ToString(), fileName);
                                var oModel = db.RefuelItems.Include(r => r.Flight).FirstOrDefault(r =>
                                    (r.Status != REFUEL_ITEM_STATUS.DONE || r.StartTime == refuel.StartTime)
                                    && r.TruckId == refuel.TruckId
                                    && r.FlightId == model.FlightId
                                    && !r.IsDeleted);
                                if (oModel != null)
                                {
                                    if (model.Status == REFUEL_ITEM_STATUS.DONE)
                                        model = oModel;
                                    else
                                    {
                                        oModel.TruckId = model.TruckId;
                                        model.TruckId = truckId;
                                    }
                                }
                                else if (model.Status == REFUEL_ITEM_STATUS.DONE || model.Status == REFUEL_ITEM_STATUS.PROCESSING)
                                {
                                    Logger.AppendLog("POST", "Create new item because of different trucks", fileName);
                                    model = new RefuelItem
                                    {
                                        FlightId = refuel.FlightId,
                                        UserCreatedId = user.Id,
                                        CreatedLocation = ITEM_CREATED_LOCATION.APP,
                                        DateCreated = DateTime.Now,
                                        DateUpdated = DateTime.Now,
                                        StartTime = DateTime.Now,
                                        TruckId = refuel.TruckId,
                                        Unit = refuel.Unit,
                                        Currency = refuel.Currency,
                                        RefuelItemType = refuel.RefuelItemType,
                                        UniqueId = Guid.NewGuid(),


                                    };

                                    db.RefuelItems.Add(model);
                                }

                            }



                            if (truck != null)
                            {
                                model.TruckId = truckId;
                            }
                            else
                                model.TruckId = refuel.TruckId;
                        }

                        //if item not exist, create new
                        if (model == null)
                        {
                            Logger.AppendLog("POST", "Create new item because of null DB item", fileName);
                            model = new RefuelItem
                            {
                                FlightId = refuel.FlightId,
                                UserCreatedId = user.Id,
                                CreatedLocation = ITEM_CREATED_LOCATION.APP,
                                DateCreated = DateTime.Now,
                                DateUpdated = DateTime.Now,
                                StartTime = DateTime.Now,
                                TruckId = refuel.TruckId,
                                Unit = refuel.Unit,
                                Currency = refuel.Currency,
                                RefuelItemType = refuel.RefuelItemType,

                                UniqueId = refuel.Id == 0 ? guid : Guid.NewGuid()

                            };

                            db.RefuelItems.Add(model);
                        }
                        if (model != null)
                        {
                            if (!(model.Status == REFUEL_ITEM_STATUS.DONE && refuel.Status != REFUEL_ITEM_STATUS.DONE))
                            {

                                if (model.DriverId == null && model.OperatorId == null)
                                {
                                    var now = DateTime.Now.TimeOfDay;// DbFunctions.CreateTime(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);


                                    var qshift = db.Shifts.Where(s => (s.StartTime < s.EndTime && DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)
                                                                    || (s.StartTime > s.EndTime && (DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) <= now
                                                                                                    || DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) >= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)))

                                        .Where(s => s.AirportId == airportId);
                                    var shift = qshift.FirstOrDefault();
                                    var start = DateTime.Today;
                                    var end = DateTime.Today;
                                    var truckAssign = db.TruckAssigns.FirstOrDefault(t => t.ShiftId == shift.Id && t.TruckId == truckId);
                                    model.DriverId = truckAssign != null ? truckAssign.DriverId : (int?)null;
                                    model.OperatorId = truckAssign != null ? truckAssign.TechnicalerId : (int?)null;
                                }

                                model.OriginalDensity = refuel.Density;
                                model.OriginalTemperature = refuel.ManualTemperature;

                                //if (!(model.Exported ?? false))
                                //{
                                model.Temperature = refuel.Temperature;
                                model.ManualTemperature = refuel.ManualTemperature;
                                model.Density = refuel.Density;


                                //}
                                model.WeightNote = refuel.WeightNote;

                                //Techlog
                                if (!string.IsNullOrEmpty(refuel.WeightNote))
                                {
                                    decimal d = 0;
                                    decimal.TryParse(refuel.WeightNote, out d);
                                    model.TechLog = d;
                                }
                                else if (refuel.TechLog > 0)
                                {
                                    model.TechLog = refuel.TechLog;
                                    model.WeightNote = refuel.TechLog.ToString("#");
                                }

                               
                                    model.OriginalGallon = Math.Round(refuel.RealAmount, 0, MidpointRounding.AwayFromZero);


                                    //if (!(model.Exported ?? false))
                                    //{
                                    model.Gallon = Math.Round(refuel.RealAmount, 0, MidpointRounding.AwayFromZero);
                                    model.Amount = Math.Round(refuel.RealAmount, 0, MidpointRounding.AwayFromZero);
                                    model.Volume = refuel.Volume;// Math.Round(refuel.RealAmount * RefuelItem.GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero);
                                    if (model.Volume == 0)
                                        model.Volume = Math.Round(refuel.RealAmount * RefuelItem.GALLON_TO_LITTER, 0, MidpointRounding.AwayFromZero);
                                    model.Weight = Math.Round((model.Volume ?? 0) * model.Density, 0, MidpointRounding.AwayFromZero);
                                    //}


                                    model.EndNumber = refuel.EndNumber;
                                    model.StartNumber = refuel.StartNumber;// model.EndNumber - model.Amount;
                                    model.Completed = refuel.Completed;


                                    if (refuel.StartTime > System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                                        model.StartTime = refuel.StartTime;

                                    if (refuel.EndTime > System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                                    {
                                        if (refuel.Status == REFUEL_ITEM_STATUS.DONE)
                                        {
                                            model.EndTime = refuel.EndTime;

                                        }
                                    }
                                    else
                                        model.EndTime = null;


                                    model.Price = refuel.Price;
                                    model.Currency = refuel.Currency;
                                    model.Unit = refuel.Unit;

                                    model.TaxRate = refuel.TaxRate;

                                    if (refuel.DeviceStartTime != null && !string.IsNullOrEmpty(refuel.DeviceStartTime.ToString()) && refuel.DeviceStartTime > System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                                        model.DeviceStartTime = refuel.DeviceStartTime;
                                    if (refuel.DeviceEndTime != null && !string.IsNullOrEmpty(refuel.DeviceEndTime.ToString()) && refuel.DeviceEndTime > System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                                        model.DeviceEndTime = refuel.DeviceEndTime;



                                


                                model.QCNo = refuel.QualityNo;
                                //if (model.Id > 0)
                                //    model.UniqueId = Guid.Parse(refuel.UniqueId);


                                model.DateUpdated = DateTime.Now;
                                model.UserUpdatedId = userId;
                                model.RefuelItemType = refuel.RefuelItemType;

                                model.ReturnAmount = refuel.ReturnAmount;
                                model.ReturnUnit = refuel.ReturnUnit;



                                model.ReturnInvoiceNumber = refuel.ReturnInvoiceNumber;

                                model.Printed = refuel.Printed;
                                model.Status = refuel.Status;

                                model.BM2508Result = refuel.BM2508Result;

                                if (refuel.Printed)
                                {

                                    model.ReceiptCount = refuel.ReceiptCount;

                                    model.ReceiptNumber = refuel.ReceiptNumber;

                                    model.ReceiptUniqueId = refuel.ReceiptUniqueId;

                                    model.InvoiceNumber = refuel.InvoiceNumber?? model.InvoiceNumber;
                                    model.PrintTemplate = refuel.PrintTemplate;
                                    if (refuel.InvoiceFormId > 0)
                                        model.InvoiceFormId = refuel.InvoiceFormId;
                                }
                                else if (refuel.Status == REFUEL_ITEM_STATUS.DONE)
                                {
                                    var defaultForm = db.InvoiceForms.FirstOrDefault(pr => pr.AirportId == airportId && (int)pr.InvoiceType == (int)refuel.PrintTemplate && pr.IsDefault && !pr.IsDeleted);
                                    if (defaultForm == null)
                                        defaultForm = db.InvoiceForms.OrderByDescending(pr => pr.StartDate).FirstOrDefault(pr => pr.AirportId == airportId && (int)pr.InvoiceType == (int)refuel.PrintTemplate && !pr.IsDeleted);
                                    if (defaultForm != null)
                                        model.InvoiceFormId = defaultForm.Id;
                                }

                                if (refuel.DriverId > 0)
                                    model.DriverId = refuel.DriverId;
                                if (refuel.OperatorId > 0)
                                    model.OperatorId = refuel.OperatorId;


                                //Price is automatic, re-calculate
                                // stop calculate price on 2022/07/01

                                if (false && !refuel.ChangeFlag.HasFlag(RefuelViewModel.CHANGE_FLAG.PRICE) && !refuel.Printed)
                                {

                                    var airport = db.Airports.FirstOrDefault(a => a.Id == airportId);

                                    var prices = (from p in db.ProductPrices.Include(p => p.Product)
                                                  where p.StartDate <= refuel.EndTime // && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch
                                                  group p by new { p.CustomerId, p.AirlineType, p.BranchId, p.DepotType, p.Unit }
                                         into groups
                                                  select groups.OrderByDescending(g => g.StartDate).FirstOrDefault()).ToList();

                                    var airlineType = db.Airlines.Where(a => a.Id == refuel.AirlineId).Select(a => a.AirlineType).FirstOrDefault() ?? 0;
                                    if (airlineType == 0)
                                    {

                                        var price = prices.OrderByDescending(p => p.StartDate)
                                                    .FirstOrDefault(p => p.AirlineType == (int)model.Flight.FlightType && p.CustomerId == refuel.AirlineId);
                                        if (price == null)
                                            price = prices.OrderByDescending(p => p.StartDate)
                                             .FirstOrDefault(p => p.AirlineType == 1 && (p.Unit == (int)refuel.Unit && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch));
                                        if (price != null)
                                        {
                                            model.Price = price.Price;
                                            model.Currency = price.Currency;
                                        }
                                    }
                                    else
                                    {
                                        var price = prices.OrderByDescending(p => p.StartDate)
                                            .FirstOrDefault(p => p.AirlineType == (refuel.IsInternational ? 1 : 0) && p.CustomerId == refuel.AirlineId);
                                        if (price == null)
                                            price = prices.OrderByDescending(p => p.StartDate)
                                        .FirstOrDefault(p => p.AirlineType == (refuel.IsInternational ? 1 : 0) && (p.Unit == (int)refuel.Unit && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch));

                                        if (price != null)
                                        {
                                            model.Price = price.Price;
                                            model.Currency = price.Currency;
                                        }
                                    }

                                }




                            }
                        }


                        var flight = db.Flights.Include(f => f.Airline).Include(f => f.RefuelItems).FirstOrDefault(f => f.Id == model.FlightId);
                        if (flight != null && flight.IsDeleted && refuel.Status != REFUEL_ITEM_STATUS.NONE)
                            flight.IsDeleted = false;

                        if (flight != null && !(model.Status == REFUEL_ITEM_STATUS.DONE && refuel.Status != REFUEL_ITEM_STATUS.DONE))
                        {

                            flight.TotalAmount = flight.RefuelItems.Where(r => r.Status == REFUEL_ITEM_STATUS.DONE && !r.IsDeleted).Sum(r => r.Amount);

                            if (flight.RefuelItems.Where(r => !r.IsDeleted).Any(r => r.Status == REFUEL_ITEM_STATUS.DONE && !string.IsNullOrEmpty(r.InvoiceNumber) && r.InvoiceFormId != null))
                                flight.Status = FlightStatus.REFUELED;
                            else if (flight.RefuelItems.Where(r => !r.IsDeleted).Any(r => r.Status == REFUEL_ITEM_STATUS.DONE || r.Status == REFUEL_ITEM_STATUS.PROCESSING))
                                flight.Status = FlightStatus.REFUELING;
                            if (flight.Status == FlightStatus.REFUELED || flight.Status == FlightStatus.REFUELING)
                            {
                                if (flight.RefuelItems.Where(r => !r.IsDeleted).Any(r => r.Status == REFUEL_ITEM_STATUS.DONE))
                                {
                                    flight.StartTime = flight.RefuelItems.Where(r => r.Status == REFUEL_ITEM_STATUS.DONE).Min(r => r.StartTime);
                                    flight.EndTime = flight.RefuelItems.Where(r => r.Status == REFUEL_ITEM_STATUS.DONE).Max(r => r.EndTime).Value;
                                    flight.RefuelTime = flight.EndTime;
                                }
                            }
                            if (refuel.AirlineId > 0)
                                flight.AirlineId = refuel.AirlineId;
                            if (!string.IsNullOrEmpty(refuel.AircraftCode))
                                flight.AircraftCode = refuel.AircraftCode;
                            if (!string.IsNullOrEmpty(refuel.AircraftType))
                                flight.AircraftType = refuel.AircraftType;

                            flight.RouteName = refuel.RouteName;
                            flight.Parking = refuel.ParkingLot;
                            flight.DateUpdated = DateTime.Now;
                            flight.Price = refuel.Price;

                            flight.UserUpdatedId = userId;
                            flight.FlightType = refuel.IsInternational ? FLIGHT_TYPE.OVERSEA : FLIGHT_TYPE.DOMESTIC;

                            if (!string.IsNullOrWhiteSpace(refuel.InvoiceNameCharter))
                                flight.InvoiceNameCharter = refuel.InvoiceNameCharter.Trim();


                            Logger.AppendLog("POST", "Save flight changes", fileName);

                        }


                        db.SaveChanges();

                        dbTransaction.Commit();
                        isOK = true;
                        var newItem = db.RefuelItems.Where(r => r.Id == model.Id).Select(r => new RefuelViewModel
                        {
                            FlightStatus = r.Flight.Status,
                            FlightId = r.FlightId,
                            FlightCode = r.Flight.Code,
                            FlightType = r.Flight.FlightType,
                            EstimateAmount = r.Flight.EstimateAmount,

                            Id = r.Id,
                            AircraftType = r.Flight.AircraftType,
                            AircraftCode = r.Flight.AircraftCode,
                            ParkingLot = r.Flight.Parking,
                            RouteName = r.Flight.RouteName,
                            Status = r.Status,
                            ArrivalTime = r.Flight.ArrivalScheduledTime == null || r.Flight.ArrivalScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, -60).Value : r.Flight.ArrivalScheduledTime.Value,
                            DepartureTime = r.Flight.DepartureScheduledTime == null || r.Flight.DepartureScheduledTime.Value.Year == 9999 ? DbFunctions.AddMinutes(r.Flight.RefuelScheduledTime, 60).Value : r.Flight.DepartureScheduledTime.Value,
                            RefuelTime = r.Flight.RefuelScheduledTime,
                            RealAmount = r.OriginalGallon ?? 0,

                            Gallon = r.OriginalGallon ?? 0,

                            Volume = r.Volume ?? 0,

                            StartTime = r.Status != REFUEL_ITEM_STATUS.NONE && r.StartTime.Year<9999 ? r.StartTime : DateTime.Now,
                            EndTime = r.EndTime ?? DateTime.Now,
                            StartNumber = r.StartNumber,
                            EndNumber = r.EndNumber,
                            DeviceEndTime = r.DeviceEndTime,
                            DeviceStartTime = r.DeviceStartTime,
                            Density = r.OriginalDensity ?? 0,
                            ManualTemperature = r.OriginalTemperature ?? 0,
                            Temperature = r.Temperature,
                            QualityNo = r.QCNo,
                            TaxRate = r.TaxRate,
                            Price = r.Price,
                            Currency = r.Currency,
                            Unit = r.Status == REFUEL_ITEM_STATUS.DONE ? (r.Unit ?? 0) : r.Flight.Airline.Unit,
                            TruckId = r.TruckId,
                            TruckNo = r.Truck.Code,

                            AirlineId = r.Flight.AirlineId ?? 1,
                            AirlineType = r.Flight.Airline == null ? 0 : r.Flight.Airline.AirlineType,

                            AirportId = r.Flight.AirportId,

                            RefuelItemType = r.RefuelItemType,

                            ReturnAmount = r.ReturnAmount,
                            ReturnUnit = r.ReturnUnit ?? RETURN_UNIT.KG,
                            WeightNote = r.WeightNote ?? (r.TechLog != null ? SqlFunctions.StringConvert(r.TechLog).Trim() : ""),
                            InvoiceNumber = r.InvoiceNumber,
                            ReturnInvoiceNumber = r.ReturnInvoiceNumber,

                            DriverId = r.DriverId ?? 0,
                            DriverName = r.DriverId == null ? "" : r.Driver.FullName,
                            OperatorId = r.OperatorId ?? 0,
                            OperatorName = r.OperatorId == null ? "" : r.Operator.FullName,

                            IsInternational = r.Flight.FlightType == FLIGHT_TYPE.OVERSEA,
                            Completed = r.Completed,
                            Printed = r.Printed,
                            InvoiceNameCharter = (r.Flight.InvoiceNameCharter ?? "").Trim() == "" ? r.Flight.Airline.Name : r.Flight.InvoiceNameCharter.Trim(),
                            InvoiceFormId = r.InvoiceFormId,
                            PrintTemplate = !r.Printed ? (r.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && r.Flight.Airline.AirlineType == 0 ? PRINT_TEMPLATE.BILL : PRINT_TEMPLATE.INVOICE) : r.PrintTemplate,

                            BM2508Result = r.BM2508Result,
                            UniqueId = r.UniqueId.ToString(),
                            ReceiptCount = r.ReceiptCount,
                            ReceiptNumber = r.Receipt != null ? r.Receipt.Number : (r.Printed || receipt_v2) ? r.ReceiptNumber : "",
                            ReceiptUniqueId = r.ReceiptUniqueId,
                            Exported = r.Exported


                        }).FirstOrDefault();


                        db.EnableFilter("IsNotDeleted");
                        
                        Logger.AppendLog("POST", "END Post " + refuel.UniqueId.ToString(), fileName);

                        return Ok(newItem);
                    }
                    catch (Exception ex)
                    {

                        //dbTransaction.Rollback();

                        Logger.AppendLog("POST", "Error: " + ex.Message + ex.StackTrace, fileName);


                        Logger.AppendLog("ERROR", string.Format("truck :{0} flight code: {1} refuel id: {2}", refuel.TruckNo, refuel.FlightCode, refuel.Id), "refuel");
                        Logger.LogException(ex, fileName);

                        foreach (var item in db.GetValidationErrors())
                        {
                            if (!item.IsValid)
                            {

                                foreach (var invalid in item.ValidationErrors)
                                {
                                    Logger.AppendLog("POST", "Error: " + invalid.PropertyName + " - " + invalid.ErrorMessage, fileName);

                                }
                            }

                        }

                        var json = JsonConvert.SerializeObject(refuel) + "\n\n\n";
                        System.IO.File.AppendAllText(HttpContext.Current.Server.MapPath("~/logs/data_error.json"), json);

                    }
                }
                Logger.AppendLog("POST", "return null", fileName);
                return this.NotFound();
            }
        }
        [HttpPost]
        [Route("api/refuels/receipt/{id}")]
        public IHttpActionResult CreateReceipt(int id, string num = "", bool m = false)
        {

            var file = HttpContext.Current.Request.Files.Count > 0 ?
                    HttpContext.Current.Request.Files[0] : null;

            var flight = db.Flights.Include(f => f.Airline).Include(f => f.RefuelItems.Select(r => r.Truck)).FirstOrDefault(f => f.RefuelItems.Any(r=>r.Id == id));

            var list = flight.RefuelItems.Where(r=>r.Id == id).ToList();

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
                CustomerCode = flight.Airline.Code,
                TaxCode = flight.Airline.TaxCode,
                CustomerId = flight.Airline.Id,
                AircraftCode = flight.AircraftCode,
                AircraftType = flight.AircraftType,
                FlightCode = flight.Code,
                FlightId = flight.Id,
                FlightType = (int)flight.FlightType,
                RouteName = flight.RouteName,


                Gallon = list.Sum(r => r.Gallon),
                Volume = (decimal)list.Sum(r => r.Volume),
                Weight = (decimal)list.Sum(r => r.Weight),
                StartTime = list.Min(r => r.StartTime),
                EndTime = list.Min(r => r.EndTime.Value),
                Items = new List<ReceiptItemModel>(),
                Manual = m


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

            var receipt = ReceiptModel.SaveReceipt(model);
            return Ok(receipt);
        }
        //public IHttpActionResult Log(string code, DateTime d)
        //{
        //    var model = db.RefuelItems.FirstOrDefault(r => r.Flight.Code.Equals(code, StringComparison.InvariantCultureIgnoreCase) && DbFunctions.TruncateTime(r.Flight.RefuelScheduledTime) == d.Date);
        //    if (model != null)
        //    {
        //        db.Database.SqlQuery<>("Select * from RefuelItem_Logs where id=" + model.Id.ToString());
        //    }
        //}

        // DELETE: api/Refuels/5
        [ResponseType(typeof(Refuel))]
        public IHttpActionResult DeleteRefuel(int id)
        {
            Refuel refuel = db.Refuels.Find(id);
            if (refuel == null)
            {
                return NotFound();
            }

            db.Refuels.Remove(refuel);
            db.SaveChanges();

            return Ok(refuel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool RefuelExists(int id)
        {
            return db.Refuels.Count(e => e.Id == id) > 0;
        }
    }
}