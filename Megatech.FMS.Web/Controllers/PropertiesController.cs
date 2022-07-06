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
    public class PropertiesController : Controller
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
        private int currentUserId
        {
            get
            {
                if (user != null)
                    return user.Id;
                else return 0;
            }
        }
        // GET: Properties
        public ActionResult Index()
        {
            var type = REPORT_TYPE.BM7009;
            if (!string.IsNullOrEmpty(Request["type"]))
                type = (REPORT_TYPE)Enum.Parse(typeof(REPORT_TYPE), Request["type"]);
            var query = db.Properties.Where(p => p.ReportType == type).OrderBy(p => p.Code);
            
            return View(query.ToList());
        }

        // GET: Properties/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Property property = db.Properties.Find(id);
            if (property == null)
            {
                return HttpNotFound();
            }
            return View(property);
        }

        // GET: Properties/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Properties/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Code,Value1,Value2,Note,SortOrder")] Property property)
        {
            if (ModelState.IsValid)
            {
                property.EntityType = ENTITY_TYPE.FLIGHT;
                property.ReportType = REPORT_TYPE.BM10002;
                if (user != null)
                    property.UserCreatedId = user.Id;
                db.Properties.Add(property);
                db.SaveChanges();
                return RedirectToAction("/Index");
            }

            return View(property);
        }

        // GET: Properties/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Property property = db.Properties.Find(id);
            if (property == null)
            {
                return HttpNotFound();
            }
            return View(property);
        }

        // POST: Properties/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,SortOrder,Code,Value1,Value2,Note")] Property property)
        {
            if (ModelState.IsValid)
            {
                property.DateUpdated = DateTime.Now;
                if (user != null)
                    property.UserCreatedId = user.Id;
                db.Entry(property).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("/Index");
            }
            return View(property);
        }

        // GET: Properties/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Property property = db.Properties.Find(id);
            if (property == null)
            {
                return HttpNotFound();
            }
            return View(property);
        }

        // POST: Properties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Property property = db.Properties.Find(id);
            db.Properties.Remove(property);
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
