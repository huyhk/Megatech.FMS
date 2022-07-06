using FMS.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FMS.Helpers
{
    public static class FMSIconExtension
    {

        public static MvcHtmlString Icon(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, string faClass)
        {
            var systemAdmin = HttpContext.Current.User.Identity.IsAuthenticated && HttpContext.Current.User.Identity.Name == "sa";
            if (HttpContext.Current.Session["user"] != null || systemAdmin)
            {
                var dbUser = (User)HttpContext.Current.Session["user"];
                if (systemAdmin || (dbUser != null && dbUser.Roles.Any(r => r.Actions.Any(a => a.ActionName == actionName && a.Controller.ControllerName == controllerName))))
                {
                    var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);
                    var url = urlHelper.Action(actionName, controllerName);

                    var text = string.Format("<li><a href='{0}'><i class='fa {1}'></i><span>{2}</span></a></li>", url, faClass, linkText);
                    return new MvcHtmlString(text);
                }
            }
            return new MvcHtmlString("<!-- not authorized -->");

        }
    }
}