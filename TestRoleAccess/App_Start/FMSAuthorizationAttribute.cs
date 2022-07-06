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
            if (httpContext.User.Identity.Name == "sa")
                return true;
            
            using (DataContext db = new DataContext())
            {
                if (httpContext.Session["user"] == null)
                {
                    var dbUser = db.Users.Include(u=>u.Airports).Include(u => u.Roles.Select(r => r.Actions.Select(a => a.Controller))).FirstOrDefault(u => u.UserName == httpContext.User.Identity.Name);
                    if (dbUser == null)
                        return false;
                    
                    httpContext.Session["user"] = dbUser;
                }
                if (httpContext.Session["user"] != null)
                {
                    var dbUser = (User)httpContext.Session["user"];
                    var rd = httpContext.Request.RequestContext.RouteData;
                    string currentAction = rd.GetRequiredString("action");
                    string currentController = rd.GetRequiredString("controller");
                    string currentArea = rd.Values["area"] as string;

                    return dbUser.Roles.Any(r => r.Actions.Any(a => a.ActionName == currentAction && a.Controller.ControllerName == currentController));
                }
                else 
                    return base.AuthorizeCore(httpContext);
            }
                //return base.AuthorizeCore(httpContext);
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