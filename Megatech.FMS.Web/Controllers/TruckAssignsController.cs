using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using Megatech.FMS.Web.Models;
using Newtonsoft.Json.Linq;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class TruckAssignsController : Controller
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
        // GET: TruckAssigns
        public ActionResult Index(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var truckAssigns = db.TruckAssigns.Include(t => t.Driver).Include(t => t.Shift).Include(t => t.Technicaler).Include(t => t.Truck).AsNoTracking() as IQueryable<TruckAssign>;
            //var count = 0;

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
                truckAssigns = truckAssigns.Where(f => f.StartDate.Day == fd.Day && f.StartDate.Month == fd.Month && f.StartDate.Year == fd.Year);
            else
            {
                td = td.AddHours(18);
                truckAssigns = truckAssigns.Where(f => f.StartDate >= fd && f.StartDate <= td);
            }
            //count = truckAssigns.Count();
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();

            ViewBag.DriverId = db.Users.Where(u => names.Contains(u.UserName)).ToList();
            ViewBag.TechnicalerId = db.Users.Where(u => names2.Contains(u.UserName)).ToList();
            ViewBag.TruckId = db.Trucks.ToList();
            ViewBag.ShiftId = db.Shifts.ToList();

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

                truckAssigns = truckAssigns.Where(t => t.AirportId == user.AirportId);
                //count = truckAssigns.Where(t => t.AirportId == user.AirportId).Count();

                ViewBag.DriverId = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == user.AirportId).ToList();
                ViewBag.TechnicalerId = db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == user.AirportId).ToList();
                ViewBag.TruckId = db.Trucks.Where(t => t.AirportId == user.AirportId).ToList();
                ViewBag.ShiftId = db.Shifts.Where(s => s.AirportId == user.AirportId).ToList();
            }
            //ViewBag.ItemCount = count;

            /*truckAssigns = truckAssigns.OrderByDescending(t => t.StartDate).Skip((p - 1) * pageSize).Take(pageSize); */
            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "truckassign" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "truckassign").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(truckAssigns.OrderByDescending(t => t.StartDate).ToList());
        }

        [HttpPost]
        public ActionResult Copy(string id, string date)
        {
            using (DataContext db = new DataContext())
            {
                var copyDate = DateTime.ParseExact(date.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                int[] ids = id.Split(new char[] { ',' }).Select(s => int.Parse(s)).ToArray();
                var lst = db.TruckAssigns.Where(a => ids.Contains(a.Id)).ToList();

                lst.ForEach(a =>
                {
                    var b = new TruckAssign
                    {
                        StartDate = copyDate,
                        TruckId = a.TruckId,
                        DriverId = a.DriverId,
                        TechnicalerId = a.TechnicalerId,
                        ShiftId = a.ShiftId,
                        AirportId = a.AirportId
                    };
                    db.TruckAssigns.Add(b);
                });
                db.SaveChanges();
                return Json(new { Status = 0, Message = "OK" });
            }
        }

        // GET: TruckAssignments/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TruckAssign truckAssignment = db.TruckAssigns.Find(id);
            if (truckAssignment == null)
            {
                return HttpNotFound();
            }
            return View(truckAssignment);
        }

        // GET: TruckAssignments/Create
        public ActionResult Create()
        {
            //ViewBag.DriverId = new SelectList(db.Users, "Id", "UserName");
            //ViewBag.Id = new SelectList(db.Shifts, "Id", "Code");
            //ViewBag.TechnicalerId = new SelectList(db.Users, "Id", "UserName");
            //ViewBag.TruckId = new SelectList(db.Trucks, "Id", "TabletId");
            return View();
        }

        // POST: TruckAssignments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TruckId,ShiftId,StartDate,DriverId,TechnicalerId")] TruckAssign truckAssignment)
        {
            var truck_assign = db.TruckAssigns.FirstOrDefault(t => t.TruckId == truckAssignment.TruckId
            && t.DriverId == truckAssignment.DriverId
            && t.TechnicalerId == truckAssignment.TechnicalerId
            && t.ShiftId == truckAssignment.ShiftId
            && t.StartDate.Day == truckAssignment.StartDate.Day
            && t.StartDate.Month == truckAssignment.StartDate.Month
            && t.StartDate.Year == truckAssignment.StartDate.Year
            );
            if (truck_assign != null)
                return Json(new { result = "NOTOK" });
            else if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                {
                    truckAssignment.UserCreatedId = user.Id;
                    truckAssignment.AirportId = (int)user.AirportId;
                }

                db.TruckAssigns.Add(truckAssignment);
                db.SaveChanges();
                return Json(new { result = "OK" });
                //return RedirectToAction("Index");
            }

            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();
            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", truckAssignment.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.TechnicalerId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", truckAssignment.TechnicalerId);
            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", truckAssignment.TruckId);
            ViewBag.ShiftId = new SelectList(db.Shifts, "Id", "Name", truckAssignment.ShiftId);

            return View(truckAssignment);
        }

        // GET: TruckAssignments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TruckAssign truckAssignment = db.TruckAssigns.Find(id);
            if (truckAssignment == null)
            {
                return HttpNotFound();
            }
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", truckAssignment.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.TechnicalerId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", truckAssignment.TechnicalerId);

            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", truckAssignment.TruckId);
            ViewBag.ShiftId = new SelectList(db.Shifts, "Id", "Name", truckAssignment.ShiftId);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

                ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == user.AirportId), "Id", "FullName", truckAssignment.DriverId);
                ViewBag.TechnicalerId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName) && u.AirportId == user.AirportId), "Id", "FullName", truckAssignment.TechnicalerId);

                ViewBag.TruckId = new SelectList(db.Trucks.Where(t => t.AirportId == user.AirportId), "Id", "Code", truckAssignment.TruckId);
                ViewBag.ShiftId = new SelectList(db.Shifts.Where(s => s.AirportId == user.AirportId), "Id", "Name", truckAssignment.ShiftId);
            }

            return View(truckAssignment);
        }

        // POST: TruckAssignments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,TruckId,ShiftId,StartDate,DriverId,TechnicalerId,AirportId")] TruckAssign truckAssignment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(truckAssignment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            ViewBag.DriverId = new SelectList(db.Users.Where(u => names.Contains(u.UserName)), "Id", "FullName", truckAssignment.DriverId);

            var names2 = (from u in context.Users
                          where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                          select u.UserName).ToArray();
            ViewBag.TechnicalerId = new SelectList(db.Users.Where(u => names2.Contains(u.UserName)), "Id", "FullName", truckAssignment.TechnicalerId);

            ViewBag.TruckId = new SelectList(db.Trucks, "Id", "Code", truckAssignment.TruckId);
            ViewBag.ShiftId = new SelectList(db.Shifts, "Id", "Name", truckAssignment.ShiftId);
            return View(truckAssignment);
        }

        [HttpPost]
        public ActionResult JEdit(string json, int id = -1, string startDate = "")
        {
            try
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.TruckAssigns.FirstOrDefault(t => t.Id == id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityName = "TruckAssign";
                entityLog.EntityDisplay = "Phân công xe";
                entityLog.DateChanged = DateTime.Now;

                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }

                if (model != null && json != null)
                {
                    JObject jsonData = JObject.Parse("{'items':" + json + "}");
                    var d = jsonData["items"].FirstOrDefault();
                    var truckId = d["truckId"].Value<int>();
                    var shiftId = d["shiftId"].Value<int>();
                    var driverId = d["driverId"].Value<int>();
                    var technicalerId = d["technicalerId"].Value<int>();

                    var truck = db.Trucks.FirstOrDefault(t => t.Id == truckId);
                    if (truck != null)
                    {
                        entityLog.KeyValues = truck.Code;
                        if (model.TruckId != d["truckId"].Value<int>())
                        {
                            entityLog.PropertyName = "Xe";
                            entityLog.NewValues = truck.Code;
                            truck = db.Trucks.FirstOrDefault(t => t.Id == model.TruckId);
                            if (truck != null)
                                entityLog.OldValues = truck.Code;
                            db.ChangeLogs.Add(entityLog);
                            db.SaveChanges();
                        }
                    }
                    if (model.DriverId != d["driverId"].Value<int>())
                    {
                        entityLog.PropertyName = "Lái xe";
                        var driver = db.Users.FirstOrDefault(u => u.Id == model.DriverId);
                        if (driver != null)
                            entityLog.OldValues = driver.FullName;
                        driver = db.Users.FirstOrDefault(u => u.Id == driverId);
                        if (driver != null)
                            entityLog.NewValues = driver.FullName;
                        db.ChangeLogs.Add(entityLog);
                        db.SaveChanges();
                    }
                    if (model.TechnicalerId != d["technicalerId"].Value<int>())
                    {
                        entityLog.PropertyName = "Nhân viên tra nạp";
                        var tech = db.Users.FirstOrDefault(u => u.Id == model.TechnicalerId);
                        if (tech != null)
                            entityLog.OldValues = tech.FullName;
                        tech = db.Users.FirstOrDefault(u => u.Id == technicalerId);
                        if (tech != null)
                            entityLog.NewValues = tech.FullName;
                        db.ChangeLogs.Add(entityLog);
                        db.SaveChanges();
                    }
                    if (model.ShiftId != d["shiftId"].Value<int>())
                    {
                        entityLog.PropertyName = "Ca";
                        var shift = db.Shifts.FirstOrDefault(u => u.Id == model.ShiftId);
                        if (shift != null)
                            entityLog.OldValues = shift.Name;
                        shift = db.Shifts.FirstOrDefault(u => u.Id == shiftId);
                        if (shift != null)
                            entityLog.NewValues = shift.Name;
                        db.ChangeLogs.Add(entityLog);
                        db.SaveChanges();
                    }
                    TryUpdateModel(model);
                    model.TruckId = truckId;
                    model.ShiftId = shiftId;
                    model.DriverId = driverId;
                    model.TechnicalerId = technicalerId;
                    if (!string.IsNullOrEmpty(startDate))
                        model.StartDate = DateTime.ParseExact(startDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Result = -1;
                ViewBag.Message = ex.Message;
            }
            return Json(new { Status = 0, Message = "Đã sửa" });
        }

        // GET: TruckAssignments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TruckAssign truckAssignment = db.TruckAssigns.Include(t => t.Driver).Include(t => t.Shift).Include(t => t.Technicaler).Include(t => t.Truck).FirstOrDefault(a => a.Id == id);
            if (truckAssignment == null)
            {
                return HttpNotFound();
            }
            return View(truckAssignment);
        }

        // POST: TruckAssignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TruckAssign item = db.TruckAssigns.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.TruckAssigns.Remove(truckAssignment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult JDelete(int id)
        {
            TruckAssign item = db.TruckAssigns.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.TruckAssigns.Remove(item);
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
