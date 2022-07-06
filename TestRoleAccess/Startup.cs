using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TestRoleAccess.Startup))]
namespace TestRoleAccess
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
