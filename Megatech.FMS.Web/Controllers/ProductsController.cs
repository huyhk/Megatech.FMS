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
    public class ProductsController : Controller
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
        // GET: Products
        public ActionResult Index()
        {
         
            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "product" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "product").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(db.Products.ToList());       
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection form)
        {
            //if (Request.IsAjaxRequest())
            //{
            if (ModelState.IsValid)
            {    
                var product = new Product { Name = form["Name"], Code = form["Code"] };
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    product.UserCreatedId = user.Id;

                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
                //return Json(new { result = "OK" });
            }
            //    else
            //        return Json(new { result = "Failed" });
            //}
            return View();
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Code,Name,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] Product product)
        {
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.Products.FirstOrDefault(p => p.Id == product.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = product.Id;
                entityLog.EntityName = "Product";
                entityLog.EntityDisplay = "Sản phẩm";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = product.Code;
                if(user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                
                if (model.Code != product.Code)
                {
                    entityLog.PropertyName = "Mã sản phẩm";
                    entityLog.OldValues = model.Code;
                    entityLog.NewValues = product.Code;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Name != product.Name)
                {
                    entityLog.PropertyName = "Tên sản phẩm";
                    entityLog.OldValues = model.Name;
                    entityLog.NewValues = product.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                TryUpdateModel(model);
                model.Code = product.Code;
                model.Name = product.Name;

                //db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            Product item = db.Products.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Products.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product item = db.Products.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.Products.Remove(product);
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
