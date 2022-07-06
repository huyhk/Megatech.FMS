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

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class AirportsController : Controller
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
        // GET: Airports
        public ActionResult Index()
        {
            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "airport" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "airport").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            var lst = Airport.ViewAll(false, 1, 50);
            if (User.IsInRole("Super Admin"))
                lst = Airport.ViewAll(true, 1, 50);
            return View(lst);
        }

        // GET: Airports/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Airport airport = db.Airports.Find(id);
            if (airport == null)
            {
                return HttpNotFound();
            }
            return View(airport);
        }

        // GET: Airports/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Airports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,DepotType,Code,Address,SetTime,Branch")] Airport airport)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    airport.UserCreatedId = user.Id;

                db.Airports.Add(airport);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(airport);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        // GET: Airports/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Airport airport = db.Airports.Find(id);
            if (airport == null)
            {
                return HttpNotFound();
            }
            return View(airport);
        }

        // POST: Airports/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,DepotType,Code,Address,SetTime,Branch")] Airport airport)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Airports.FirstOrDefault(p => p.Id == airport.Id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = airport.Id;
                entityLog.EntityName = "Airport";
                entityLog.EntityDisplay = "Sân bay";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = airport.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }

                if (model.Code != airport.Code)
                {
                    entityLog.PropertyName = "Mã sân bay";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = airport.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Name != airport.Name)
                {
                    entityLog.PropertyName = "Tên sân bay";
                    entityLog.OldValues = model.Name;
                    entityLog.NewValues = airport.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Address != airport.Address)
                {
                    entityLog.PropertyName = "Địa chỉ";
                    entityLog.OldValues = model.Address;
                    entityLog.NewValues = airport.Address;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Branch != airport.Branch)
                {
                    entityLog.PropertyName = "Vùng miền";
                    entityLog.OldValues = model.Branch == Branch.MB ? "Miền Bắc": model.Branch == Branch.MN ? "Miền Nam":"Miền Trung";
                    entityLog.NewValues = airport.Branch == Branch.MB ? "Miền Bắc" : model.Branch == Branch.MN ? "Miền Nam" : "Miền Trung";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                TryUpdateModel(model);
                model.Code = airport.Code;
                model.Name = airport.Name;
                model.Address = airport.Address;
                model.Branch = airport.Branch;
                model.DepotType = airport.DepotType;

                //db.Entry(airport).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(airport);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Airport item = db.Airports.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Airports.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        // GET: Airports/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Airport airport = db.Airports.Find(id);
            if (airport == null)
            {
                return HttpNotFound();
            }
            return View(airport);
        }

        // POST: Airports/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Airport airport = db.Airports.Find(id);
            airport.IsDeleted = true;
            airport.DateDeleted = DateTime.Now;
            if (user != null)
                airport.UserDeletedId = user.Id;
            //db.Airports.Remove(airport);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Import()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Import(FormCollection form)
        {
            if (Request != null)
            {
                var folder = Path.GetTempPath();
                var fileToSave = Path.Combine(folder, Path.GetTempFileName());
                HttpPostedFileBase file = Request.Files[0];
                file.SaveAs(fileToSave);
                var list = AirportImporter.ImportFile(fileToSave);
                if (list.Count > 0)
                {
                    using (DataContext db = new DataContext())
                    {
                        foreach (var item in list)
                        {
                            db.Airports.AddOrUpdate(a => a.Code, item);

                        }
                        db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("index");
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
