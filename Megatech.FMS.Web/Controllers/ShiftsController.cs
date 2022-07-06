using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using FMS.Data;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class ShiftsController : Controller
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
        // GET: Shifts
        public ActionResult Index()
        {
            ViewBag.Airports = db.Airports.ToList();
            var shifts = db.Shifts.Include(s => s.Airport);

              if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
                shifts = shifts.Where(s => s.AirportId == user.AirportId);
            }
           
            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "shift" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "shift").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(shifts.ToList());
        }

        // GET: Shifts/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shift shift = db.Shifts.Find(id);
            if (shift == null)
            {
                return HttpNotFound();
            }
            return View(shift);
        }

        // GET: Shifts/Create
        public ActionResult Create()
        {
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code");

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.AirportId = new SelectList(db.Airports.Where(ar => ar.Id == user.AirportId), "Id", "Code");
            }
            return View();
        }

        // POST: Shifts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,AirportId,StartTime,EndTime,Code,Name,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] Shift shift)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    shift.UserCreatedId = user.Id;

                db.Shifts.Add(shift);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code", shift.AirportId);
            return View(shift);
        }

        // GET: Shifts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shift shift = db.Shifts.Find(id);
            if (shift == null)
            {
                return HttpNotFound();
            }
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code", shift.AirportId);

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.AirportId = new SelectList(db.Airports.Where(ar => ar.Id == user.AirportId), "Id", "Code", shift.AirportId);
            }
            return View(shift);
        }

        // POST: Shifts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,AirportId,StartTime,EndTime,Code,Name,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] Shift shift)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Shifts.FirstOrDefault(p => p.Id == shift.Id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = shift.Id;
                entityLog.EntityName = "Shift";
                entityLog.EntityDisplay = "Quản lý ca";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = shift.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                if (model.AirportId != shift.AirportId)
                {
                    entityLog.PropertyName = "Sân bay";
                    var air = db.Airports.FirstOrDefault(a => a.Id == model.Id);
                    if (air != null)
                        entityLog.OldValues = air.Name;
                    air = db.Airports.FirstOrDefault(a => a.Id == shift.Id);
                    if (air != null)
                        entityLog.NewValues = air.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Code != shift.Code)
                {
                    entityLog.PropertyName = "Mã";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = shift.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Name != shift.Name)
                {
                    entityLog.PropertyName = "Tên";
                    entityLog.OldValues = model.Name;
                    entityLog.NewValues = shift.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.StartTime.ToString("HH:mm") != shift.StartTime.ToString("HH:mm"))
                {
                    entityLog.PropertyName = "Giờ bắt đầu";
                    entityLog.OldValues = model.StartTime.ToString("HH:mm");
                    entityLog.NewValues = shift.StartTime.ToString("HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.EndTime.ToString("HH:mm") != shift.EndTime.ToString("HH:mm"))
                {
                    entityLog.PropertyName = "Giờ kết thúc";
                    entityLog.OldValues = model.EndTime.ToString("HH:mm");
                    entityLog.NewValues = shift.EndTime.ToString("HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                TryUpdateModel(model);
                model.AirportId = shift.AirportId;
                model.Code = shift.Code;
                model.Name = shift.Name;
                model.StartTime = shift.StartTime;
                model.EndTime = shift.EndTime;
                //db.Entry(shift).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code", shift.AirportId);
            return View(shift);
        }

        // GET: Shifts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shift shift = db.Shifts.Find(id);
            if (shift == null)
            {
                return HttpNotFound();
            }
            return View(shift);
        }

        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Shift item = db.Shifts.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Shifts.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // POST: Shifts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Shift item = db.Shifts.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Shifts.Remove(shift);
            db.SaveChanges();
            return RedirectToAction("Index");
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
