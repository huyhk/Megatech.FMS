using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
namespace Megatech.FMS.WebAPI.Controllers
{
    public class UsersController : ApiController
    {
        private DataContext db = new DataContext();
        public IQueryable<UserViewModel> GetUsers()
        {
            string[] techRole = new string[] { "a7661639-6d36-4b29-a8e3-0f425afcd955", "32008037-b18f-422c-897d-2328c968285b" };
            var ctx = ApplicationDbContext.Create();
            var users = (from u in ctx.Users
                         from r in u.Roles
                         where techRole.Contains(r.RoleId)
                         select u.UserId).ToArray();


            //var users = ctx.Users.Where(u => userRoles.Contains(u.Id)).Select(u => u.UserId).ToArray();



            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var airportId = user != null ? user.AirportId : 0;

            return db.Users.Where(u => users.Contains(u.Id) && u.AirportId == airportId).OrderBy(u => u.DisplayName).ThenBy(u => u.FullName).Select(u => new UserViewModel { Id = u.Id, Name = u.FullName }).AsQueryable();
        }
    }
}
