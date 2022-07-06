using FMS.Data;
using Megatech.FMS.ReportSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Megatech.FMS.ReportSample.Controllers
{
    public class BM2508Controller : Controller
    {
        private DataContext db = new DataContext();
        // GET: BM2508
        public ActionResult Index()
        {
            var model = db.RefuelItems.Select(r => new BM2508ViewModel { /*some assignments */
            }).FirstOrDefault();

            return View(model);
        }
    }
}