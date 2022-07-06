using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Megatech.FMS.Web.Startup))]
namespace Megatech.FMS.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
