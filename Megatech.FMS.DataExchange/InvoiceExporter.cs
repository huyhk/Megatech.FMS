using EntityFramework.DynamicFilters;
using FMS.Data;
using Megatech.FMS.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Megatech.FMS.DataExchange
{
    public class InvoiceExporter
    {
        public InvoiceExporter()
        {
            this.conf = Configuration.GetConfiguration("invoice_exporter");
        }
        private Configuration conf;

        private static string API_BASE_URL = ConfigurationManager.AppSettings["AITS_BASE_URL"];

        private static string TAXCODE_LIST = ConfigurationManager.AppSettings["EINVOICE_CODE_LIST"];
        private static string token;

        public static bool Login(string loginTaxCode, string userName = "INV_SOHOA", string password = "$kypec123@321")
        {
            try
            {
                HttpClient client = new HttpClient();
                //client.BaseAddress = new Uri(API_BASE_URL);
                var param = new Dictionary<string, string>();

                param.Add("username", userName);
                param.Add("password", password);
                param.Add("ma_dvcs", "VP");

                string url = API_BASE_URL + "/Account/Login";
                HttpContent content = new FormUrlEncodedContent(param);
                client.DefaultRequestHeaders.Add("MaSoThue", loginTaxCode);
                var resp = client.PostAsync(url, content);

                if (resp.Result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var loginModel = JsonConvert.DeserializeObject<LoginModel>(resp.Result.Content.ReadAsStringAsync().Result.ToString());
                    token = loginModel.Token;
                    return !string.IsNullOrEmpty(token);
                }
                else return false;
            }
            catch (Exception ex)
            {
                Logger.AppendLog("AITS", "Login() : " + ex.Message, "aits");
                return false;
            }
        }
        private static string _filePath;


        public static void SetFilePath(string v)
        {
            _filePath = v;
        }

        private class LoginModel
        {
            public string Token { get; set; }
        }
        private static bool running = false;
        //private static DataContext db = new DataContext();

        public static ExportResultModel Export(int id, bool old = false)
        {
            if (running)
                return new ExportResultModel { code = "999", message = "busy" };

            ExportResultModel result = null;
            try
            {
                running = true;
                Logger.AppendLog("Export", "invoice id: " + id.ToString(), "aits");
                using (DataContext db = new DataContext())
                {
                    //db.DisableFilter("IsNotDeleted");
                    var inv = db.Invoices.Include(a => a.Receipt)
                        .Include(a => a.Customer)
                        .Include(a => a.Flight.Airport)
                        .Include(a => a.Items).FirstOrDefault(i => i.Id == id);
                    //var taxCode = inv.LoginTaxCode.Substring(inv.LoginTaxCode.LastIndexOf("-")+1);
                    if (inv == null)
                        result = new ExportResultModel { code = "404", message = "invoice not found" };
                    else if (inv.BillDate.Month < DateTime.Today.Month && !old)
                        result = new ExportResultModel { code = "405", message = "different receipt and invoice month" };
                    //else if (TAXCODE_LIST != null && !TAXCODE_LIST.Contains(taxCode))
                    //    result = new ExportResultModel { code = "402", message = "Login tax code " + inv.LoginTaxCode + " not in official list" };
                    else if (inv.Price <= 0 && inv.InvoiceType == INVOICE_TYPE.INVOICE)
                        result = new ExportResultModel { code = "406", message = "Invoice Price = 0" };
                    else if (inv.Items.Count <= 0)
                        result = new ExportResultModel { code = "401", message = "Items not found" };
                    else
                    {
                        Logger.AppendLog("Export", "bill no: " + inv.BillNo, "aits");
                        if (Login(inv.LoginTaxCode))
                        {
                            result = ExportInvoice(inv);

                            if (result.success && result.data.hoadon68_id != null)
                            {
                                inv.InvoiceNumber = result.data.shdon;
                                inv.SignNo = result.data.khieu;
                                inv.Exported_AITS = true;
                                inv.Exported_AITS_Date = DateTime.Now;
                                if (result.data.tdlap != null)
                                    inv.Date = result.data.tdlap;

                                inv.UniqueId = Guid.Parse(result.data.hoadon68_id);
                                var ids = inv.Items.Select(it => it.RefuelItemId).ToArray();

                                db.RefuelItems.Where(r => ids.Contains(r.Id))
                                    .Update(r => new RefuelItem { InvoiceNumber = inv.InvoiceNumber, DateUpdated = DateTime.Now });

                                //inv.Receipt.Image = null;

                                //inv.Receipt.SellerImage = null;

                                //inv.Receipt.Signature = null;

                                db.SaveChanges();


                                Logger.AppendLog("Export", "save ok " + inv.InvoiceNumber, "aits");
                            }

                        }
                        else
                        {

                            result = new ExportResultModel { code = "-1", message = "login failed" };
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.AppendLog("ERROR", "ExportInvoice(id)", "aits");
                Logger.LogException(ex, "aits");
                result = new ExportResultModel { code = "-1", message = ex.Message };

            }
            finally {
                running = false;

            }
            Logger.AppendLog("Export", result.code + " " + result.message, "aits");
            return result;

        }

        internal static ExportResultModel ExportInvoice(Invoice inv)
        {
            try
            {
                var folderPath2 = @"E:\FMS\fms01api\receipts";
                var folderPath = AppDomain.CurrentDomain.BaseDirectory + "receipts";
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                var json = JsonConvert.SerializeObject(new InvoiceJsonData(inv));
                Logger.AppendLog("export", json, "export-data");

                HttpClient client = new HttpClient();
                //client.BaseAddress =new Uri(API_BASE_URL);
                var url = API_BASE_URL + "/Pattern/SaveInvoiceAttack";
                client.DefaultRequestHeaders.Add("MaSoThue", inv.LoginTaxCode);
                client.DefaultRequestHeaders.Add("Authorization", "Bear " + token + ";VP");

                var requestContent = new MultipartFormDataContent();
                var fileInvoicePath = @"C:\Temp\invoice.jpg";
                var fileSignPath = @"C:\Temp\sign.jpg";
                //using (var filePDF = new StreamContent(new FileStream(fileInvoicePath, FileMode.Open)))
                //using (var fileSign = new StreamContent(new FileStream(fileSignPath, FileMode.Open)))
                //using (var filePDF = new StreamContent(new MemoryStream(inv.Image)))
                //using (var fileSign = new StreamContent(new MemoryStream(inv.Image)))
                //{
                StreamContent filePDF = null, fileSign = null;
                if (inv.Receipt.Image != null)
                {
                    filePDF = new StreamContent(new MemoryStream(inv.Receipt.Image));

                    filePDF.Headers.Add("Content-Type", "image/*");
                    filePDF.Headers.Add("Content-Disposition", "form-data; name=\"filePdfAttack\"; filename=\"" + Path.GetFileName(fileInvoicePath) + "\"");
                }
                else if (inv.Receipt.ImagePath != null)
                {
                    var fullPath = Path.Combine(folderPath, inv.Receipt.ImagePath);
                    if (!File.Exists(fullPath))
                    {
                        fullPath = Path.Combine(folderPath2, inv.Receipt.ImagePath);
                    }

                    if (File.Exists(fullPath))
                    {
                        filePDF = new StreamContent(new FileStream(fullPath, FileMode.Open));

                        filePDF.Headers.Add("Content-Type", "image/*");
                        filePDF.Headers.Add("Content-Disposition", "form-data; name=\"filePdfAttack\"; filename=\"" + Path.GetFileName(fileInvoicePath) + "\"");

                        Logger.AppendLog("export", "Image attach OK: " + fullPath, "export");
                    }
                    else
                        Logger.AppendLog("export", "Image Path not found: " + fullPath, "export");

                }

                if (inv.Receipt.Signature != null)
                {
                    fileSign = new StreamContent(new MemoryStream(inv.Receipt.Signature));

                    fileSign.Headers.Add("Content-Type", "image/*");
                    fileSign.Headers.Add("Content-Disposition", "form-data; name=\"fileSignAttack\"; filename=\"" + Path.GetFileName(fileSignPath) + "\"");

                }
                else if (inv.Receipt.SignaturePath != null)
                {
                    var fullPath = Path.Combine(folderPath, inv.Receipt.SignaturePath);
                    if (!File.Exists(fullPath))
                    {
                        fullPath = Path.Combine(folderPath2, inv.Receipt.ImagePath);
                    }
                    if (File.Exists(fullPath))
                    {
                        fileSign = new StreamContent(new FileStream(fullPath, FileMode.Open));

                        fileSign.Headers.Add("Content-Type", "image/*");
                        fileSign.Headers.Add("Content-Disposition", "form-data; name=\"fileSignAttack\"; filename=\"" + Path.GetFileName(fileSignPath) + "\"");
                        Logger.AppendLog("export", "Signature attach OK: " + fullPath, "export");
                    }
                }


                var jsonContent = new StringContent(json);
                jsonContent.Headers.Add("Content-Disposition", "form-data; name=\"invoices\"");

                requestContent.Add(jsonContent, "invoices");
                if (filePDF != null)
                    requestContent.Add(filePDF, "filePdfAttack", Path.GetFileName(fileInvoicePath));
                if (fileSign != null)
                    requestContent.Add(fileSign, " fileSignAttack", Path.GetFileName(fileSignPath));

                using (var httpResponse = client.PostAsync(url, requestContent))
                {
                    if (httpResponse.IsFaulted)
                    {
                        var ex = httpResponse.Exception;
                    }
                    else
                    {
                        if (httpResponse.Result.StatusCode == HttpStatusCode.OK)
                        {

                            var content = httpResponse.Result.Content.ReadAsStringAsync();

                            var responseModel = JsonConvert.DeserializeObject<ExportResultModel>(content.Result, new JsonSerializerSettings { DateFormatString = "yyyy-MM-ddTHH:mm:ss" });
                            //if (responseModel.code == "05")

                            //if (responseModel.code != "00")
                            //    responseModel.data = json;
                            if (responseModel.code == "00" || responseModel.code == "05")
                                responseModel.success = true;
                            if (responseModel.success)
                                Logger.AppendLog("Result" + responseModel.code, content.Result, "aits");
                            return responseModel;
                        }
                    }
                }
                //}

                if (filePDF != null)
                    filePDF.Dispose();
                if (fileSign != null)
                    fileSign.Dispose();
                return new ExportResultModel { code = "-1", message = "failed" };
            }
            catch (Exception ex)
            {
                Logger.AppendLog("ERROR", "ExportInvoice(inv)", "aits");
                Logger.LogException(ex, "aits");
                return new ExportResultModel { code = "-1", message = ex.Message };
            }
        }
        public static ExportResultModel Cancel(int id)
        {

            Logger.AppendLog("Cancel", "invoice id: " + id.ToString(), "aits");
            using (DataContext db = new DataContext())
            {
                var inv = db.Invoices.Include(a => a.Flight.Airport).FirstOrDefault(i => i.Id == id);
                if (inv != null && (bool)inv.RequestCancel)
                {
                    if (Login(inv.LoginTaxCode))
                    {
                        var result = CancelInvoice(inv);
                        if (result.success)
                        {
                            inv.Cancelled = true;
                            inv.CancelledTime = DateTime.Now;
                            db.SaveChanges();
                        }
                        return result;
                    }
                }
            }
            return null;
        }
        internal static ExportResultModel CancelInvoice(Invoice inv)
        {
            if (inv.InvoiceType == INVOICE_TYPE.INVOICE) return null;
            HttpClient client = new HttpClient();
            //client.BaseAddress =new Uri(API_BASE_URL);
            var url = API_BASE_URL + "/Invoice68/XoaBo";
            client.DefaultRequestHeaders.Add("MaSoThue", inv.Flight.Airport.TaxCode);
            client.DefaultRequestHeaders.Add("Authorization", "Bear " + token + ";VP");

            var json = JsonConvert.SerializeObject(new InvoiceCancelModel(inv));
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            using (var httpResponse = client.PostAsync(url, jsonContent))
            {
                if (httpResponse.IsFaulted)
                {
                    var ex = httpResponse.Exception;
                }
                else
                {
                    if (httpResponse.Result.StatusCode == HttpStatusCode.OK)
                    {

                        var content = httpResponse.Result.Content.ReadAsStringAsync();

                        var responseModel = JsonConvert.DeserializeObject<ExportResultModel>(content.Result, new JsonSerializerSettings { DateFormatString = "yyyy-MM-ddTHH:mm:ss" });
                        //if (responseModel.code == "05")

                        //if (responseModel.code != "00")
                        //    responseModel.data = json;
                        if (responseModel.code == "00" || responseModel.code == "05" || responseModel.code == "30009")
                            responseModel.success = true;
                        if (responseModel.success)
                            Logger.AppendLog("Result" + responseModel.code, content.Result, "aits");
                        return responseModel;
                    }
                }
            }
            return null;
        }

        public static string UpdateImage(int? id = null)
        {
            var folderPath = AppDomain.CurrentDomain.BaseDirectory + "receipts";

            using (var db = new DataContext())
            {
                var lst = db.Invoices
                  .Include(re => re.Receipt)
                  .Where(re => ((id == null) || re.Id == id) //&& re.Exported == null
                  && (bool)re.Exported_AITS && re.RefuelCompany == REFUEL_COMPANY.NAFSC 
                  && re.Date >= new DateTime(2022, 07, 31)
                  && re.Receipt.AircraftType == null
                    //&& re.Receipt.Signature == null
                  && re.FHSUniqueId == null).ToList();
                var result = string.Empty;
                Logger.AppendLog("UPDATE", $"COUNT {lst.Count}", "fhs-update");
                foreach (var inv in lst)
                {
                    try
                    {
                        if (Login(inv.LoginTaxCode))
                        {


                            Logger.AppendLog("UPDATE", $"Invoice Number:  {inv.InvoiceNumber} uniqueId {inv.UniqueId} image {inv.Receipt.ImagePath}", "fhs-update");
                            using (var httpClient = new HttpClient())
                            {
                                var url = API_BASE_URL + "/Pattern/UploadReceipt";
                                httpClient.DefaultRequestHeaders.Add("MaSoThue", inv.LoginTaxCode);
                                httpClient.DefaultRequestHeaders.Add("Authorization", "Bear " + token + ";VP");
                                StreamContent filePDF = null;
                                if (inv.Receipt.ImagePath != null)
                                {
                                    var fullPath = Path.Combine(folderPath, inv.Receipt.ImagePath);


                                    if (File.Exists(fullPath))
                                    {
                                        filePDF = new StreamContent(new FileStream(fullPath, FileMode.Open));

                                        filePDF.Headers.Add("Content-Type", "image/*");
                                        filePDF.Headers.Add("Content-Disposition", "form-data; name=\"upload\"; filename=\"" + Path.GetFileName(fullPath) + "\"");

                                    }


                                }
                                var requestContent = new MultipartFormDataContent();
                                requestContent.Add(filePDF, "upload");
                                requestContent.Add(new StringContent(inv.UniqueId.ToString()), "hoadon68_id");
                                using (var httpResponse = httpClient.PostAsync(url, requestContent))
                                {

                                    var content = httpResponse.Result.Content.ReadAsStringAsync();
                                    if (httpResponse.Result.StatusCode == HttpStatusCode.OK)
                                    {

                                        result += content.Result + "\n";
                                        inv.Exported = true;
                                        Logger.AppendLog("UPDATE", $"Success uniqueId {inv.UniqueId}", "fhs-update");

                                    }
                                    else
                                    {
                                        Logger.AppendLog("UPDATE", $"Failed:  {inv.InvoiceNumber} uniqueId {inv.UniqueId}", "fhs-update");

                                        Logger.AppendLog("UPDATE", $"Status:  {httpResponse.Result.StatusCode} content {content}", "fhs-update");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AppendLog("UPDATE",$"Error : {ex.Message}", "fhs-update");

                        Logger.LogException(ex, "fhs-update");
                    }
                }
                db.SaveChanges();
                return result;
            }
        }

        public static  string SendEmail(string customerCode, DateTime? fdate, DateTime? tdate)
        {
            Logger.AppendLog("EMAIL", $"Customer Code: {customerCode} From Date: {fdate} To Date: {tdate}", "invoice-email");
            using (var db = new DataContext())
            {
                var query = db.Invoices.Include(re=>re.Customer)
                  .Where(re => re.CustomerCode.Equals(customerCode, StringComparison.OrdinalIgnoreCase));
               
                if (fdate != null)
                    query = query.Where(re => re.Date >= fdate);
                if (tdate != null)
                    query = query.Where(re => re.Date <= tdate);
                var file = Path.Combine(Logger.GetPath(), "invoice-ids.txt");
                var s = File.ReadAllText(file);
                var ids = s.Split(new char[] { ',', '\n', '\r' });

                var allLst = query.ToList();
                var lst = allLst.Where(re => !ids.Contains(re.UniqueId.ToString())).ToList();
                var result = string.Empty;
                Logger.AppendLog("EMAIL", $"COUNT {lst.Count}", "invoice-email");
                foreach (var inv in lst)
                {
                    if (ids.Contains(inv.UniqueId.ToString())) continue;
                    
                    try
                    {
                        if (Login(inv.LoginTaxCode))
                        {

                            Logger.AppendLog("EMAIL", $"Invoice Number:  {inv.InvoiceNumber} uniqueId {inv.UniqueId} ", "invoice-email");
                            using (var httpClient = new HttpClient())
                            {
                                var url = API_BASE_URL + "/Invoice68/SendInvoiceByEmail";
                                httpClient.DefaultRequestHeaders.Add("MaSoThue", inv.LoginTaxCode);
                                httpClient.DefaultRequestHeaders.Add("Authorization", "Bear " + token + ";VP");
                                var json = $"{{\"id\":\"{inv.UniqueId}\",\"nguoinhan\":\"{inv.CustomerEmail ?? inv.Customer.Email}\"}}";
                                Logger.AppendLog("EMAIL", json, "invoice-email");

                                HttpContent jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                                using (var httpResponse = httpClient.PostAsync(url, jsonContent))
                                {

                                    var content = httpResponse.Result.Content.ReadAsStringAsync();
                                    if (httpResponse.Result.StatusCode == HttpStatusCode.OK)
                                    {

                                        result += content.Result + "\n";
                                        // inv.Exported = true;
                                        Logger.AppendLog("EMAIL", $"Success uniqueId {inv.UniqueId}", "invoice-email");
                                        if (content.Result.Contains("\"00\""))
                                            File.AppendAllText(file, inv.UniqueId.ToString() + ",\n");

                                    }
                                    else
                                    {
                                        result += $"failed Id {inv.UniqueId} ";
                                       Logger.AppendLog("EMAIL", $"Failed:  {inv.InvoiceNumber} uniqueId {inv.UniqueId}", "invoice-email");

                                        Logger.AppendLog("EMAIL", $"Status:  {httpResponse.Result.StatusCode} content {content}", "invoice-email");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AppendLog("EMAIL", $"Error : {ex.Message}", "invoice-email");

                        Logger.LogException(ex, "invoice-email");
                    }
                    Thread.Sleep(5000);
                }
                db.SaveChanges();
                return result;
            }
        }
    }
}
