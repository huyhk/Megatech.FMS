using FMS.Data;
using Megatech.FMS.WebAPI.App_Start;
using Megatech.FMS.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Description;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class AirlinesController : ApiController
    {
        public AirlinesController()
        {
            ImportTask.Execute();
        }
        private DataContext db = new DataContext();

        [ApiAuthorize]
        // GET: api/Airlines
        public IEnumerable<AirlineViewModel> GetAirlines()
        {


            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var airportId = user != null ? user.AirportId : 0;

            var airport = db.Airports.FirstOrDefault(a => a.Id == airportId);
            var branchId = airportId != null ? (int)airport.Branch : 1;


            var today = DateTime.Today;


            //select general price
            var gprice = db.ProductPrices.Include(p => p.Product).FirstOrDefault(p => p.StartDate <= today && p.Customer == null);
            if (gprice == null) gprice = new ProductPrice { Price = 0, Product = new Product { Name = "" } };

            var localG = new ProductPrice { Price = 50000, Unit = (int)UNIT.GALLON, Currency = CURRENCY.VND };
            var localK = new ProductPrice { Price = 10000, Unit = (int)UNIT.KG, Currency = CURRENCY.VND };

            //var localprices = from p in db.ProductPrices.Include(p => p.Product)
            //                  where p.StartDate <= today && p.DepotType == 0 // && p.AirlineType == 0
            //                  group p by new { p.AirlineType, p.BranchId, p.DepotType, p.Unit }
            //             into groups
            //                  select groups.OrderByDescending(g => g.StartDate).FirstOrDefault();

            var prices = from p in db.ProductPrices.Include(p => p.Product)
                         where p.StartDate <= today //&& p.AirlineType == 1 && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch
                         group p by new { p.CustomerId, p.AirlineType, p.BranchId, p.DepotType, p.Unit }
                         into groups
                         select groups.OrderByDescending(g => g.StartDate).FirstOrDefault();

            var priceList = prices.ToList();

            var list = (from a in db.Airlines
                        orderby a.Name

                        //let  p = prices.Where(p=>p.BranchId == branchId && p.DepotType == airport.DepotType && p.AirlineType == a.AirlineType && a.Unit  == (int) p.Unit).FirstOrDefault()

                        //let  p1 = localprices.Where(p=>(int)p.Unit == a.Unit && p.AirlineType == a.AirlineType).FirstOrDefault()


                        //join p in prices
                        //on a.Id equals p.CustomerId into hasPrice
                        //from hs in hasPrice.DefaultIfEmpty()
                        select new AirlineViewModel
                        {
                            Id = a.Id,
                            Name = a.Name ?? "",
                            Code = a.Code ?? "",
                            TaxCode = a.TaxCode ?? "",
                            Address = a.Address ?? "",

                            InvoiceAddress = a.InvoiceAddress,
                            InvoiceName = a.InvoiceName,
                            InvoiceTaxCode = a.InvoiceTaxCode,
                            IsInternational = a.AirlineType == 1,
                            Unit = (int)a.Unit

                        }).ToList();

            // Debug.WriteLine("Start loop:" + DateTime.Now.ToString());

            for (int i = 0; i < list.Count; i++)
            {

                var a = list[i];


                if (!a.IsInternational)
                {
                    a.Currency = CURRENCY.VND;
                    a.Price = a.Unit == (int)UNIT.GALLON ? localG.Price : localK.Price;
                    a.Price01 = a.Unit == (int)UNIT.GALLON ? localG.Price : localK.Price;
                    a.ProductName = gprice.Product.Name;

                    var pp = priceList.OrderByDescending(p => p.StartDate)
                        .FirstOrDefault(p => p.CustomerId == a.Id);
                    //if (pp == null)
                    //    pp = priceList.OrderByDescending(p => p.StartDate)
                    //     .FirstOrDefault(p => (p.Unit == a.Unit && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch));

                    if (pp != null)
                    {
                        a.Price = pp.Price;
                        a.Price01 = pp.Price;
                        a.Currency = pp.Currency;
                        a.ProductName = pp.Product.Name;
                        a.PriceId = pp.Id;


                    }
                }
                else
                {
                    var pp = priceList.OrderByDescending(p => p.StartDate)
                         .FirstOrDefault(p => p.AirlineType == 1 && p.CustomerId == a.Id);
                    if (pp == null)
                        pp = priceList.OrderByDescending(p => p.StartDate)
                         .FirstOrDefault(p => p.AirlineType == 1 && (p.Unit == a.Unit && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch));

                    var pp01 = priceList.OrderByDescending(p => p.StartDate)
                         .FirstOrDefault(p => p.AirlineType == 0 && p.CustomerId == a.Id);
                    if (pp01 == null)
                        pp01 = priceList.OrderByDescending(p => p.StartDate)
                         .FirstOrDefault(p => p.AirlineType == 0 && (p.Unit == a.Unit && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch));
                    if (pp != null)
                    {
                        a.Price = pp.Price;
                        a.Currency = pp.Currency;
                        a.ProductName = pp.Product.Name;
                        a.PriceId = pp.Id;


                    }
                    if (pp01 != null)
                    {
                        a.Price01 = pp01.Price;
                        a.Currency = pp01.Currency;
                        a.ProductName = pp01.Product.Name;
                        a.Price01Id = pp01.Id;
                    }
                    else a.Price01 = a.Price;
                }

            }
            //Debug.WriteLine("End loop:" + DateTime.Now.ToString());
            return list;

        }

        // GET: api/Airlines/5
        [ResponseType(typeof(Airline))]
        public IHttpActionResult GetAirline(int id)
        {
            Airline airline = db.Airlines.Find(id);
            if (airline == null)
            {
                return NotFound();
            }

            return Ok(airline);
        }

        // PUT: api/Airlines/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutAirline(int id, Airline airline)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != airline.Id)
            {
                return BadRequest();
            }

            db.Entry(airline).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AirlineExists(id))
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

        // POST: api/Airlines
        [ResponseType(typeof(AirlineViewModel))]
        public IHttpActionResult PostAirline(AirlineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var airline = db.Airlines.FirstOrDefault(a => a.Code.Equals(model.Code, StringComparison.InvariantCultureIgnoreCase));
            if (airline == null)
            {
                airline = new Airline { Code = model.Code ?? "N/A", Name = model.Name ?? "N/A", Address = model.Address, TaxCode = model.TaxCode };
                var product = db.Products.FirstOrDefault();
                var price = new ProductPrice { StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), Customer = airline, Product = product, Price = model.Price };
                db.ProductPrices.Add(price);
                db.SaveChanges();
                model.Id = airline.Id;
                model.ProductName = product.Name;

            }
            return Ok(model);
        }

        // DELETE: api/Airlines/5
        [ResponseType(typeof(Airline))]
        public IHttpActionResult DeleteAirline(int id)
        {
            Airline airline = db.Airlines.Find(id);
            if (airline == null)
            {
                return NotFound();
            }

            db.Airlines.Remove(airline);
            db.SaveChanges();

            return Ok(airline);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AirlineExists(int id)
        {
            return db.Airlines.Count(e => e.Id == id) > 0;
        }
    }
}