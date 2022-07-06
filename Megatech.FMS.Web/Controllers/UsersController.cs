using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using Megatech.FMS.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IO;
using OfficeOpenXml;

namespace Megatech.FMS.Web.Controllers
{
    [Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý chi nhánh, Quản lý tổng công ty, Tra nạp, Quản lý miền")]
    public class UsersController : Controller
    {

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        private IdentityRoleManager _roleManager;
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
        private DataContext db = new DataContext();
        private User user
        {
            get
            {
                var user = db.Users.Include(u => u.Airport).FirstOrDefault(u => u.UserName == User.Identity.Name);
                return user;
            }
        }
        private int currentUserId
        {
            get
            {
                if (user != null)
                    return user.Id;
                else return 0;
            }
        }

        public ActionResult ListGroup(int pageIndex = 1)
        {
            var pageSize = 20;
            var context = new ApplicationDbContext();

            var query = db.UserGroups.AsNoTracking() as IQueryable<UserGroup>;
            if (user != null)
                query = query.Where(u => u.AirportId == user.AirportId);

            ViewBag.ItemCount = query.Count();
            var users = query.OrderBy(a => a.Name_1).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return View(users);
        }
        // GET: Users
        public async Task<ActionResult> Index(int pageIndex = 1)
        {
            var pageSize = 20;
            ViewBag.Airports = db.Airports.ToList();
            ViewBag.Roles = RoleManager.Roles.Where(rl => rl.Name != "Super Admin").ToList();

            var query = db.Users.Include(u => u.Airport);
            if (!string.IsNullOrEmpty(Request["a"]))
            {
                int a_id = Convert.ToInt32(Request["a"]);
                query = query.Where(u => u.AirportId == a_id);
            }


            if (User.IsInRole("Quản lý miền") || User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Tra nạp"))
            {
                var context = new ApplicationDbContext();
                var names = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955" || ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                             select u.UserName).ToArray();
                if (User.IsInRole("Quản lý miền"))
                {
                    names = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955"
                             || ua.RoleId == "32008037-b18f-422c-897d-2328c968285b"
                             || ua.RoleId == "6ac24b4a-e524-4447-b701-2221dfc7af24"
                             || ua.RoleId == "9f825828-7c85-466a-91a4-26acddd688b5"
                             || ua.RoleId == "26d38c84-4e32-4ecb-bf50-186862bdad39"
                             )
                             select u.UserName).ToArray();
                    ViewBag.Roles = RoleManager.Roles.Where(rl => rl.Name == "Quản lý chi nhánh"
                    || rl.Name == "NV tra nạp"
                    || rl.Name == "Lái xe"
                    || rl.Name == "Điều hành"
                    || rl.Name == "Tra nạp").ToList();
                    var ids = db.Airports.Where(a => a.Branch == user.Branch).Select(a => a.Id);
                    query = query.Where(u => names.Contains(u.UserName) && ids.Contains((int)u.AirportId));
                }
                else if (User.IsInRole("Quản lý chi nhánh"))
                {
                    names = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955"
                             || ua.RoleId == "32008037-b18f-422c-897d-2328c968285b"
                             || ua.RoleId == "6ac24b4a-e524-4447-b701-2221dfc7af24"
                             || ua.RoleId == "9f825828-7c85-466a-91a4-26acddd688b5")
                             select u.UserName).ToArray();
                    ViewBag.Roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                    || rl.Name == "Lái xe"
                    || rl.Name == "Điều hành"
                    || rl.Name == "Tra nạp").ToList();
                    query = query.Where(u => names.Contains(u.UserName) && u.AirportId == user.AirportId);
                }
                else
                {
                    ViewBag.Roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp" || rl.Name == "Lái xe").ToList();
                    query = query.Where(u => names.Contains(u.UserName) && u.AirportId == user.AirportId);
                }
            }
            if (!string.IsNullOrEmpty(Request["roles"]) && Request["roles"] != "Chọn nhóm người dùng")
            {
                var r = Request["roles"];
                var context = new ApplicationDbContext();
                var names = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == r)
                             select u.UserName).ToArray();
                query = query.Where(u => names.Contains(u.UserName));
                ViewBag.Role = RoleManager.Roles.FirstOrDefault(rl => rl.Id == r);
            }

            if (!string.IsNullOrEmpty(Request["keyword"]))
            {
                var kw = Request["keyword"];
                query = query.Where(u => u.FullName.Contains(kw) || u.UserName.Contains(kw));
            }


            ViewBag.ItemCount = query.Count();
            var users = query.OrderBy(a => a.DateCreated).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            var temp_users = new List<User>();
            foreach (var item in users)
            {
                var temp_user = new User();
                temp_user.Id = item.Id;
                temp_user.FullName = item.FullName;
                temp_user.ImportName = item.ImportName;
                temp_user.UserName = item.UserName;
                temp_user.Email = item.Email;
                temp_user.IsEnabled = item.IsEnabled;
                var tr_name = "";
                var tus = await UserManager.FindByNameAsync(item.UserName);
                if (tus != null)
                {
                    var r_name = await UserManager.GetRolesAsync(tus.Id);
                    tr_name = string.Join(",", r_name.Select(t => t).ToArray());
                }
                temp_user.RoleName = tr_name;
                temp_users.Add(temp_user);
            }

            return View(temp_users);
        }

        // GET: Personnel
        public ActionResult Personnel(int pageIndex = 1, string r = "")
        {
            var pageSize = 20;
            var airportId = 0;
            ViewBag.Airports = db.Airports.ToList();
            ViewBag.Roles = RoleManager.Roles.FirstOrDefault(rl => rl.Id == r);

            var query = db.Users.Include(u => u.Airport);

            if (User.IsInRole("Quản lý chi nhánh") || User.IsInRole("Tra nạp"))
            {

                var context = new ApplicationDbContext();
                var names = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955" || ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                             select u.UserName).ToArray();
                query = query.Where(u => names.Contains(u.UserName) && u.AirportId == user.AirportId);
                ViewBag.Airports = db.Airports.Where(a => a.Id == user.AirportId).ToList();
            }
            else if (!string.IsNullOrEmpty(Request["a"]))
            {
                int.TryParse(Request["a"], out airportId);
                query = query.Where(u => u.AirportId == airportId);
            }

            if (!string.IsNullOrWhiteSpace(r))
            {
                var context = new ApplicationDbContext();
                var names = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == r)
                             select u.UserName).ToArray();

                query = query.Where(u => names.Contains(u.UserName));
            }

            ViewBag.ItemCount = query.Count();
            var users = query.OrderBy(a => a.DateCreated).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return View(users);
        }
        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            var roles = RoleManager.Roles.Where(r => r.Name != "Super Admin").ToList();
            ViewBag.Airports = db.Airports.ToList();
            if (User.IsInRole("Quản lý miền"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                || rl.Name == "Quản lý chi nhánh"
                || rl.Name == "Lái xe"
                || rl.Name == "Điều hành"
                || rl.Name == "Tra nạp").ToList();
                ViewBag.Airports = db.Airports.Where(a => a.Branch == user.Branch).ToList();
            }
            else if (User.IsInRole("Quản lý chi nhánh"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                || rl.Name == "Lái xe"
                || rl.Name == "Điều hành"
                || rl.Name == "Tra nạp").ToList();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            else if (User.IsInRole("Điều hành") || User.IsInRole("Tra nạp"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp" || rl.Name == "Lái xe").ToList();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            ViewBag.Roles = roles;
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(RegisterViewModel model)
        {
            var roles = RoleManager.Roles.Where(r => r.Name != "Super Admin").ToList();
            ViewBag.Airports = db.Airports.ToList();

            if (User.IsInRole("Quản lý miền"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                || rl.Name == "Quản lý chi nhánh"
                || rl.Name == "Lái xe"
                || rl.Name == "Tra nạp"
                || rl.Name == "Điều hành").ToList();
                ViewBag.Airports = db.Airports.Where(a => a.Branch == user.Branch).ToList();
            }
            else if (User.IsInRole("Quản lý chi nhánh"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                || rl.Name == "Lái xe"
                || rl.Name == "Tra nạp"
                || rl.Name == "Điều hành").ToList();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            else if (User.IsInRole("Tra nạp"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp" || rl.Name == "Lái xe").ToList();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }

            ViewBag.Roles = roles;

            if (ModelState.IsValid)
            {
                var user = ApplicationUser.CreateUser(model.UserName, model.Email, model.FullName);
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    foreach (var item in model.RoleId)
                    {
                        //add to role
                        var role = RoleManager.FindById(item);
                        if (role != null)
                            await UserManager.AddToRoleAsync(user.Id, role.Name);
                    }
                    var airport = db.Airports.FirstOrDefault(a => a.Id == model.AirportId);
                    if (airport != null)
                        user.DbUser.Branch = airport.Branch;
                    else
                        user.DbUser.Branch = Branch.MB;

                    user.DbUser.AirportId = model.AirportId;
                    user.DbUser.IsCreateRefuel = model.IsCreateRefuel;
                    user.DbUser.IsCreateExtract = model.IsCreateExtract;
                    user.DbUser.IsCreateCustomer = model.IsCreateCustomer;
                    user.DbUser.ImportName = model.ImportName;
                    db.Users.Add(user.DbUser);
                    await db.SaveChangesAsync();
                    user.UserId = user.DbUser.Id;
                    await UserManager.UpdateAsync(user);
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Tên đăng nhập hoặc email đã tồn tại.";
                    return View(model);
                }
            }

            return View(model);
        }
        public ActionResult CreateGroup()
        {
            var context = new ApplicationDbContext();
            var names = (from u in context.Users
                         where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                         select u.UserName).ToArray();

            var names_2 = (from u in context.Users
                           where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                           select u.UserName).ToArray();

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                var u_airportId = user.AirportId;
                var ag = db.UserGroups.Where(a => a.AirportId == u_airportId).ToList();
                var id_1 = ag.Select(a => a.Id_1).ToArray();
                var id_2 = ag.Select(a => a.Id_2).ToArray();
                if (User.IsInRole("Quản lý miền") && user.Airport != null)
                {
                    var ids = db.Airports.Where(a => a.Branch == user.Airport.Branch).Select(a => a.Id);
                    ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && ids.Contains((int)u.AirportId) && !id_1.Contains(u.Id)).OrderBy(a => a.FullName).ToList();
                    ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && ids.Contains((int)u.AirportId) && !id_2.Contains(u.Id)).OrderBy(a => a.FullName).ToList();
                }
                else
                {
                    ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && u.AirportId == u_airportId && !id_1.Contains(u.Id)).OrderBy(a => a.FullName).ToList();
                    ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && u.AirportId == u_airportId && !id_2.Contains(u.Id)).OrderBy(a => a.FullName).ToList();
                }
            }
            else
            {
                var ag = db.UserGroups.ToList();
                var id_1 = ag.Select(a => a.Id_1).ToArray();
                var id_2 = ag.Select(a => a.Id_2).ToArray();
                ViewBag.Driver = db.Users.Where(u => names.Contains(u.UserName) && !id_1.Contains(u.Id)).OrderBy(a => a.FullName).ToList();
                ViewBag.Operator = db.Users.Where(u => names_2.Contains(u.UserName) && !id_2.Contains(u.Id)).OrderBy(a => a.FullName).ToList();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateGroup(FormCollection form)
        {
            if (form["d"] != null && form["o"] != null)
            {
                var driver_id = Convert.ToInt32(form["d"]);
                var oper_id = Convert.ToInt32(form["o"]);
                var driver = db.Users.FirstOrDefault(u => u.Id == driver_id);
                var oper = db.Users.FirstOrDefault(u => u.Id == oper_id);
                if(driver != null && oper != null)
                {
                    var ug = new UserGroup();
                    ug.Id_1 = driver.Id;
                    ug.Name_1 = driver.FullName;
                    ug.AirportId = driver.AirportId;

                    ug.Id_2 = oper.Id;
                    ug.Name_2 = oper.FullName;

                    db.UserGroups.Add(ug);
                    db.SaveChanges();
                }      
            }
            return RedirectToAction("ListGroup");
        }

        // GET: Users/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            var roles = RoleManager.Roles.Where(r => r.Name != "Super Admin").ToList();
            ViewBag.Airports = db.Airports.ToList();

            if (User.IsInRole("Quản lý miền"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                || rl.Name == "Quản lý chi nhánh"
                || rl.Name == "Lái xe"
                || rl.Name == "Điều hành"
                || rl.Name == "Tra nạp").ToList();
                ViewBag.Airports = db.Airports.Where(a => a.Branch == user.Branch).ToList();
            }
            else if (User.IsInRole("Quản lý chi nhánh"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                || rl.Name == "Lái xe"
                || rl.Name == "Điều hành"
                || rl.Name == "Tra nạp").ToList();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            else if (User.IsInRole("Tra nạp"))
            {
                roles = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp" || rl.Name == "Lái xe").ToList();
                ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User us = db.Users.Find(id);
            if (us == null)
            {
                return HttpNotFound();
            }

            var temp_user = await UserManager.FindByNameAsync(us.UserName);
            if (temp_user != null)
            {
                var role = await UserManager.GetRolesAsync(temp_user.Id);
                ViewBag.ExistingRoleId = RoleManager.Roles.Where(r => role.Contains(r.Name)).Select(r => r.Id).ToArray();
            }

            ViewBag.Roles = roles;
            return View(us);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,FullName,Email,AirportId,UserName,ImportName,LastLogin,DateCreated,DateUpdated,IsEnabled,DateDeleted,UserDeletedId,IsCreateRefuel,IsCreateExtract,IsCreateCustomer")] User user)
        {
            var rls = RoleManager.Roles.Where(r => r.Name != "Super Admin").ToList();
            ViewBag.Airports = db.Airports.ToList();
            if (ModelState.IsValid)
            {
                //Edit UserGroup
                var user_1 = db.UserGroups.FirstOrDefault(u => u.Id_1 == user.Id);
                if (user_1 != null)
                {
                    user_1.Name_1 = user.FullName;
                    db.SaveChanges();
                }
                var user_2 = db.UserGroups.FirstOrDefault(u => u.Id_2 == user.Id);
                if (user_2 != null)
                {
                    user_2.Name_2 = user.FullName;
                    db.SaveChanges();
                }
                // End Edit UserGroup

                var airport = db.Airports.FirstOrDefault(a => a.Id == user.AirportId);
                if (airport != null)
                    user.Branch = airport.Branch;
                else
                    user.Branch = Branch.MB;

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                if (Request["RoleId"] != null)
                {
                    var temp_user = await UserManager.FindByNameAsync(user.UserName);
                    string[] roles = Request["RoleId"].Split(',').Where(s => s != "false").ToArray();

                    //remove old roles
                    var list = await UserManager.GetRolesAsync(temp_user.Id);
                    if (list.Count() > 0)
                        await UserManager.RemoveFromRolesAsync(temp_user.Id, list.ToArray());

                    foreach (var item in roles)
                    {
                        //add to role
                        var role = RoleManager.FindById(item);
                        if (role != null)
                            await UserManager.AddToRoleAsync(temp_user.Id, role.Name);
                    }
                    await db.SaveChangesAsync();
                }
                if (User.IsInRole("Quản lý miền"))
                {
                    rls = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                    || rl.Name == "Quản lý chi nhánh"
                    || rl.Name == "Lái xe"
                    || rl.Name == "Tra nạp"
                    || rl.Name == "Điều hành").ToList();
                    ViewBag.Airports = db.Airports.Where(ar => ar.Branch == user.Branch).ToList();
                }
                if (User.IsInRole("Quản lý chi nhánh"))
                {
                    rls = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                    || rl.Name == "Lái xe"
                    || rl.Name == "Tra nạp"
                    || rl.Name == "Điều hành").ToList();
                    ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
                }
                else if (User.IsInRole("Tra nạp"))
                {
                    rls = RoleManager.Roles.Where(rl => rl.Name == "NV tra nạp"
                    || rl.Name == "Lái xe").OrderBy(rl => rl.Name).ToList();
                    ViewBag.Airports = db.Airports.Where(ar => ar.Id == user.AirportId).ToList();
                }

                ViewBag.Roles = rls;
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            //Delete UserGroup
            var ug_1 = db.UserGroups.FirstOrDefault(u => u.Id_1 == id);
            if (ug_1 != null)
            {
                db.UserGroups.Remove(ug_1);
                db.SaveChanges();
            }
            var ug_2 = db.UserGroups.FirstOrDefault(u => u.Id_2 == id);
            if (ug_2 != null)
            {
                db.UserGroups.Remove(ug_2);
                db.SaveChanges();
            }
            //End delete UserGroup
            User c_user = db.Users.Find(id);
            var user = await UserManager.FindByNameAsync(c_user.UserName);
            var logins = user.Logins;
            var rolesForUser = await UserManager.GetRolesAsync(user.Id);

            foreach (var login in logins.ToList())
            {
                await UserManager.RemoveLoginAsync(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
            }

            if (rolesForUser.Count() > 0)
            {
                foreach (var item in rolesForUser.ToList())
                {
                    // item should be the name of the role
                    var result = await UserManager.RemoveFromRoleAsync(user.Id, item);
                }
            }

            await UserManager.DeleteAsync(user);
            c_user.IsDeleted = true;
            c_user.UserDeletedId = currentUserId;
            c_user.DateDeleted = DateTime.Now;
            //db.Users.Remove(c_user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DeleteGroup(int id)
        {
            var ug = db.UserGroups.Find(id);
            db.UserGroups.Remove(ug);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }

        [HttpPost]
        public async Task<JsonResult> SetPassword(int id, string password)
        {
            if (Request.IsAjaxRequest())
            {
                if (ModelState.IsValid)
                {
                    var dbuser = db.Users.Find(id);
                    if (dbuser != null)
                    {
                        var user = await UserManager.FindByNameAsync(dbuser.UserName);
                        var token = await UserManager.GeneratePasswordResetTokenAsync(user.Id);

                        var result = await UserManager.ResetPasswordAsync(user.Id, token, password);
                        if (result.Succeeded)
                            return Json(new { result = "OK" });
                        else
                            return Json(new { result = result.Errors.FirstOrDefault().ToString() });
                    }
                }
            }

            return Json(new { result = "Failed" });

        }
        [HttpPost]
        public async Task<JsonResult> UpdateAirport(int id, int newAirportId)
        {
            if (Request.IsAjaxRequest())
            {
                if (ModelState.IsValid)
                {
                    var dbuser = db.Users.Find(id);
                    if (dbuser != null)
                    {
                        dbuser.AirportId = newAirportId;
                        await db.SaveChangesAsync();

                    }
                }
            }
            return Json(new { result = "OK" });
        }

        [HttpPost]
        public async Task<JsonResult> Suspend(int id, bool isEnabled)
        {
            if (Request.IsAjaxRequest())
            {
                if (ModelState.IsValid)
                {

                    var dbuser = db.Users.Find(id);
                    if (dbuser != null)
                    {
                        var user = await UserManager.FindByNameAsync(dbuser.UserName);
                        user.IsEnabled = isEnabled;
                        var result = await UserManager.UpdateAsync(user);
                        if (result.Succeeded)
                        {
                            dbuser.IsEnabled = isEnabled;
                            await db.SaveChangesAsync();
                            return Json(new { result = "OK" });
                        }
                        else
                            return Json(new { result = result.Errors.FirstOrDefault().ToString() });
                    }
                }
            }
            return Json(new { result = "OK" });
        }
        [Authorize(Roles = "Super Admin")]
        public ActionResult Import()
        {
            return View();
        }
        [Authorize(Roles = "Super Admin")]
        [HttpPost]
        [ValidateInput(false)]
        public async Task<ActionResult> Import(FormCollection form)
        {
            using (DataContext db = new DataContext())
            {
                var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

                if (Request.Files["Import"] != null)
                {
                    #region Upload file
                    string[] allowdFile = { ".xls", ".xlsx", ".xml" };
                    string ext = Path.GetExtension(Request.Files["Import"].FileName);
                    string filename = Request.Files["Import"].FileName;

                    bool isValidFile = allowdFile.Contains(ext);
                    if (!isValidFile)
                        return Json(new { error = "Lỗi khi import file" });

                    var filePath = Path.Combine("/UserUpload/", "Data");
                    var physicalPath = System.Web.HttpContext.Current.Server.MapPath(filePath);

                    if (!Directory.Exists(physicalPath))
                        Directory.CreateDirectory(physicalPath);

                    FileInfo newFile = new FileInfo(Path.Combine(physicalPath, filename));
                    if (newFile.Exists)
                    {
                        newFile.Delete();
                        newFile = new FileInfo(Path.Combine(physicalPath, filename));
                    }

                    Request.Files["Import"].SaveAs(Path.Combine(physicalPath, filename));
                    #endregion
                    if (ext == ".xls" || ext == ".xlsx")
                    {
                        using (ExcelPackage xlPackage = new ExcelPackage(newFile))
                        {
                            ExcelWorkbook wkb = xlPackage.Workbook;
                            if (wkb != null)
                            {
                                for (int i = 1; i < wkb.Worksheets.Count + 1; i++)
                                {
                                    int startRow = 2;
                                    ExcelWorksheet currentworksheet = wkb.Worksheets[i];

                                    for (int rowNumber = startRow; rowNumber <= currentworksheet.Dimension.End.Row; rowNumber++)
                                    {
                                        int col = 1;
                                        var userName = "";
                                        if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 3].Text))
                                            userName = currentworksheet.Cells[rowNumber, 3].Text;
                                        else col += 1;
                                        var c_user = db.Users.FirstOrDefault(u => u.UserName == userName);

                                        if (c_user != null)
                                        {
                                            //Update UserGroup
                                            var ug_1 = db.UserGroups.FirstOrDefault(u => u.Id_1 == c_user.Id);
                                            if (ug_1 != null)
                                            {
                                                ug_1.Name_1 = c_user.FullName;
                                                db.SaveChanges();
                                            }
                                            var ug_2 = db.UserGroups.FirstOrDefault(u => u.Id_2 == c_user.Id);
                                            if (ug_2 != null)
                                            {
                                                ug_2.Name_2 = c_user.FullName;
                                                db.SaveChanges();
                                            }
                                            //End update UserGroup
                                            var a_code = "";
                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 1].Text))
                                                a_code = currentworksheet.Cells[rowNumber, 3].Text;

                                            var airport = db.Airports.FirstOrDefault(a => a.Code == a_code);
                                            if (airport != null)
                                            {
                                                c_user.Branch = airport.Branch;
                                            }

                                            else
                                                c_user.Branch = Branch.MB;
                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 4].Text))
                                                c_user.Email = currentworksheet.Cells[rowNumber, 4].Text;

                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 2].Text))
                                                c_user.FullName = currentworksheet.Cells[rowNumber, 2].Text;

                                            db.Entry(c_user).State = EntityState.Modified;
                                            db.SaveChanges();

                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 6].Text))
                                            {
                                                var temp_user = await UserManager.FindByNameAsync(c_user.UserName);
                                                string[] roles = currentworksheet.Cells[rowNumber, 6].Text.Split(',').ToArray();

                                                //remove old roles
                                                var list = await UserManager.GetRolesAsync(temp_user.Id);
                                                if (list.Count() > 0)
                                                    await UserManager.RemoveFromRolesAsync(temp_user.Id, list.ToArray());

                                                foreach (var item in roles)
                                                {
                                                    //add to role
                                                    var role = RoleManager.FindByName(item);
                                                    if (role != null)
                                                        await UserManager.AddToRoleAsync(temp_user.Id, role.Name);
                                                }
                                                await db.SaveChangesAsync();
                                            }
                                        }
                                        else
                                        {
                                            var model = new RegisterViewModel();
                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 3].Text))
                                                model.UserName = currentworksheet.Cells[rowNumber, 3].Text;

                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 4].Text))
                                                model.Email = currentworksheet.Cells[rowNumber, 4].Text;

                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 2].Text))
                                                model.FullName = currentworksheet.Cells[rowNumber, 2].Text;

                                            if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 5].Text))
                                                model.Password = currentworksheet.Cells[rowNumber, 5].Text;

                                            var user = ApplicationUser.CreateUser(model.UserName, model.Email, model.FullName);
                                            var result = await UserManager.CreateAsync(user, model.Password);
                                            if (result.Succeeded)
                                            {
                                                if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 6].Text))
                                                    model.RoleId = currentworksheet.Cells[rowNumber, 6].Text.Split(',').ToList();

                                                foreach (var item in model.RoleId)
                                                {
                                                    //add to role
                                                    var role = RoleManager.FindByName(item);
                                                    if (role != null)
                                                        await UserManager.AddToRoleAsync(user.Id, role.Name);
                                                }

                                                if (!string.IsNullOrWhiteSpace(currentworksheet.Cells[rowNumber, 1].Text))
                                                {
                                                    var code = currentworksheet.Cells[rowNumber, 1].Text;
                                                    var apt = db.Airports.FirstOrDefault(a => a.Code == code);
                                                    if (apt != null)
                                                    {
                                                        user.DbUser.Branch = apt.Branch;
                                                        model.AirportId = apt.Id;
                                                    }
                                                    else
                                                        user.DbUser.Branch = Branch.MB;
                                                }

                                                user.DbUser.AirportId = model.AirportId;
                                                user.DbUser.IsCreateRefuel = model.IsCreateRefuel;
                                                user.DbUser.IsCreateExtract = model.IsCreateExtract;
                                                user.DbUser.IsCreateCustomer = model.IsCreateCustomer;
                                                db.Users.Add(user.DbUser);
                                                await db.SaveChangesAsync();
                                                user.UserId = user.DbUser.Id;
                                                await UserManager.UpdateAsync(user);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                    return Json(new { error = "Lỗi import file" });

                return RedirectToAction("Index");
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
