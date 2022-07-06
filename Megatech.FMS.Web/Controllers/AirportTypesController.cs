using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;

namespace Megatech.FMS.Web.Controllers
{
    [FMSAuthorize]
    //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class AirportTypesController : Controller
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
            return View(db.AirportTypes.OrderBy(a => a.Code).ToList());
        }

        // GET: Airports/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AirportType airport = db.AirportTypes.Find(id);
            if (airport == null)
            {
                return HttpNotFound();
            }
            return View(airport);
        }

        // GET: Airports/Create
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Airports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Type,Code")] AirportType airport)
        {
            if (ModelState.IsValid)
            {
                if (user != null)
                    airport.UserCreatedId = user.Id;

                if (!string.IsNullOrEmpty(airport.Code))
                    airport.Code = airport.Code.Trim();
                db.AirportTypes.Add(airport);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(airport);
        }

        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        // GET: Airports/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AirportType airport = db.AirportTypes.Find(id);
            if (airport == null)
            {
                return HttpNotFound();
            }
            return View(airport);
        }

        // POST: Airports/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Type,Code")] AirportType airport)
        {
            if (ModelState.IsValid)
            {
                var model = db.AirportTypes.FirstOrDefault(p => p.Id == airport.Id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = airport.Id;
                entityLog.EntityName = "AirportType";
                entityLog.EntityDisplay = "Loại sân bay";
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

                if (model.Code != airport.Code)
                {
                    entityLog.PropertyName = "Loại sân bay";
                    entityLog.OldValues = model.Type == FlightType.DOMESTIC ? "Quốc nội" : "Quốc ngoại";
                    entityLog.NewValues = airport.Type == FlightType.DOMESTIC ? "Quốc nội" : "Quốc ngoại";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                TryUpdateModel(model);
                model.DateUpdated = DateTime.Now;
                if (!string.IsNullOrEmpty(airport.Code))
                    model.Code = airport.Code.Trim();
                model.Type = airport.Type;

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(airport);
        }

        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            AirportType item = db.AirportTypes.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AirportType airport = db.AirportTypes.Find(id);
            if (airport == null)
            {
                return HttpNotFound();
            }
            return View(airport);
        }


        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AirportType airport = db.AirportTypes.Find(id);
            airport.IsDeleted = true;
            airport.DateDeleted = DateTime.Now;
            if (user != null)
                airport.UserDeletedId = user.Id;

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