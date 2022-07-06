using System.Web;
using System.Web.Optimization;

namespace Megatech.FMS.Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css")
                .Include("~/Content/bootstrap.css","~/Content/site.css")
                .Include("~/admin-lte2.4/css/AdminLTE.css", "~/admin-lte2.4/css/skins/_all-skins.css")
                .Include("~/admin-lte2.4/plugins/timepicker/*.css")
                .Include("~/admin-lte2.4/plugins/DataTables/datatables.min.css")
                .Include("~/Content/select2/css/*.css"));
        }
    }
}
