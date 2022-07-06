using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Megatech.FMS.Web.Models
{
    public class BreadcrumbModel: List<BreadcrumbItem>
    {
        private static BreadcrumbModel _model;
        public void AddItems(params BreadcrumbItem[] items)
        {
            this.Clear();
            this.AddRange(items);
        }
        
        public static BreadcrumbModel CurrentBreadcrumb {
            get {
                if (_model == null)
                    _model = new BreadcrumbModel();
                return _model;
            }
        }
    }
    public class BreadcrumbItem {
        public string Text { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
    }
}