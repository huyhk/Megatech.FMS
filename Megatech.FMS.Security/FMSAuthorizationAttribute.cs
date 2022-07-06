using FMS.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;

namespace Megatech.FMS.Web
{
    public class FMSAuthorizeAttribute: AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            using (DataContext db = new DataContext())
            {
                var dbUser = db.Users.Include(u => u.Roles.Select(r => r.Actions.Select(a => a.Controller))).FirstOrDefault(u => u.UserName == httpContext.User.Identity.Name);
                if (dbUser == null)
                    return false;
                var rd = httpContext.Request.RequestContext.RouteData;
                string currentAction = rd.GetRequiredString("action");
                string currentController = rd.GetRequiredString("controller");
                string currentArea = rd.Values["area"] as string;

                return db.Roles.Any(r => r.Actions.Any(a => a.ActionName == currentAction && a.Controller.ControllerName == currentController));
            }
                return base.AuthorizeCore(httpContext);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            base.HandleUnauthorizedRequest(filterContext);
        }
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
        }
    }
}