using FMS.Data;
using Megatech.FMS.DataExchange;
using Megatech.FMS.WebAPI.App_Start;
using System;
using System.Web.Hosting;
using System.Web.Http;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class ExportController : ApiController
    {
        public IHttpActionResult Post(int id = 0, REFUEL_ITEM_TYPE t = REFUEL_ITEM_TYPE.REFUEL)
        {

            if (t == REFUEL_ITEM_TYPE.REFUEL)
                ExportTask.Execute(id);
            else
                ExportTask.Extract(id);
            return Ok();
        }

        public IHttpActionResult Post(DateTime date, REFUEL_ITEM_TYPE t = REFUEL_ITEM_TYPE.REFUEL, ExportTask.REQUIRED_FIELD r = ExportTask.REQUIRED_FIELD.INVOICE_FORM | ExportTask.REQUIRED_FIELD.INVOICE_NUMBER)
        {

            if (t == REFUEL_ITEM_TYPE.REFUEL)
                ExportTask.Execute(date, r);
            else
                ExportTask.Extract(date);
            return Ok();
        }

        [Route("api/export/invoice/{id}")]
        public IHttpActionResult PostInvoice(int id)
        {
            Logger.SetPath(HostingEnvironment.MapPath("~/logs"));
            var resp = InvoiceExporter.Export(id);
            if (resp.success)
                resp = InventoryExporter.Export(id);
            return Ok(resp);
        }

        [Route("api/export/invoice/cancel/{id}")]
        public IHttpActionResult CancelInvoice(int id)
        {
            Logger.SetPath(HostingEnvironment.MapPath("~/logs"));
            var resp = InvoiceExporter.Cancel(id);

            return Ok(resp);
        }

    }
}
