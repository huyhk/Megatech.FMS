using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class InvoiceFormController : ApiController
    {
        private DataContext db = new DataContext();
        [Authorize]
        public List<InvoiceFormModel> Get()
        {

            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var userId = user != null ? user.Id : 0;
            var airportId = user != null ? user.AirportId : 0;

            return db.InvoiceForms.Where(r => r.AirportId == airportId)
                .Select(r => new InvoiceFormModel { Id = r.Id, AirportId = (int)r.AirportId, FormNo = r.FormNo, InvoiceType = r.InvoiceType, IsDefault = r.IsDefault, Sign = r.Sign }).ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (db != null)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
