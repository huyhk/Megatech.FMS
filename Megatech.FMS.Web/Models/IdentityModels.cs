using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using FMS.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Megatech.FMS.Web.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            
                // Add custom user claims here
                return userIdentity;
        }


        public bool IsEnabled { get; set; }

        public int UserId { get; set; }

        [NotMapped]
        //dbcontext user
        public virtual User DbUser { get; set; }

        public static ApplicationUser CreateUser(string username, string email, string fullName)
        {
            var appUser = new ApplicationUser { Email = email, UserName = username, IsEnabled = true, DbUser = new User { UserName = username, FullName = fullName, IsEnabled = true, Email = email } };
           
            
            return appUser;

        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("FMSConnection", throwIfV1Schema: false)
        {
            //Database.SetInitializer(new ApplicationDbContextInitializer(this));
            //Database.Initialize(true);
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
            
        }
    }

    public class ApplicationRole : IdentityRole
    {

        public string Code { get; set; }


        public int RoleId { get; set; }

        public Role DbRole { get; set; }
    }

    public class ApplicationDbContextInitializer : CreateDatabaseIfNotExists<ApplicationDbContext>
    {
        private ApplicationDbContext _context;

        public ApplicationDbContextInitializer(ApplicationDbContext context)
        {
            _context = context;
        }
        protected override void Seed(ApplicationDbContext context)
        {
            base.Seed(context);

            var roleManager = new IdentityRoleManager(new RoleStore<IdentityRole>(context));
            var userManager = new ApplicationUserManager(new UserStore<ApplicationUser>(context));
            SeedRoles(roleManager);
            SeedUsers(userManager);
        }

        public void SeedRoles(IdentityRoleManager roleManager)
        {

            if (!roleManager.RoleExistsAsync("Super Admin").Result)
            {
                IdentityRole role = new IdentityRole();
                role.Name = "Super Admin";
                IdentityResult roleResult = roleManager.CreateAsync(role).Result;

            }
        }

        public void SeedUsers(UserManager<ApplicationUser> userManager)
        {



            if (userManager.FindByNameAsync("sa").Result == null)
            {
                ApplicationUser user = new ApplicationUser();
                user.UserName = "sa";
                user.Email = "support@viennam.com";
                user.IsEnabled = true;
                

                IdentityResult result = userManager.CreateAsync(user, "viennam").Result;

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user.Id, "Super Admin").Wait();
                }
            }
        }


    }

}