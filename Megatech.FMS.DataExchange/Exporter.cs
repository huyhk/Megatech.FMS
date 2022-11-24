using FMS.Data;
using Megatech.FMS.Logging;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading;

namespace Megatech.FMS.DataExchange
{
    public class Exporter
    {

        //private static DataContext db = new DataContext();
        private static bool running = false;
        private static bool TEST_EXPORT = ConfigurationManager.AppSettings["EINVOICE_EXPORT"] != null && ConfigurationManager.AppSettings["EINVOICE_EXPORT"].Equals("1");
        private static string TAXCODE_LIST = ConfigurationManager.AppSettings["EINVOICE_CODE_LIST"];
        private static DateTime lastCall;
        public static void ExportInvoice()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                FlightImporter.Import(DateTime.Today);
            }).Start();

#if DEBUG
            TEST_EXPORT = true;
#endif
            if (lastCall == null)
                lastCall = DateTime.Now.AddMinutes(-10);
            if (running && lastCall < DateTime.Now.AddMinutes(-1))
            {
                running = false;

            }
            // Logger.AppendLog("AITS", "start scanning " + running.ToString(), "aits");

          

            if (!running && !TEST_EXPORT)
            {
                Logger.AppendLog("EXPORT", "=================== Auto run started ===================", "exporter");
                lastCall = DateTime.Now;
                using (DataContext db = new DataContext())
                {
                    try
                    {
                        db.Database.ExecuteSqlCommand("exec usp_fix_invoices");
                        running = true;
                        //Logger.AppendLog("AITS", "start scanning AITS " + running.ToString(), "aits");
                        //var lstCancel = db.Invoices.Where(inv => (bool)inv.Exported_AITS && (bool)inv.RequestCancel && !(bool)inv.Cancelled).Select(inv => inv.Id).ToList();
                        //Logger.AppendLog("AITS", "Cancel list count:" + lstCancel.Count.ToString(), "exporter");
                        //foreach (var item in lstCancel)
                        //{
                        //    var result = InvoiceExporter.Cancel(item);
                        //    //Logger.AppendLog("AITS", "cancel id: " + item.ToString() + " result "+ result.code + " - " + result.message, "aits");
                        //}
                        var loginList = string.IsNullOrEmpty(TAXCODE_LIST) ? new string[0] : TAXCODE_LIST.Split(new char[] { ',', ';' });
                        var lst = db.Invoices.Where(inv => !(true == (bool)inv.Exported_AITS) && inv.Items.Count > 0
                        && DbFunctions.DiffDays(DateTime.Today, inv.BillDate) >-10 ).Select(inv => inv.Id).ToList();
                        //Logger.AppendLog("AITS", "invoice list : " + TAXCODE_LIST, "aits");
                        Logger.AppendLog("AITS", "AITS list count:" + lst.Count.ToString(), "exporter");

                        foreach (var item in lst)
                        {
                            var result = InvoiceExporter.Export(item);
                            //if (!result.success)
                            //Logger.AppendLog("AITS", "result: " + result.code + " - " + result.message, "aits");
                        }
                        var thre = DateTime.Today.AddDays(-30);
                        //Logger.AppendLog("OMEGA", "start scanning OMEGA " + running.ToString(), "aits");
                        var lstOMEGA = db.Invoices.Where(inv => inv.Date> thre && ((bool)inv.Exported_AITS && !(true == (bool)inv.Exported_OMEGA))).Select(inv => inv.Id).ToList();
                        Logger.AppendLog("OMEGA", "OMEGA list count:" + lstOMEGA.Count.ToString(), "exporter");
                        foreach (var item in lstOMEGA)
                        {
                            var result = InventoryExporter.Export(item);

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, "export-error");
                    }
                    finally
                    {
                        running = false;
                    }
                }

                Logger.AppendLog("EXPORT", "=================== Auto run ended ===================", "exporter");
            }
        }

    }
}
