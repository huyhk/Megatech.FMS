//using MigraDoc.DocumentObjectModel;
//using MigraDoc.DocumentObjectModel.Shapes;
//using MigraDoc.DocumentObjectModel.Tables;
//using PdfSharp.Drawing;
//using PdfSharp.Pdf;
using FMS.Data;
using Megatech.FMS.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Megatech.FMS.WebAPI.Models
{
    public class ReceiptModel : BaseViewModel
    {
        public string Number { get; set; }

        public DateTime Date { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string QualityNo { get; set; }


        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }

        public decimal Weight { get; set; }

        public int CustomerId { get; set; }


        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string TaxCode { get; set; }

        public int? CustomerType { get; set; }


        public int FlightId { get; set; }
        public int? FlightType { get; internal set; }
        public string FlightCode { get; set; }
        public String AircraftType { get; set; }

        public string RouteName { get; set; }

        public string AircraftCode { get; set; }

        public decimal ReturnAmount { get; set; }

        public string DefuelingNo { get; set; }

        public bool InvoiceSplit { get; set; }

        public decimal SplitAmount { get; set; }

        
        public string PdfImageString { internal get; set; }

        public string SignImageString { internal get; set; }

        public string SellerImageString { internal get; set; }

        public byte[] Signature { get; set; }

        public byte[] SellerSignature { get; set; }
        public List<ReceiptItemModel> Items { get; set; }

        public bool IsReturn { get; set; }

        public bool? IsFHS { get; set; }

        public bool IsCancelled { get; set; }

        public string CancelReason { get; set; }

        public bool? Manual { get; set; }

        public decimal? TechLog { get; set; }

        public bool Overwrite { get; set; }
        public string CCID { get; internal set; }
        public string SignNo { get; internal set; }
        public DateTime? InvoiceDate { get; internal set; }
        public string InvoiceNumber { get; internal set; }
        public int? SignType { get; internal set; }
        public string ReplaceNumber { get; internal set; }

        public Image CreateReceiptImage()
        {
            var nameLines = (int)Math.Ceiling((decimal)this.CustomerName.Length / 18);
            var height = 30 * 2;
            height += 40 * 2;
            height += 20 * 2;
            height += 30 * (nameLines + 9);
            height += 40;
            height += 30 * 9 * Items.Count;
            height += 40;
            height += 3 * 30;
            height += 2 * (80 + 200);


            var width = 635;

            Image img = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(img))
            {
                g.Clear(Color.White);
                var fontName = "Open Sans";
                Font f = new Font(fontName, 30, GraphicsUnit.Pixel);
                StringFormat center = new StringFormat();
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;

                StringFormat left = new StringFormat();
                left.Alignment = StringAlignment.Near;
                left.LineAlignment = StringAlignment.Center;

                StringFormat right = new StringFormat();
                right.Alignment = StringAlignment.Far;
                right.LineAlignment = StringAlignment.Center;


                height = 50;

                g.DrawString("VIET NAM PETROL LIMITED COMPANY", f, Brushes.Black, new RectangleF(0, height, width, 60), center);

                height += 60;
                f = new Font(fontName, 40, GraphicsUnit.Pixel);

                g.DrawString(IsReturn ? "FUEL RETURNING FORM" : "FUEL DELIVERY RECEIPT", f, Brushes.Black, new RectangleF(0, height, width, 40), center);
                height += 40;
                g.DrawString(IsReturn ? "(PHIẾU HOÀN TRẢ NHIÊN LIỆU)" : "(PHIẾU GIAO NHIÊN LIỆU)", f, Brushes.Black, new RectangleF(0, height, width, 40), center);
                height += 40;
                f = new Font(fontName, 20, GraphicsUnit.Pixel);
                g.DrawString(string.Format("Receipt No.: {0}", this.Number), f, Brushes.Black, new RectangleF(0, height, width, 20), center);
                height += 20;
                g.DrawString(string.Format("{0:dd/MM/yyyy}", this.Date), f, Brushes.Black, new RectangleF(0, height, width, 20), center);
                height += 20;
                g.DrawLine(new Pen(Color.Black, 1), 0, height + 5, width, height + 5);
                height += 10;
                f = new Font(fontName, 30, GraphicsUnit.Pixel);

                DrawText(g, "Buyer", this.CustomerName, height, nameLines, f, left);

                height += nameLines * 30;
                DrawText(g, "Flight No.", this.FlightCode, height, 1, f);
                height += 30;
                DrawText(g, "Route", this.RouteName, height, 1, f);
                height += 30;
                DrawText(g, "A/C Type", this.AircraftType, height, 1, f);

                height += 30;
                DrawText(g, "A/C Reg", this.AircraftCode, height, 1, f);
                height += 30;
                DrawText(g, "Cert No.", this.QualityNo, height, 1, f);
                height += 30;
                DrawText(g, "Start Time", string.Format("{0:HH:mm dd/MM/yyyy}", this.StartTime), height, 1, f);
                height += 30;
                DrawText(g, "End Time", string.Format("{0:HH:mm dd/MM/yyyy}", this.EndTime), height, 1, f);
                height += 30;
                DrawText(g, "Product Name", "JET A-1", height, 1, f);
                height += 30;
                DrawText(g, "Refueling Method", "FHS", height, 1, f);
                height += 30;
                g.DrawLine(new Pen(Color.Black, 1), 0, height + 5, width, height + 5);

                height += 10;
                f = new Font(fontName, 40, GraphicsUnit.Pixel);
                g.DrawString("DETAIL", f, Brushes.Black, new RectangleF(0, height, width, 40), center);
                height += 40;
                g.DrawLine(new Pen(Color.Black, 1), 0, height + 5, width, height + 5);
                height += 10;
                f = new Font(fontName, 30, GraphicsUnit.Pixel);
                var i = 1;
                foreach (var item in Items)
                {
                    DrawText(g, "#", i.ToString(), height, 1, f);
                    height += 30;
                    DrawText(g, "Refueler No.", item.TruckNo, height, 1, f);
                    height += 30;
                    DrawText(g, "Start Meter", item.StartNumber.ToString("0"), height, 1, f);
                    height += 30;
                    DrawText(g, "End Meter", item.EndNumber.ToString("0"), height, 1, f);
                    height += 30;
                    DrawText(g, "Temp(°C)", item.Temperature.ToString("0.00"), height, 1, f);
                    height += 30;
                    DrawText(g, "Density(kg/l)", item.Density.ToString("0.0000"), height, 1, f);
                    height += 30;
                    DrawText(g, "USG", item.Gallon.ToString("0"), height, 1, f);
                    height += 30;
                    DrawText(g, "Liter", item.Volume.ToString("0"), height, 1, f);
                    height += 30;
                    DrawText(g, "Kg", item.Weight.ToString("0"), height, 1, f);
                    height += 30;
                    g.DrawLine(new Pen(Color.Black, 1), 0, height + 5, width, height + 5);
                    height += 10;
                    i++;
                }
                f = new Font(fontName, 40, GraphicsUnit.Pixel);
                g.DrawString("TOTAL", f, Brushes.Black, new RectangleF(0, height, width, 40), center);
                height += 40;
                g.DrawLine(new Pen(Color.Black, 1), 0, height + 5, width, height + 5);
                height += 10;
                f = new Font(fontName, 30, GraphicsUnit.Pixel);
                DrawText(g, "USG", this.Items.Sum(ri => ri.Gallon).ToString("0"), height, 1, f);
                height += 30;
                DrawText(g, "Liter", this.Items.Sum(ri => ri.Volume).ToString("0"), height, 1, f);
                height += 30;
                DrawText(g, "Kg", this.Items.Sum(ri => ri.Weight).ToString("0"), height, 1, f);
                height += 30;
                g.DrawLine(new Pen(Color.Black, 1), 0, height + 5, width, height + 5);
                height += 10;
                g.DrawString("Buyer", f, Brushes.Black, new RectangleF(0, height, width, 30), center);

                height += 30;
                if (this.Signature != null)
                {
                    Bitmap bmp;
                    using (var ms = new MemoryStream(this.Signature))
                    {
                        bmp = new Bitmap(ms);
                        g.DrawImage(bmp, bmp.Width / 2, height, bmp.Width, bmp.Height);
                    }
                }
                height += 200;

                g.DrawString("Seller", f, Brushes.Black, new RectangleF(0, height, width, 30), center);
                height += 30;
                if (this.SellerSignature != null)
                {
                    Bitmap bmp;
                    using (var ms = new MemoryStream(this.SellerSignature))
                    {
                        bmp = new Bitmap(ms);
                        g.DrawImage(bmp, bmp.Width / 2, height, bmp.Width, bmp.Height);
                    }
                }
                g.Flush();

            }
            return img;
        }
        private void DrawText(Graphics g, string text, string value, int height, int lines, Font f)
        {
            StringFormat right = new StringFormat();
            right.Alignment = StringAlignment.Far;
            right.LineAlignment = StringAlignment.Center;
            DrawText(g, text, value, height, lines, f, right);

        }
        private void DrawText(Graphics g, string text, string value, int height, int lines, Font f, StringFormat format)
        {
            StringFormat center = new StringFormat();
            center.Alignment = StringAlignment.Center;
            center.LineAlignment = StringAlignment.Center;

            StringFormat left = new StringFormat();
            left.Alignment = StringAlignment.Near;
            left.LineAlignment = StringAlignment.Center;


            g.DrawString(text, f, Brushes.Black, new RectangleF(0, height, 270, 30), left);
            g.DrawString(":", f, Brushes.Black, new RectangleF(270, height, 5, 30), center);
            g.DrawString(value, f, Brushes.Black, new RectangleF(275, height, 360, lines * 30), format);


        }

        public static ReceiptModel SaveReceipt(ReceiptModel receipt)
        {
            try
            {
                using (var db = new DataContext())
                {
                    db.Database.BeginTransaction();
                    var number = receipt.Number;
                    var model = db.Receipts.FirstOrDefault(r => r.Number == number);
                    while (model != null)
                    {
                        var num = number.Substring(number.Length - 1, 1);
                        num = (int.Parse(num) + 1).ToString();
                        number = number.Substring(0, number.Length - 1) + num;
                        model = db.Receipts.FirstOrDefault(r => r.Number == number);
                    }
                    if (model == null && !receipt.IsCancelled)
                    {
                        model = new Receipt
                        {
                            //UserCreatedId = userId,

                            Number = number,
                            Date = receipt.Date,
                            InvoiceSplit = receipt.InvoiceSplit,
                            SplitAmount = receipt.SplitAmount,

                            ReturnAmount = receipt.ReturnAmount,
                            DefuelingNo = receipt.DefuelingNo,


                            StartTime = receipt.StartTime,
                            EndTime = receipt.EndTime,
                            CustomerId = receipt.CustomerId,
                            CustomerName = receipt.CustomerName,
                            CustomerCode = receipt.CustomerCode,
                            FlightType = receipt.FlightType,
                            TaxCode = receipt.TaxCode,
                            CustomerAddress = receipt.CustomerAddress,
                            RouteName = receipt.RouteName,
                            FlightId = receipt.FlightId,
                            AircraftCode = receipt.AircraftCode,
                            FlightCode = receipt.FlightCode,

                            IsReturn = receipt.IsReturn,
                            Manual = receipt.Manual,

                            Items = new List<ReceiptItem>()
                        };

                        foreach (var item in receipt.Items)
                        {
                            var guid = Guid.Parse(item.RefuelItemId);
                            var refuelItem = db.RefuelItems.Include(r => r.Truck).Include(r => r.Flight)
                                .Where(r => r.UniqueId == guid && r.TruckId == item.TruckId && r.StartTime == item.StartTime).FirstOrDefault();

                            var modelItem = new ReceiptItem
                            {
                                RefuelId = refuelItem?.Id,
                                TruckId = item.TruckId,
                                //Truck = refuelItem?.Truck,
                                StartTime = item.StartTime,
                                EndTime = item.EndTime,
                                StartNumber = item.StartNumber,
                                EndNumber = item.EndNumber,
                                Temperature = item.Temperature,
                                Density = item.Density,
                                Gallon = item.Gallon == 0 ? (decimal)refuelItem?.Gallon : item.Gallon,
                                Volume = item.Volume == 0 ? (decimal)refuelItem?.Volume : item.Volume,
                                Weight = item.Weight == 0 ? (decimal)refuelItem?.Weight : item.Weight,
                                QualityNo = item.QualityNo,
                                OperatorId = refuelItem?.OperatorId,
                                DriverId = refuelItem?.DriverId
                            };
                            item.Gallon = modelItem.Gallon;
                            item.Volume = modelItem.Volume;
                            item.Weight = modelItem.Weight;
                            model.Items.Add(modelItem);

                            if (model.FlightId == 0 && refuelItem != null)
                            {
                                //find the flight 
                                model.Flight = refuelItem.Flight;
                            }

                            Truck truck = db.Trucks.FirstOrDefault(t => t.Id == item.TruckId);
                            if (truck != null)
                                model.RefuelCompany = truck.RefuelCompany;

                            if (refuelItem != null)
                                refuelItem.Receipt = model;


                        }

                        if (receipt.PdfImageString != null)
                            model.Image = Convert.FromBase64String(receipt.PdfImageString);
                        if (receipt.SignImageString != null)
                        {
                            model.Signature = Convert.FromBase64String(receipt.SignImageString);
                            receipt.Signature = model.Signature;
                        }

                        if (receipt.SellerImageString != null)
                        {
                            model.SellerImage = Convert.FromBase64String(receipt.SellerImageString);
                            receipt.SellerSignature = model.SellerImage;
                        }

                        //create pdf
                        if (model.IsFHS && model.Image != null)
                        {
                            var imgIn = receipt.CreateReceiptImage();
                            using (var ms = new MemoryStream())
                            {
                                imgIn.Save(ms, ImageFormat.Jpeg);
                                model.Image = ms.ToArray();

                            }
                            //imgIn.Save(Path.Combine(HostingEnvironment.MapPath("~/logs/"), model.Number + ".jpg"), ImageFormat.Jpeg);
                        }



                        db.Receipts.Add(model);


                        db.SaveChanges();


                        model = db.Receipts.Include(r => r.Items.Select(iv => iv.Truck)).Include(r => r.Flight.Airline).Include(r => r.Flight.Airport).FirstOrDefault(r => r.Id == model.Id);

                        var price = 0.0M;
                        var currency = CURRENCY.VND;
                        var unit = UNIT.GALLON;

                        //var exRate = 1.0M;
                        var exRate = db.ProductPrices.Where(p => p.StartDate <= model.EndTime && p.Currency == CURRENCY.USD)
                            .OrderByDescending(p => p.StartDate).Select(p => p.ExchangeRate)
                            .FirstOrDefault();

                        var airport = db.Airports.FirstOrDefault(a => a.Id == model.Flight.AirportId);

                        var refuelCompany = model.Items.Select(mi => mi.Truck.RefuelCompany).FirstOrDefault();
                        var depotType = (receipt.IsFHS ?? false) ? 4 : airport.DepotType;
                        var airlineType = db.Airlines.Where(a => a.Id == model.CustomerId).Select(a => a.AirlineType).FirstOrDefault() ?? 0;
                        var flightType = model.FlightType ?? (int)model.Flight.FlightType;


                        var prices = (from p in db.ProductPrices.Include(p => p.Product)
                                      where p.StartDate <= model.EndTime // && p.DepotType == airport.DepotType && p.BranchId == (int)airport.Branch
                                      group p by new { p.CustomerId, p.AirlineType, p.BranchId, p.DepotType, p.Unit }
                             into groups
                                      select groups.OrderByDescending(g => g.StartDate).FirstOrDefault()).ToList();

                        var pPrice = prices.OrderByDescending(p => p.StartDate)
                                                            .FirstOrDefault(p => p.AirlineType == flightType  && p.CustomerId == model.Flight.AirlineId);
                        if (pPrice == null && airlineType == (int)CUSTOMER_TYPE.LOCAL)
                        {
                            pPrice = prices.OrderByDescending(p => p.StartDate)
                                                            .FirstOrDefault(p => p.CustomerId == model.Flight.AirlineId);
                        }

                        if (pPrice == null)
                            pPrice = prices.OrderByDescending(p => p.StartDate)
                        .FirstOrDefault(p => p.AirlineType == (flightType == (int)FLIGHT_TYPE.OVERSEA ? 1 : 0) && (p.Unit == (int)0 && p.DepotType == depotType && p.BranchId == (int)airport.Branch));

                        if (pPrice != null)
                        {
                            price = pPrice.Price;
                            currency = pPrice.Currency;
                            unit = unit = pPrice.Unit == 0 ? UNIT.GALLON : UNIT.KG;
                        }

                        Logger.AppendLog("RECEIPT", "PRICE OK", "receipt-create");


                        var invoices = model.CreateInvoices();

                        Logger.AppendLog("RECEIPT", "Invoices created", "receipt-create");


                        foreach (var item in invoices)
                        {
                            item.Date = receipt.InvoiceDate;
                            item.InvoiceNumber = receipt.InvoiceNumber;
                            item.SignNo = receipt.SignNo;
                            item.CCID = receipt.CCID;
                            item.SignType = receipt.SignType;
                            item.Exported_AITS = item.InvoiceNumber != null;

                            item.RefuelCompany = refuelCompany;
                            item.Price = price;
                            item.Currency = currency;
                            item.ExchangeRate = currency == CURRENCY.USD ? exRate : 1.0M;
                            item.Unit = unit;

                            item.Gallon = item.Items.Sum(m => m.Gallon);
                            item.Volume = (decimal)item.Items.Sum(m => m.Volume);
                            item.Weight = (decimal)item.Items.Sum(m => m.Weight);
                            item.TaxRate = model.Flight.FlightType == FLIGHT_TYPE.DOMESTIC && model.Flight.Airline.AirlineType == 1 ? 0.1M : 0;
                            item.SaleAmount = Math.Round((decimal)item.Price * (item.Unit == UNIT.GALLON ? item.Gallon : item.Weight), item.Currency == CURRENCY.USD ? 2 : 0, MidpointRounding.AwayFromZero);
                            item.TotalAmount = item.SaleAmount + item.TaxAmount;
                            item.ReceiptId = model.Id;
                        }

                        db.Invoices.AddRange(invoices);
                        //model.Invoices.AddRange(invoices);

                        db.SaveChanges();
                    }

                    db.Database.CurrentTransaction.Commit();

                    if (receipt.IsCancelled && model != null)
                    {
                        Logger.AppendLog("INV", "Cancel invoice Reason:" + receipt.CancelReason, "invoice.log");
                        var invoices = db.Invoices.Where(inv => inv.ReceiptId == model.Id).ToList();
                        foreach (var item in invoices)
                        {
                            Logger.AppendLog("INV", "Invoice Id : " + item.Id.ToString(), "invoice.log");
                            item.CancelReason = receipt.CancelReason;
                            item.RequestCancel = true;
                            db.SaveChanges();
                            //DataExchange.InvoiceExporter.Cancel(item.Id);
                        }
                        Logger.AppendLog("INV", "END cancel invoice", "invoice.log");
                    }

                    receipt.Id = model.Id;
                    receipt.PdfImageString = null;
                    receipt.SignImageString = null;
                    receipt.SellerImageString = null;
                    return receipt;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class ReceiptItemModel
    {
        public int? TruckId { get; set; }

        public string TruckNo { get; set; }

        public int? RefuelId { get; set; }
        public string RefuelItemId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal Gallon { get; set; }

        public decimal Volume { get; set; }
        public decimal Weight { get; set; }
        public decimal Temperature { get; set; }
        public decimal Density { get; set; }

        public string QualityNo { get; set; }
        public decimal StartNumber { get; set; }
        public decimal EndNumber { get; set; }
        public int? DriverId { get; set; }

        public int? OperatorId { get; set; }


        
    }


}