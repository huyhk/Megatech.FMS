using FMS.Data;
using Megatech.FMS.WebAPI.App_Start;
using Megatech.FMS.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Web.Hosting;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class ReviewController : ApiController
    {
        //private DataContext db = DataContext.GetInstance();

        [HttpPost]
        [Authorize]
        public IHttpActionResult Post(ReviewModel model)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var userName = ClaimsPrincipal.Current.Identity.Name;
            var unqiueId = Guid.Parse(model.UniqueId);
            using (DataContext db = new DataContext())
            {
                var user = db.Users.FirstOrDefault(u => u.UserName == userName);

                if (ModelState.IsValid)
                {
                    var entity = db.Reviews.FirstOrDefault(bm => bm.Id == model.Id || bm.UniqueId == unqiueId);
                    if (entity == null && model.Id > 0)
                        return NotFound();
                    var folder = HostingEnvironment.MapPath("~/reviews");
                    var fileName = "REVIEW_" + model.UniqueId + ".jpg";
                    if (model.ImageString != null)
                    {
                        
                        SaveImage(model.ImageString, fileName, folder);
                        //model.ImagePath = Path.Combine(folder, fileName);
                    }

                    else model.ImagePath = null;
                    if (model.Id == 0)
                    {
                        if (model.FlightId == 0)
                        {
                            var f = db.Flights.FirstOrDefault(fl => fl.UniqueId == model.FlightUniqueId);
                            model.FlightId = f == null ? 0 : f.Id;
                        }
                        if (model.FlightId > 0)
                        {
                            entity = new Review
                            {
                                FlightId = model.FlightId,
                                Rate = model.Rate,
                                BadReason = model.BadReviewReason,
                                OtherReason = model.OtherReason,
                                ReviewDate = model.ReviewDate,
                                ImagePath = Path.Combine(folder, fileName),

                                IsDeleted = model.IsDeleted,
                                UserCreatedId = user.Id,
                                DateCreated = DateTime.Now,
                                UserUpdatedId = user.Id,
                                DateUpdated = DateTime.Now


                            };
                            db.Reviews.Add(entity);
                        }
                    }
                    else
                    {
                        entity.Rate = model.Rate;
                        entity.BadReason = model.BadReviewReason;
                        entity.OtherReason = model.OtherReason;
                        entity.ReviewDate = model.ReviewDate;
                        entity.ImagePath = Path.Combine(folder, fileName);
                        entity.IsDeleted = model.IsDeleted;
                        if (entity.IsDeleted)
                            entity.UserDeletedId = user.Id;
                        entity.DateUpdated = DateTime.Now;
                        

                    }

                    db.SaveChanges();
                    model.Id = entity.Id;
                    return Ok(model);

                }
            }
            return BadRequest();

        }

        private void SaveImage(string base64String, string fileName, string folderPath)
        {
            SaveImage(Convert.FromBase64String(base64String), fileName, folderPath);
        }
        private void SaveImage(byte[] bytes, string fileName, string folderPath)
        {
           
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    Image img = Image.FromStream(ms);
                    img.Save(Path.Combine(folderPath, fileName), ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                Logger.AppendLog("IMAGE", "Save image failed " + Path.Combine(folderPath, fileName), "review");
                Logger.LogException(ex, "review");
            }
        }

        public IHttpActionResult Get()
        {
            return Ok();
        }

        public IHttpActionResult Get(int id)
        {
            using (var db = new DataContext())
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
                    DensityDiff = b.DensityDiff,
                    AppearanceCheck = b.AppearanceCheck,
                    DensityCheck = b.DensityCheck,
                    WaterCheck = b.WaterCheck,
                    PressureDiff = b.PressureDiff,
                    HosePressure = b.HosePressure,
                    OperatorId = b.OperatorId,
                    OperatorName = b.Operator.FullName,
                    Time = b.Time,
                    Note = b.Note
                }).ToList();

                return Ok(lst);
            }
        }

    }
}
