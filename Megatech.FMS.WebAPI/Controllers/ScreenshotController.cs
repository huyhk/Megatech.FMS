using System.Web;
using System.Web.Http;
using System.Web.Hosting;
using System.IO;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class ScreenshotController : ApiController
    {
        public IHttpActionResult Post()
        {
            if (HttpContext.Current.Request.Files.Count == 0)
                return BadRequest();
            var file = HttpContext.Current.Request.Files[0];
            var folder = HostingEnvironment.MapPath("~/screenshots");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            file.SaveAs(Path.Combine(folder, file.FileName));
            return Ok();
        }
    }
}
