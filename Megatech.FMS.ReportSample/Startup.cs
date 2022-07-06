using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Megatech.FMS.ReportSample.Startup))]
namespace Megatech.FMS.ReportSample
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
