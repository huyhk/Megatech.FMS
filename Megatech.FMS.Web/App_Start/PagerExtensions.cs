using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Mvc.Ajax;
using System.Web.Routing;

namespace Megatech.FMS.Web
{
    public static class PagerExtensions
    {
        public static MvcHtmlString Pager(this AjaxHelper html, string actionName, string controllerName, object routeValues, PagerOptions pagerOptions, AjaxOptions ajaxOptions, object htmlAttributes)
        {
            if (pagerOptions == null)
                pagerOptions = new PagerOptions();

            TagBuilder builder = new TagBuilder("div");

            builder.MergeAttributes(new RouteValueDictionary(htmlAttributes));
            builder.MergeAttribute("class", "pager");
            builder.MergeAttribute("pager-id", pagerOptions.UniqueId);
            builder.MergeAttribute("pager-count", pagerOptions.PageCount.ToString());

            RouteValueDictionary routes = new RouteValueDictionary(routeValues);
            routes["pageSize"] = pagerOptions.PageSize;
            int start = pagerOptions.PageIndex - pagerOptions.VisibleItemCount / 2;
            if (start <= 0) start = 1;
            int end = start + pagerOptions.VisibleItemCount - 1;
            if (end > pagerOptions.PageCount) end = pagerOptions.PageCount;
            IDictionary<string, object> lnkAttributes = new Dictionary<string, object>();
            MvcHtmlString lnk;
            var oldSuccesss = pagerOptions.Callback.Trim(); if (string.IsNullOrEmpty(oldSuccesss)) oldSuccesss = "null";
            //add first link
            if (pagerOptions.HasFirsLast)
            {
                routes[pagerOptions.IndexName] = 1;
                lnkAttributes.Add("class", "first item");
                if (start == 1)
                    lnkAttributes.Add("style", "display:none");
                ajaxOptions.OnSuccess = string.Format("pagerSuccess(data, status, xhr, {{PageIndex:{0},PagerId:'{1}'}},{2})", 1, pagerOptions.UniqueId, oldSuccesss);
                lnk = html.ActionLink(pagerOptions.FirstText, actionName, controllerName, routes, (AjaxOptions)ajaxOptions, lnkAttributes);
                builder.InnerHtml += lnk.ToString();
            }

            //add number items
            for (int i = start; i <= end; i++)
            {
                routes[pagerOptions.IndexName] = i;
                lnkAttributes = new Dictionary<string, object>();
                if (i == pagerOptions.PageIndex)
                    lnkAttributes.Add("class", "active item");
                else
                    lnkAttributes.Add("class", "item");
                lnkAttributes.Add("id", "page_" + i.ToString());
                ajaxOptions.OnSuccess = string.Format("pagerSuccess(data, status, xhr, {{PageIndex:{0},PagerId:'{1}'}},{2})", i, pagerOptions.UniqueId, oldSuccesss);
                lnk = html.ActionLink(i.ToString(), actionName, controllerName, routes, (AjaxOptions)ajaxOptions, lnkAttributes);
                builder.InnerHtml += lnk.ToString();
            }
            //add last link
            if (pagerOptions.HasFirsLast)
            {
                routes[pagerOptions.IndexName] = pagerOptions.PageCount;
                lnkAttributes = new Dictionary<string, object>();
                lnkAttributes.Add("class", "last item");
                if (end >= pagerOptions.PageCount)
                    lnkAttributes.Add("style", "display:none");
                ajaxOptions.OnSuccess = string.Format("pagerSuccess(data, status, xhr, {{PageIndex:{0},PagerId:'{1}'}},{2})", pagerOptions.PageCount, pagerOptions.UniqueId, oldSuccesss);
                lnk = html.ActionLink(pagerOptions.LastText, actionName, controllerName, routes, (AjaxOptions)ajaxOptions, lnkAttributes);
                builder.InnerHtml += lnk.ToString();
            }
            if (pagerOptions.PageCount > 1)
                return new MvcHtmlString(builder.ToString(TagRenderMode.Normal));
            else
                return new MvcHtmlString("");

        }

        public static MvcHtmlString Pager(this HtmlHelper html, string actionName, string controllerName, object routeValues, PagerOptions pagerOptions, object htmlAttributes)
        {
            if (pagerOptions == null)
                pagerOptions = new PagerOptions();
            var url = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path);
            RouteInfo info = new RouteInfo(new Uri(url), HttpContext.Current.Request.ApplicationPath);

            if (info.RouteData.Values[pagerOptions.IndexName] != null)
                pagerOptions.PageIndex = int.Parse(info.RouteData.Values[pagerOptions.IndexName].ToString());
            if (info.RouteData.Values[pagerOptions.SizeName] != null)
                pagerOptions.PageSize = int.Parse(info.RouteData.Values[pagerOptions.SizeName].ToString());

            if (html.ViewContext.RequestContext.HttpContext.Request[pagerOptions.IndexName] != null)
                pagerOptions.PageIndex = int.Parse(html.ViewContext.RequestContext.HttpContext.Request[pagerOptions.IndexName].ToString());
            if (html.ViewContext.RequestContext.HttpContext.Request[pagerOptions.SizeName] != null)
                pagerOptions.PageSize = int.Parse(html.ViewContext.RequestContext.HttpContext.Request[pagerOptions.SizeName].ToString());

            TagBuilder builder = new TagBuilder("div");

            builder.MergeAttributes(new RouteValueDictionary(htmlAttributes));
            builder.MergeAttribute("class", "pager");
            builder.MergeAttribute("pager-id", pagerOptions.UniqueId);
            builder.MergeAttribute("pager-count", pagerOptions.PageCount.ToString());

            RouteValueDictionary routes = new RouteValueDictionary(routeValues);

            var query = html.ViewContext.RequestContext.HttpContext.Request.QueryString;
            if (query != null)
            {
                foreach (string item in query.Keys)
                {
                    routes[item] = query[item]; ;
                }
            }

            if (string.IsNullOrEmpty(actionName))
                actionName = info.RouteData.Values["action"].ToString();// html.ViewContext.RouteData.Values["action"].ToString();
            if (string.IsNullOrEmpty(controllerName))
                controllerName = info.RouteData.Values["controller"].ToString();// html.ViewContext.RouteData.Values["controller"].ToString();

            int start = (int)Math.Ceiling((double)pagerOptions.PageIndex - pagerOptions.VisibleItemCount / 2);
            if (start <= 0) start = 1;
            int end = start + pagerOptions.VisibleItemCount - 1;
            if (end > pagerOptions.PageCount) end = pagerOptions.PageCount;
            if (end - start < pagerOptions.VisibleItemCount - 1) start = (end - pagerOptions.VisibleItemCount > 0) ? (end - pagerOptions.VisibleItemCount) : 1;
            IDictionary<string, object> lnkAttributes = new Dictionary<string, object>();
            MvcHtmlString lnk;
            routes[pagerOptions.SizeName] = pagerOptions.PageSize;
            //add first link
            if (pagerOptions.HasFirsLast)
            {
                routes[pagerOptions.IndexName] = 1;
                lnkAttributes.Add("class", "first item");
                if (start == 1)
                    lnkAttributes.Add("style", "display:none");

                lnk = html.ActionLink(pagerOptions.FirstText, actionName, controllerName, routes, lnkAttributes);
                builder.InnerHtml += lnk.ToString();
            }

            //add number items
            for (int i = start; i <= end; i++)
            {
                routes[pagerOptions.IndexName] = i;
                lnkAttributes = new Dictionary<string, object>();
                if (i == pagerOptions.PageIndex)
                    lnkAttributes.Add("class", "active item");
                else
                    lnkAttributes.Add("class", "item");
                lnkAttributes.Add("id", "page_" + i.ToString());

                lnk = html.ActionLink(i.ToString(), actionName, controllerName, routes, lnkAttributes);
                builder.InnerHtml += lnk.ToString();
            }
            //add last link
            if (pagerOptions.HasFirsLast)
            {
                routes[pagerOptions.IndexName] = pagerOptions.PageCount;
                lnkAttributes = new Dictionary<string, object>();
                lnkAttributes.Add("class", "last item");
                if (end >= pagerOptions.PageCount)
                    lnkAttributes.Add("style", "display:none");

                lnk = html.ActionLink(pagerOptions.LastText, actionName, controllerName, routes, lnkAttributes);
                builder.InnerHtml += lnk.ToString();
            }
            if (pagerOptions.PageCount > 1)
                return new MvcHtmlString(builder.ToString(TagRenderMode.Normal));
            else
                return new MvcHtmlString("");

        }

        public static MvcHtmlString Pager(this HtmlHelper html, PagerOptions pagerOptions, object htmlAttributes)
        {
            return Pager(html, null, null, null, pagerOptions, htmlAttributes);
           
        }

    }
    public class PagerOptions
    {
        public PagerOptions()
        {
            FirstText = "<<";
            NextText = ">";
            PrevText = "<";
            LastText = ">>";
            PageIndex = 1;
            PageSize = 10;
            VisibleItemCount = 5;
            UniqueId = Guid.NewGuid().ToString();
            Callback = "null";
            IndexName = "pageIndex";
            SizeName = "pageSize";

        }
        public string IndexName { get; set; }
        public string SizeName { get; set; }
        public string FirstText { get; set; }
        public string NextText { get; set; }
        public string PrevText { get; set; }
        public string LastText { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int ItemCount { get; set; }
        public int VisibleItemCount { get; set; }
        public int PageCount
        {
            get
            {
                return (int)Math.Ceiling(1.0M * ItemCount / (PageSize == 0 ? 1 : PageSize));
            }
        }
        public string UniqueId { get; set; }

        public bool HasFirsLast
        {
            get
            {
                return PageCount > VisibleItemCount;
            }
        }

        public string Callback { get; set; }
    }
}