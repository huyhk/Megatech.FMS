using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class BM2505Controller : ApiController
    {
        private DataContext db = DataContext.GetInstance();

        [HttpPost]
        [Authorize]
        public IHttpActionResult Post(BM2505Model model)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            if (ModelState.IsValid)
            {
                var entity = db.BM2505s.FirstOrDefault(bm => bm.Id == model.Id);
                if (entity == null && model.Id > 0)
                    return NotFound();

                else if (model.Id == 0)
                {
                    entity = new BM2505
                    {
                        TruckId = model.TruckId,
                        FlightId = model.FlightId,
                        TankNo = model.TankNo,
                        RTCNo = model.RTCNo,
                        Temperature = model.Temperature,
                        Density = model.Density,
                        Density15 = model.Density15,
                        AppearanceCheck = model.AppearanceCheck,
                        DensityCheck = model.DensityCheck,
                        WaterCheck = model.WaterCheck,
                        PressureDiff = model.PressureDiff,
                        HosePressure = model.HosePressure,
                        OperatorId = model.OperatorId,
                        Time = model.Time,
                        IsDeleted = model.IsDeleted,
                        UserCreatedId = user.Id,
                        DateCreated = DateTime.Now,
                        UserUpdatedId = user.Id,
                        DateUpdated = DateTime.Now


                    };
                    db.BM2505s.Add(entity);
                }
                else
                {
                    entity.TruckId = model.TruckId;
                    entity.FlightId = model.FlightId;
                    entity.TankNo = model.TankNo;
                    entity.RTCNo = model.RTCNo;
                    entity.Temperature = model.Temperature;
                    entity.Density = model.Density;
                    entity.Density15 = model.Density15;
                    entity.AppearanceCheck = model.AppearanceCheck;
                    entity.DensityCheck = model.DensityCheck;
                    entity.WaterCheck = model.WaterCheck;
                    entity.PressureDiff = model.PressureDiff;
                    entity.HosePressure = model.HosePressure;
                    entity.OperatorId = model.OperatorId;
                    entity.Time = model.Time;
                    entity.IsDeleted = model.IsDeleted;
                    if (entity.IsDeleted)
                        entity.UserDeletedId = user.Id;

                }

                db.SaveChanges();
                model.Id = entity.Id;
                return Ok(model);

            }
            return BadRequest();

        }

        public IHttpActionResult Get()
        {
            return Ok();
        }

        public IHttpActionResult Get(int id)
        {
            var date = DateTime.Today.AddDays(-7);
            var lst = db.BM2505s.Where(b => b.TruckId == id && b.Time >= date).Select(b => new BM2505Model
            {
                Id = b.Id,
                TruckId = b.TruckId,
                FlightId = b.FlightId,
                FlightCode = b.Flight.Code,
                AircraftCode = b.Flight.AircraftCode,
                TankNo = b.TankNo,
                RTCNo = b.RTCNo,
                Temperature = b.Temperature,
                Density = b.Density,
                Density15 = b.Density15,
                AppearanceCheck = b.AppearanceCheck,
                DensityCheck = b.DensityCheck,
                WaterCheck = b.WaterCheck,
                PressureDiff = b.PressureDiff,
                HosePressure = b.HosePressure,
                OperatorId = b.OperatorId,
                OperatorName = b.Operator.FullName,
                Time = b.Time
            }).ToList();

            return Ok(lst);

        }

    }
}
