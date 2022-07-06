using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Megatech.FMS.Web
{
    public class AppContext
    {
        public static bool IsAdmin(bool upper = false)
        {
            if (!upper)
                return HttpContext.Current.User.IsInRole("Administrators") || HttpContext.Current.User.IsInRole("Administrator");
            else
                return HttpContext.Current.User.IsInRole("Administrators") || HttpContext.Current.User.IsInRole("Administrator") || IsSuperAdmin();
        }
        public static bool IsSuperAdmin()
        {
            return HttpContext.Current.User.IsInRole("Super Admin") || HttpContext.Current.User.IsInRole("SuperAdmin") || HttpContext.Current.User.IsInRole("Super Admins");
        }

        public static string UploadFolder
        {
            get
            {
                if (HttpContext.Current.Application["upload:folder"] == null)
                {
                    var folder = ConfigurationManager.AppSettings["upload:folder"];
                    HttpContext.Current.Application["upload:folder"] = folder;
                }
                return HttpContext.Current.Application["upload:folder"].ToString();
            }
        }
    }
}