using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class CheckTrucksController : Controller
    {
        private DataContext db = new DataContext();
        private User user
        {
            get
            {
                var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                return user;
            }
        }
        // GET: CheckTrucks
        public ActionResult Index(int p = 1)
        {
            int count = 0;
            int pageSize = 100;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            ViewBag.Airports = db.Airports.ToList();
            ViewBag.Trucks = db.Trucks.ToList();
            ViewBag.Shifts = db.Shifts.ToList();
            var checkTrucks = db.CheckTrucks
                .Include(c => c.Airport)
                .Include(c => c.Shift)
                .Include(c => c.Truck)
                .AsNoTracking() as IQueryable<CheckTruck>;

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                //var arp = db.Airports.FirstOrDefault(ar => ar.Id == user.AirportId);
                checkTrucks = checkTrucks.Where(f => f.AirportId == user.AirportId);

                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
                ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == user.AirportId).ToList();
                ViewBag.Shifts = db.Shifts.Where(t => t.AirportId == user.AirportId).ToList();
            }
            if (Request["a"] != null)
            {
                var airportId = 0;
                int.TryParse(Request["a"], out airportId);
                if (airportId > 0)
                    checkTrucks = checkTrucks.Where(a => a.AirportId == airportId);
            }

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            {
                count = db.CheckTrucks.Count(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year);
                checkTrucks = checkTrucks.Where(f =>
                f.DateCreated.Day == fd.Day
                && f.DateCreated.Month == fd.Month
                && f.DateCreated.Year == fd.Year);
            }
            else
            {
                td = td.AddHours(23);
                count = db.CheckTrucks.Count(f => f.DateCreated >= fd && f.DateCreated <= td);
                checkTrucks = checkTrucks.Where(f =>
                f.DateCreated >= fd
                && f.DateCreated <= td
                );
            }

            count = checkTrucks.Count();
            var lst = checkTrucks.OrderByDescending(c => c.DateCreated).Skip((p - 1) * pageSize).Take(pageSize).ToList();

            return View(lst);
        }

        // GET: CheckTrucks/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckTruck checkTruck = db.CheckTrucks.Include(c => c.Shift).Include(c => c.Truck).FirstOrDefault(c => c.Id == id);
            if (checkTruck == null)
            {
                return HttpNotFound();
            }
            return View(checkTruck);
        }

        // GET: CheckTrucks/Create
        public ActionResult Create()
        {
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code");
            ViewBag.DriverId = new SelectList(db.Users, "Id", "UserName");
            ViewBag.OperatorId = new SelectList(db.Users, "Id", "UserName");
            ViewBag.ShiftId = new SelectList(db.Shifts, "Id", "Code");
            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "TabletId");
            return View();
        }

        // POST: CheckTrucks/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,KmNumber,AirportId,TruckId,ShiftId,DateCreated,Hours," +
            "Result1,Note1," +
            "Result2,Note2," +
            "Result3,Note3," +
            "Result4,Note4," +
            "Result5,Note5," +
            "Result6,Note6," +
            "Result7,Note7," +
            "Result8,Note8," +
            "Result9,Note9," +
            "Result10,Note10," +
            "Result11,Note11," +
            "Result12,Note12," +
            "Result13,Note13," +
            "Result14,Note14," +
            "Result15,Note15," +
            "Result16,Note16," +
            "Result17,Note17," +
            "Result18,Note18," +
            "Result19,Note19," +
            "Result20,Note20," +
            "Result21,Note21," +
            "Result22,Note22," +
            "Result23,Note23," +
            "Result24,Note24," +
            "Result25,Note25," +
            "Result26,Note26," +
            "Result27,Note27," +
            "Result28,Note28," +
            "Result29,Note29," +
            "Result30,Note30," +
            "Result31,Note31," +
            "Result32,Note32," +
            "Result33,Note33," +
            "Result34,Note34," +
            "Result35,Note35," +
            "Result36,Note36")] CheckTruck checkTruck)
        {
            ViewBag.Airports = db.Airports.ToList();
            ViewBag.Trucks = db.Trucks.ToList();
            ViewBag.Shifts = db.Shifts.ToList();
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                checkTruck.DateCreated = checkTruck.DateCreated.AddHours(checkTruck.Hours.Value.Hour).AddMinutes(checkTruck.Hours.Value.Minute);
                if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty"))
                {
                    checkTruck.AirportId = user.AirportId;
                    ViewBag.Trucks = db.Trucks.Where(t => t.AirportId == user.AirportId).ToList();
                    ViewBag.Shifts = db.Shifts.Where(t => t.AirportId == user.AirportId).ToList();
                }
                checkTruck.UserCreatedId = user.Id;

                db.CheckTrucks.Add(checkTruck);
                db.SaveChanges();
                //return RedirectToAction("Index");
                return Json(new { result = "OK" });
            }
            return View(checkTruck);
        }

        // GET: CheckTrucks/Edit/5
        public ActionResult Edit(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckTruck checkTruck = db.CheckTrucks.Find(id);
            if (checkTruck == null)
            {
                return HttpNotFound();
            }

            ViewBag.ShiftId = new SelectList(db.Shifts, "Id", "Name", checkTruck.ShiftId);
            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", checkTruck.TruckId);
            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.ShiftId = new SelectList(db.Shifts.Where(t => t.AirportId == user.AirportId).ToList(), "Id", "Name", checkTruck.ShiftId);
                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == user.AirportId).ToList(), "Id", "Code", checkTruck.TruckId);
            }
            return View(checkTruck);
        }

        // POST: CheckTrucks/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,KmNumber,AirportId,TruckId,ShiftId,DriverId,OperatorId,DateCreated,Hours," +
            "Result1,Note1," +
            "Result2,Note2," +
            "Result3,Note3," +
            "Result4,Note4," +
            "Result5,Note5," +
            "Result6,Note6," +
            "Result7,Note7," +
            "Result8,Note8," +
            "Result9,Note9," +
            "Result10,Note10," +
            "Result11,Note11," +
            "Result12,Note12," +
            "Result13,Note13," +
            "Result14,Note14," +
            "Result15,Note15," +
            "Result16,Note16," +
            "Result17,Note17," +
            "Result18,Note18," +
            "Result19,Note19," +
            "Result20,Note20," +
            "Result21,Note21," +
            "Result22,Note22," +
            "Result23,Note23," +
            "Result24,Note24," +
            "Result25,Note25," +
            "Result26,Note26," +
            "Result27,Note27," +
            "Result28,Note28," +
            "Result29,Note29," +
            "Result30,Note30," +
            "Result31,Note31," +
            "Result32,Note32," +
            "Result33,Note33," +
            "Result34,Note34," +
            "Result35,Note35," +
            "Result36,Note36")] CheckTruck checkTruck)
        {
            if (ModelState.IsValid)
            {
                db.Entry(checkTruck).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Airports = db.Airports.ToList();
            ViewBag.ShiftId = new SelectList(db.Shifts, "Id", "Name", checkTruck.ShiftId);
            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", checkTruck.TruckId);
            return View(checkTruck);
        }

        // GET: CheckTrucks/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckTruck checkTruck = db.CheckTrucks.Find(id);
            if (checkTruck == null)
            {
                return HttpNotFound();
            }
            return View(checkTruck);
        }

        // POST: CheckTrucks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CheckTruck checkTruck = db.CheckTrucks.Find(id);
            checkTruck.IsDeleted = true;
            checkTruck.DateDeleted = DateTime.Now;
            if (user != null)
                checkTruck.UserDeletedId = user.Id;
            //db.CheckTrucks.Remove(checkTruck);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            CheckTruck checkTruck = db.CheckTrucks.Find(id);
            checkTruck.IsDeleted = true;
            checkTruck.DateDeleted = DateTime.Now;
            if (user != null)
                checkTruck.UserDeletedId = user.Id;
            //db.CheckTrucks.Remove(checkTruck);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
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
