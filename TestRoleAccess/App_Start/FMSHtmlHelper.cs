using FMS.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Mvc.Html;

namespace FMS.Helpers
{
    public static class FMSLinkExtension
    {
        public static MvcHtmlString ActionLinkF(this HtmlHelper htmlHelper, string linkText, string actionName)
        {
            return ActionLinkF(htmlHelper, linkText, actionName, "", null, null);
        }
        public static MvcHtmlString ActionLinkF(this HtmlHelper htmlHelper, string linkText, string actionName, object routeValues) {
            return ActionLinkF(htmlHelper, linkText, actionName, "", routeValues, null);
        }

        public static MvcHtmlString ActionLinkF(this HtmlHelper htmlHelper, string linkText, string actionName, RouteValueDictionary routeValues) {
            return ActionLinkF(htmlHelper, linkText, actionName, "", routeValues, null);
        }

        public static MvcHtmlString ActionLinkF(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName) {
            return ActionLinkF(htmlHelper, linkText, actionName, controllerName, null, null);
        }
       
        public static MvcHtmlString ActionLinkF(this HtmlHelper htmlHelper, string linkText, string actionName, object routeValues, object htmlAttributes) {
            return ActionLinkF(htmlHelper, linkText, actionName, "", routeValues, htmlAttributes);
        }
        public static MvcHtmlString ActionLinkF(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes)
        {
            var systemAdmin = HttpContext.Current.User.Identity.IsAuthenticated && HttpContext.Current.User.Identity.Name == "sa";

            var html = htmlHelper.ActionLink(linkText, actionName, controllerName, routeValues, htmlAttributes);
            if (string.IsNullOrEmpty(controllerName))
                controllerName = htmlHelper.ViewContext.RouteData.GetRequiredString("controller");
            if (systemAdmin || HttpContext.Current.Session["user"] != null)
            {
                var dbUser = (User)HttpContext.Current.Session["user"];
                if (systemAdmin || (dbUser != null && dbUser.Roles.Any(r => r.Actions.Any(a => a.ActionName == actionName && a.Controller.ControllerName == controllerName))))
                    return html;
            }

            return new MvcHtmlString("<!-- not authorized -->");
        }
    }
}