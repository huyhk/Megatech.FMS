using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Megatech.FMS.Web.Models;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles ="Super Admin, Admins")]
    public class RolesController : Controller
    {
        IdentityRoleManager _roleManager;
        public IdentityRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().GetUserManager<IdentityRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }
        // GET: Role
        public ActionResult Index()
        {
            var roles = RoleManager.Roles.Where(r => r.Name != "Super Admin").ToList();
            return View(roles);
        }
        public ActionResult Create()
        {

            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Create(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                
                var role = new IdentityRole { Name = form["Name"] };

                var result = await this.RoleManager.CreateAsync(role);

                if (!result.Succeeded)
                {
                    return Json(new { result = "Failed" });
                }

                return Json(new { result = "OK" });
            }
            return Json(new { result = "Failed" });

            

        }
    }
}