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
using Microsoft.AspNet.Identity;
using Newtonsoft.Json.Linq;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class TrucksController : Controller
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
        // GET: Trucks
        public ActionResult Index(int p = 1, int a = 0)
        {
            ViewBag.Airports = db.Airports.ToList();

            var pageSize = 20;
            var count = db.Trucks.Count();

            var trucks = db.Trucks.Include(t => t.Device).Include(t => t.CurrentAirport);
            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                trucks = trucks.Where(t => t.AirportId == user.AirportId);
                count = trucks.Count();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            if (a != 0)
            {
                trucks = trucks.Where(t => t.AirportId == a);
                count = trucks.Count();
            }

            trucks = trucks.OrderBy(t => t.Id).Skip((p - 1) * pageSize).Take(pageSize);

            var items = trucks.ToList();
            var currentAriport = db.Airports;
            items.ForEach(item => item.CurrentAirport = currentAriport.FirstOrDefault(ap => ap.Id == item.AirportId));

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "truck" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "truck").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            ViewBag.PageModel = new PagingViewModel { PageIndex = p, PageSize = pageSize, TotalRecords = count, Url = Url.Action("Index") };
            return View(items);
        }

        // GET: Trucks/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Truck truck = db.Trucks.Find(id);
            if (truck == null)
            {
                return HttpNotFound();
            }
            return View(truck);
        }

        // GET: Trucks/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Create()
        {
            ViewBag.Airports = db.Airports.ToList();

            if (User.IsInRole("Quản lý chi nhánh"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            ViewBag.DeviceId = new SelectList(db.Devices, "Id", "SerialNumber");
            return View();
        }

        // POST: Trucks/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TruckNumber,Code,TruckType,ImportCode,MaxAmount,Unit,AirportId,MaterialCode")] Truck truck)
        {
            ViewBag.Airports = db.Airports.ToList();
            ViewBag.DeviceId = new SelectList(db.Devices, "Id", "SerialNumber", truck.DeviceId);
            //if (Request.IsAjaxRequest())
            //{
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var t_truck = new Truck();
                if (user != null)
                {
                    truck.UserCreatedId = user.Id;
                    t_truck = db.Trucks.FirstOrDefault(t => t.ImportCode == truck.ImportCode && truck.AirportId == user.AirportId);
                }
                if (t_truck != null)
                {
                    Response.Write("<script language=javascript>alert('Mã import này đã tồn tại.Vui lòng nhập mã import khác.');</script>");
                    return View();
                }
                else
                {
                    db.Trucks.Add(truck);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                    //return Json(new { result = "OK" });
                }
            }
            //else
            //    return Json(new { result = "Failed" });
            //}   
            return View(truck);
        }

        // GET: Trucks/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Truck truck = db.Trucks.Find(id);
            if (truck == null)
            {
                return HttpNotFound();
            }
            var arp = db.Airports.ToList();

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                arp = arp.Where(ar => ar.Id == user.AirportId).ToList();
            }
            ViewBag.AirportId = new SelectList(arp, "Id", "Name", truck.AirportId);
            ViewBag.DeviceId = new SelectList(db.Devices, "Id", "SerialNumber", truck.DeviceId);
            return View(truck);
        }

        // POST: Trucks/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,DeviceId,TabletId,AirportId,Code,ImportCode,MaxAmount,Unit,CurrentAmount,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId,TruckNumber,TruckType,MaterialCode")] Truck truck)
        {
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name", truck.AirportId);
            ViewBag.DeviceId = new SelectList(db.Devices, "Id", "SerialNumber", truck.DeviceId);
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Trucks.FirstOrDefault(p => p.Id == truck.Id);

                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = truck.Id;
                entityLog.EntityName = "Truck";
                entityLog.EntityDisplay = "Xe tra nạp";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = truck.Code;

                var t_truck = new Truck();
                var airport_id = -1;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                    airport_id = (int)user.AirportId;
                }

                if (User.IsInRole("Super Admin"))
                    t_truck = db.Trucks.FirstOrDefault(t => t.ImportCode == truck.ImportCode && t.Id != truck.Id);
                else
                    t_truck = db.Trucks.FirstOrDefault(t => t.ImportCode == truck.ImportCode && truck.AirportId == airport_id && t.Id != truck.Id);

                if (model.AirportId != truck.AirportId)
                {
                    entityLog.PropertyName = "Sân bay";
                    var air = db.Airports.FirstOrDefault(a => a.Id == model.AirportId);
                    if (air != null)
                        entityLog.OldValues = air.Name;
                    air = db.Airports.FirstOrDefault(a => a.Id == truck.AirportId);
                    if (air != null)
                        entityLog.NewValues = air.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Code != truck.Code)
                {
                    entityLog.PropertyName = "Mã số xe";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = truck.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.MaxAmount != truck.MaxAmount)
                {
                    entityLog.PropertyName = "Dung tích bồn";
                    entityLog.OldValues = Math.Round(model.MaxAmount).ToString();
                    entityLog.NewValues = Math.Round(truck.MaxAmount).ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Unit != truck.Unit)
                {
                    entityLog.PropertyName = "Đơn vị";
                    entityLog.OldValues = model.Unit;
                    entityLog.NewValues = truck.Unit;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (t_truck != null)
                {
                    Response.Write("<script language=javascript>alert('Mã import này đã tồn tại.Vui lòng nhập mã import khác.');</script>");
                    return View(truck);
                }
                else
                {
                    TryUpdateModel(model);
                    model.AirportId = truck.AirportId;
                    model.Code = truck.Code;
                    model.MaxAmount = truck.MaxAmount;
                    model.Unit = truck.Unit;
                    model.TruckType = truck.TruckType;
                    model.MaterialCode = truck.MaterialCode;
                    model.TruckNumber = truck.TruckNumber;
                    model.ImportCode = truck.ImportCode;
                    //db.Entry(truck).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(truck);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(FormCollection form)
        {
            if (Request.IsAjaxRequest())
            {
                try
                {
                    if (!string.IsNullOrEmpty(form["id"]))
                    {
                        var ids = form["id"].Split(',').Select(x => int.Parse(x.ToString()));
                        if (ids.Count() > 0 && !string.IsNullOrEmpty(form["AirportId"]))
                        {
                            var air_id = Convert.ToInt32(form["AirportId"]);
                            var list = db.Trucks.Where(c => ids.ToList().Contains(c.Id)).ToList();
                            list.ForEach(a =>
                            {
                                a.AirportId = air_id;
                            });
                            db.SaveChanges();

                            //var truck_ass = new TruckAssignment();
                            //truck_ass.AirportId = air_id;
                            //var user_name = User.Identity.GetUserName();
                            //var current_user = db.Users.FirstOrDefault(u => u.UserName == user_name);
                            //if (current_user != null)
                            //    truck_ass.UserId = current_user.Id;
                            //else truck_ass.UserId = 0;
                            //truck_ass.DateCreated = DateTime.Today;
                            //db.TruckAssigns.Add(truck_ass);
                            //db.SaveChanges();

                            return Json(new { result = "OK" });
                        }
                        else
                            return Json(new { result = "Failed" });
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Result = -1;
                    ViewBag.Message = ex.Message;
                }
            }
            ViewBag.Airports = db.Airports.ToList();
            return View();
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Truck item = db.Trucks.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Trucks.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // GET: Trucks/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Truck truck = db.Trucks.Find(id);
            if (truck == null)
            {
                return HttpNotFound();
            }
            truck.CurrentAirport = db.Airports.FirstOrDefault(ap => ap.Id == truck.AirportId);
            return View(truck);
        }

        // POST: Trucks/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Truck truck = db.Trucks.Find(id);
            truck.IsDeleted = true;
            truck.DateDeleted = DateTime.Now;
            if (user != null)
                truck.UserDeletedId = user.Id;
            //db.Trucks.Remove(truck);
            db.SaveChanges();

            TruckAssign item = db.TruckAssigns.FirstOrDefault(t => t.TruckId == id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.TruckAssigns.Remove(truck_assign);
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
