using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class FlightsController : ApiController
    {
        private DataContext db = new DataContext();

        // GET: api/Flights
        public IQueryable<Flight> GetFlights()
        {
            return db.Flights;
        }

        // GET: api/Flights/5
        [ResponseType(typeof(FlightViewModel))]
        public IHttpActionResult GetFlight(int id)
        {
            var item = db.Flights.Include(f => f.Airline).Where(f => f.Id == id)
                .Select(f => new FlightViewModel
                {
                    Id = f.Id,
                    AircraftCode = f.AircraftCode,
                    FlightCode = f.Code,
                    AircraftType = f.AircraftType,
                    CustomerName = f.Airline.Name,
                    TaxCode = f.Airline.TaxCode,
                    Address = f.Airline.Address,
                    RouteName = f.RouteName,


                })
                .FirstOrDefault();
            var refuels = db.RefuelItems.Where(r => r.FlightId == id && r.Status == REFUEL_ITEM_STATUS.DONE)
                .Select(r => new RefuelViewModel
                {
                    TruckNo = r.Truck.Code,
                    RealAmount = r.Amount,
                    Density = r.Density,
                    StartNumber = r.StartNumber,
                    EndNumber = r.EndNumber,
                    ManualTemperature = r.ManualTemperature,
                    Price = r.Price,
                    TaxRate = r.TaxRate
                }).ToList();
            item.RefuelItems = refuels;

            return Ok(item);
        }

        // PUT: api/Flights/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutFlight(int id, Flight flight)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != flight.Id)
            {
                return BadRequest();
            }

            db.Entry(flight).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlightExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Flights
        [ResponseType(typeof(Flight))]
        public IHttpActionResult PostFlight(Flight flight)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Flights.Add(flight);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = flight.Id }, flight);
        }

        // DELETE: api/Flights/5
        [ResponseType(typeof(Flight))]
        public IHttpActionResult DeleteFlight(int id)
        {
            Flight flight = db.Flights.Find(id);
            if (flight == null)
            {
                return NotFound();
            }

            db.Flights.Remove(flight);
            db.SaveChanges();

            return Ok(flight);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool FlightExists(int id)
        {
            return db.Flights.Count(e => e.Id == id) > 0;
        }
    }
}