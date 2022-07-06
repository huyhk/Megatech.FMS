﻿using EntityFramework.DynamicFilters;
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

        public static ExportResultModel Export(int id)
        {
            if (running)
                return new   ExportResultModel { code = "999", message = "busy" };
           
            ExportResultModel result = null;
            try
            {
                running = true;
                Logger.AppendLog("Export", "invoice id: " + id.ToString(), "aits");
                using (DataContext db = new DataContext())
                {
                    db.DisableFilter("IsNotDeleted");
                    var inv = db.Invoices.Include(a => a.Receipt).Include(a => a.Customer).Include(a => a.Flight.Airport).Include(a => a.Items).FirstOrDefault(i => i.Id == id);
                    var taxCode = inv.LoginTaxCode.Substring(inv.LoginTaxCode.LastIndexOf("-")+1);
                    if (inv == null )
                        result = new ExportResultModel { code = "404", message = "invoice not found" };
                    else if (inv.BillDate.Month < DateTime.Today.Month)
                        result = new ExportResultModel { code = "405", message = "different receipt and invoice month" };
                    else if (TAXCODE_LIST != null && !TAXCODE_LIST.Contains(taxCode))
                        result = new ExportResultModel { code = "402", message = "Login tax code " + inv.LoginTaxCode + " not in official list" };
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

                                db.RefuelItems.Where(r => ids.Contains(r.Id)).Update(r => new RefuelItem { InvoiceNumber = inv.InvoiceNumber , DateUpdated = DateTime.Now});

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
                var inv = db.Invoices.Include(a => a.Flight.Airport).FirstOrDefault(i => i.Id == id );
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
    }
}