using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using TestRoleAccess.App_Start;

namespace TestRoleAccess.Controllers
{
    public class FlightsController : FMSController
    {
       

        // GET: Flights
        public ActionResult Index()
        {
            var flights = db.Flights.Include(f => f.Aircraft).Include(f => f.Airline).Include(f => f.Airport).Include(f => f.ParkingLot);
            return View(flights.ToList());
        }

        // GET: Flights/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return HttpNotFound();
            }
            return View(flight);
        }

        // GET: Flights/Create
        public ActionResult Create()
        {
            ViewBag.AircraftId = new SelectList(db.Aircrafts, "Id", "Name");
            ViewBag.AirlineId = new SelectList(db.Airlines, "Id", "Pattern");
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code");
            ViewBag.ParkingLotId = new SelectList(db.ParkingLots, "Id", "Code");
            return View();
        }

        // POST: Flights/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Code,DepartureScheduledTime,DepartuteTime,ArrivalScheduledTime,ArrivalTime,FlightTime,RouteName,AirlineId,AircraftId,AircraftCode,AircraftType,EstimateAmount,RefuelScheduledTime,RefuelScheduledHours,RefuelTime,ParkingLotId,Parking,DriverName,TechnicalerName,Shift,ShiftStartTime,ShiftEndTime,AirportName,TruckName,Status,AirportId,TotalAmount,Price,StartTime,EndTime,Note,CreatedLocation,FlightType,InvoiceNameCharter,DateCreated,DateUpdated,UserCreatedId,UserUpdatedId,IsDeleted,DateDeleted,UserDeletedId")] Flight flight)
        {
            if (ModelState.IsValid)
            {
                db.Flights.Add(flight);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AircraftId = new SelectList(db.Aircrafts, "Id", "Name", flight.AircraftId);
            ViewBag.AirlineId = new SelectList(db.Airlines, "Id", "Pattern", flight.AirlineId);
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code", flight.AirportId);
            ViewBag.ParkingLotId = new SelectList(db.ParkingLots, "Id", "Code", flight.ParkingLotId);
            return View(flight);
        }

        // GET: Flights/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return HttpNotFound();
            }
            ViewBag.AircraftId = new SelectList(db.Aircrafts, "Id", "Name", flight.AircraftId);
            ViewBag.AirlineId = new SelectList(db.Airlines, "Id", "Pattern", flight.AirlineId);
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code", flight.AirportId);
            ViewBag.ParkingLotId = new SelectList(db.ParkingLots, "Id", "Code", flight.ParkingLotId);
            return View(flight);
        }

        // POST: Flights/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Code,DepartureScheduledTime,DepartuteTime,ArrivalScheduledTime,ArrivalTime,FlightTime,RouteName,AirlineId,AircraftId,AircraftCode,AircraftType,EstimateAmount,RefuelScheduledTime,RefuelScheduledHours,RefuelTime,ParkingLotId,Parking,DriverName,TechnicalerName,Shift,ShiftStartTime,ShiftEndTime,AirportName,TruckName,Status,AirportId,TotalAmount,Price,StartTime,EndTime,Note,CreatedLocation,FlightType,InvoiceNameCharter,DateCreated,DateUpdated,UserCreatedId,UserUpdatedId,IsDeleted,DateDeleted,UserDeletedId")] Flight flight)
        {
            if (ModelState.IsValid)
            {
                db.Entry(flight).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AircraftId = new SelectList(db.Aircrafts, "Id", "Name", flight.AircraftId);
            ViewBag.AirlineId = new SelectList(db.Airlines, "Id", "Pattern", flight.AirlineId);
            ViewBag.AirportId = new SelectList(db.Airports, "Id", "Code", flight.AirportId);
            ViewBag.ParkingLotId = new SelectList(db.ParkingLots, "Id", "Code", flight.ParkingLotId);
            return View(flight);
        }

        // GET: Flights/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return HttpNotFound();
            }
            return View(flight);
        }

        // POST: Flights/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Flight flight = db.Flights.Find(id);
            db.Flights.Remove(flight);
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
