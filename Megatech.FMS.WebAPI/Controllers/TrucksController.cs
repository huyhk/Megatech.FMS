using EntityFramework.DynamicFilters;
using FMS.Data;
using Megatech.FMS.WebAPI.App_Start;
using Megatech.FMS.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Description;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class TrucksController : ApiController
    {
        private DataContext db = new DataContext();
        [Route("api/trucks/amount")]
        public IHttpActionResult PostAmount(TruckViewModel truck)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var model = db.Trucks.FirstOrDefault(r => r.Code == truck.Code);
            if (model != null)
            {
                model.CurrentAmount = truck.CurrentAmount;

                db.SaveChanges();
            }
            return Ok();
        }

        [Authorize]
        [Route("api/trucks/fuel")]
        public IHttpActionResult PostFuel(TruckFuelViewModel truckfuel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            db.DisableFilter("IsNotDeleted");

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var model = db.TruckFuels.FirstOrDefault(tf => tf.Id == truckfuel.Id);
            if (model == null)
            {
                model = new TruckFuel
                {
                    UserCreatedId = user.Id,
                    UserUpdatedId = user.Id,
                    QCNo = truckfuel.QCNo,
                    Amount = truckfuel.Amount,
                    TruckId = truckfuel.TruckId,
                    OperatorId = truckfuel.OperatorId,
                    Time = truckfuel.Time,
                    TankNo = truckfuel.TankNo,
                    TicketNo = truckfuel.TicketNo,
                    MaintenanceStaff = truckfuel.MaintenanceStaff,
                    IsDeleted = truckfuel.IsDeleted
                };

                db.TruckFuels.Add(model);
            }
            else
            {

                model.QCNo = truckfuel.QCNo;
                model.Amount = truckfuel.Amount;
                model.TruckId = truckfuel.TruckId;
                model.OperatorId = truckfuel.OperatorId;
                model.Time = truckfuel.Time;
                model.TankNo = truckfuel.TankNo;
                model.TicketNo = truckfuel.TicketNo;
                model.MaintenanceStaff = truckfuel.MaintenanceStaff;
                model.DateUpdated = DateTime.Now;
                model.UserUpdatedId = user.Id;

                model.IsDeleted = truckfuel.IsDeleted;
                if (model.IsDeleted)
                    model.UserDeletedId = user.Id;



            }
            if (db.SaveChanges() > 0)
            {
                var viewModel = db.TruckFuels.Where(tf => tf.Id == model.Id)

                 .Select(tf => new TruckFuelViewModel
                 {
                     Id = tf.Id,
                     TruckId = tf.TruckId,
                     QCNo = tf.QCNo,
                     Amount = tf.Amount,
                     OperatorId = tf.OperatorId,
                     OperatorName = tf.Operator.FullName,
                     Time = tf.Time,
                     TankNo = tf.TankNo,
                     TicketNo = tf.TicketNo,
                     MaintenanceStaff = tf.MaintenanceStaff,
                     IsDeleted = tf.IsDeleted
                 }).FirstOrDefault();

                return Ok(viewModel);

            }
            return Ok();
        }

        [Route("api/trucks/check")]
        public IHttpActionResult PostCheck(TruckViewModel truck)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var model = db.Trucks.FirstOrDefault(r => r.Code == truck.TruckNo && r.Id == truck.Id);

            return Ok(model != null);
        }

        [ResponseType(typeof(TruckViewModel))]
        public IHttpActionResult GetTrucks(string truckNo)
        {
            string denyNewRefuel = ConfigurationManager.AppSettings["DenyNewRefuel"];
            var model = db.Trucks.Select(t => new TruckViewModel
            {
                Id = t.Id,
                Code = truckNo,
                CurrentAmount = t.CurrentAmount,
                AirportId = t.AirportId.Value,
                AirportCode = t.CurrentAirport.Code,
                TaxCode = t.CurrentAirport.TaxCode,
                AllowNewRefuel = t.CurrentAirport.Code != denyNewRefuel,
                ReceiptCount = (int)t.ReceiptCount,
                RefuelCompany = t.RefuelCompany,
                IsFHS = t.RefuelCompany == REFUEL_COMPANY.NAFSC || t.RefuelCompany == REFUEL_COMPANY.TAPETCO,
                //ReceiptCode = t.ReceiptCode
            }).FirstOrDefault(r => r.Code == truckNo);
            return Ok(model);
        }
        [Authorize]
        [Route("api/trucks/{id}")]
        public IHttpActionResult GetTrucks(int id)
        {
            var truckId = Request.Headers.GetValues("Truck-Id").FirstOrDefault();
            var truckCode = Request.Headers.GetValues("Truck-Code").FirstOrDefault();
            Logger.AppendLog("TRUCK", "GetTrucks TruckId: " + truckId + " TruckCode: " + truckCode, "truck");
            var model = db.Trucks.Where(t=>t.Id == id).Select(t => new TruckViewModel
            {
                Id = t.Id,
                Code = t.Code,
                TruckNo = t.Code,
                
                CurrentAmount = t.CurrentAmount,
                AirportId = t.AirportId.Value,
                AirportCode = t.CurrentAirport.Code,
                TaxCode = t.CurrentAirport.TaxCode,
                ReceiptCount = (int)t.ReceiptCount,
                RefuelCompany = t.RefuelCompany,
                IsFHS = t.RefuelCompany == REFUEL_COMPANY.NAFSC || t.RefuelCompany == REFUEL_COMPANY.TAPETCO,
             
            }).FirstOrDefault();

            Logger.AppendLog("TRUCK", "Receipt Count: " + model.ReceiptCount.ToString(), "truck");
            return Ok(model);
        }
        [Authorize]
        public IEnumerable<TruckViewModel> GetTrucks()
        {
            try
            {

                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

                var userName = ClaimsPrincipal.Current.Identity.Name;

                var user = db.Users.FirstOrDefault(u => u.UserName == userName);

                var airportId = user != null ? user.AirportId : 0;
                //Logger.AppendLog("TRUCK", airportId.ToString(), "truck");
                return db.Trucks.Where(t => t.AirportId == airportId).Select(t => new TruckViewModel
                {
                    Id = t.Id,
                    TruckNo = t.Code,
                    Code = t.Code,
                    CurrentAmount = t.CurrentAmount,
                    AirportId = t.AirportId.Value,
                    AirportCode = t.CurrentAirport.Code,
                    TaxCode = t.CurrentAirport.TaxCode,
                    Capacity = t.MaxAmount,
                    RefuelCompany = t.RefuelCompany,
                    IsFHS = t.RefuelCompany == REFUEL_COMPANY.NAFSC || t.RefuelCompany == REFUEL_COMPANY.TAPETCO,
                    ReceiptCount = (int)t.ReceiptCount,
                    //ReceiptCode = t.ReceiptCode
                }).ToList();
            }
            catch (Exception ex)
            {
               //Logger.AppendLog("TRUCK", "Error: " + ex.Message, "truck");
               // Logger.LogException(ex, "truck");
                return null;
            }

        }
        [Authorize]
        [ResponseType(typeof(TruckViewModel))]
        public IHttpActionResult PostTruck(TruckViewModel model)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);
            var truckId = Request.Headers.GetValues("Truck-Id").FirstOrDefault();
            var truckCode = Request.Headers.GetValues("Truck-Code").FirstOrDefault();
            //Logger.AppendLog("TRUCK", "PostTruck TruckId: " + truckId + " TruckCode: " + truckCode + " ReceiptCount: " + model.ReceiptCount.ToString(), "truck");

            var truck = db.Trucks.Where(t => t.Code == model.TruckNo).FirstOrDefault();
            if (truck != null)
            {
                truck.DeviceSerial = model.DeviceSerial;
                truck.TabletSerial = model.TabletSerial;
                truck.DeviceIP = model.DeviceIP;
                truck.PrinterIP = model.PrinterIP;

                truck.CurrentAmount = model.CurrentAmount;

                truck.UserUpdatedId = user.Id;
                truck.DateUpdated = DateTime.Now;
                truck.ReceiptCount = model.ReceiptCount;
                model.Id = truck.Id;

                db.SaveChanges();
            }


            return Ok(model);

        }
        [Route("api/trucks/fuels")]
        public IHttpActionResult GetFuels(int truckId)
        {
            db.DisableFilter("IsNotDeleted");

            var date = DateTime.Today.AddDays(-5);

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var list = db.TruckFuels.Where(tf => tf.TruckId == truckId && tf.Time >= date)
                .OrderByDescending(tf => tf.Time)
                .Select(tf => new TruckFuelViewModel
                {
                    Id = tf.Id,
                    TruckId = tf.TruckId,
                    QCNo = tf.QCNo,
                    Amount = tf.Amount,
                    OperatorId = tf.OperatorId,
                    OperatorName = tf.Operator.FullName,
                    Time = tf.Time,
                    TankNo = tf.TankNo,
                    TicketNo = tf.TicketNo,
                    MaintenanceStaff = tf.MaintenanceStaff,
                    IsDeleted = tf.IsDeleted

                }).ToList();
            return Ok(list);
        }

        [Route("api/trucks/assign")]
        public IHttpActionResult GetAssign(int truckId)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var userId = user != null ? user.Id : 0;
            var airportId = user != null ? user.AirportId : 0;

            var now = DateTime.Now.TimeOfDay;// DbFunctions.CreateTime(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);


            var qshift = db.Shifts.Where(s => (s.StartTime < s.EndTime && DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)
                                            || (s.StartTime > s.EndTime && (DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) <= now
                                                                            || DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) >= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)))

                .Where(s => s.AirportId == airportId);
            var shift = qshift.FirstOrDefault();
            var start = DateTime.Today;
            var end = DateTime.Today;
            var truckAssign = db.TruckAssigns.FirstOrDefault(t => t.ShiftId == shift.Id && t.TruckId == truckId);

            return Ok(truckAssign);
        }
    }
}
