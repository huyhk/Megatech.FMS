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
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class AirlinesController : Controller
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
        // GET: Airlines
        public ActionResult Index()
        {

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "airline" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "airline").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(db.Airlines.ToList());
        }

        // GET: Airlines/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Airline airline = db.Airlines.Find(id);
            if (airline == null)
            {
                return HttpNotFound();
            }
            return View(airline);
        }

        // GET: Airlines/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Airlines/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Code,Name,AirlineType,TaxCode,Unit,Address,InvoiceName,InvoiceTaxCode,InvoiceAddress,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId,Pattern,IsCharter")] Airline airline)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    airline.UserCreatedId = user.Id;

                db.Airlines.Add(airline);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(airline);
        }

        // GET: Airlines/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Airline airline = db.Airlines.Find(id);
            if (airline == null)
            {
                return HttpNotFound();
            }
            return View(airline);
        }

        // POST: Airlines/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Code,Name,AirlineType,TaxCode,Unit,Address,InvoiceName,InvoiceTaxCode,InvoiceAddress,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId,Pattern,IsCharter")] Airline airline)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Airlines.FirstOrDefault(p => p.Id == airline.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = airline.Id;
                entityLog.EntityName = "Airline";
                entityLog.EntityDisplay = "Hãng bay";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = airline.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                if (model.Code != airline.Code)
                {
                    entityLog.PropertyName = "Mã hãng bay";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = airline.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Name != airline.Name)
                {
                    entityLog.PropertyName = "Tên hãng bay";
                    entityLog.OldValues = model.Name;
                    entityLog.NewValues = airline.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.TaxCode != airline.TaxCode)
                {
                    entityLog.PropertyName = "Mã số thuế";
                    entityLog.OldValues = model.TaxCode;
                    entityLog.NewValues = airline.TaxCode;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Address != airline.Address)
                {
                    entityLog.PropertyName = "Địa chỉ";
                    entityLog.OldValues = model.Address;
                    entityLog.NewValues = airline.Address;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.InvoiceName != airline.InvoiceName)
                {
                    entityLog.PropertyName = "Tên hóa đơn";
                    entityLog.OldValues = model.InvoiceName;
                    entityLog.NewValues = airline.InvoiceName;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.InvoiceTaxCode != airline.InvoiceTaxCode)
                {
                    entityLog.PropertyName = "Mã số hóa đơn";
                    entityLog.OldValues = model.InvoiceTaxCode;
                    entityLog.NewValues = airline.InvoiceTaxCode;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.InvoiceAddress != airline.InvoiceAddress)
                {
                    entityLog.PropertyName = "Địa chỉ hóa đơn";
                    entityLog.OldValues = model.InvoiceAddress;
                    entityLog.NewValues = airline.InvoiceAddress;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Pattern != airline.Pattern)
                {
                    entityLog.PropertyName = "Mẫu biểu thức";
                    entityLog.OldValues = model.Pattern;
                    entityLog.NewValues = airline.Pattern;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.AirlineType != airline.AirlineType)
                {
                    entityLog.PropertyName = "Loại hãng bay";
                    entityLog.OldValues = model.AirlineType == 0 ? "Quốc nội" : "Quốc ngoại";
                    entityLog.NewValues = airline.AirlineType == 0 ? "Quốc nội" : "Quốc ngoại";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Unit != airline.Unit)
                {
                    entityLog.PropertyName = "Đơn vị";
                    entityLog.OldValues = model.Unit == 0 ? "Kg" : "Gal";
                    entityLog.NewValues = airline.Unit == 0 ? "Kg" : "Gal";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                TryUpdateModel(model);
                model.Code = airline.Code;
                model.Name = airline.Name;
                model.TaxCode = airline.TaxCode;
                model.Address = airline.Address;
                model.InvoiceName = airline.InvoiceName;
                model.InvoiceTaxCode = airline.InvoiceTaxCode;
                model.InvoiceAddress = airline.InvoiceAddress;
                model.Pattern = airline.Pattern;
                model.IsCharter = airline.IsCharter;
                model.AirlineType = airline.AirlineType;
                model.Unit = airline.Unit;
                //db.Entry(airline).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(airline);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Airline item = db.Airlines.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Airlines.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // GET: Airlines/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Airline airline = db.Airlines.Find(id);
            if (airline == null)
            {
                return HttpNotFound();
            }
            return View(airline);
        }

        // POST: Airlines/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Airline airline = db.Airlines.Find(id);
            airline.IsDeleted = true;
            airline.DateDeleted = DateTime.Now;
            if (user != null)
                airline.UserDeletedId = user.Id;
            //db.Airlines.Remove(airline);
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
