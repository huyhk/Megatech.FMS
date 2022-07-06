using System.Web.Http;
using System.Web.Http.Controllers;

namespace Megatech.FMS.WebAPI
{
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return base.IsAuthorized(actionContext);
        }
    }
}