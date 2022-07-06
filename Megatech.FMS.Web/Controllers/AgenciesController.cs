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
   
    //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class AgenciesController : Controller
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
        [FMSAuthorize]
        // GET: Airlines
        public ActionResult Index()
        {

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "agency" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "agency").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(db.Agencies.ToList());
        }

        // GET: Airlines/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Agency agency = db.Agencies.Find(id);
            if (agency == null)
            {
                return HttpNotFound();
            }
            return View(agency);
        }

        // GET: Airlines/Create
        [FMSAuthorize]
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Airlines/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Code,Name,TaxCode,Address,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] Agency agency)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    agency.UserCreatedId = user.Id;

                db.Agencies.Add(agency);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(agency);
        }
        [FMSAuthorize]
        // GET: Airlines/Edit/5
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Agency agency = db.Agencies.Find(id);
            if (agency == null)
            {
                return HttpNotFound();
            }
            return View(agency);
        }

        // POST: Airlines/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Code,Name,TaxCode,Address,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] Agency agency)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Agencies.FirstOrDefault(p => p.Id == agency.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = agency.Id;
                entityLog.EntityName = "Agency";
                entityLog.EntityDisplay = "Đại lý";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = agency.Code;
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                if (model.Code != agency.Code)
                {
                    entityLog.PropertyName = "Mã đại lý";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = agency.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Name != agency.Name)
                {
                    entityLog.PropertyName = "Tên đại lý";
                    entityLog.OldValues = model.Name;
                    entityLog.NewValues = agency.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                
                if (model.Address != agency.Address)
                {
                    entityLog.PropertyName = "Địa chỉ";
                    entityLog.OldValues = model.Address;
                    entityLog.NewValues = agency.Address;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                
                
                TryUpdateModel(model);

                model.DateUpdated = DateTime.Now;
                model.Code = agency.Code;
                model.Name = agency.Name;
                model.TaxCode = agency.TaxCode;
                model.Address = agency.Address;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(agency);
        }

        [FMSAuthorize]
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Agency item = db.Agencies.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Airlines.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        [FMSAuthorize]
        // GET: Airlines/Delete/5
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Agency agency = db.Agencies.Find(id);
            if (agency == null)
            {
                return HttpNotFound();
            }
            return View(agency);
        }

        [FMSAuthorize]
        // POST: Airlines/Delete/5
        //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Agency agency = db.Agencies.Find(id);
            agency.IsDeleted = true;
            agency.DateDeleted = DateTime.Now;
            if (user != null)
                agency.UserDeletedId = user.Id;
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