using FMS.Data;
using Megatech.FMS.DataExchange;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Mvc;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
           
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult Test()
        {
            return Content("Test authorize");
        }

        public ActionResult Receipt(int id)
        {
            using (DataContext db = new DataContext())
            {
                var re = db.Receipts.FirstOrDefault(r => r.Id == id);
                MemoryStream stream = new MemoryStream();

                ///byte[] image = re.Image;
                //stream.Write(image, 0, image.Length);

                //Bitmap bitmap = new Bitmap(stream);
                //bitmap.Save(@"E:\Temp\" + id.ToString() + ".jpg");
                //stream.Close();
                //string base64 = Convert.ToBase64String(image);
                return View(re);
            }
        }
    }
}
