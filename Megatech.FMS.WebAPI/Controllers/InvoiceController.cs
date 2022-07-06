using FMS.Data;
using Megatech.FMS.DataExchange;
using Megatech.FMS.WebAPI.App_Start;
using Megatech.FMS.WebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Description;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class InvoicesController : ApiController
    {
        private DataContext db = new DataContext();

        // GET: api/Invoices
        public IQueryable<Invoice> GetInvoices()
        {
            return db.Invoices;
        }

        // GET: api/Invoices/5
        [ResponseType(typeof(Invoice))]
        public IHttpActionResult GetInvoice(int id)
        {
            var inv = db.Invoices.Include(r => r.Items).Where(r => r.Id == id)
            .Select(r => new InvoiceModel
            {

            }).FirstOrDefault();


            return Ok(inv);
        }

        // PUT: api/Invoices/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutInvoice(int id, InvoiceModel inv)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != inv.Id)
            {
                return BadRequest();
            }

            db.Entry(inv).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Invoices
        [ResponseType(typeof(Invoice))]
        public IHttpActionResult PostInvoice(InvoiceModel inv)
        {
            int version = 0;
            if (Request.Headers.Contains("App-Version"))
            {
                var appVersion = Request.Headers.GetValues("App-Version").FirstOrDefault();
                var idx = appVersion.LastIndexOf(".");
                if (idx > 0)
                {
                    version = int.Parse(appVersion.Substring(idx + 1));
                }
            }
            Logger.AppendLog("VER", version.ToString(), "Invoice");
            if (version < 38)
                return Ok();


            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var userId = user != null ? user.Id : 0;
            var json = JsonConvert.SerializeObject(inv);
            Logger.AppendLog("DATA", json, "Invoice");

            int newId = 0;
            db.Database.BeginTransaction();
            try
            {

                var model = db.Invoices.FirstOrDefault(r => r.InvoiceNumber == inv.InvoiceNumber && inv.InvoiceFormId == r.InvoiceFormId);
                if (model == null)
                {
                    model = new Invoice
                    {
                        UserCreatedId = userId,

                        InvoiceNumber = inv.InvoiceNumber,
                        Date = inv.Date.Date,
                        BillDate = inv.Date,
                        CustomerId = inv.CustomerId,
                        CustomerName = inv.CustomerName,
                        CustomerCode = inv.CustomerCode,
                        TaxCode = inv.TaxCode,
                        CustomerAddress = inv.CustomerAddress,

                        FormNo = inv.FormNo,
                        SignNo = inv.Sign,
                        InvoiceFormId = inv.InvoiceFormId,
                        FlightId = inv.FlightId,
                        FlightCode = inv.FlightCode,
                        AircraftCode = inv.AircraftCode,
                        AircraftType = inv.AircraftType,
                        RouteName = inv.RouteName,
                        InvoiceType = inv.InvoiceType,
                        FlightType = inv.FlightType,
                        Price = inv.InvoiceType == INVOICE_TYPE.INVOICE ? inv.Price : 0,
                        Currency = inv.Currency,
                        Unit = inv.Unit,
                        SaleAmount = inv.InvoiceType == INVOICE_TYPE.INVOICE ? inv.SaleAmount : 0,
                        TaxRate = inv.TaxRate,
                        TotalAmount = inv.InvoiceType == INVOICE_TYPE.INVOICE ? inv.SaleAmount + inv.VatAmount : 0,
                        Exported_AITS = true,

                        Gallon = inv.TotalGallon,
                        Volume = inv.TotalVolume,
                        Weight = inv.TotalWeight,

                        IsElectronic = false,
                        TechLog = inv.TechLog, 
                        

                        Items = new List<InvoiceItem>()
                    };

                    

                    foreach (var item in inv.Items.Where(it=>!it.IsReturn))
                    {

                        var modelItem = new InvoiceItem
                        {

                            TruckId = item.TruckId,
                            TruckNo = item.TruckNo,
                            //Truck = refuelItem?.Truck,
                            StartTime = item.StartTime,
                            EndTime = item.EndTime,
                            StartNumber = item.StartNumber,
                            EndNumber = item.EndNumber,
                            Temperature = inv.Temperature,
                            Density = inv.Density,
                            Gallon = item.Gallon,
                            Volume = item.Volume,
                            Weight = item.Weight,
                            QCNo = item.QualityNo,
                            OperatorId = item.OperatorId,
                            DriverId = item.DriverId,
                            WeightNote = item.WeightNote,
                        };
                      
                        model.Items.Add(modelItem);

                        RefuelItem rItem = null;
                        if (item.RefuelItemId > 0)
                            rItem = db.RefuelItems.FirstOrDefault(r => r.Id == item.RefuelItemId);

                        else if (!string.IsNullOrEmpty(item.RefuelUniqueId))
                        {
                            Guid guid = Guid.Parse(item.RefuelUniqueId);
                            rItem = db.RefuelItems.FirstOrDefault(r => r.UniqueId == guid);

                        }

                        if (rItem != null)
                        {
                            modelItem.RefuelItemId = rItem.Id;
                            if (model.FlightId == 0)
                                model.FlightId = rItem.FlightId;
                            rItem.InvoiceNumber = model.InvoiceNumber;
                            rItem.Exported = true;
                            if (modelItem.Gallon == 0 && rItem.Amount > 0)
                                modelItem.Gallon = rItem.Amount;
                        }

                        Truck truck = db.Trucks.FirstOrDefault(t => t.Id == item.TruckId);
                        if (truck != null)
                            model.RefuelCompany = truck.RefuelCompany;



                    }
                    

                    if (model.Items.Count > 0 && model.Gallon>0)
                        db.Invoices.Add(model);

                    if (inv.HasReturn)
                    {

                        model.Gallon = model.Items.Sum(it => it.Gallon);
                        model.Volume = (decimal)model.Items.Sum(it => it.Volume);
                        model.Weight = (decimal)model.Items.Sum(it => it.Weight);

                        var returnModel = new Invoice
                        {
                            UserCreatedId = userId,

                            InvoiceNumber = inv.InvoiceNumber,
                            Date = inv.Date.Date,
                            BillDate = inv.Date,
                            CustomerId = inv.CustomerId,
                            CustomerName = inv.CustomerName,
                            CustomerCode = inv.CustomerCode,
                            TaxCode = inv.TaxCode,
                            CustomerAddress = inv.CustomerAddress,

                            FormNo = inv.FormNo,
                            SignNo = inv.Sign,
                            InvoiceFormId = inv.InvoiceFormId,
                            FlightId = inv.FlightId,
                            FlightCode = inv.FlightCode,
                            AircraftCode = inv.AircraftCode,
                            AircraftType = inv.AircraftType,
                            RouteName = inv.RouteName,
                            InvoiceType = INVOICE_TYPE.RETURN,
                            FlightType = inv.FlightType,

                            Exported_AITS = true,


                            IsElectronic = false,
                            TechLog = inv.TechLog,

                            Items = new List<InvoiceItem>()
                        };



                        foreach (var item in inv.Items.Where(it => it.IsReturn && it.Gallon > 0))
                        {

                            var modelItem = new InvoiceItem
                            {

                                TruckId = item.TruckId,
                                TruckNo = item.TruckNo,
                                //Truck = refuelItem?.Truck,
                                StartTime = item.StartTime,
                                EndTime = item.EndTime,
                                StartNumber = item.StartNumber,
                                EndNumber = item.EndNumber,
                                Temperature = item.Temperature,
                                Density = item.Density,
                                Gallon = item.Gallon,
                                Volume = item.Volume,
                                Weight = item.Weight,
                                QCNo = item.QualityNo,
                                OperatorId = item.OperatorId,
                                DriverId = item.DriverId,
                                WeightNote = item.WeightNote,
                            };

                            returnModel.Items.Add(modelItem);

                            RefuelItem rItem = null;
                            if (item.RefuelItemId > 0)
                                rItem = db.RefuelItems.FirstOrDefault(r => r.Id == item.RefuelItemId);
                            else if (!string.IsNullOrEmpty(item.RefuelUniqueId))
                            {
                                Guid guid = Guid.Parse(item.RefuelUniqueId);
                                rItem = db.RefuelItems.FirstOrDefault(r => r.UniqueId == guid);

                            }

                            if (rItem != null)
                            {
                                modelItem.RefuelItemId = rItem.Id;
                                if (model.FlightId == 0)
                                    model.FlightId = rItem.FlightId;
                                
                                rItem.ExportExtract = true;
                               
                            }


                        }

                        foreach (var item in inv.ReturnItems.Where(it => it.Gallon > 0))
                        {

                            var modelItem = new InvoiceItem
                            {

                                TruckId = item.TruckId,
                                TruckNo = item.TruckNo,
                                //Truck = refuelItem?.Truck,
                                StartTime = item.StartTime,
                                EndTime = item.EndTime,
                                StartNumber = item.StartNumber,
                                EndNumber = item.EndNumber,
                                Temperature = item.Temperature,
                                Density = item.Density,
                                Gallon = item.Gallon,
                                Volume = item.Volume,
                                Weight = item.Weight,
                                QCNo = item.QualityNo,
                                OperatorId = item.OperatorId,
                                DriverId = item.DriverId,
                                WeightNote = item.WeightNote,
                            };

                            returnModel.Items.Add(modelItem);

                            RefuelItem rItem = null;
                            if (item.RefuelItemId > 0)
                                rItem = db.RefuelItems.FirstOrDefault(r => r.Id == item.RefuelItemId);
                            else if (!string.IsNullOrEmpty(item.RefuelUniqueId))
                            {
                                Guid guid = Guid.Parse(item.RefuelUniqueId);
                                rItem = db.RefuelItems.FirstOrDefault(r => r.UniqueId == guid);

                            }

                            if (rItem != null)
                            {
                                modelItem.RefuelItemId = rItem.Id;
                                if (model.FlightId == 0)
                                    model.FlightId = rItem.FlightId;

                                rItem.ExportExtract = true;

                            }


                        }
                        returnModel.Gallon = returnModel.Items.Sum(it => it.Gallon);
                        returnModel.Volume = (decimal)returnModel.Items.Sum(it => it.Volume);
                        returnModel.Weight = (decimal)returnModel.Items.Sum(it => it.Weight);

                        
                        if (returnModel.Gallon > 0 && false)
                            db.Invoices.Add(returnModel);
                    }

                    //db.SaveChanges();

                    ///create invoices
                    /// 
                    if (db.SaveChanges() > 0)
                    {
                        newId = model.Id;
                    }


                }

                db.Database.CurrentTransaction.Commit();

                if (newId > 0)
                    Exporter.ExportInvoice();
                
                return Ok(inv);
            }
            catch (Exception ex)
            {
                Logger.AppendLog("DATA", json, "Invoice");
                //Logger.AppendLog("Invoice", json, "Invoice-data");
                Logger.LogException(ex, "Invoice");

                db.Database.CurrentTransaction.Rollback();
                return BadRequest();
            }

        }


        // DELETE: api/Invoices/5
        [ResponseType(typeof(Invoice))]
        public IHttpActionResult DeleteInvoice(int id)
        {
            Invoice Invoice = db.Invoices.Find(id);
            if (Invoice == null)
            {
                return NotFound();
            }

            db.Invoices.Remove(Invoice);
            db.SaveChanges();

            return Ok(Invoice);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool InvoiceExists(int id)
        {
            return db.Invoices.Count(e => e.Id == id) > 0;
        }
    }
}