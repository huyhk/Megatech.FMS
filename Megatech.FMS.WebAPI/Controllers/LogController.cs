using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Data;
using System.Linq;
using Megatech.FMS.WebAPI.Models;

namespace Megatech.FMS.WebAPI.Controllers
{
    [Route("api/log")]
    public class LogController : ApiController
    {
        [HttpPost]
        [Authorize]
        [Route("api/log/list")]
        public IHttpActionResult PostList(IList<TruckLogModel> model)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            using (var db = new DataContext())
            {
                var user = db.Users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

                foreach (var item in model)
                {
                    var logEntry = new TruckLog
                    {
                        TruckId = item.TruckId,
                        LogTime = item.LogTime,
                        LogType = item.LogType,
                        LogText = item.LogText,
                        TabletId = item.TabletId,
                        DateCreated = DateTime.Now,
                        UserCreatedId = user?.Id,
                        UserId = item.UserId
                    };


                    db.TruckLog.Add(logEntry);
                }
                db.SaveChanges();
            }
            return Ok();
        }
        public IHttpActionResult Post()
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            if (HttpContext.Current.Request.Files.Count > 0)
            {
                HttpPostedFile file = HttpContext.Current.Request.Files[0];
                var ext = Path.GetExtension(file.FileName);
                var name = Path.GetFileNameWithoutExtension(file.FileName);

                file.SaveAs(HostingEnvironment.MapPath("~/logs/" + name + DateTime.Today.ToString("-yyyyMMdd") + ext));
            }

            return Ok();
        }

        public HttpResponseMessage Get(string f = "export.log")
        {
            var filePath = HostingEnvironment.MapPath("~/logs/" + f);
            if (File.Exists(filePath))
            {

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read));
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = Path.GetFileName(filePath);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");

                return response;
            }
            else
                return Request.CreateResponse(HttpStatusCode.Gone);

        }
        [Route("[action]/{id}")]
        public IHttpActionResult Refuel(int id)
        {
            using (DataContext db = DataContext.GetInstance())
            {
                var list = db.Database.SqlQuery<RefuelItemLogModel>("Select * from RefuelItem_logs where id = " + id.ToString());
                return Ok(list);
            }

        }
    }
}
