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
    //[Authorize(Roles = "Super Admin, Admins, Administrators")]
    public class InvoiceFormsController : Controller
    {
        private DataContext db = new DataContext();
        private User user
        {
            get
            {
                var user = db.Users.Include(u => u.Airports).FirstOrDefault(u => u.UserName == User.Identity.Name);
                return user;
            }
        }
        // GET: InvoiceForms
        public ActionResult Index(int p = 1)
        {
            int pageSize = 20;

            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var arports = db.Airports.ToList();
            var invoiceForms = db.InvoiceForms.Include(i => i.Airport).AsNoTracking() as IQueryable<InvoiceForm>;
            var count = invoiceForms.Count();

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var airportId = 0;
                int.TryParse(Request["a"], out airportId);
                if (airportId > 0)
                {
                    invoiceForms = invoiceForms.Where(a => a.AirportId == airportId);
                    count = invoiceForms.Count();
                }           
            }

            if (!string.IsNullOrEmpty(Request["invoicetype"]))
            {
                var invoicetype = 0;
                int.TryParse(Request["invoicetype"], out invoicetype);
                if (invoicetype == 0)
                {
                    invoiceForms = invoiceForms.Where(a => a.InvoiceType == INVOICE_TYPE.INVOICE);
                    count = invoiceForms.Count();
                }          
                else if (invoicetype == 1)
                {
                    invoiceForms = invoiceForms.Where(a => a.InvoiceType == INVOICE_TYPE.BILL);
                    count = invoiceForms.Count();
                }
            }

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "invoiceform" && c.UserUpdatedId == user.Id).OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();
            }      
            else
                ViewBag.ChangeLogs = db.ChangeLogs.Where(c => c.EntityName.ToLower() == "invoiceform").OrderByDescending(c => c.DateChanged).Skip(0).Take(5).ToList();

            ViewBag.Airports = arports;
            ViewBag.ItemCount = count;
            invoiceForms = invoiceForms.OrderByDescending(i => i.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            return View(invoiceForms.ToList());
        }

        // GET: InvoiceForms/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InvoiceForm invoiceForm = db.InvoiceForms.Find(id);
            if (invoiceForm == null)
            {
                return HttpNotFound();
            }
            return View(invoiceForm);
        }

        // GET: InvoiceForms/Create
        public ActionResult Create()
        {
            var airports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id)).ToList();
            }
               
            ViewBag.AirportId = new SelectList(airports, "Id", "Code");
            return View();
        }

        // POST: InvoiceForms/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[Authorize(Roles = "Super Admin, Admins, Administrators")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AirportId,InvoiceType,FormNo,Sign,IsDefault")] InvoiceForm invoiceForm)
        {
            if (Request.IsAjaxRequest())
            {
                var ivf = db.InvoiceForms.FirstOrDefault(i => i.AirportId == invoiceForm.AirportId && i.InvoiceType == invoiceForm.InvoiceType && i.IsDefault);
                if (invoiceForm.IsDefault && ivf != null)
                {
                    if (ivf.InvoiceType == INVOICE_TYPE.BILL)
                        return Json(new { result = "NOT_OK_BILL" });
                    else
                        return Json(new { result = "NOT_OK_INVOICE" });
                }
                else
                {
                    if (user != null)
                        invoiceForm.UserCreatedId = user.Id;

                    db.InvoiceForms.Add(invoiceForm);
                    db.SaveChanges();
                    return Json(new { result = "OK" });
                }
            }
            var airports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id)).ToList();
            }
            ViewBag.AirportId = new SelectList(airports, "Id", "Code", invoiceForm.AirportId);
            return View(invoiceForm);
        }

        // GET: InvoiceForms/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InvoiceForm invoiceForm = db.InvoiceForms.Find(id);
            if (invoiceForm == null)
            {
                return HttpNotFound();
            }
            var airports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id)).ToList();
            }
            ViewBag.AirportId = new SelectList(airports, "Id", "Code", invoiceForm.AirportId);
            return View(invoiceForm);
        }

        // POST: InvoiceForms/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,AirportId,InvoiceType,FormNo,Sign,IsDefault")] InvoiceForm invoiceForm)
        {
            if (ModelState.IsValid)
            {
                var ivf = db.InvoiceForms.FirstOrDefault(i => i.AirportId == invoiceForm.AirportId && i.InvoiceType == invoiceForm.InvoiceType && i.IsDefault && i.Id != invoiceForm.Id);
                invoiceForm.DateUpdated = DateTime.Now;
                if (invoiceForm.IsDefault && ivf != null)
                {
                    if (ivf.InvoiceType == INVOICE_TYPE.BILL)
                        ViewBag.Error = "Sân bay này đã tồn tại phiếu xuất kho mặc định";
                    else
                        ViewBag.Error = "Sân bay này đã tồn tại hóa đơn mặc định";
                }
                else
                {
                    db.Entry(invoiceForm).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            var airports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id)).ToList();
            }
            ViewBag.AirportId = new SelectList(airports, "Id", "Code", invoiceForm.AirportId);
            return View(invoiceForm);
        }

        // GET: InvoiceForms/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InvoiceForm invoiceForm = db.InvoiceForms.Find(id);
            if (invoiceForm == null)
            {
                return HttpNotFound();
            }
            return View(invoiceForm);
        }

        // POST: InvoiceForms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            InvoiceForm invoiceForm = db.InvoiceForms.Find(id);
            db.InvoiceForms.Remove(invoiceForm);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //[Authorize(Roles = "Super Admin, Admins, Administrators")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            InvoiceForm item = db.InvoiceForms.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;

            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
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
