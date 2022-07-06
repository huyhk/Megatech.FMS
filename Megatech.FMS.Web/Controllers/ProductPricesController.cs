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
    public class ProductPricesController : Controller
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
        // GET: ProductPrices
        public ActionResult Index()
        {
            ViewBag.Airlines = db.Airlines.ToList();
            ViewBag.Products = db.Products.ToList();
            ViewBag.Airports = db.Airports.ToList();
            ViewBag.Argencies = db.Agencies.ToList();

            var productPrices = db.ProductPrices.Include(p => p.Customer);

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "productprice" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "productprice").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            return View(productPrices.ToList());
        }

        // GET: ProductPrices/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductPrice productPrice = db.ProductPrices.Find(id);
            if (productPrice == null)
            {
                return HttpNotFound();
            }
            return View(productPrice);
        }

        // GET: ProductPrices/Create
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Create()
        {
            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Code");
            return View();
        }

        // POST: ProductPrices/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductId,CustomerId,AirportId,AgencyId,StartDate,Price,Currency,EndDate")] ProductPrice productPrice)
        {
            if (Request.IsAjaxRequest() && ModelState.IsValid)
            {
                //find the price with conflict date range

                //var conflict = db.ProductPrices.Any(p => p.CustomerId == productPrice.CustomerId && p.ProductId == productPrice.ProductId
                //        && ((p.StartDate < productPrice.StartDate && p.EndDate > productPrice.StartDate)
                //        || (p.StartDate < productPrice.EndDate && p.EndDate > productPrice.EndDate)
                //        || (p.StartDate > productPrice.StartDate && p.EndDate < productPrice.EndDate)));
                var conflict = db.ProductPrices.Any(p => p.CustomerId == productPrice.CustomerId && p.ProductId == productPrice.ProductId
                        && (p.StartDate < productPrice.StartDate || p.StartDate > productPrice.StartDate));

                if (conflict)
                    return Json(new { result = "failed", message = "Phạm vi ngày,khách hàng,sản phẩm bị bị trừng lặp hoặc xung đột" });
                var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                    productPrice.UserCreatedId = user.Id;

                db.ProductPrices.Add(productPrice);
                db.SaveChanges();
                return Json(new { result = "OK" });
            }

            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Code", productPrice.CustomerId);
            return View(productPrice);
        }

        // GET: ProductPrices/Edit/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductPrice productPrice = db.ProductPrices.Find(id);
            if (productPrice == null)
            {
                return HttpNotFound();
            }
            ViewBag.ProductId = new SelectList(db.Products, "Id", "Name", productPrice.ProductId);
            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Name", productPrice.CustomerId);

            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name", productPrice.AirportId);
            ViewBag.AgencyId = new SelectList(db.Agencies, "Id", "Name", productPrice.AgencyId);
            return View(productPrice);
        }

        // POST: ProductPrices/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ProductId,CustomerId,AirportId,AgencyId,EndDate,StartDate,Price,Currency,DateCreated,DateUpdated,IsDeleted,DateDeleted,UserDeletedId")] ProductPrice productPrice)
        {
            if (ModelState.IsValid)
            {
                //find the price with conflict date range

                //var conflict = db.ProductPrices.Any(p => p.CustomerId == productPrice.CustomerId && p.ProductId == productPrice.ProductId
                //        && ((p.StartDate < productPrice.StartDate && p.EndDate > productPrice.StartDate)
                //        || (p.StartDate < productPrice.EndDate && p.EndDate > productPrice.EndDate)
                //        || (p.StartDate > productPrice.StartDate && p.EndDate < productPrice.EndDate)));

                //if (conflict)
                //{   
                //    ViewBag.Error = "Phạm vi ngày bị xung đột";
                //}
                //else
                //{
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                var model = db.ProductPrices.FirstOrDefault(p => p.Id == productPrice.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = productPrice.Id;
                entityLog.EntityName = "ProductPrice";
                entityLog.EntityDisplay = "Giá nguyên liệu";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = Math.Round(productPrice.Price).ToString("#,##0");
                if (user != null)
                {
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }
                if (model.ProductId != productPrice.ProductId)
                {
                    entityLog.PropertyName = "Sản phẩm";
                    var product = db.Products.FirstOrDefault(p => p.Id == model.ProductId);
                    if (product != null)
                        entityLog.OldValues = product.Name;
                    product = db.Products.FirstOrDefault(p => p.Id == productPrice.ProductId);
                    if (product != null)
                        entityLog.NewValues = product.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.CustomerId != productPrice.CustomerId)
                {
                    entityLog.PropertyName = "Khách hàng";
                    var customer = db.Airlines.FirstOrDefault(p => p.Id == model.CustomerId);
                    if (customer != null)
                        entityLog.OldValues = customer.Name;
                    customer = db.Airlines.FirstOrDefault(p => p.Id == productPrice.CustomerId);
                    if (customer != null)
                        entityLog.NewValues = customer.Name;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.StartDate.ToString("dd/MM/yyyy") != productPrice.StartDate.ToString("dd/MM/yyyy"))
                {
                    entityLog.PropertyName = "Ngày bắt đầu";
                    entityLog.OldValues = model.StartDate.ToString("dd/MM/yyyy");
                    entityLog.NewValues = productPrice.StartDate.ToString("dd/MM/yyyy");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.EndDate.Value.ToString("dd/MM/yyyy") != productPrice.EndDate.Value.ToString("dd/MM/yyyy"))
                {
                    entityLog.PropertyName = "Ngày kết thúc";
                    entityLog.OldValues = model.EndDate.Value.ToString("dd/MM/yyyy");
                    entityLog.NewValues = productPrice.EndDate.Value.ToString("dd/MM/yyyy");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Price != productPrice.Price)
                {
                    entityLog.PropertyName = "Giá";
                    entityLog.OldValues = Math.Round(model.Price).ToString("#,##0");
                    entityLog.NewValues = Math.Round(productPrice.Price).ToString("#,##0");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Price != productPrice.Currency)
                {
                    entityLog.PropertyName = "Đơn vị";
                    entityLog.OldValues = model.Currency == 0 ? "VNĐ" : "USD";
                    entityLog.NewValues = productPrice.Currency == 0 ? "VNĐ" : "USD";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                TryUpdateModel(model);
                model.ProductId = productPrice.ProductId;
                model.CustomerId = productPrice.CustomerId;
                model.StartDate = productPrice.StartDate;
                model.EndDate = productPrice.EndDate;
                model.Price = productPrice.Price;
                model.Currency = productPrice.Currency;
                //db.Entry(productPrice).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
                //}       
            }

            ViewBag.ProductId = new SelectList(db.Products, "Id", "Name", productPrice.ProductId);
            ViewBag.CustomerId = new SelectList(db.Airlines, "Id", "Name", productPrice.CustomerId);
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Name", productPrice.AirportId);
            ViewBag.AgencyId = new SelectList(db.Agencies, "Id", "Name", productPrice.AgencyId);
            return View(productPrice);
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            ProductPrice item = db.ProductPrices.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.ProductPrices.Remove(item);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        // GET: ProductPrices/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductPrice productPrice = db.ProductPrices.Find(id);
            if (productPrice == null)
            {
                return HttpNotFound();
            }
            return View(productPrice);
        }

        // POST: ProductPrices/Delete/5
        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Điều hành")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProductPrice item = db.ProductPrices.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.ProductPrices.Remove(productPrice);
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
