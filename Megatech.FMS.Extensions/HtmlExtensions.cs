using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Megatech.FMS.Extensions
{
    public static class HtmlExtensions
    {

        public static MvcHtmlString Filter(this HtmlHelper helper, string nameFilter, string actionName, string controllerName, object routeValues, object htmlAttributes = null)
        {
            TagBuilder builder = new TagBuilder("a");
            var url = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path);
            RouteInfo info = new RouteInfo(new Uri(url), HttpContext.Current.Request.ApplicationPath);

            if (string.IsNullOrEmpty(actionName))
                actionName = info.RouteData.Values["action"].ToString();
            if (string.IsNullOrEmpty(controllerName))
                controllerName = info.RouteData.Values["controller"].ToString();

            RouteValueDictionary routes = new RouteValueDictionary(routeValues);

            var query = helper.ViewContext.RequestContext.HttpContext.Request.QueryString;
            if (query != null)
            {
                List<string> update = new List<string>() { "pageIndex" };
                List<string> skip = new List<string>() { "sortorder", "sortdirection", "mode" };
                foreach (string item in query.Keys)
                {
                    if (!skip.Contains(item))
                        routes[item] = query[item];
                    if (update.Contains(item))
                        routes[item] = "1";
                }
            }

            UrlHelper Url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            builder.Attributes.Add("href", Url.Action(actionName, controllerName, routes).ToString());
            builder.InnerHtml = nameFilter;
            if (htmlAttributes != null)
                builder.MergeAttributes(new RouteValueDictionary(htmlAttributes));
            return new MvcHtmlString(builder.ToString(TagRenderMode.Normal));
        }
        public static MvcHtmlString Filter(this HtmlHelper html, string nameFilter, object routeValues, object htmlAttributes)
        {
            return Filter(html, nameFilter, null, null, routeValues, htmlAttributes);
        }

    }

}
