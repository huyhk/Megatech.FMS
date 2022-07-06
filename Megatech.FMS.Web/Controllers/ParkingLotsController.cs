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
using Megatech.FMS.Web.Models;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class ParkingLotsController : Controller
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
        // GET: ParkingLots
        public ActionResult Index(int p = 1, int a = 0)
        {
            var pageSize = 20;

            ViewBag.Airports = db.Airports.ToList();
            var parkingLots = db.ParkingLots.Include(pl => pl.Airport);

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
                parkingLots = parkingLots.Where(ar => ar.AirportId == user.AirportId);
            }
            if (a > 0)
                parkingLots = parkingLots.Where(ar => ar.AirportId == a);
            parkingLots = parkingLots.OrderBy(t => t.Id).Skip((p - 1) * pageSize).Take(pageSize);
            ViewBag.PageModel = new PagingViewModel { PageIndex = p, PageSize = pageSize, TotalRecords = db.Airports.Count(), Url = Url.Action("Index") };

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "parkinglot" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "parkinglot").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(parkingLots.ToList());
        }

        // GET: ParkingLots/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ParkingLot parkingLot = db.ParkingLots.Find(id);
            if (parkingLot == null)
            {
                return HttpNotFound();
            }
            return View(parkingLot);
        }

        // GET: ParkingLots/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Create()
        {
            ViewBag.Airports = db.Airports.ToList();

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            //ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name");
            return View();
        }

        // POST: ParkingLots/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,AirportId,Code,Name,ParkingLotType")] ParkingLot parkingLot)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    parkingLot.UserCreatedId = user.Id;

                db.ParkingLots.Add(parkingLot);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            //ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name", parkingLot.AirportId);
            return View(parkingLot);
        }

        // GET: ParkingLots/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ParkingLot parkingLot = db.ParkingLots.Find(id);
            if (parkingLot == null)
            {
                return HttpNotFound();
            }
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name", parkingLot.AirportId);

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.AirportId = new SelectList(db.Airports.Where(ar => ar.Id == user.AirportId), "Id", "Name", parkingLot.AirportId);
            }

            return View(parkingLot);
        }

        // POST: ParkingLots/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,AirportId,Code,Name,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId,ParkingLotType")] ParkingLot parkingLot)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.ParkingLots.FirstOrDefault(p => p.Id == parkingLot.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = parkingLot.Id;
                entityLog.EntityName = "ParkingLot";
                entityLog.EntityDisplay = "Bãi đỗ";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = parkingLot.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                if (model.AirportId != parkingLot.AirportId)
                {
                    entityLog.PropertyName = "Sân bay";
                    var air = db.Airports.FirstOrDefault(a => a.Id == model.AirportId);
                    if (air != null)
                        entityLog.OldValues = air.Name;
                    air = db.Airports.FirstOrDefault(a => a.Id == parkingLot.AirportId);
                    if (air != null)
                        entityLog.NewValues = air.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Code != parkingLot.Code)
                {
                    entityLog.PropertyName = "Bãi đỗ";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = parkingLot.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Name != parkingLot.Name)
                {
                    entityLog.PropertyName = "Bến đỗ";
                    entityLog.OldValues = model.Name;
                    entityLog.NewValues = parkingLot.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                TryUpdateModel(model);
                model.AirportId = parkingLot.AirportId;
                model.Code = parkingLot.Code;
                model.Name = parkingLot.Name;
                model.ParkingLotType = parkingLot.ParkingLotType;
                //db.Entry(parkingLot).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name", parkingLot.AirportId);
            return View(parkingLot);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            ParkingLot item = db.ParkingLots.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.ParkingLots.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // GET: ParkingLots/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ParkingLot parkingLot = db.ParkingLots.Find(id);
            if (parkingLot == null)
            {
                return HttpNotFound();
            }
            return View(parkingLot);
        }

        // POST: ParkingLots/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ParkingLot parkingLot = db.ParkingLots.Find(id);
            parkingLot.IsDeleted = true;
            parkingLot.DateDeleted = DateTime.Now;
            if (user != null)
                parkingLot.UserDeletedId = user.Id;
            //db.ParkingLots.Remove(parkingLot);
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
