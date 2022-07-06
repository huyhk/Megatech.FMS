using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Megatech.FMS.Web.Models
{
    public class PagingViewModel
    {
        public PagingViewModel() {
            PageIndex = 1;
            PageDisplay = 10;
            PageSize = 20;
        }
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string Url { get; set; }
        public int PageDisplay { get; set; }
        public int PageCount {
            get {
                var page = (int)Math.Floor((decimal)TotalRecords / PageSize);
                var remain = TotalRecords % PageSize;
                return page + (remain > 0 ? 1 : 0);
            }
        }
        public int PageStart {
            get
            {
                int start = (int)Math.Ceiling((double)PageIndex - PageDisplay/ 2);
                if (start <= 0) start = 1;
                int end = start + PageDisplay - 1;
                if (end > PageCount) end = PageCount;
                
                if (end - start < PageDisplay - 1) start = (end - PageDisplay > 0) ? (end - PageDisplay) : 1;
                return start;
                
            }
        }
        public int PageEnd {
            get
            {
                int start = (int)Math.Ceiling((double)PageIndex - PageDisplay / 2);
                if (start <= 0) start = 1;
                int end = start + PageDisplay - 1;
                if (end > PageCount) end = PageCount;

                if (end - start < PageDisplay - 1) start = (end - PageDisplay > 0) ? (end - PageDisplay) : 1;
                return end;
            }
        }
    }
}