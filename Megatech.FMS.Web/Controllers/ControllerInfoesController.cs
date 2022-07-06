using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using Megatech.FMS.Data.Permissions;

namespace Megatech.FMS.Web.Controllers
{
    public class ControllerInfoesController : Controller
    {
        private DataContext db = new DataContext();

        // GET: ControllerInfoes
        public ActionResult Index()
        {
            return View(db.Controllers.ToList());
        }

        // GET: ControllerInfoes/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ControllerInfo controllerInfo = db.Controllers.Find(id);
            if (controllerInfo == null)
            {
                return HttpNotFound();
            }
            return View(controllerInfo);
        }

        // GET: ControllerInfoes/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ControllerInfoes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,ControllerName,DisplayName,GroupId")] ControllerInfo controllerInfo)
        {
            if (ModelState.IsValid)
            {
                db.Controllers.Add(controllerInfo);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(controllerInfo);
        }

        // GET: ControllerInfoes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ControllerInfo controllerInfo = db.Controllers.Find(id);
            if (controllerInfo == null)
            {
                return HttpNotFound();
            }
            return View(controllerInfo);
        }

        // POST: ControllerInfoes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ControllerName,DisplayName,GroupId")] ControllerInfo controllerInfo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(controllerInfo).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(controllerInfo);
        }

        // GET: ControllerInfoes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ControllerInfo controllerInfo = db.Controllers.Find(id);
            if (controllerInfo == null)
            {
                return HttpNotFound();
            }
            return View(controllerInfo);
        }

        // POST: ControllerInfoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ControllerInfo controllerInfo = db.Controllers.Find(id);
            db.Controllers.Remove(controllerInfo);
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
