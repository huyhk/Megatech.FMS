using System.Web;
using System.Web.Mvc;

namespace Megatech.FMS.ReportSample
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
