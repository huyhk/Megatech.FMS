using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Web;
using System.Web.Mvc;
using FMS.Data;
using Megatech.FMS.Web;
using System.Data.Entity;

namespace TestRoleAccess.Controllers
{
    [FMSAuthorize]
    public class RoleController : Controller
    {
        private DataContext _db = new DataContext();
        public ActionResult Index()
        {
            var roles = _db.Roles.ToList();
            return View(roles);
        }
        public ActionResult Assign(int roleId)
        {
            var controllers = _db.Controllers.Include(c => c.Actions).ToList();
            var role = _db.Roles.Include(r => r.Actions).FirstOrDefault(r => r.Id == roleId);
            ViewBag.Role = role;      
            return View(controllers);
        }
        [HttpPost]
        public ActionResult Assign(FormCollection form)
        {
            var roleId = int.Parse(form["roleId"] ?? "0");
            var role = _db.Roles.Include(r=>r.Actions).FirstOrDefault(r => r.Id == roleId);
            if (role != null)
            {
                var chks = form["chk"].Split(',');
                var ids = chks.Where(chk => chk != "false").Select(chk => int.Parse(chk)).ToArray();
                role.Actions = _db.Actions.Where(a => ids.Contains(a.Id)).ToList();
                _db.SaveChanges();
            }
           
            return RedirectToAction("Index");
        }
    }
}
