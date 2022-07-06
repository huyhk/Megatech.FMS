using FMS.Data;
using Megatech.FMS.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EntityFramework.DynamicFilters;
using System.Web.Routing;

namespace TestRoleAccess.App_Start
{
    [FMSAuthorize]
    public class FMSController:Controller
    {
        protected DataContext db = new DataContext();
        public FMSController()
        {
        }
        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);

           
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session["user"] != null)
            {
                var dbUser = (User)filterContext.HttpContext.Session["user"];
                if (dbUser != null)
                {
                    var airportList = dbUser.Airports.Select(a => a.Id).ToList();
                    db.EnableFilter("UserAirport");
                    db.SetFilterScopedParameterValue("UserAirport", "airportList", airportList);
                    // db.SetFilterScopedParameterValue("UserAirport", "branch", dbUser.Branch);
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}