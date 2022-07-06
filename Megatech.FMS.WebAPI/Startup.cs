using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Megatech.FMS.WebAPI.Startup))]

namespace Megatech.FMS.WebAPI
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
