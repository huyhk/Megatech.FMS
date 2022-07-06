using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Điều hành, Tra nạp")]
    public class ParkingReportsController : Controller
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
        public ActionResult Index(int p = 1)
        {
            int count = 0;
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var parkingReports = db.ParkingReports
                .AsNoTracking() as IQueryable<ParkingReport>;

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);


                if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                {
                    count = db.CheckTrucks.Count(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year);
                    parkingReports = parkingReports.Where(f =>
                    f.DateCreated.Day == fd.Day
                    && f.DateCreated.Month == fd.Month
                    && f.DateCreated.Year == fd.Year);
                }
                else
                {
                    td = td.AddHours(23);
                    count = db.CheckTrucks.Count(f => f.DateCreated >= fd && f.DateCreated <= td);
                    parkingReports = parkingReports.Where(f =>
                    f.DateCreated >= fd
                    && f.DateCreated <= td
                    );
                }
            }

            count = parkingReports.Count();
            var lst = parkingReports.OrderByDescending(c => c.DateCreated).Skip((p - 1) * pageSize).Take(pageSize).ToList();

            return View(lst);
        }

       
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ParkingReport parkingReport = db.ParkingReports.FirstOrDefault(c => c.Id == id);
            if (parkingReport == null)
                return HttpNotFound();
            return View(parkingReport);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,DateCreated,FromDate,ToDate," +
            "CNKVMB1,CNKVMT1,CNKVMN1,Note1," +
            "CNKVMB2,CNKVMT2,CNKVMN2,Note2," +
            "CNKVMB21,CNKVMT21,CNKVMN21,Note21," +
            "CNKVMB22,CNKVMT22,CNKVMN22,Note22," +
            "CNKVMB23,CNKVMT23,CNKVMN23,Note23," +
            "CNKVMB24,CNKVMT24,CNKVMN24,Note24," +
            "CNKVMB3,CNKVMT3,CNKVMN3,Note3," +
            "CNKVMB4,CNKVMT4,CNKVMN4,Note4," +
            "CNKVMB5,CNKVMT5,CNKVMN5,Note5," +
            "CNKVMB6,CNKVMT6,CNKVMN6,Note6," +
            "CNKVMB7,CNKVMT7,CNKVMN7,Note7," +
            "CNKVMB8,CNKVMT8,CNKVMN8,Note8," +
            "CNKVMB9,CNKVMT9,CNKVMN9,Note9," +
            "CNKVMB10,CNKVMT10,CNKVMN10,Note10," +
            "CNKVMB11,CNKVMT11,CNKVMN11,Note11," +
            "CNKVMB_CB,CNKVMB_L,CNKVMT_CB,CNKVMT_L,CNKVMN_CB,CNKVMN_L,Note12")] ParkingReport parkingReport)
        {
           
            if (ModelState.IsValid)
            {
                //var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                parkingReport.DateCreated = parkingReport.DateCreated;
                if(user != null)
                    parkingReport.UserCreatedId = user.Id;

                db.ParkingReports.Add(parkingReport);
                db.SaveChanges();
                
                return Json(new { result = "OK" });
            }
            return View(parkingReport);
        }

        public ActionResult Edit(int? id)
        {

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ParkingReport parkingReport = db.ParkingReports.Find(id);
            if (parkingReport == null)
                return HttpNotFound();
            return View(parkingReport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,DateCreated,FromDate,ToDate," +
           "CNKVMB1,CNKVMT1,CNKVMN1,Note1," +
            "CNKVMB2,CNKVMT2,CNKVMN2,Note2," +
            "CNKVMB21,CNKVMT21,CNKVMN21,Note21," +
            "CNKVMB22,CNKVMT22,CNKVMN22,Note22," +
            "CNKVMB23,CNKVMT23,CNKVMN23,Note23," +
            "CNKVMB24,CNKVMT24,CNKVMN24,Note24," +
            "CNKVMB3,CNKVMT3,CNKVMN3,Note3," +
            "CNKVMB4,CNKVMT4,CNKVMN4,Note4," +
            "CNKVMB5,CNKVMT5,CNKVMN5,Note5," +
            "CNKVMB6,CNKVMT6,CNKVMN6,Note6," +
            "CNKVMB7,CNKVMT7,CNKVMN7,Note7," +
            "CNKVMB8,CNKVMT8,CNKVMN8,Note8," +
            "CNKVMB9,CNKVMT9,CNKVMN9,Note9," +
            "CNKVMB10,CNKVMT10,CNKVMN10,Note10," +
            "CNKVMB11,CNKVMT11,CNKVMN11,Note11," +
            "CNKVMB_CB,CNKVMB_L,CNKVMT_CB,CNKVMT_L,CNKVMN_CB,CNKVMN_L,Note12")] ParkingReport parkingReport)
        {
            if (ModelState.IsValid)
            {
                db.Entry(parkingReport).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(parkingReport);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ParkingReport parkingReport = db.ParkingReports.Find(id);
            if (parkingReport == null)
                return HttpNotFound();
            return View(parkingReport);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ParkingReport item = db.ParkingReports.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.ParkingReports.Remove(parkingReport);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh")]
        [HttpPost]
        public ActionResult JDelete(int id)
        {
            ParkingReport item = db.ParkingReports.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            //db.ParkingReports.Remove(parkingReport);
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
