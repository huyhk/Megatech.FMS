using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using System.IO;
using System.Data.Entity.Migrations;
using Newtonsoft.Json;
using Megatech.FMS.Web.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Configuration;
using EntityFramework.DynamicFilters;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class FlightsController : Controller
    {
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        private DataContext db = new DataContext();
        private User user
        {
            get
            {
                var user = db.Users.Include(u => u.Airport).FirstOrDefault(u => u.UserName == User.Identity.Name);
                return user;
            }
        }
        private int currentUserId
        {
            get
            {
                if (user != null)
                    return user.Id;
                else return 0;
            }
        }

        public string ChangeDate(string date)
        {
            var temp = date.Split('/');
            if (temp.Length > 0)
                return temp[1] + "/" + temp[0] + "/" + temp[2];
            else return date;
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        public ActionResult JFlightItem(int? id, int? order)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();

            var truckAssigns = db.TruckAssigns
               .Include(t => t.Truck)
               .Include(t => t.Driver)
               .Include(t => t.Technicaler)
               .Include(t => t.Shift).AsNoTracking() as IQueryable<TruckAssign>;

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                var u_airportId = user.AirportId;
                if (User.IsInRole("Quản lý miền") && user.Airport != null)
                {
                    var ids = db.Airports.Where(a => a.Branch == user.Airport.Branch).Select(a => a.Id);
                    truckAssigns = truckAssigns.Where(t => ids.Contains((int)t.AirportId));
                    ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && ids.Contains((int)u.AirportId)).OrderBy(a => a.FullName).ToList();
                    ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && ids.Contains((int)u.AirportId)).OrderBy(a => a.FullName).ToList();
                    ViewBag.Trucks = db.Trucks.Where(t => ids.Contains((int)t.AirportId)).OrderBy(a => a.Code).ToList();
                }
                else
                {
                    truckAssigns = truckAssigns.Where(t => t.AirportId == u_airportId);
                    ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId).OrderBy(a => a.FullName).ToList();
                    ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == u_airportId).OrderBy(a => a.FullName).ToList();
                    ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == u_airportId).OrderBy(a => a.Code).ToList();
                }
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "flight" && c.UserUpdatedId == currentUserId).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            }
            else
            {
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).OrderBy(a => a.FullName).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).OrderBy(a => a.FullName).ToList();
                ViewBag.Trucks = db.Trucks.OrderBy(a => a.Code).ToList();
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "flight").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            }

            var model = db.Flights
                .Include(f => f.RefuelItems.Select(ri => ri.Truck))
                .Include(f => f.RefuelItems.Select(ri => ri.Driver))
                .Include(f => f.RefuelItems.Select(ri => ri.Operator))
                .Include(f => f.Airline)
                .FirstOrDefault(r => r.Id == id);
            if (model == null)
                return HttpNotFound();
            model.SortOrder = order;
            ViewBag.TruckAssigns = truckAssigns.ToList();
            return PartialView("_FlightItem", model);
        }

        // GET: Flights
        public ActionResult Index(int p = 1)
        {
            int pageSize = 250;
            if (User.IsInRole("Super Admin") || User.IsInRole("Quản lý tổng công ty") || User.IsInRole("Quản lý miền"))
                pageSize = 20;

            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            FlightSortOrder sortOrder = FlightSortOrder.SORT_ORDER;
            SortDirection direction = SortDirection.DESCENDING;
            if (Request["sortorder"] != null)
                sortOrder = (FlightSortOrder)Enum.Parse(typeof(FlightSortOrder), Request["sortorder"]);
            if (Request["sortdirection"] != null)
                direction = (SortDirection)Enum.Parse(typeof(SortDirection), Request["sortdirection"]);

            var flights = db.Flights
                .Include(f => f.RefuelItems.Select(ri => ri.Truck))
                .Include(f => f.RefuelItems.Select(ri => ri.Driver))
                .Include(f => f.RefuelItems.Select(ri => ri.Operator))
                .Include(f => f.Airline)
                .AsNoTracking() as IQueryable<Flight>;
            int count = 0;// db.Flights.Count();

            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var truckAssigns = db.TruckAssigns
                .Include(t => t.Truck)
                .Include(t => t.Driver)
                .Include(t => t.Technicaler)
                .Include(t => t.Shift).AsNoTracking() as IQueryable<TruckAssign>;

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                var u_airportId = user.AirportId;
                if (User.IsInRole("Quản lý miền") && user.Airport != null)
                {
                    var ids = db.Airports.Where(a => a.Branch == user.Airport.Branch).Select(a => a.Id);
                    flights = flights.Where(f => ids.Contains((int)f.AirportId));
                    truckAssigns = truckAssigns.Where(t => ids.Contains((int)t.AirportId));
                    ViewBag.Airport = db.Airports.FirstOrDefault(a => ids.Contains((int)a.Id));
                    ViewBag.Airports = db.Airports.Where(ar => ids.Contains((int)ar.Id)).OrderBy(a => a.Name).ToList();
                    ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && ids.Contains((int)u.AirportId)).OrderBy(a => a.FullName).ToList();
                    ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && ids.Contains((int)u.AirportId)).OrderBy(a => a.FullName).ToList();
                    ViewBag.Trucks = db.Trucks.Where(t => ids.Contains((int)t.AirportId)).OrderBy(a => a.Code).ToList();
                }
                else
                {
                    flights = flights.Where(f => f.AirportId == u_airportId);
                    truckAssigns = truckAssigns.Where(t => t.AirportId == u_airportId);
                    ViewBag.Airport = db.Airports.FirstOrDefault(a => a.Id == u_airportId);
                    ViewBag.Airports = db.Airports.Where(ar => ar.Id == u_airportId).OrderBy(a => a.Name).ToList();
                    ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId).OrderBy(a => a.FullName).ToList();
                    ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == u_airportId).OrderBy(a => a.FullName).ToList();
                    ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == u_airportId).OrderBy(a => a.Code).ToList();
                }
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "flight" && c.UserUpdatedId == currentUserId).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            }
            else
            {
                ViewBag.Airports = db.Airports.OrderBy(a => a.Name).ToList();
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).OrderBy(a => a.FullName).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).OrderBy(a => a.FullName).ToList();
                ViewBag.Trucks = db.Trucks.OrderBy(a => a.Code).ToList();
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "flight").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            }

            if (!string.IsNullOrEmpty(Request["t"]))
            {
                var truckId = 0;
                int.TryParse(Request["t"], out truckId);
                if (truckId > 0)
                    flights = flights.Where(a => a.RefuelItems.Any(r => r.TruckId == truckId));
            }

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var airportId = 0;
                int.TryParse(Request["a"], out airportId);
                if (airportId > 0)
                    flights = flights.Where(a => a.AirportId == airportId);
                ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == airportId).OrderBy(a => a.Code).ToList();
                truckAssigns = truckAssigns.Where(t => t.AirportId == airportId);
            }

            if (!string.IsNullOrEmpty(Request["d"]))
            {
                var driverId = 0;
                int.TryParse(Request["d"], out driverId);
                if (driverId > 0)
                    flights = flights.Where(f => f.RefuelItems.Any(r => r.DriverId == driverId));
            }

            if (!string.IsNullOrEmpty(Request["o"]))
            {
                var operatorId = 0;
                int.TryParse(Request["o"], out operatorId);
                if (operatorId > 0)
                    flights = flights.Where(f => f.RefuelItems.Any(r => r.OperatorId == operatorId));
            }

            if (!string.IsNullOrEmpty(Request["airline"]))
            {
                var airlineId = 0;
                int.TryParse(Request["airline"], out airlineId);
                if (airlineId > 0)
                    flights = flights.Where(f => f.AirlineId == airlineId);
            }

            if (!string.IsNullOrEmpty(Request["carry"]))
            {
                var carry = Request["carry"];
                if (carry != "-1")
                    flights = flights.Where(f => carry == "1" ? f.FlightCarry == FlightCarry.CCO : carry == "2" ? f.FlightCarry == FlightCarry.CGO : f.FlightCarry == FlightCarry.PAX);
            }

            if (!string.IsNullOrEmpty(Request["keyword"]))
            {
                var keyword = Request["keyword"];
                flights = flights.Where(f => f.AircraftType.Contains(keyword)
                || f.AircraftCode.Contains(keyword)
                || f.Code.Contains(keyword)
                || f.RouteName.Contains(keyword));
            }

            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            //var start_time = fd.Hour * 60 + fd.Minute;
            //var end_time = td.Hour * 60 + td.Minute;

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //{
            //    //count = db.Flights.Count(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year);
            //    flights = flights.Where(f =>
            //    f.RefuelScheduledTime.Value.Day == fd.Day
            //    && f.RefuelScheduledTime.Value.Month == fd.Month
            //    && f.RefuelScheduledTime.Value.Year == fd.Year
            //    && (f.RefuelScheduledTime.Value.Hour * 60 + f.RefuelScheduledTime.Value.Minute) >= start_time
            //    && (f.RefuelScheduledTime.Value.Hour * 60 + f.RefuelScheduledTime.Value.Minute) <= end_time
            //    );

            //    truckAssigns = truckAssigns.Where(t => t.StartDate.Day <= fd.Day
            //    && t.StartDate.Month == fd.Month
            //    && t.StartDate.Year == fd.Year);
            //}
            //else
            //{
            //    td = td.AddHours(23);
            //    //count = db.Flights.Count(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td);
            //    flights = flights.Where(f =>
            //    f.RefuelScheduledTime.Value >= fd
            //    && f.RefuelScheduledTime.Value <= td);

            //    truckAssigns = truckAssigns.Where(t => t.StartDate >= fd
            //    && t.StartDate <= td);
            //}

            //So sánh giờ trong từng ngày
            //flights = flights.Where(f => (f.RefuelScheduledTime.Value.Hour * 60 + f.RefuelScheduledTime.Value.Minute) >= start_time
            //&& (f.RefuelScheduledTime.Value.Hour * 60 + f.RefuelScheduledTime.Value.Minute) <= end_time);

            var fd_cp = new DateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0);
            var td_cp = new DateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0);

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0) >= fd_cp
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0) <= td_cp);

            var fd_1 = fd.AddDays(-1);
            var fd_1_cp = new DateTime(fd_1.Year, fd_1.Month, fd_1.Day, fd_1.Hour, fd_1.Minute, 0); 
            var td_1_cp = new DateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0);

            truckAssigns = truckAssigns.Where(t => DbFunctions.CreateDateTime(t.StartDate.Year, t.StartDate.Month, t.StartDate.Day, t.StartDate.Hour, t.StartDate.Minute, 0) >= fd_1_cp
            && DbFunctions.CreateDateTime(t.StartDate.Year, t.StartDate.Month, t.StartDate.Day, t.StartDate.Hour, t.StartDate.Minute, 0) <= td_1_cp);

            count = flights.Count();

            switch (sortOrder)
            {
                case FlightSortOrder.ArrivalScheduledTime:
                    if (direction == SortDirection.ASCENDING)
                        flights = flights.OrderBy(x => x.ArrivalScheduledTime);
                    else
                        flights = flights.OrderByDescending(x => x.ArrivalScheduledTime);
                    break;

                case FlightSortOrder.DepartureScheduledTime:
                    if (direction == SortDirection.ASCENDING)
                        flights = flights.OrderBy(x => x.DepartureScheduledTime);
                    else
                        flights = flights.OrderByDescending(x => x.DepartureScheduledTime);
                    break;

                case FlightSortOrder.RefuelScheduledTime:
                    if (direction == SortDirection.ASCENDING)
                        flights = flights.OrderBy(x => x.RefuelScheduledTime);
                    else
                        flights = flights.OrderByDescending(x => x.RefuelScheduledTime);
                    break;
                default:
                    flights = flights.OrderBy(x => x.RefuelScheduledTime);
                    break;
            }

            ViewBag.TruckAssigns = truckAssigns.ToList();
            flights = flights.Skip((p - 1) * pageSize).Take(pageSize);
            var list = flights.ToList();
            ViewBag.ItemCount = count;
            //ViewBag.PageModel = new PagingViewModel { PageIndex = p, PageSize = pageSize, TotalRecords = count, Url = Url.Action("Index") };
            ViewBag.Airlines = db.Airlines.ToList();
            ViewBag.AirlineId = new SelectList(db.Airlines.Where(a => a.IsCharter), "Id", "Name");
            return View(list);
        }

        public ActionResult Extract(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            FlightSortOrder sortOrder = FlightSortOrder.SORT_ORDER;
            SortDirection direction = SortDirection.DESCENDING;
            if (Request["sortorder"] != null)
                sortOrder = (FlightSortOrder)Enum.Parse(typeof(FlightSortOrder), Request["sortorder"]);
            if (Request["sortdirection"] != null)
                direction = (SortDirection)Enum.Parse(typeof(SortDirection), Request["sortdirection"]);

            var extract = db.RefuelItems
                .Include(r => r.Truck)
                .Include(r => r.Operator)
                .Include(r => r.Driver)
                .Where(r => r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT)
                .AsNoTracking() as IQueryable<RefuelItem>;
            int count = 0;

            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();


            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                int u_airportId = (int)user.AirportId;
                extract = extract.Where(r => r.Truck.AirportId == u_airportId);
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == u_airportId).ToList();
                ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == u_airportId).ToList();
            }
            else
            {
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).ToList();
                ViewBag.Trucks = db.Trucks.ToList();
            }
            if (Request["d"] != null)
            {
                var driverId = 0;
                int.TryParse(Request["d"], out driverId);
                if (driverId > 0)
                    extract = extract.Where(f => f.DriverId == driverId);
            }

            if (Request["o"] != null)
            {
                var operatorId = 0;
                int.TryParse(Request["o"], out operatorId);
                if (operatorId > 0)
                    extract = extract.Where(f => f.OperatorId == operatorId);
            }

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }
            count = extract.Count();
            var list = extract.ToList();
            ViewBag.ItemCount = count;
            return View(list);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult EditExtract(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            RefuelItem refuelItem = db.RefuelItems.Find(id);
            if (refuelItem == null)
                return HttpNotFound();
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", refuelItem.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", refuelItem.OperatorId);

            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", refuelItem.TruckId);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                int u_airportId = (int)user.AirportId;
                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.DriverId);
                ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.OperatorId);
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == u_airportId), "Id", "Code", refuelItem.TruckId);
            }
            return View(refuelItem);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditExtract([Bind(Include = "Id,TruckId,DriverId,OperatorId,StartTime,EndTime,Amount,ManualTemperature,Density,QCNo,FlightId,DeviceStartTime,DeviceEndTime,EndNumber,StartNumber,Status,Price,TaxRate,Gallon,Temperature,Completed,Printed")] RefuelItem refuelItem)
        {
            //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (ModelState.IsValid)
            {
                var model = db.RefuelItems.FirstOrDefault(r => r.Id == refuelItem.Id);
                TryUpdateModel(model);
                model.TruckId = refuelItem.TruckId;
                model.DriverId = refuelItem.DriverId;
                model.OperatorId = refuelItem.OperatorId;
                model.StartTime = refuelItem.StartTime;
                model.EndTime = refuelItem.EndTime;
                model.Amount = refuelItem.Amount;
                model.ManualTemperature = refuelItem.ManualTemperature;
                model.Density = refuelItem.Density;
                model.QCNo = refuelItem.QCNo;
                db.SaveChanges();
                return Redirect(Request["returnUrl"].ToString());
            }
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", refuelItem.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", refuelItem.OperatorId);

            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", refuelItem.TruckId);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            {
                int u_airportId = (int)user.AirportId;
                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.DriverId);
                ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.OperatorId);
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == u_airportId), "Id", "Code", refuelItem.TruckId);
            }
            return View(refuelItem);
        }
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateExtractAjax([Bind(Include = "Id,TruckId,DriverId,OperatorId,StartTime,EndTime,Amount,ManualTemperature,Density,QCNo")] RefuelItem refuelItem)
        {
            if (Request.IsAjaxRequest() && ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    refuelItem.UserCreatedId = currentUserId;
                refuelItem.RefuelItemType = REFUEL_ITEM_TYPE.EXTRACT;

                db.RefuelItems.Add(refuelItem);
                db.SaveChanges();
                return Json(new { result = "OK" });
            }
            return View(refuelItem);
        }

        #region Create Excel
        private ExcelWorksheet CreateHeader(ExcelWorksheet ws1)
        {
            var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

            using (var rng = ws1.Cells["A:XFD"])
            {
                rng.Style.Font.SetFromFont(new Font("Arial", 10));
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            ///
            /// header property
            /// 
            ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws1.Row(1).Style.Fill.BackgroundColor.SetColor(Color.White);
            ws1.Row(1).Style.Font.Bold = true;
            ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;

            ws1.View.FreezePanes(2, 1);
            var rowfix = 1;
            var colfix = 1;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix++].Value = "Stt";
            ws1.Column(colfix++).Width = 5;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix++].Value = "Thời gian";
            ws1.Column(colfix++).Width = 10;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Số hiệu tàu bay";
            ws1.Column(colfix++).Width = 20;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Số hiệu chuyến bay";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Hãng hàng không";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix++].Value = "Số phiếu hóa nghiệm";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Giờ tra nạp dự kiến";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Giờ bắt đầu tra nạp";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Giờ kết thúc tra nạp";
            ws1.Column(colfix++).Width = 20;


            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Xe tra nạp";
            ws1.Column(colfix++).Width = 25;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Lái xe";
            ws1.Column(colfix++).Width = 25;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Nhân viên kỹ thuật tra nạp";
            ws1.Column(colfix++).Width = 25;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Số đồng hồ bắt đầu";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Số đồng hồ kết thúc";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Lượng nhiên liệu đã nạp";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Nhiệt độ thực tế (độ C)";
            ws1.Column(colfix++).Width = 20;

            if (User.IsInRole("Super Admin"))
            {
                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[1, rowfix++].Value = "Sân bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[1, rowfix++].Value = "Nhiệt độ 2";
                ws1.Column(colfix++).Width = 20;
            }

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Tỷ trọng thực tế (kg/m3)";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Khối lượng (kg)";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Đơn giá";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Tổng tiền thanh toán";
            ws1.Column(colfix++).Width = 20;

            return ws1;
        }
        private ExcelWorksheet GenerateData(ExcelWorksheet ws1, int rowIndexCurrent, List<Flight> list)
        {
            var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());
            var index = 1;
            foreach (var item in list)
            {
                foreach (var ritem in item.RefuelItems.Where(r => r.Status == REFUEL_ITEM_STATUS.DONE))
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime.Value.ToString("dd/MM/yyyy");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftCode;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Code;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Airline != null ? item.Airline.Name : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.QCNo;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime.Value.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.StartTime.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.EndTime.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Truck != null ? ritem.Truck.Code : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Driver != null ? ritem.Driver.FullName : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Operator != null ? ritem.Operator.FullName : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.StartNumber).ToString();
                    //ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.EndNumber).ToString();
                    ws1.Cells[rowIndexCurrent, col++].Value = (Math.Round(ritem.StartNumber) + Math.Round(ritem.Amount)).ToString();
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.Amount).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.ManualTemperature.ToString("#,##0.00");

                    if (User.IsInRole("Super Admin"))
                    {
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Airport.Name;
                        ws1.Cells[rowIndexCurrent, col++].Value = ritem.Temperature.ToString("#,##0.00");
                    }

                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Density.ToString("#,##0.0000");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.Weight).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.Price).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.TotalSalesAmount).ToString("#,##0");
                    rowIndexCurrent++;
                }
            }
            return ws1;
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        public ActionResult FlightExport(string daterange = "", string d = "", string o = "", string airline = "", string Carry = "", string a = "", string keyword = "")
        {
            var name = "Danhsachchuyenbay";
            string fileName = name + ".xlsx";

            var flights = db.Flights
                .Include(f => f.RefuelItems.Select(ri => ri.Truck))
                .Include(f => f.RefuelItems.Select(ri => ri.Driver))
                .Include(f => f.RefuelItems.Select(ri => ri.Operator))
                .Include(f => f.Airline)
                .AsNoTracking() as IQueryable<Flight>;

            //if ((User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp")) && user != null)
            //    flights = flights.Where(f => f.AirportId == user.AirportId);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                var u_airportId = user.AirportId;
                if (User.IsInRole("Quản lý miền") && user.Airport != null)
                {
                    var ids = db.Airports.Where(ap => ap.Branch == user.Airport.Branch).Select(ap => ap.Id);
                    flights = flights.Where(f => ids.Contains((int)f.AirportId));
                }
                else
                {
                    flights = flights.Where(f => f.AirportId == u_airportId);
                }
            }

            if (!string.IsNullOrEmpty(a))
            {
                var airportId = 0;
                int.TryParse(a, out airportId);
                if (airportId > 0)
                    flights = flights.Where(f => f.AirportId == airportId);
            }

            if (!string.IsNullOrEmpty(d))
            {
                var driverId = 0;
                int.TryParse(d, out driverId);
                if (driverId > 0)
                    flights = flights.Where(f => f.RefuelItems.Any(r => r.DriverId == driverId));
            }

            if (!string.IsNullOrEmpty(o))
            {
                var operatorId = 0;
                int.TryParse(o, out operatorId);
                if (operatorId > 0)
                    flights = flights.Where(f => f.RefuelItems.Any(r => r.OperatorId == operatorId));
            }

            if (!string.IsNullOrEmpty(airline))
            {
                var airlineId = 0;
                int.TryParse(airline, out airlineId);
                if (airlineId > 0)
                    flights = flights.Where(f => f.AirlineId == airlineId);
            }

            if (!string.IsNullOrEmpty(Request["carry"]))
            {
                var carry = Request["carry"];
                flights = flights.Where(f => carry == "1" ? f.FlightCarry == FlightCarry.CCO : carry == "2" ? f.FlightCarry == FlightCarry.CGO : f.FlightCarry == FlightCarry.PAX);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                //flights = flights.Where(f => f.AircraftType.ToLower() == keyword.ToLower()
                //|| f.AircraftCode.ToLower() == keyword.ToLower()
                //|| f.Code.ToLower() == keyword.ToLower()
                //|| f.RouteName.ToLower() == keyword.ToLower());

                flights = flights.Where(f => f.AircraftType.Contains(keyword)
                || f.AircraftCode.Contains(keyword)
                || f.Code.Contains(keyword)
                || f.RouteName.Contains(keyword));
            }

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
            );

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //{
            //    flights = flights.Where(f =>
            //    f.RefuelScheduledTime.Value.Day == fd.Day
            //    && f.RefuelScheduledTime.Value.Month == fd.Month
            //    && f.RefuelScheduledTime.Value.Year == fd.Year);
            //}
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f =>
            //    f.RefuelScheduledTime.Value >= fd
            //    && f.RefuelScheduledTime.Value <= td
            //    );
            //}

            var list = flights.OrderBy(f => f.RefuelScheduledTime).ToList();

            var filePath = Path.Combine(AppContext.UploadFolder, "Data");
            var physicalPath = System.Web.HttpContext.Current.Server.MapPath(filePath);
            if (!Directory.Exists(physicalPath))
                Directory.CreateDirectory(physicalPath);

            FileInfo newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws1 = package.Workbook.Worksheets.Add(name);
                var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

                using (var rng = ws1.Cells["A:XFD"])
                {
                    rng.Style.Font.SetFromFont(new Font("Arial", 10));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                ///
                /// header property
                /// 
                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws1.Row(1).Style.Fill.BackgroundColor.SetColor(Color.White);
                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;

                ws1.View.FreezePanes(2, 1);
                var rowfix = 1;
                var colfix = 1;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Stt";
                ws1.Column(colfix++).Width = 5;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Thời gian";
                ws1.Column(colfix++).Width = 5;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Loại tàu bay";
                ws1.Column(colfix++).Width = 10;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Số hiệu tàu bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Số hiệu chuyến bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Đường bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[1, rowfix++].Value = "Sản lượng dự kiến";
                ws1.Column(colfix++).Width = 10;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Giờ hạ cánh dự kiến";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Giờ cất cánh dự kiến";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Giờ tra nạp dự kiến";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Xe tra nạp";
                ws1.Column(colfix++).Width = 25;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Lái xe";
                ws1.Column(colfix++).Width = 25;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Nhân viên tra nạp";
                ws1.Column(colfix++).Width = 25;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Bãi đỗ";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Loại chuyến bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, rowfix++].Value = "Hãng bay";
                ws1.Column(colfix++).Width = 20;

                int rowIndexBegin = 2;
                int rowIndexCurrent = rowIndexBegin;

                var index = 1;
                foreach (var item in list)
                {
                    if (item.RefuelItems.Count > 0)
                    {
                        var t_ids = item.RefuelItems.Select(r => r.TruckId).ToArray();
                        db.DisableFilter("IsNotDeleted");
                        var db_Trucks = db.Trucks.Where(t => t.IsDeleted && t_ids.Contains(t.Id)).ToList();
                        db.EnableFilter("IsNotDeleted");
                        foreach (var ritem in item.RefuelItems)
                        {
                            var col = 1;
                            ws1.Cells[rowIndexCurrent, col++].Value = index++;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime != null ? item.RefuelScheduledTime.Value.ToString("dd/MM/yyyy") : "";
                            ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftType;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftCode;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.Code;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.RouteName;
                            ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.EstimateAmount).ToString("#,##0");
                            ws1.Cells[rowIndexCurrent, col++].Value = (item.ArrivalScheduledTime != null && item.ArrivalScheduledTime.Value.Year != 9999) ? item.ArrivalScheduledTime.Value.ToString("HH:mm") : "";
                            ws1.Cells[rowIndexCurrent, col++].Value = item.DepartureScheduledTime != null ? item.DepartureScheduledTime.Value.ToString("HH:mm") : "";
                            ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime != null ? item.RefuelScheduledTime.Value.ToString("HH:mm") : "";
                            var truck_name = "";
                            if (ritem.Truck != null)
                                truck_name = ritem.Truck.Code;
                            else
                            {
                                var truck_d = db_Trucks.FirstOrDefault(t => t.Id == ritem.TruckId);
                                if (truck_d != null)
                                    truck_name = truck_d.Code;
                            }
                            ws1.Cells[rowIndexCurrent, col++].Value = truck_name;
                            //var driver = "";
                            //foreach (var ritem in item.RefuelItems)
                            //{
                            //    driver += ritem.Driver != null ? ritem.Driver.FullName + " \n" : "";
                            //}
                            ws1.Cells[rowIndexCurrent, col++].Value = ritem.Driver != null ? ritem.Driver.FullName : "";
                            //var oper = "";
                            //foreach (var ritem in item.RefuelItems)
                            //{
                            //    oper += ritem.Operator != null ? ritem.Operator.FullName + " \n" : "";
                            //}
                            ws1.Cells[rowIndexCurrent, col++].Value = ritem.Operator != null ? ritem.Operator.FullName : "";
                            ws1.Cells[rowIndexCurrent, col++].Value = item.Parking;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.FlightCarry == FlightCarry.CCO ? "CCO" : item.FlightCarry == FlightCarry.CGO ? "CGO" : "PAX";
                            ws1.Cells[rowIndexCurrent, col++].Value = item.Airline != null ? item.Airline.Name : "";
                            rowIndexCurrent++;
                        }
                    }
                    else
                    {
                        var col = 1;
                        ws1.Cells[rowIndexCurrent, col++].Value = index++;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime != null ? item.RefuelScheduledTime.Value.ToString("dd/MM/yyyy") : "";
                        ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftType;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftCode;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Code;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.RouteName;
                        ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.EstimateAmount).ToString("#,##0");
                        ws1.Cells[rowIndexCurrent, col++].Value = item.ArrivalScheduledTime != null ? item.ArrivalScheduledTime.Value.ToString("HH:mm") : "";
                        ws1.Cells[rowIndexCurrent, col++].Value = item.DepartureScheduledTime != null ? item.DepartureScheduledTime.Value.ToString("HH:mm") : "";
                        ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime != null ? item.RefuelScheduledTime.Value.ToString("HH:mm") : "";
                        ws1.Cells[rowIndexCurrent, col++].Value = "";
                        ws1.Cells[rowIndexCurrent, col++].Value = "";
                        ws1.Cells[rowIndexCurrent, col++].Value = "";
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Parking;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.FlightCarry == FlightCarry.CCO ? "CCO" : item.FlightCarry == FlightCarry.CGO ? "CGO" : "PAX";
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Airline != null ? item.Airline.Name : "";
                        rowIndexCurrent++;
                    }
                }
                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        #endregion
        public ActionResult AmountReport()
        {
            //        var test =
            //db.Flights.Include(f => f.Airport).GroupBy(f => f.AirportId)
            //.Select(
            //    g => new
            //    {
            //        Key = g.Key,
            //        kh = g.Sum(s => s.EstimateAmount),
            //        th = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.EstimateAmount) : 0,
            //        Name = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
            //    });


            var flights = db.Flights
                .Include(f => f.Airport)
                .AsNoTracking() as IQueryable<Flight>;

            var count = flights.Count();

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    //td = Convert.ToDateTime(ChangeDate(range[1]));
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                ).OrderByDescending(f => f.RefuelScheduledTime);

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Hour == fd.Hour && f.RefuelScheduledTime.Value.Minute == fd.Minute && f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
            //}

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   //TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
               });

            var list = temp.ToList();
            return View(list);
        }

        public ActionResult TotalAmountReport()
        {
            var flights = db.Flights
                .Include(f => f.Airport)
                .Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            //var count = flights.Count();
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    //td = Convert.ToDateTime(ChangeDate(range[1]));
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
            );

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Hour == fd.Hour && f.RefuelScheduledTime.Value.Minute == fd.Minute && f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year);
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td);
            //}

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   //TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
               });

            var list = temp.ToList();

            flights = db.Flights
                .Include(f => f.Airport)
                .AsNoTracking() as IQueryable<Flight>;

            fd = DateTime.Today.AddHours(0).AddMinutes(0);
            td = DateTime.Today.AddHours(23).AddMinutes(59);
            daterange = Request["daterange2"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                //td = Convert.ToDateTime(ChangeDate(range[1]));
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0));

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Hour == fd.Hour && f.RefuelScheduledTime.Value.Minute == fd.Minute && f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year);
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td);
            //}

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   KH_Number2 = g.Count(),
                   //TH_Number2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount2 = g.Sum(s => s.EstimateAmount),
                   //TH_Amount2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
               });
            list.AddRange(temp);
            var tlist = list.OrderBy(a => a.AirportName).GroupBy(a => a.AirportId)
                .Select(
                g => new AmountReport
                {
                    KH_Number = g.Sum(t => t.KH_Number),
                    KH_Number2 = g.Sum(t => t.KH_Number2),

                    TH_Number = g.Sum(t => t.TH_Number),
                    TH_Number2 = g.Sum(t => t.TH_Number2),

                    KH_Amount = g.Sum(t => t.KH_Amount),
                    KH_Amount2 = g.Sum(t => t.KH_Amount2),

                    TH_Amount = g.Sum(t => t.TH_Amount),
                    TH_Amount2 = g.Sum(t => t.TH_Amount2),

                    AirportName = g.FirstOrDefault() != null ? g.FirstOrDefault().AirportName : "",
                    AirportId = g.FirstOrDefault() != null ? g.FirstOrDefault().AirportId : 0,
                }
                );
            return View(tlist.ToList());
        }

        public ActionResult RefuelHistory(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var flights = db.Flights
                .Include(f => f.ParkingLot)
                .Include(f => f.Airline)
                .Include(f => f.RefuelItems.Select(r => r.Truck))
                .Include(f => f.RefuelItems.Select(r => r.Driver))
                .Include(f => f.RefuelItems.Select(r => r.Operator))
                .Where(f => f.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).AsNoTracking() as IQueryable<Flight>;
            var count = flights.Count();

            ViewBag.Airports = db.Airports.ToList();
            if ((User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp")) && user != null)
            {
                int u_airportId = (int)user.AirportId;
                var arp = db.Airports.FirstOrDefault(ar => ar.Id == u_airportId);
                if (arp != null)
                {
                    flights = flights.Where(f => f.AirportId == arp.Id);
                    count = flights.Count();
                }
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == u_airportId).ToList();
            }

            //if (Request["fd"] != null && Request["td"] != null)
            //{
            //var fd = Convert.ToDateTime(ChangeDate(Request["fd"]));
            //var td = Convert.ToDateTime(ChangeDate(Request["td"]));

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                int a = 0;
                int.TryParse(Request["a"], out a);

                ViewBag.Total = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                && f.AirportId == a).Select(f => f.RefuelItems.Sum(r => r.Amount)).DefaultIfEmpty().Sum();

                flights = flights.Where(f =>
                 DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                && f.AirportId == a);
                count = flights.Count();

                //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                //{
                //    ViewBag.Total = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year && f.AirportId == a).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year && f.AirportId == a).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
                //else
                //{
                //    td = td.AddHours(23);
                //    ViewBag.Total = flights.Where(f => f.RefuelScheduledTime >= fd && f.RefuelScheduledTime <= td && f.AirportId == a).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => f.RefuelScheduledTime >= fd && f.RefuelScheduledTime <= td && f.AirportId == a).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
            }
            else
            {
                ViewBag.Total = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                ).ToList().Select(f => f.RefuelItems.Sum(r => r.Amount)).DefaultIfEmpty().Sum();

                flights = flights.Where(f =>
                 DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );
                count = flights.Count();

                //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                //{
                //    ViewBag.Total = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
                //else
                //{
                //    td = td.AddHours(23);
                //    ViewBag.Total = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
            }
            //}
            flights = flights.OrderBy(f => f.RefuelScheduledTime).Skip((p - 1) * pageSize).Take(pageSize);
            var list = flights.ToList();
            ViewBag.ItemCount = count;

            ViewBag.Trucks = db.Trucks.ToList();
            return View(list);
        }
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        public ActionResult Export(string dr, string a)
        {
            var name = "Congtactranap";
            string fileName = name + ".xlsx";

            var flights = db.Flights
                .Include(f => f.ParkingLot)
                .Include(f => f.Airport)
                .Include(f => f.Airline)
                .Include(f => f.RefuelItems.Select(r => r.Truck))
                .Include(f => f.RefuelItems.Select(r => r.Driver))
                .Include(f => f.RefuelItems.Select(r => r.Operator))
                .Where(f => f.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).AsNoTracking() as IQueryable<Flight>;

            if ((User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp")) && user != null)
            {
                var arp = db.Airports.FirstOrDefault(ar => ar.Id == user.AirportId);
                if (arp != null)
                    flights = flights.Where(f => f.AirportId == arp.Id);
            }

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (!string.IsNullOrEmpty(dr))
            {
                var range = dr.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(a))
            {
                int aID = 0;
                int.TryParse(a, out aID);

                flights = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                && f.AirportId == aID);

                //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year && f.AirportId == aID).OrderByDescending(f => f.RefuelScheduledTime);
                //else
                //{
                //    td = td.AddHours(23);
                //    flights = flights.Where(f => f.RefuelScheduledTime >= fd && f.RefuelScheduledTime <= td && f.AirportId == aID).OrderByDescending(f => f.RefuelScheduledTime);
                //}
            }
            else
            {
                flights = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0));

                //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
                //else
                //{
                //    td = td.AddHours(23);
                //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
                //}
            }
            var list = flights.OrderBy(f => f.RefuelScheduledTime).ToList();

            var filePath = Path.Combine(AppContext.UploadFolder, "Data");
            var physicalPath = System.Web.HttpContext.Current.Server.MapPath(filePath);
            if (!Directory.Exists(physicalPath))
                Directory.CreateDirectory(physicalPath);

            FileInfo newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws1 = package.Workbook.Worksheets.Add(name);
                CreateHeader(ws1);

                int rowIndexBegin = 2;
                int rowIndexCurrent = rowIndexBegin;

                GenerateData(ws1, rowIndexCurrent, list);
                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult Copy(string id, string date)
        {

            date = ChangeDate(date);
            int[] ids = id.Split(new char[] { ',' }).Select(s => int.Parse(s)).ToArray();
            var lst = db.Flights.Include(f => f.RefuelItems).Where(a => ids.Contains(a.Id)).ToList();

            var copyDate = Convert.ToDateTime(date);
            var cDate = DateTime.Now;

            lst.ForEach(a =>
            {
                var b = new Flight
                {
                    CreateType = 2,
                    Code = a.Code + "-01",
                    ArrivalScheduledTime = copyDate.AddHours(a.ArrivalScheduledTime != null ? a.ArrivalScheduledTime.Value.Hour : cDate.Hour).AddMinutes(a.ArrivalScheduledTime != null ? a.ArrivalScheduledTime.Value.Minute : cDate.Minute),
                    DepartureScheduledTime = copyDate.AddHours(a.DepartureScheduledTime != null ? a.DepartureScheduledTime.Value.Hour : cDate.Hour).AddMinutes(a.DepartureScheduledTime != null ? a.DepartureScheduledTime.Value.Minute : cDate.Minute),
                    RefuelScheduledTime = copyDate.AddHours(a.RefuelScheduledTime != null ? a.RefuelScheduledTime.Value.Hour : cDate.Hour).AddMinutes(a.RefuelScheduledTime != null ? a.RefuelScheduledTime.Value.Minute : cDate.Minute),
                    ShiftStartTime = copyDate.AddHours(a.ShiftStartTime != null ? a.ShiftStartTime.Value.Hour : cDate.Hour).AddMinutes(a.ShiftStartTime != null ? a.ShiftStartTime.Value.Minute : cDate.Minute),
                    ShiftEndTime = copyDate.AddHours(a.ShiftEndTime != null ? a.ShiftEndTime.Value.Hour : cDate.Hour).AddMinutes(a.ShiftEndTime != null ? a.ShiftEndTime.Value.Minute : cDate.Minute),
                    AircraftCode = a.AircraftCode,
                    AircraftType = a.AircraftType,
                    AirlineId = a.AirlineId,
                    AirportId = a.AirportId,
                    Status = FlightStatus.NONE,
                    Parking = a.Parking,
                    RouteName = a.RouteName,
                    EndTime = DateTime.Now,
                    FlightCarry = a.FlightCarry,
                    StartTime = DateTime.Now,
                    UserCreatedId = currentUserId,
                };

                //// thực hiện lấy thêm phần tra nạp
                //b.RefuelItems = new List<RefuelItem>();
                //b.RefuelItems.AddRange(a.RefuelItems.Select(r => new RefuelItem
                //{
                //    TruckId = r.TruckId,
                //    DriverId = r.DriverId,
                //    OperatorId = r.OperatorId,
                //    StartTime = DateTime.Now
                //}).ToList());

                //b.Status = b.RefuelItems.Count > 0 ? FlightStatus.ASSIGNED : FlightStatus.NONE;
                ////kết thúc phần lấy tra nạp

                //if (db.Flights.Any(f => f.Code == b.Code && DbFunctions.TruncateTime(f.ArrivalScheduledTime.Value) == DbFunctions.TruncateTime(b.ArrivalScheduledTime.Value)))
                //{
                //    b.Id = db.Flights.FirstOrDefault(f => f.Code == b.Code && DbFunctions.TruncateTime(f.ArrivalScheduledTime.Value) == DbFunctions.TruncateTime(b.ArrivalScheduledTime.Value)).Id;
                //    db.Flights.Attach(b);
                //}
                //else
                db.Flights.Add(b);
            });
            db.SaveChanges();
            return Json(new { Status = 0, Message = "OK" });

        }

        // GET: Flights/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return HttpNotFound();
            }
            return View(flight);
        }

        public JsonResult GetById(int? id)
        {
            Flight flight = db.Flights.Find(id);
            return Json(flight, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUserGroup(int? id)
        {
            var id_2 = 0;
            var model = db.UserGroups.FirstOrDefault(f => f.Id_1 == id);
            if (model != null)
                id_2 = model.Id_2;
            return Json(new { Status = id_2, Message = "Đã thay đổi" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUserGroup2(int? id)
        {
            var id_1 = 0;
            var model = db.UserGroups.FirstOrDefault(f => f.Id_2 == id);
            if (model != null)
                id_1 = model.Id_1;
            return Json(new { Status = id_1, Message = "Đã thay đổi" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetByRefuelId(int? id)
        {
            RefuelItem refuelItem = db.RefuelItems.Find(id);
            return Json(refuelItem, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DetailAjax(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return HttpNotFound();
            }
            return Json(new { Status = 0, aircraftType = flight.AircraftType, aircraftCode = flight.AircraftCode });
        }

        // GET: Flights/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult Create()
        {
            //var parkingLots = db.ParkingLots.ToList();
            //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            //if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin"))
            //    parkingLots = parkingLots.Where(p => p.AirportId == (int)user.AirportId).ToList();
            if (user != null)
            {
                var ap = db.Airports.FirstOrDefault(a => a.Id == user.AirportId);
                if (ap != null)
                    ViewBag.AirportCode = ap.Code;
            }

            //ViewBag.AircraftId = new SelectList(db.Aircrafts, "Id", "Name");
            //ViewBag.ArrivalId = new SelectList(db.Airports, "Id", "Name");
            //ViewBag.DepartureId = new SelectList(db.Airports, "Id", "Name");
            //ViewBag.ParkingLotId = new SelectList(parkingLots, "Id", "Name");
            ViewBag.AirlineId = new SelectList(db.Airlines.Where(a => a.IsCharter), "Id", "Name");


            //var context = new ApplicationDbContext();
            //var names = (from u in context.Users
            //             where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
            //             select u.UserName).ToArray();
            //ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).ToList();

            //var names_2 = (from u in context.Users
            //               where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
            //               select u.UserName).ToArray();
            //ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).ToList();

            //DateTime c_day = DateTime.Now;
            //var truck_ids = db.TruckAssigns.Include(t => t.Shift).Where(t => t.StartDate.Day >= c_day.Day && t.StartDate.Month >= c_day.Month && t.StartDate.Year >= c_day.Year).Select(t => t.TruckId);
            //ViewBag.Trucks = db.Trucks.Where(t => truck_ids.Contains(t.Id)).ToList();

            return View();
        }

        // POST: Flights/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,AircraftCode,Code,AirlineId,Parking,RouteName,AircraftType,RefuelScheduledTime,EstimateAmount,ArrivalScheduledTime,DepartureScheduledTime,RefuelScheduledHours,Note,FlightCarry")] Flight flight)
        {
            ViewBag.AirlineId = new SelectList(db.Airlines.Where(a => a.IsCharter), "Id", "Name");
            //var parkingLots = db.ParkingLots.ToList();
            //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            //if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin"))
            //    parkingLots = parkingLots.Where(p => p.AirportId == (int)user.AirportId).ToList();
            var flight_ = db.Flights.FirstOrDefault(f => f.Code.ToLower() == flight.Code.ToLower()
            && f.RefuelScheduledTime.Value.Day == flight.RefuelScheduledTime.Value.Day
            && f.RefuelScheduledTime.Value.Month == flight.RefuelScheduledTime.Value.Month
            && f.RefuelScheduledTime.Value.Year == flight.RefuelScheduledTime.Value.Year
            && f.Status != FlightStatus.REFUELED
            );
            if (flight_ != null)
            {
                Response.Write("<script language=javascript>alert('Số chuyến bay này đã tồn tại.Vui lòng nhập số chuyến khác.');</script>");
                return View();
            }
            else
            {
                if (ModelState.IsValid)
                {
                    var parking = db.ParkingLots.FirstOrDefault(p => p.Id == flight.ParkingLotId);
                    if (parking != null)
                        flight.Parking = parking.Name;

                    flight.Status = (int)FlightStatus.NONE;
                    if (flight.ArrivalScheduledTime is DateTime)
                        flight.ArrivalScheduledTime = flight.RefuelScheduledTime.Value.AddHours(flight.ArrivalScheduledTime.Value.Hour).AddMinutes(flight.ArrivalScheduledTime.Value.Minute);
                    if (flight.DepartureScheduledTime is DateTime)
                        flight.DepartureScheduledTime = flight.RefuelScheduledTime.Value.AddHours(flight.DepartureScheduledTime.Value.Hour).AddMinutes(flight.DepartureScheduledTime.Value.Minute);

                    if (flight.RefuelScheduledTime is DateTime)
                        flight.RefuelScheduledTime = flight.RefuelScheduledTime.Value.AddHours(flight.RefuelScheduledHours.Value.Hour).AddMinutes(flight.RefuelScheduledHours.Value.Minute);

                    //flight.ShiftStartTime = DateTime.Today.AddHours(flight.ShiftStartTime.Value.Hour).AddMinutes(flight.ShiftStartTime.Value.Minute);
                    //flight.ShiftEndTime = DateTime.Today.AddHours(flight.ShiftEndTime.Value.Hour).AddMinutes(flight.ShiftEndTime.Value.Minute);

                    flight.StartTime = flight.RefuelScheduledTime.Value;
                    flight.EndTime = flight.RefuelScheduledTime.Value;
                    flight.RefuelScheduledHours = flight.RefuelScheduledTime.Value;

                    if (user != null)
                        flight.UserCreatedId = user.Id;

                    if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
                        flight.AirportId = user.AirportId;

                    //Quốc nội quốc ngoại dựa vào đường bay và bảng loại sân bay nội địa
                    if (!string.IsNullOrEmpty(flight.RouteName))
                    {
                        var rname = flight.RouteName.Split('-');
                        var at_count = db.AirportTypes.Where(a => rname.Contains(a.Code) && a.Type == FlightType.DOMESTIC).Count();
                        if (at_count > 1)
                            flight.FlightType = FlightType.DOMESTIC;
                        else flight.FlightType = FlightType.OVERSEA;
                    }
                    //end quốc nội quốc ngoại

                    // dựa vào pattern và mã chuyến bay để lấy id hãng bay khi AirlineId > 0
                    if (flight.AirlineId == null)
                    {
                        foreach (var i in db.Airlines)
                        {
                            if (i.Pattern != null && Regex.IsMatch(flight.Code, i.Pattern))
                            {
                                flight.AirlineId = i.Id;
                                break;
                            }
                        }
                    }
                    flight.CreateType = 0;
                    db.Flights.Add(flight);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                //ViewBag.ParkingLotId = new SelectList(parkingLots, "Id", "Name");
                return View(flight);
            }

        }
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult CreateExtract()
        {
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                int u_airportId = (int)user.AirportId;
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == u_airportId).ToList();
                ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == u_airportId).ToList();
            }
            else
            {
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).ToList();
                ViewBag.Trucks = db.Trucks.ToList();
            }
            return View();
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateExtract([Bind(Include = "TruckId,DriverId,OperatorId,StartTime,EndTime,Amount,ManualTemperature,Density,QCNo,FlightId")] RefuelItem refuelItem)
        {
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                int u_airportId = (int)user.AirportId;
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == u_airportId).ToList();
                ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == u_airportId).ToList();
            }
            else
            {
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).ToList();
                ViewBag.Trucks = db.Trucks.ToList();
            }

            if (ModelState.IsValid)
            {
                refuelItem.RefuelItemType = REFUEL_ITEM_TYPE.EXTRACT;

                db.RefuelItems.Add(refuelItem);
                db.SaveChanges();
                Response.Write("<script>window.open('" + (Request["returnUrl"] != null ? Request["returnUrl"].ToString() : "~/Flights/Index") + "','_parent');</script>");
            }
            return View(refuelItem);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult Refuel(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Flight flight = db.Flights.Include(f => f.RefuelItems).FirstOrDefault(f => f.Id == id);
            if (flight == null)
                return HttpNotFound();

            //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();
            if (AppContext.IsSuperAdmin())
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName)).ToList();
            else
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == flight.AirportId).ToList();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();

            if (AppContext.IsSuperAdmin())
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName)).ToList();
            else
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == flight.AirportId).ToList();

            DateTime c_day = DateTime.Now.AddHours(-18);
            var truck_assigns = db.TruckAssigns.Include(t => t.Shift).Where(t => t.Shift.StartTime.Hour <= flight.RefuelScheduledTime.Value.Hour && flight.RefuelScheduledTime.Value.Hour <= t.Shift.EndTime.Hour && t.StartDate >= c_day);
            ViewBag.TruckAssigns = truck_assigns.ToList();
            var truck_id = truck_assigns.Select(t => t.TruckId);

            if (AppContext.IsSuperAdmin())
                ViewBag.Trucks = db.Trucks.Where(t => truck_id.Contains(t.Id)).ToList();
            else
                ViewBag.Trucks = db.Trucks.Where(t => truck_id.Contains(t.Id) && t.AirportId == flight.AirportId).ToList();

            //ViewBag.Refuel = db.Refuels.Include(r => r.Items).OrderByDescending(r => r.Id).FirstOrDefault(r => r.FlightId == id);
            return View(flight);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        public ActionResult JRefuelInfo(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var model = db.RefuelItems
                .Include(r => r.Truck)
                .Include(r => r.Operator)
                .Include(r => r.Driver)
                .Where(r => r.FlightId == id && (r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL || r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT));
            if (model == null)
                return HttpNotFound();
            return PartialView("_RefuelInfo", model);
        }

        [HttpPost]
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Tra nạp")]
        public ActionResult JRefuel(string json, int id = -1)
        {
            try
            {
                var flight = db.Flights.FirstOrDefault(t => t.Id == id);
                flight.TruckName = "";

                if (flight != null && json != null)
                {
                    JObject jsonData = JObject.Parse("{'items':" + json + "}");
                    var refuelItem = (from d in jsonData["items"]
                                      select new RefuelItem { TruckId = d["truckId"].Value<int>(), FlightId = id, StartTime = DateTime.MaxValue, Amount = 0, Completed = false, Printed = false, DateCreated = DateTime.Now, DateUpdated = DateTime.Now, IsDeleted = false, Price = 0, TaxRate = 0, DriverId = d["driverId"].Value<int>(), OperatorId = d["operatorId"].Value<int>() }
                          ).ToList();

                    //Save ChangeLog
                    var entityLog = new ChangeLog();
                    entityLog.EntityName = "Flight";
                    entityLog.EntityDisplay = "Chuyến bay";
                    entityLog.DateChanged = DateTime.Now;
                    entityLog.KeyValues = flight.Code;
                    //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                    if (user != null)
                    {
                        entityLog.UserUpdatedId = currentUserId;
                        entityLog.UserUpdatedName = user.FullName;
                    }
                    foreach (var refu in refuelItem)
                    {
                        var truck_name = "";
                        var truck = db.Trucks.FirstOrDefault(t => t.Id == refu.TruckId);
                        if (truck != null)
                            truck_name = "(" + truck.Code + ")";
                        var oitem = db.RefuelItems.FirstOrDefault(r => r.TruckId == refu.TruckId && r.FlightId == id && r.Status != REFUEL_ITEM_STATUS.DONE);
                        if (oitem != null)
                        {
                            if (oitem.DriverId != refu.DriverId)
                            {
                                var us = db.Users.FirstOrDefault(u => u.Id == oitem.DriverId);
                                if (us != null)
                                    entityLog.OldValues = us.FullName;
                            }
                        }
                        entityLog.PropertyName = "Lái xe" + truck_name;
                        var us_new = db.Users.FirstOrDefault(u => u.Id == refu.DriverId);
                        if (us_new != null)
                            entityLog.NewValues = us_new.FullName;
                        db.ChangeLogs.Add(entityLog);
                        db.SaveChanges();

                        if (oitem != null)
                        {
                            if (oitem.OperatorId != refu.OperatorId)
                            {
                                var us = db.Users.FirstOrDefault(u => u.Id == oitem.OperatorId);
                                if (us != null)
                                    entityLog.OldValues = us.FullName;
                            }
                        }
                        entityLog.PropertyName = "NV tra nạp" + truck_name;
                        us_new = db.Users.FirstOrDefault(u => u.Id == refu.OperatorId);
                        if (us_new != null)
                            entityLog.NewValues = us_new.FullName;
                        db.ChangeLogs.Add(entityLog);
                        db.SaveChanges();
                    }
                    //End Save ChangeLog

                    var refuelItem_Old = db.RefuelItems.Where(f => f.FlightId == id && f.Status != REFUEL_ITEM_STATUS.DONE).ToList();
                    if (refuelItem_Old.Count > 0)
                    {
                        refuelItem_Old.ForEach(f =>
                        {
                            f.IsDeleted = true;
                            f.DateDeleted = DateTime.Now;
                            f.UserDeletedId = currentUserId;
                        });
                        //db.RefuelItems.RemoveRange(refuelItem_Old);
                        db.SaveChanges();
                    }

                    db.RefuelItems.AddRange(refuelItem);
                    db.SaveChanges();
                    //flight.RefuelItems = refuelItem;

                    var ids = refuelItem.Select(a => a.TruckId).ToArray();
                    var trucks = db.Trucks.Where(c => ids.Contains(c.Id)).ToList();
                    flight.TruckName = string.Join(", ", trucks.Select(t => t.Code).ToArray());
                    flight.Status = FlightStatus.ASSIGNED;
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Result = -1;
                ViewBag.Message = ex.Message;
            }
            return Json(new { Status = 0, Message = "Đã phân công" });
            //if (Request.IsAjaxRequest())
            //    return Json(new { status = 0 });
            //return RedirectToAction("Index");
        }

        public ActionResult JApprove(string status, int id = -1)
        {
            try
            {
                var refuel = db.RefuelItems.FirstOrDefault(t => t.Id == id);
                if (refuel != null && !string.IsNullOrEmpty(status))
                {
                    if (status.ToLower() == "none")
                        refuel.ApprovalStatus = ITEM_APPROVE_STATUS.NONE;
                    else if (status.ToLower() == "approved")
                        refuel.ApprovalStatus = ITEM_APPROVE_STATUS.APPROVED;
                    else
                        refuel.ApprovalStatus = ITEM_APPROVE_STATUS.REJECTED;
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Result = -1;
                ViewBag.Message = ex.Message;
            }
            return Json(new { Status = 0, Message = "Đã thay đổi trạng thái" });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Refuel(string json, FormCollection form)
        //{
        //    var id = -1;
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(form["id"]))
        //            id = Convert.ToInt32(form["id"]);

        //        var flight = db.Flights.FirstOrDefault(t => t.Id == id);
        //        flight.Status = FlightStatus.NONE;
        //        flight.TruckName = "";

        //        //Delete Refuels by FlightId
        //        var refu = db.RefuelItems.Where(f => f.FlightId == id).ToList();
        //        foreach (var item in refu) { db.RefuelItems.Remove(item); }
        //        db.SaveChanges();

        //        if (flight != null && !string.IsNullOrEmpty(form["trucks"]))
        //        {
        //            JObject jsonData = JObject.Parse("{'items':" + json + "}");
        //            var refuelItem = (from d in jsonData["items"]
        //                            select new RefuelItem { TruckId = d["id"].Value<int>(), StartTime = DateTime.Now, Amount = 0, Completed = false, Printed = false, DateCreated = DateTime.Now, DateUpdated = DateTime.Now, IsDeleted = false, Price = 0, TaxRate = 0, DriverId = d["driverId"].Value<int>(), OperatorId = d["operatorId"].Value<int>() }
        //                  ).ToList();

        //            var trs = form["trucks"].Split(new char[] { ',' });
        //            var trucks = db.Trucks.Where(c => trs.Contains(c.Id.ToString())).ToList();

        //            //var model = new RefuelItem();
        //            //List<Truck> lstTruck = new List<Truck>();
        //            //lstTruck.AddRange(trucks);

        //            //model.FlightId = flight.Id;
        //            ////model.TotalAmount = 0;
        //            ////model.Price = 0;
        //            //model.StartTime = DateTime.Now;
        //            //model.EndTime = DateTime.Now;
        //            ////model.Status = REFUEL_STATUS.NONE;
        //            //model.DateCreated = DateTime.Now;
        //            //model.DateUpdated = DateTime.Now;
        //            //model.IsDeleted = false;
        //            //model.Items = trucks.Select(t => new RefuelItem { TruckId = t.Id, StartTime = DateTime.Now, Amount = 0, Completed = false, Printed = false, DateCreated = DateTime.Now, DateUpdated = DateTime.Now, IsDeleted = false }).ToList();

        //            flight.RefuelItems = trucks.Select(t => new RefuelItem { TruckId = t.Id, StartTime = DateTime.Now, Amount = 0, Completed = false, Printed = false, DateCreated = DateTime.Now, DateUpdated = DateTime.Now, IsDeleted = false, Price = 0, TaxRate = 0 }).ToList();
        //            //db.RefuelItems.Add(model);
        //            //db.SaveChanges();

        //            flight.TruckName = string.Join(", ", trucks.Select(t => t.Code).ToArray());
        //            flight.Status = FlightStatus.ASSIGNED;
        //        }
        //        db.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.Result = -1;
        //        ViewBag.Message = ex.Message;
        //    }
        //    //}
        //    //ViewBag.Trucks = db.Trucks.ToList();
        //    return RedirectToAction("Index");
        //}

        // GET: Flights/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Flight flight = db.Flights.Find(id);
            if (flight == null)
                return HttpNotFound();

            //var parkingLots = db.ParkingLots.ToList();

            //if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin"))
            //{
            //    var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            //    parkingLots = parkingLots.Where(p => p.AirportId == (int)user.AirportId).ToList();
            //}

            //if (flight.ParkingLotId != null)
            //    ViewBag.ParkingLotId = new SelectList(parkingLots, "Id", "Name", flight.ParkingLotId);
            //else
            //{
            //    var temp = db.ParkingLots.FirstOrDefault(p => p.Name == flight.Parking);
            //    if (temp != null)
            //        ViewBag.ParkingLotId = new SelectList(parkingLots, "Id", "Name", temp.Id);
            //}
            ViewBag.AirlineId = new SelectList(db.Airlines.Where(a => a.IsCharter), "Id", "Name", flight.AirlineId);

            return View(flight);
        }

        // POST: Flights/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public int Edit([Bind(Include = "Id,AircraftCode,Code,AirlineId,ParkingLotId,RouteName,AircraftType,RefuelScheduledTime,EstimateAmount,ArrivalScheduledTime,DepartureScheduledTime,RefuelScheduledHours,Note,Status,Parking,FlightCarry")] Flight flight)
        {
            //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            //if (ModelState.IsValid)
            //{
            if (flight.ArrivalScheduledTime is DateTime)
                flight.ArrivalScheduledTime = DateTime.Today.AddHours(flight.ArrivalScheduledTime.Value.Hour).AddMinutes(flight.ArrivalScheduledTime.Value.Minute);
            if (flight.DepartureScheduledTime is DateTime)
                flight.DepartureScheduledTime = DateTime.Today.AddHours(flight.DepartureScheduledTime.Value.Hour).AddMinutes(flight.DepartureScheduledTime.Value.Minute);
            if (flight.RefuelScheduledTime is DateTime)
                flight.RefuelScheduledTime = flight.RefuelScheduledTime.Value.AddHours(flight.RefuelScheduledHours.Value.Hour).AddMinutes(flight.RefuelScheduledHours.Value.Minute);
            var model = db.Flights.FirstOrDefault(f => f.Id == flight.Id);

            //Save ChangeLog
            var entityLog = new ChangeLog();
            entityLog.EntityId = flight.Id;
            entityLog.EntityName = "Flight";
            entityLog.EntityDisplay = "Chuyến bay";
            entityLog.DateChanged = DateTime.Now;
            entityLog.KeyValues = flight.Code;
            if (user != null)
            {
                model.UserUpdatedId = currentUserId;
                entityLog.UserUpdatedId = currentUserId;
                entityLog.UserUpdatedName = user.FullName;
            }

            if (model.AircraftCode != flight.AircraftCode)
            {
                entityLog.PropertyName = "Số hiệu";
                entityLog.OldValues = model.AircraftCode;
                entityLog.NewValues = flight.AircraftCode;
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }
            if (model.Code != flight.Code)
            {
                entityLog.PropertyName = "Số chuyến";
                entityLog.OldValues = model.Code;
                entityLog.NewValues = flight.Code;
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }
            //if (model.AirlineId != flight.AirlineId)
            //{
            //    entityLog.PropertyName = "Hãng hàng không";
            //    var air = db.Airlines.FirstOrDefault(a => a.Id == model.AirlineId);
            //    if (air != null)
            //        entityLog.OldValues = air.Name;
            //    air = db.Airlines.FirstOrDefault(a => a.Id == flight.AirlineId);
            //    if (air != null)
            //        entityLog.NewValues = air.Name;
            //    db.ChangeLogs.Add(entityLog);
            //    db.SaveChanges();
            //}
            //if (model.ParkingLotId != flight.ParkingLotId)
            //{
            //    entityLog.PropertyName = "Bãi đỗ";
            //    var park = db.ParkingLots.FirstOrDefault(a => a.Id == model.ParkingLotId);
            //    if (park != null)
            //        entityLog.OldValues = park.Name;
            //    park = db.ParkingLots.FirstOrDefault(a => a.Id == flight.ParkingLotId);
            //    if (park != null)
            //        entityLog.NewValues = park.Name;
            //    db.ChangeLogs.Add(entityLog);
            //    db.SaveChanges();
            //}
            if (model.Parking != flight.Parking)
            {
                entityLog.PropertyName = "Bãi đỗ";
                entityLog.OldValues = model.Parking;
                entityLog.NewValues = flight.Parking;
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }
            if (model.RouteName != flight.RouteName)
            {
                entityLog.PropertyName = "Đường bay";
                entityLog.OldValues = model.RouteName;
                entityLog.NewValues = flight.RouteName;
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }
            if (model.AircraftType != flight.AircraftType)
            {
                entityLog.PropertyName = "Loại tàu bay";
                entityLog.OldValues = model.AircraftType;
                entityLog.NewValues = flight.AircraftType;
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }

            var refuTime = DateTime.Now;
            if (model.RefuelScheduledTime != null)
                refuTime = model.RefuelScheduledTime.Value;
            if (flight.RefuelScheduledTime != null)
            {
                if (refuTime.ToString("dd/MM/yyyy") != flight.RefuelScheduledTime.Value.ToString("dd/MM/yyyy"))
                {
                    entityLog.PropertyName = "Ngày nạp dầu";
                    entityLog.OldValues = refuTime.ToString("dd/MM/yyyy");
                    entityLog.NewValues = flight.RefuelScheduledTime.Value.ToString("dd/MM/yyyy");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
            }

            if (refuTime.ToString("HH:mm") != flight.RefuelScheduledTime.Value.ToString("HH:mm"))
            {
                entityLog.PropertyName = "Giờ nạp dầu";
                entityLog.OldValues = refuTime.ToString("HH:mm");
                entityLog.NewValues = flight.RefuelScheduledTime.Value.ToString("HH:mm");
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }

            if (model.EstimateAmount != flight.EstimateAmount)
            {
                entityLog.PropertyName = "Sản lượng dự kiến";
                entityLog.OldValues = model.EstimateAmount.ToString();
                entityLog.NewValues = flight.EstimateAmount.ToString();
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }

            var arrTime = DateTime.Now;
            if (model.ArrivalScheduledTime != null && flight.ArrivalScheduledTime != null)
            {
                arrTime = model.ArrivalScheduledTime.Value;
                if (arrTime.ToString("HH:mm") != flight.ArrivalScheduledTime.Value.ToString("HH:mm"))
                {
                    entityLog.PropertyName = "Giờ hạ cánh dự kiến";
                    entityLog.OldValues = arrTime.ToString("HH:mm");
                    entityLog.NewValues = flight.ArrivalScheduledTime.Value.ToString("HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
            }


            var depaTime = DateTime.Now;
            if (model.DepartureScheduledTime != null)
                depaTime = model.DepartureScheduledTime.Value;
            if (depaTime.ToString("HH:mm") != flight.DepartureScheduledTime.Value.ToString("HH:mm"))
            {
                entityLog.PropertyName = "Giờ cất cánh dự kiến";
                entityLog.OldValues = depaTime.ToString("HH:mm");
                entityLog.NewValues = flight.DepartureScheduledTime.Value.ToString("HH:mm");
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }

            if (model.Note != flight.Note)
            {
                entityLog.PropertyName = "Ghi chú";
                entityLog.OldValues = model.Note;
                entityLog.NewValues = flight.Note;
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }

            if (model.FlightCarry != flight.FlightCarry)
            {
                entityLog.PropertyName = "Chuyên chở";
                entityLog.OldValues = model.FlightCarry == FlightCarry.CCO ? "CCO" : model.FlightCarry == FlightCarry.CGO ? "CGO" : "PAX";
                entityLog.NewValues = flight.FlightCarry == FlightCarry.CCO ? "CCO" : flight.FlightCarry == FlightCarry.CGO ? "CGO" : "PAX";
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }
            //End Save ChangeLog

            model.AircraftCode = flight.AircraftCode;
            model.Code = flight.Code;

            //Quốc nội quốc ngoại dựa vào đường bay và bảng loại sân bay nội địa
            if (!string.IsNullOrEmpty(flight.RouteName))
            {
                var rname = flight.RouteName.Split('-');
                var at_count = db.AirportTypes.Where(a => rname.Contains(a.Code) && a.Type == FlightType.DOMESTIC).Count();
                if (at_count > 1)
                    model.FlightType = FlightType.DOMESTIC;
                else model.FlightType = FlightType.OVERSEA;
            }
            //end quốc nội quốc ngoại

            // dựa vào pattern và mã chuyến bay để lấy id hãng bay
            if (flight.AirlineId == null)
            {
                foreach (var i in db.Airlines)
                {
                    if (i.Pattern != null && Regex.IsMatch(flight.Code, i.Pattern))
                    {
                        model.AirlineId = i.Id;
                        break;
                    }
                }
            }
            else
                model.AirlineId = flight.AirlineId;

            model.ParkingLotId = flight.ParkingLotId;
            model.Parking = flight.Parking;
            model.RouteName = flight.RouteName;
            model.AircraftType = flight.AircraftType;
            model.RefuelScheduledTime = flight.RefuelScheduledTime;
            model.EstimateAmount = flight.EstimateAmount;
            model.ArrivalScheduledTime = flight.ArrivalScheduledTime;
            model.DepartureScheduledTime = flight.DepartureScheduledTime;
            model.RefuelScheduledHours = flight.RefuelScheduledHours;
            model.Note = flight.Note;
            model.FlightCarry = flight.FlightCarry;

            var parking = db.ParkingLots.FirstOrDefault(p => p.Id == flight.ParkingLotId);
            if (parking != null)
                model.Parking = parking.Name;

            db.SaveChanges();
            //Response.Write("<script>window.open('" + Request["returnUrl"].ToString() + "','_parent');</script>");
            //return Redirect(Request["returnUrl"].ToString());
            //return RedirectToAction("Index");
            //}
            //var parkingLots = db.ParkingLots.ToList();

            //if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            //    parkingLots = parkingLots.Where(p => p.AirportId == (int)user.AirportId).ToList();

            //if (flight.ParkingLotId != null)
            //    ViewBag.ParkingLotId = new SelectList(parkingLots, "Id", "Name", flight.ParkingLotId);
            //else
            //{
            //    var temp = db.ParkingLots.FirstOrDefault(p => p.Name == flight.Parking);
            //    if (temp != null)
            //        ViewBag.ParkingLotId = new SelectList(parkingLots, "Id", "Name", temp.Id);
            //}
            ViewBag.AirlineId = new SelectList(db.Airlines.Where(a => a.IsCharter), "Id", "Name", flight.AirlineId);
            //return View(flight);
            return 0;
        }

        public JsonResult JEdit(Flight flight)
        {
            return Json(Edit(flight), JsonRequestBehavior.AllowGet);
        }
        public JsonResult JEditReturnAmount(RefuelItem refuelItem)
        {
            return Json(EditReturnAmount(refuelItem), JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult CreateRefuelItem()
        {
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName");
            ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName");
            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code");

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            {
                int u_airportId = (int)user.AirportId;
                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName");
                ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName");
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == u_airportId), "Id", "Code");
            }

            return View();
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRefuelItem([Bind(Include = "TruckId,DriverId,OperatorId,StartTime,EndTime,Amount,Volume,Weight,ManualTemperature,Density,QCNo,FlightId")] RefuelItem refuelItem)
        {
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName");
            ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName");
            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code");

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            {
                int u_airportId = (int)user.AirportId;
                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName");
                ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName");
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == u_airportId), "Id", "Code");
            }
            if (ModelState.IsValid)
            {
                var flight = db.Flights.FirstOrDefault(f => f.Id == refuelItem.FlightId);
                flight.Status = FlightStatus.REFUELED;
                db.SaveChanges();

                db.RefuelItems.Add(refuelItem);
                db.SaveChanges();
                //return Redirect(Request["returnUrl"].ToString());
                Response.Write("<script>window.open('" + (Request["returnUrl"] != null ? Request["returnUrl"].ToString() : "~/Flights/Index") + "','_parent');</script>");
            }

            return View(refuelItem);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult EditRefuelItem(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RefuelItem refuelItem = db.RefuelItems.Find(id);
            if (refuelItem == null)
            {
                return HttpNotFound();
            }

            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", refuelItem.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", refuelItem.OperatorId);

            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", refuelItem.TruckId);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                int u_airportId = (int)user.AirportId;
                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.DriverId);
                ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.OperatorId);
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == u_airportId), "Id", "Code", refuelItem.TruckId);
            }
            return View(refuelItem);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRefuelItem([Bind(Include = "Id,TruckId,DriverId,OperatorId,StartTime,EndTime,Amount,ReturnAmount,ManualTemperature,Density,QCNo,FlightId,DeviceStartTime,DeviceEndTime,EndNumber,StartNumber,Status,Price,TaxRate,Gallon,Temperature,Completed,Printed")] RefuelItem refuelItem)
        {
            //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (ModelState.IsValid)
            {
                //update table flights
                var flight = db.Flights.FirstOrDefault(f => f.Id == refuelItem.FlightId);
                if (flight.Status != FlightStatus.REFUELED)
                    flight.Status = FlightStatus.REFUELED;

                var model = db.RefuelItems.FirstOrDefault(r => r.Id == refuelItem.Id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityName = "Flight";
                entityLog.EntityDisplay = "Chuyến bay";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = flight.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = currentUserId;
                    entityLog.UserUpdatedName = user.FullName;
                }

                if (model.TruckId != refuelItem.TruckId)
                {
                    entityLog.PropertyName = "Xe";
                    var truck = db.Trucks.FirstOrDefault(a => a.Id == model.TruckId);
                    if (truck != null)
                        entityLog.OldValues = truck.Code;
                    truck = db.Trucks.FirstOrDefault(a => a.Id == refuelItem.TruckId);
                    if (truck != null)
                        entityLog.NewValues = truck.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.DriverId != refuelItem.DriverId)
                {
                    entityLog.PropertyName = "Lái xe";
                    var us = db.Users.FirstOrDefault(a => a.Id == model.DriverId);
                    if (us != null)
                        entityLog.OldValues = us.FullName;
                    us = db.Users.FirstOrDefault(a => a.Id == refuelItem.DriverId);
                    if (us != null)
                        entityLog.NewValues = us.FullName;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.OperatorId != refuelItem.OperatorId)
                {
                    entityLog.PropertyName = "NV tra nạp";
                    var us = db.Users.FirstOrDefault(a => a.Id == model.OperatorId);
                    if (us != null)
                        entityLog.OldValues = us.FullName;
                    us = db.Users.FirstOrDefault(a => a.Id == refuelItem.OperatorId);
                    if (us != null)
                        entityLog.NewValues = us.FullName;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                var sTime = DateTime.Now;
                if (model.StartTime != null)
                    sTime = model.StartTime;
                if (sTime.ToString("HH:mm") != refuelItem.StartTime.ToString("HH:mm"))
                {
                    entityLog.PropertyName = "Thời gian bắt đầu";
                    entityLog.OldValues = sTime.ToString("HH:mm");
                    entityLog.NewValues = refuelItem.StartTime.ToString("HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.EndTime != null)
                    sTime = model.EndTime.Value;
                if (sTime.ToString("HH:mm") != refuelItem.EndTime.Value.ToString("HH:mm"))
                {
                    entityLog.PropertyName = "Thời gian kết thúc";
                    entityLog.OldValues = sTime.ToString("HH:mm");
                    entityLog.NewValues = refuelItem.EndTime.Value.ToString("HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Amount != refuelItem.Amount)
                {
                    entityLog.PropertyName = "Số lượng Gallon";
                    entityLog.OldValues = Math.Round(model.Amount).ToString();
                    entityLog.NewValues = Math.Round(refuelItem.Amount).ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.ReturnAmount != refuelItem.ReturnAmount)
                {
                    entityLog.PropertyName = "Hoàn trả kg";
                    entityLog.OldValues = model.ReturnAmount != null ? Math.Round((decimal)model.ReturnAmount).ToString() : "";
                    entityLog.NewValues = refuelItem.ReturnAmount != null ? Math.Round((decimal)refuelItem.ReturnAmount).ToString() : "";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.ManualTemperature != refuelItem.ManualTemperature)
                {
                    entityLog.PropertyName = "Nhiệt độ";
                    entityLog.OldValues = Math.Round(model.ManualTemperature).ToString();
                    entityLog.NewValues = Math.Round(refuelItem.ManualTemperature).ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Density != refuelItem.Density)
                {
                    entityLog.PropertyName = "Tỷ trọng";
                    entityLog.OldValues = model.Density.ToString("#,##0.0000");
                    entityLog.NewValues = refuelItem.Density.ToString("#,##0.0000");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.QCNo != refuelItem.QCNo)
                {
                    entityLog.PropertyName = "Phiếu hóa nghiệm";
                    entityLog.OldValues = model.QCNo;
                    entityLog.NewValues = refuelItem.QCNo;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                //End Save ChangeLog
                TryUpdateModel(model);
                model.TruckId = refuelItem.TruckId;
                model.DriverId = refuelItem.DriverId;
                model.OperatorId = refuelItem.OperatorId;
                model.StartTime = refuelItem.StartTime;
                model.EndTime = refuelItem.EndTime;
                model.Amount = refuelItem.Amount;
                model.ReturnAmount = refuelItem.ReturnAmount;
                model.ManualTemperature = refuelItem.ManualTemperature;
                model.Density = refuelItem.Density;
                model.QCNo = refuelItem.QCNo;
                model.DateUpdated = DateTime.Now;
                //db.Entry(refuelItem).State = EntityState.Modified;
                db.SaveChanges();
                Response.Write("<script>window.open('" + Request["returnUrl"].ToString() + "','_parent');</script>");
                //return Redirect(Request["returnUrl"].ToString());
            }
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", refuelItem.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", refuelItem.OperatorId);

            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", refuelItem.TruckId);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && user != null)
            {
                var u_airportId = user.AirportId;
                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.DriverId);
                ViewBag.OperatorId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == u_airportId), "Id", "FullName", refuelItem.OperatorId);
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == u_airportId), "Id", "Code", refuelItem.TruckId);
            }
            return View(refuelItem);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        public ActionResult EditReturnAmount(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            RefuelItem refuelItem = db.RefuelItems.Find(id);
            if (refuelItem == null)
                return HttpNotFound();
            return View(refuelItem);
        }
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public int EditReturnAmount([Bind(Include = "Id,FlightId,ReturnAmount")] RefuelItem refuelItem)
        {

            //if (ModelState.IsValid)
            //{
            //update table flights
            var flight = db.Flights.FirstOrDefault(f => f.Id == refuelItem.FlightId);
            var model = db.RefuelItems.FirstOrDefault(r => r.Id == refuelItem.Id);

            //Save ChangeLog
            var entityLog = new ChangeLog();
            entityLog.EntityName = "Flight";
            entityLog.EntityDisplay = "Chuyến bay";
            entityLog.DateChanged = DateTime.Now;
            entityLog.KeyValues = flight.Code;
            if (user != null)
            {
                entityLog.UserUpdatedId = currentUserId;
                entityLog.UserUpdatedName = user.FullName;
            }

            if (model.ReturnAmount != refuelItem.ReturnAmount)
            {
                entityLog.PropertyName = "Hoàn trả kg";
                entityLog.OldValues = model.ReturnAmount != null ? Math.Round((decimal)model.ReturnAmount).ToString() : "";
                entityLog.NewValues = refuelItem.ReturnAmount != null ? Math.Round((decimal)refuelItem.ReturnAmount).ToString() : "";
                db.ChangeLogs.Add(entityLog);
                db.SaveChanges();
            }

            //End Save ChangeLog
            TryUpdateModel(model);
            model.ReturnAmount = refuelItem.ReturnAmount;
            db.SaveChanges();
            //Response.Write("<script>window.open('" + Request["returnUrl"].ToString() + "','_parent');</script>");
            //}
            //return View(refuelItem);
            return 0;
        }
        // GET: Flights/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return HttpNotFound();
            }
            return View(flight);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Flight flight = db.Flights.Find(id);
            //Delete Refuels by FlightId
            if (flight != null)
            {
                var refue_lst = db.RefuelItems.Where(r => r.FlightId == flight.Id).ToList();
                refue_lst.ForEach(f =>
                {
                    f.IsDeleted = true;
                    f.DateDeleted = DateTime.Now;
                    f.UserDeletedId = currentUserId;
                });
                //db.RefuelItems.RemoveRange(refue_lst);
                db.SaveChanges();
            }
            flight.IsDeleted = true;
            flight.DateDeleted = DateTime.Now;
            flight.UserDeletedId = currentUserId;
            //db.Flights.Remove(flight);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult JRFDelete(int id)
        {
            RefuelItem item = db.RefuelItems.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            item.UserDeletedId = currentUserId;
            //db.RefuelItems.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            var model = db.Flights.FirstOrDefault(f => f.Id == id);
            model.Status = FlightStatus.CANCELED;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã hủy" });
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult Follow(int id)
        {
            var model = db.Flights.FirstOrDefault(f => f.Id == id);
            if (model != null)
            {
                if (model.Follow == 1)
                    model.Follow = 0;
                else
                    model.Follow = 1;
                db.SaveChanges();
            }
            return Json(new { Status = 0, Message = "Đã hủy" });
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult DeleteS(string id)
        {
            int[] ids = id.Split(new char[] { ',' }).Select(s => int.Parse(s)).ToArray();
            var lst = db.Flights.Where(a => ids.Contains(a.Id)).ToList();
            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
                lst = lst.Where(f => f.Status != FlightStatus.REFUELED).ToList();

            //Delete Refuels by FlightId
            foreach (var flight in lst)
            {
                var refue_lst = db.RefuelItems.Where(r => r.FlightId == flight.Id).ToList();
                refue_lst.ForEach(r =>
                {
                    r.IsDeleted = true;
                    r.DateDeleted = DateTime.Now;
                    r.UserDeletedId = currentUserId;
                });
                //db.RefuelItems.RemoveRange(refue_lst);
                db.SaveChanges();
            }

            lst.ForEach(f =>
            {
                f.IsDeleted = true;
                f.DateDeleted = DateTime.Now;
                f.UserDeletedId = currentUserId;
            });
            //db.Flights.RemoveRange(lst);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        // POST: Flights/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Flight flight = db.Flights.Find(id);
            flight.IsDeleted = true;
            flight.DateDeleted = DateTime.Now;
            flight.UserDeletedId = currentUserId;
            //db.Flights.Remove(flight);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        public ActionResult Import()
        {
            return View();
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult BeforeImport(FormCollection form)
        {
            var airportCode = "";
            if (user != null)
            {
                var airport = db.Airports.FirstOrDefault(a => a.Id == user.AirportId);
                if (airport != null)
                    airportCode = airport.Code;
            }

            var folder = Path.GetTempPath();
            var fileToSave = Path.Combine(folder, Path.GetTempFileName());
            HttpPostedFileBase file = Request.Files[0];
            file.SaveAs(fileToSave);
            //var list = FlightImporter.ImportFile(fileToSave, FlightFileType.MN);
            var fileMode = FlightFileMode.Mode1;
            if (!string.IsNullOrEmpty(form["fileMode"]))
            {
                if (form["fileMode"] == "Mode2")
                    fileMode = FlightFileMode.Mode2;
                else if (form["fileMode"] == "Mode3")
                    fileMode = FlightFileMode.Mode3;
                else if (form["fileMode"] == "Mode4")
                    fileMode = FlightFileMode.Mode4;
                else if (form["fileMode"] == "Mode5")
                    fileMode = FlightFileMode.Mode5;
                else if (form["fileMode"] == "Mode6")
                    fileMode = FlightFileMode.Mode6;
            }
            var setPlanDay = DateTime.Now.ToString("MM/dd/yyyy");
            if (!string.IsNullOrEmpty(form["SetPlanDay"]))
                setPlanDay = Convert.ToDateTime(ChangeDate(form["SetPlanDay"])).ToString("MM/dd/yyyy");

            var list = FlightImporter.ImportFromExcel(fileToSave, fileMode, setPlanDay, airportCode);
            if (!string.IsNullOrEmpty(form["exCode"]))
            {
                var str = form["exCode"].Split(',').Select(s => s.ToLower()).ToList();
                list = list.Where(f => !str.Contains(f.Code.Split('-')[0].ToLower())).ToList();
            }
            return View(list);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult Import(string json)
        {
            JObject jsonData = JObject.Parse("{'items':" + json + "}");
            var list = (from d in jsonData["items"]
                        select new Flight
                        {
                            AircraftType = d["aircraftType"] != null ? d["aircraftType"].Value<string>() : "",
                            AircraftCode = d["aircraftCode"] != null ? d["aircraftCode"].Value<string>() : "",
                            Code = d["code"] != null ? d["code"].Value<string>() : "",
                            RouteName = d["routeName"] != null ? d["routeName"].Value<string>() : "",
                            EstimateAmount = d["estimateAmount"] != null ? d["estimateAmount"].Value<decimal>() : 0,
                            ArrivalScheduledTime = !string.IsNullOrEmpty(d["arrivalScheduledTime"].Value<string>()) ? d["arrivalScheduledTime"].Value<DateTime>() : DateTime.MaxValue,
                            DepartureScheduledTime = !string.IsNullOrEmpty(d["departureScheduledTime"].Value<string>()) ? d["departureScheduledTime"].Value<DateTime>() : DateTime.Now,
                            RefuelScheduledTime = !string.IsNullOrEmpty(d["refuelScheduledTime"].Value<string>()) ? d["refuelScheduledTime"].Value<DateTime>() : DateTime.Now,
                            Parking = d["parking"] != null ? d["parking"].Value<string>() : "",
                            FlightCarry = d["flightCarry"] != null ? d["flightCarry"].Value<string>() == "PAX" ? FlightCarry.PAX : d["flightCarry"].Value<string>() == "CCO" ? FlightCarry.CCO : FlightCarry.CGO : FlightCarry.PAX,
                            Note = d["note"] != null ? d["note"].Value<string>() : "",
                            TruckName = d["truckName"].Value<string>(),
                            DriverName = d["driverName"].Value<string>(),
                            TechnicalerName = d["technicalerName"].Value<string>(),
                            //Shift = d["shift"].Value<string>(),
                            //ShiftStartTime = d["shiftStartTime"].Value<DateTime>(),
                            //ShiftEndTime = d["shiftEndTime"].Value<DateTime>(),
                            //AirportName = d["airportName"].Value<string>()
                        }
                       ).ToList();

            //if (Request != null)
            //{
            //var folder = Path.GetTempPath();
            //var fileToSave = Path.Combine(folder, Path.GetTempFileName());
            //HttpPostedFileBase file = Request.Files[0];
            //file.SaveAs(fileToSave);
            //var list = FlightImporter.ImportFile(fileToSave, FlightFileType.MN);

            if (list.Count > 0)
            {
                var t_user = UserManager.FindById(User.Identity.GetUserId());
                var dbUser = db.Users.FirstOrDefault(u => u.UserName == t_user.UserName);
                foreach (var item in list)
                {
                    //var airports = item.RouteName.Split('-');

                    //var ad = airports[0];
                    //var aa = airports[0]; if (airports.Length > 1) aa = airports[1];
                    //var departure = db.Airports.FirstOrDefault(a => a.Code.Equals(ad));
                    //var arrival = db.Airports.FirstOrDefault(a => a.Code.Equals(aa));
                    //if (arrival == null)
                    //{
                    //    arrival = new Airport { Code = aa };
                    //    db.Airports.AddOrUpdate(a=>a.Code, arrival);

                    //}
                    //if (departure == null )
                    //{
                    //    departure = new Airport { Code = ad };
                    //    db.Airports.AddOrUpdate(a => a.Code, departure);

                    //}

                    //item.Departure = departure;
                    //item.Arrival = arrival;

                    //if (departure != null & arrival != null)
                    //    item.Route = new Route { Arrival = arrival, Departure = departure };

                    var flight = db.Flights.Include(f => f.RefuelItems).FirstOrDefault(a => a.Code.ToLower() == item.Code.ToLower()
                    && a.RefuelScheduledTime.Value.Day == item.RefuelScheduledTime.Value.Day
                    && a.RefuelScheduledTime.Value.Month == item.RefuelScheduledTime.Value.Month
                    && a.RefuelScheduledTime.Value.Year == item.RefuelScheduledTime.Value.Year
                    && a.Status != FlightStatus.REFUELED

                    );

                    if (dbUser != null && dbUser.AirportId != null)
                    {
                        item.AirportId = dbUser.AirportId;

                        flight = db.Flights.Include(f => f.RefuelItems).FirstOrDefault(a => a.Code.ToLower() == item.Code.ToLower()
                    && a.AirportId == dbUser.AirportId
                    && a.RefuelScheduledTime.Value.Day == item.RefuelScheduledTime.Value.Day
                    && a.RefuelScheduledTime.Value.Month == item.RefuelScheduledTime.Value.Month
                    && a.RefuelScheduledTime.Value.Year == item.RefuelScheduledTime.Value.Year
                    && a.Status != FlightStatus.REFUELED

                    );
                    }

                    var id = -1;
                    if (flight == null)
                    {
                        if (dbUser != null)
                            item.UserCreatedId = dbUser.Id;

                        item.CreateType = 1;
                        item.StartTime = DateTime.Now;
                        item.EndTime = DateTime.Now;

                        db.Flights.Add(item);
                        db.SaveChanges();
                        db.Entry(item).GetDatabaseValues();
                        id = item.Id;
                    }
                    else
                    {
                        if (dbUser != null)
                            flight.UserCreatedId = dbUser.Id;

                        if (flight.StartTime.Year == 0001)
                            flight.StartTime = DateTime.Now;
                        if (flight.EndTime.Year == 0001)
                            flight.EndTime = DateTime.Now;

                        id = flight.Id;
                        flight.CreateType = 1;
                        flight.AircraftType = item.AircraftType;
                        flight.AircraftCode = item.AircraftCode;
                        flight.RouteName = item.RouteName;
                        flight.EstimateAmount = item.EstimateAmount;
                        if (item.ArrivalScheduledTime != DateTime.MinValue)
                            flight.ArrivalScheduledTime = item.ArrivalScheduledTime;
                        flight.DepartureScheduledTime = item.DepartureScheduledTime;
                        flight.RefuelScheduledTime = item.RefuelScheduledTime;
                        flight.Parking = item.Parking;
                        var temp = db.ParkingLots.FirstOrDefault(p => p.Name == item.Parking);
                        if (temp != null)
                            flight.ParkingLotId = temp.Id;

                        flight.TruckName = item.TruckName;
                        //flight.DriverName = item.DriverName;
                        //flight.TechnicalerName = item.TechnicalerName;
                        //flight.Shift = item.Shift;
                        //flight.ShiftStartTime = item.ShiftStartTime;
                        //flight.ShiftEndTime = item.ShiftEndTime;
                        //flight.AirportName = item.AirportName;
                        db.SaveChanges();
                    }

                    //Delete Refuels by FlightId
                    var refu = db.Refuels.Where(f => f.FlightId == id).ToList();
                    foreach (var i in refu) { db.Refuels.Remove(i); db.SaveChanges(); }

                    flight = db.Flights.Include(f => f.RefuelItems).FirstOrDefault(a => a.Id == id);
                    flight.Status = FlightStatus.NONE;
                    flight.AirportId = item.AirportId;

                    //Quốc nội quốc ngoại dựa vào đường bay và bảng loại sân bay nội địa
                    var rname = flight.RouteName.Split('-');
                    var at_count = db.AirportTypes.Where(a => rname.Contains(a.Code) && a.Type == FlightType.DOMESTIC).Count();
                    if (at_count > 1)
                        flight.FlightType = FlightType.DOMESTIC;
                    else flight.FlightType = FlightType.OVERSEA;
                    //end quốc nội quốc ngoại

                    // dựa vào pattern và mã chuyến bay để lấy id hãng bay
                    foreach (var i in db.Airlines)
                    {
                        if (i.Pattern != null && Regex.IsMatch(flight.Code, i.Pattern))
                        {
                            flight.AirlineId = i.Id;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(item.TruckName))
                    {
                        var import_code = Regex.Split(item.TruckName.Trim(), @"\s*[,;]\s*");
                        var trucks = db.Trucks.Where(t => import_code.Contains(t.ImportCode)).ToList();

                        var import_driver = Regex.Split(item.DriverName.Trim(), @"\s*[,;]\s*");
                        var import_technicaler = Regex.Split(item.TechnicalerName.Trim(), @"\s*[,;]\s*");

                        if (trucks.Count > 0 && import_code.Length == import_driver.Length && import_code.Length == import_technicaler.Length)
                        {
                            //var model = new Refuel();
                            //model.FlightId = id;
                            //model.TotalAmount = 0;
                            //model.Price = 0;
                            //model.StartTime = DateTime.Now;
                            //model.EndTime = DateTime.Now;
                            //model.Status = REFUEL_STATUS.NONE;
                            //model.DateCreated = DateTime.Now;
                            //model.DateUpdated = DateTime.Now;
                            //model.IsDeleted = false;
                            //model.Items = trucks.Select(t => new RefuelItem { TruckId = t.Id, StartTime = DateTime.Now, Amount = 0, Completed = false, Printed = false, DateCreated = DateTime.Now, DateUpdated = DateTime.Now, IsDeleted = false }).ToList();
                            //db.Refuels.Add(model);
                            //db.SaveChanges();

                            DateTime c_day = DateTime.Now;
                            //var truckAssigns = db.TruckAssigns.Where(t => t.Shift.StartTime.Hour <= item.RefuelScheduledTime.Value.Hour && item.RefuelScheduledTime.Value.Hour <= t.Shift.EndTime.Hour && t.StartDate.Day >= c_day.Day && t.StartDate.Month >= c_day.Month && t.StartDate.Year >= c_day.Year);
                            for (int i = 0; i < import_code.Length; i++)
                            {
                                //var truckAssign = truckAssigns.FirstOrDefault(tr => tr.TruckId == id);
                                var importCode = import_code[i];
                                var truck = db.Trucks.FirstOrDefault(t => t.ImportCode == importCode);
                                if (!User.IsInRole("Super Admin") && user != null)
                                    truck = db.Trucks.FirstOrDefault(t => t.ImportCode == importCode && t.AirportId == user.AirportId);

                                var importDriver = import_driver[i];
                                var driver = db.Users.FirstOrDefault(t => t.ImportName.ToLower() == importDriver.ToLower());
                                if (!User.IsInRole("Super Admin") && user != null)
                                    driver = db.Users.FirstOrDefault(t => t.ImportName.ToLower() == importDriver.ToLower() && t.AirportId == user.AirportId);

                                var importTechnicaler = import_technicaler[i];
                                var technicaler = db.Users.FirstOrDefault(t => t.ImportName.ToLower() == importTechnicaler.ToLower());
                                if (!User.IsInRole("Super Admin") && user != null)
                                    technicaler = db.Users.FirstOrDefault(t => t.ImportName.ToLower() == importTechnicaler.ToLower() && t.AirportId == user.AirportId);

                                if (truck != null && driver != null && technicaler != null)
                                {
                                    var r_item = new RefuelItem();

                                    r_item.TruckId = truck.Id;
                                    r_item.DriverId = driver.Id;
                                    r_item.OperatorId = technicaler.Id;

                                    //if (truckAssign != null)
                                    //{
                                    //    r_item.DriverId = truckAssign.DriverId;
                                    //    r_item.OperatorId = truckAssign.DriverId;
                                    //}

                                    r_item.StartTime = DateTime.Now;
                                    r_item.Amount = 0;
                                    r_item.Completed = false;
                                    r_item.Printed = false;
                                    r_item.DateCreated = DateTime.Now;
                                    r_item.DateUpdated = DateTime.Now;
                                    r_item.IsDeleted = false;
                                    r_item.Price = 0;
                                    r_item.TaxRate = 0;
                                    r_item.Gallon = 0;
                                    r_item.Density = 0;
                                    r_item.Temperature = 0;
                                    flight.RefuelItems.Add(r_item);
                                }
                            }
                            //flight.RefuelItems = trucks.Select(t => new RefuelItem { TruckId = t.Id, StartTime = DateTime.Now, Amount = 0, Completed = false, Printed = false, DateCreated = DateTime.Now, DateUpdated = DateTime.Now, IsDeleted = false, Price = 0, TaxRate = 0 }).ToList();

                            if (trucks.Count > 0)
                                flight.TruckName = string.Join(", ", trucks.Select(t => t.Code).ToArray());
                            flight.Status = FlightStatus.ASSIGNED;
                        }
                    }
                    db.SaveChanges();
                }
            }
            //}
            return Json(new { Status = 0, Message = "Đã lưu" });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
