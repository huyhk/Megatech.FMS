using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using Megatech.FMS.Web.Models;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class AircraftsController : Controller
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
        // GET: Aircraft
        public ActionResult Index(int p = 1, int a = 0)
        {
            var pageSize = 20;
            ViewBag.Customer = db.Airlines.ToList();
            var air = db.Aircrafts.Include(t => t.Customer);
            if (a != 0)
                air = air.Where(t => t.CustomerId == a);
            var aircrafts = air.OrderBy(ar => ar.Id).Skip((p - 1) * pageSize).Take(pageSize);
            ViewBag.PageModel = new PagingViewModel { PageIndex = p, PageSize = pageSize, TotalRecords = db.Aircrafts.Count(), Url = Url.Action("Index") };
            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "aircraft" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "aircraft").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(aircrafts.ToList());
        }

        // GET: Aircraft/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aircraft aircraft = db.Aircrafts.Find(id);
            if (aircraft == null)
            {
                return HttpNotFound();
            }
            return View(aircraft);
        }

        // GET: Aircraft/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Create()
        {
            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Name");
            return View();
        }

        // POST: Aircraft/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,AircraftType,Code,CustomerId,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] Aircraft aircraft)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    aircraft.UserCreatedId = user.Id;

                db.Aircrafts.Add(aircraft);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Code", aircraft.CustomerId);
            return View(aircraft);
        }

        // GET: Aircraft/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aircraft aircraft = db.Aircrafts.Find(id);
            if (aircraft == null)
            {
                return HttpNotFound();
            }
            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Name", aircraft.CustomerId);
            return View(aircraft);
        }

        // POST: Aircraft/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,AircraftType,Code,CustomerId")] Aircraft aircraft)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Aircrafts.FirstOrDefault(p => p.Id == aircraft.Id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = aircraft.Id;
                entityLog.EntityName = "Aircraft";
                entityLog.EntityDisplay = "Tàu bay";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = aircraft.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                if (model.AircraftType != aircraft.AircraftType)
                {
                    entityLog.PropertyName = "Loại tàu bay";
                    entityLog.OldValues = model.AircraftType;
                    entityLog.NewValues = aircraft.AircraftType;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Code != aircraft.Code)
                {
                    entityLog.PropertyName = "Số hiệu";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = aircraft.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.CustomerId != aircraft.CustomerId)
                {
                    entityLog.PropertyName = "Hãng bay";
                    var cus = db.Airlines.FirstOrDefault(a => a.Id == model.CustomerId);
                    if (cus != null)
                        entityLog.OldValues = cus.Name;
                    cus = db.Airlines.FirstOrDefault(a => a.Id == aircraft.CustomerId);
                    if (cus != null)
                        entityLog.NewValues = cus.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                TryUpdateModel(model);
                model.AircraftType = aircraft.AircraftType;
                model.Code = aircraft.Code;
                model.CustomerId = aircraft.CustomerId;

                //db.Entry(aircraft).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Code", aircraft.CustomerId);
            return View(aircraft);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Aircraft item = db.Aircrafts.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Aircrafts.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // GET: Aircraft/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aircraft aircraft = db.Aircrafts.Find(id);
            if (aircraft == null)
            {
                return HttpNotFound();
            }
            return View(aircraft);
        }

        // POST: Aircraft/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aircraft aircraft = db.Aircrafts.Find(id);
            aircraft.IsDeleted = true;
            aircraft.DateDeleted = DateTime.Now;
            if (user != null)
                aircraft.UserDeletedId = user.Id;
            //db.Aircrafts.Remove(aircraft);
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
