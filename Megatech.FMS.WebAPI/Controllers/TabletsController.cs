using FMS.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;

namespace Megatech.FMS.WebAPI.Controllers
{
    [RoutePrefix("api/tablets")]
    public class TabletsController : ApiController
    {
        private DataContext db = new DataContext();
        [Route("")]
        public IEnumerable<Tablet> GetTablets()
        {
            return db.Tablets.ToList();
        }
        [Route("imeilist")]
        public string GetImeiList()
        {
            return string.Join(",", db.Tablets.Select(t => t.SerialNumber).ToArray());
        }

        [Route("check/{imei}")]
        public bool GetCheck(string imei)
        {
            bool checkImei = bool.Parse(ConfigurationManager.AppSettings["checkImei"]);
            if (checkImei)
                return db.Tablets.Any(t => t.SerialNumber.Equals(imei, StringComparison.InvariantCultureIgnoreCase));
            else
                return true;

        }

    }
}
