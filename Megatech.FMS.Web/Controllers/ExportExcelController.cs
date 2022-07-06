using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS.Data;
using System.IO;
using System.Data.Entity.Migrations;
using Newtonsoft.Json;
using Megatech.FMS.Web.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Configuration;

namespace Megatech.FMS.Web.Controllers
{
    [FMSAuthorize]
    //[Authorize(Roles = "Super Admin, Admins, Administrators, Quản lý miền, Quản trị, Giám sát, Điều phối, Tra nạp, Kỹ thuật, Thống kê")]
    public class ExportExcelController : Controller
    {
        private DataContext db = new DataContext();
        private User user
        {
            get
            {
                var user = db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                return user;
            }
        }
        public string ChangeDate(string date)
        {
            var temp = date.Split('/');
            if (temp.Length > 0)
                return temp[1] + "/" + temp[0] + "/" + temp[2];
            else return date;
        }
        public ActionResult CheckTrucks(string dr, int airport_id = -1)
        {
            var name = "Phieubaoduonghangngay";
            string fileName = name + ".xlsx";

            var airport = db.Airports.FirstOrDefault(a => a.Id == airport_id);
            var checkTrucks = db.CheckTrucks
                .Include(c => c.Airport)
                .Include(c => c.Shift)
                .Include(c => c.Truck)
                .AsNoTracking() as IQueryable<CheckTruck>;

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty"))
                checkTrucks = checkTrucks.Where(f => f.AirportId == user.AirportId);

            if (airport_id > 0)
                checkTrucks = checkTrucks.Where(a => a.AirportId == airport_id);

            var fd = DateTime.Today;
            var td = DateTime.Today;

            if (!string.IsNullOrEmpty(dr))
            {
                var range = dr.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            }
            if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            {
                checkTrucks = checkTrucks.Where(f =>
                f.DateCreated.Day == fd.Day
                && f.DateCreated.Month == fd.Month
                && f.DateCreated.Year == fd.Year);
            }
            else
            {
                td = td.AddHours(23);
                checkTrucks = checkTrucks.Where(f =>
                f.DateCreated >= fd
                && f.DateCreated <= td
                );
            }

            var list = checkTrucks.OrderByDescending(c => c.DateCreated).ToList();

            var filePath = Path.Combine(AppContext.UploadFolder, "Data");
            var physicalPath = System.Web.HttpContext.Current.Server.MapPath(filePath);
            if (!Directory.Exists(physicalPath))
                Directory.CreateDirectory(physicalPath);

            FileInfo newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws1 = package.Workbook.Worksheets.Add(name);
                var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

                using (var rng = ws1.Cells["A:XFD"])
                {
                    rng.Style.Font.SetFromFont(new Font("Arial", 10));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws1.Row(1).Style.Fill.BackgroundColor.SetColor(Color.White);
                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;

                ws1.View.FreezePanes(2, 1);

                ws1.Column(1).Style.WrapText = true;
                ws1.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, 3].Value = "Phiếu bảo dưỡng hàng ngày \n" + " Từ ngày: " + fd.ToString("dd/MM/yyyy") + " - Đến ngày: " + td.ToString("dd/MM/yyyy") + " \n" + (airport != null ? "Sấn bay:" + airport.Name : "");
                ws1.Column(1).Width = 35;

                var rowfix = 1;
                var colfix = 1;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Giờ";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Ngày tháng";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Biển số xe";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Ca";
                ws1.Column(colfix++).Width = 20;

                int rowIndexBegin = 3;
                int rowIndexCurrent = rowIndexBegin;

                foreach (var item in list)
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.DateCreated.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.DateCreated.ToString("dd/MM/yyyy");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Truck != null ? item.Truck.Code : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Shift != null ? item.Shift.Name : "";

                    rowIndexCurrent++;
                }
                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        public ActionResult AmountReport(string dr)
        {
            var name = "Baocaochuyenbaysanluong";
            string fileName = name + ".xlsx";

            var flights = db.Flights
                .Include(f => f.Airport)
                .AsNoTracking() as IQueryable<Flight>;

             var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            
            if (dr != null)
            {
                var range = dr.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    //td = Convert.ToDateTime(ChangeDate(range[1]));
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
               >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
               && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
               <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
               ).OrderByDescending(f => f.RefuelScheduledTime);

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Hour == fd.Hour && f.RefuelScheduledTime.Value.Minute == fd.Minute && f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
            //}

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   //TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
               });

            //var list = temp.ToList();

            var filePath = Path.Combine(AppContext.UploadFolder, "Data");
            var physicalPath = System.Web.HttpContext.Current.Server.MapPath(filePath);
            if (!Directory.Exists(physicalPath))
                Directory.CreateDirectory(physicalPath);

            FileInfo newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws1 = package.Workbook.Worksheets.Add(name);
                var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

                using (var rng = ws1.Cells["A:XFD"])
                {
                    rng.Style.Font.SetFromFont(new Font("Arial", 10));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws1.Row(1).Style.Fill.BackgroundColor.SetColor(Color.White);
                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;

                ws1.View.FreezePanes(2, 1);

                ws1.Column(1).Style.WrapText = true;
                ws1.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, 3].Value = "Báo cáo chuyến bay - sản lượng \n" + " Từ ngày: " + fd.ToString("dd/MM/yyyy") + " - Đến ngày: " + td.ToString("dd/MM/yyyy");
                ws1.Column(1).Width = 35;

                var rowfix = 1;
                var colfix = 1;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Cells[2, rowfix++].Value = "TT";
                ws1.Column(colfix++).Width = 10;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Đơn vị";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-TH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-TH/KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-TH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-TH/KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Ghi chú";
                ws1.Column(colfix++).Width = 20;

                int rowIndexBegin = 3;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                foreach (var item in temp)
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.AirportName;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.KH_Number;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TH_Number;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.TH_Number + "/" + item.KH_Number;
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.KH_Amount).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.TH_Amount).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.TH_Amount).ToString("#,##0") + "/" + @Math.Round(item.KH_Amount).ToString("#,##0");

                    rowIndexCurrent++;
                }
                var s_KH_Number = temp.Sum(f => f.KH_Number);
                var s_TH_Number = temp.Sum(f => f.TH_Number);
                var s_KH_Amount = temp.Sum(f => f.KH_Amount);
                var s_TH_Amount = temp.Sum(f => f.TH_Amount);
                ws1.Cells[rowIndexCurrent, 2].Value = "Tổng";
                ws1.Cells[rowIndexCurrent, 3].Value = s_KH_Number;
                ws1.Cells[rowIndexCurrent, 4].Value = s_TH_Number;
                ws1.Cells[rowIndexCurrent, 5].Value = s_TH_Number + "/" + s_KH_Number;
                ws1.Cells[rowIndexCurrent, 6].Value = Math.Round(s_KH_Amount).ToString("#,##0");
                ws1.Cells[rowIndexCurrent, 7].Value = Math.Round(s_TH_Amount).ToString("#,##0");
                ws1.Cells[rowIndexCurrent, 8].Value = Math.Round(s_TH_Amount).ToString("#,##0") + "/" + Math.Round(s_KH_Amount).ToString("#,##0");
                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        public ActionResult TotalAmountReport(string dr1, string dr2)
        {
            var name = "Baocaotonghopchuyenbaysanluong";
            string fileName = name + ".xlsx";

            var tf = db.Flights
                .Include(f => f.Airport)
                .Include(f => f.RefuelItems)
                .Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            var flights = tf;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (dr1 != null)
            {
                var range = dr1.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);

                if (range.Length > 1)
                    //td = Convert.ToDateTime(ChangeDate(range[1]));
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
            );

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Hour == fd.Hour && f.RefuelScheduledTime.Value.Minute == fd.Minute && f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
            //}


            var fd1 = fd;
            var td1 = td;
            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   //KH_Number = g.Count(),
                   //TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   //KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   //AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   //AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,

                   KH_Number = g.Count(),
                   //TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
               });

            //flights = db.Flights
            //    .Include(f => f.Airport)
            //    .AsNoTracking() as IQueryable<Flight>;
            flights = tf;

            fd = DateTime.Today.AddHours(0).AddMinutes(0);
            td = DateTime.Today.AddHours(23).AddMinutes(59);
            //fd = DateTime.Today;
            //td = DateTime.Today;
            
            if (dr2 != null)
            {
                var range = dr2.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    //td = Convert.ToDateTime(ChangeDate(range[1]));
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0));

            //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
            //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Hour == fd.Hour && f.RefuelScheduledTime.Value.Minute == fd.Minute && f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
            //else
            //{
            //    td = td.AddHours(23);
            //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
            //}

            var fd2 = fd;
            var td2 = td;
            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   //KH_Number2 = g.Count(),
                   //TH_Number2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   //KH_Amount2 = g.Sum(s => s.EstimateAmount),
                   //TH_Amount2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   //AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   //AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,

                   KH_Number2 = g.Count(),
                   //TH_Number2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount2 = g.Sum(s => s.EstimateAmount),
                   //TH_Amount2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
               });

            var list = temp.ToList();
            list.AddRange(temp);
            var tlist = list.OrderBy(a => a.AirportName).GroupBy(a => a.AirportId)
                .Select(
                g => new AmountReport
                {
                    KH_Number = g.Sum(t => t.KH_Number),
                    KH_Number2 = g.Sum(t => t.KH_Number2),

                    TH_Number = g.Sum(t => t.TH_Number),
                    TH_Number2 = g.Sum(t => t.TH_Number2),

                    KH_Amount = g.Sum(t => t.KH_Amount),
                    KH_Amount2 = g.Sum(t => t.KH_Amount2),

                    TH_Amount = g.Sum(t => t.TH_Amount),
                    TH_Amount2 = g.Sum(t => t.TH_Amount2),

                    AirportName = g.FirstOrDefault() != null ? g.FirstOrDefault().AirportName : "",
                    AirportId = g.FirstOrDefault() != null ? g.FirstOrDefault().AirportId : 0,
                }
                );

            var lists = tlist.ToList();

            var filePath = Path.Combine(AppContext.UploadFolder, "Data");
            var physicalPath = System.Web.HttpContext.Current.Server.MapPath(filePath);
            if (!Directory.Exists(physicalPath))
                Directory.CreateDirectory(physicalPath);

            FileInfo newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(Path.Combine(physicalPath, fileName));
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws1 = package.Workbook.Worksheets.Add(name);
                var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

                using (var rng = ws1.Cells["A:XFD"])
                {
                    rng.Style.Font.SetFromFont(new Font("Arial", 10));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws1.Row(1).Style.Fill.BackgroundColor.SetColor(Color.White);
                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;

                ws1.View.FreezePanes(2, 1);

                ws1.Column(1).Style.WrapText = true;
                ws1.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[1, 1].Value = "Báo cáo tổng hợp chuyến bay - sản lượng";
                ws1.Cells[1, 3].Value = "Từ ngày: " + fd1.ToString("dd/MM/yyyy") + " - Đến ngày: " + td1.ToString("dd/MM/yyyy");
                ws1.Cells[1, 9].Value = "Từ ngày: " + fd2.ToString("dd/MM/yyyy") + " - Đến ngày: " + td2.ToString("dd/MM/yyyy");
                ws1.Column(1).Width = 35;

                var rowfix = 1;
                var colfix = 1;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Cells[2, rowfix++].Value = "TT";
                ws1.Column(colfix++).Width = 10;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Đơn vị";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-TH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-TH/KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-TH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-TH/KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-TH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "CB-TH/KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-TH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "SL-TH/KH";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "C.BAY";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "S.LƯỢNG";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "TUẦN...";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "TUẦN...";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = ".../...";
                ws1.Column(colfix++).Width = 20;

                int rowIndexBegin = 3;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                foreach (var item in lists)
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.AirportName;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.KH_Number;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TH_Number;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TH_Number + "/" + item.KH_Number;
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.KH_Amount).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.TH_Amount).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.TH_Amount).ToString("#,##0") + "/" + @Math.Round(item.KH_Amount).ToString("#,##0");

                    ws1.Cells[rowIndexCurrent, col++].Value = item.KH_Number2;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TH_Number2;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TH_Number2 + "/" + item.KH_Number2;
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.KH_Amount2).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.TH_Amount2).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.TH_Amount2).ToString("#,##0") + "/" + @Math.Round(item.KH_Amount2).ToString("#,##0");

                    ws1.Cells[rowIndexCurrent, col++].Value = "";
                    ws1.Cells[rowIndexCurrent, col++].Value = "";
                    ws1.Cells[rowIndexCurrent, col++].Value = "";
                    ws1.Cells[rowIndexCurrent, col++].Value = "";
                    ws1.Cells[rowIndexCurrent, col++].Value = "";

                    rowIndexCurrent++;
                }
                var s_KH_Number = list.Sum(f => f.KH_Number);
                var s_TH_Number = list.Sum(f => f.TH_Number);
                var s_KH_Amount = list.Sum(f => f.KH_Amount);
                var s_TH_Amount = list.Sum(f => f.TH_Amount);

                var s_KH_Number2 = list.Sum(f => f.KH_Number2);
                var s_TH_Number2 = list.Sum(f => f.TH_Number2);
                var s_KH_Amount2 = list.Sum(f => f.KH_Amount2);
                var s_TH_Amount2 = list.Sum(f => f.TH_Amount2);

                ws1.Cells[rowIndexCurrent, 2].Value = "Tổng";

                ws1.Cells[rowIndexCurrent, 3].Value = s_KH_Number;
                ws1.Cells[rowIndexCurrent, 4].Value = s_TH_Number;
                ws1.Cells[rowIndexCurrent, 5].Value = s_TH_Number + "/" + s_KH_Number;
                ws1.Cells[rowIndexCurrent, 6].Value = Math.Round(s_KH_Amount).ToString("#,##0");
                ws1.Cells[rowIndexCurrent, 7].Value = Math.Round(s_TH_Amount).ToString("#,##0");
                ws1.Cells[rowIndexCurrent, 8].Value = Math.Round(s_TH_Amount).ToString("#,##0") + "/" + Math.Round(s_KH_Amount).ToString("#,##0");

                ws1.Cells[rowIndexCurrent, 9].Value = s_KH_Number2;
                ws1.Cells[rowIndexCurrent, 10].Value = s_TH_Number2;
                ws1.Cells[rowIndexCurrent, 11].Value = s_TH_Number2 + "/" + s_KH_Number2;
                ws1.Cells[rowIndexCurrent, 12].Value = Math.Round(s_KH_Amount2).ToString("#,##0");
                ws1.Cells[rowIndexCurrent, 13].Value = Math.Round(s_TH_Amount2).ToString("#,##0");
                ws1.Cells[rowIndexCurrent, 14].Value = Math.Round(s_TH_Amount2).ToString("#,##0") + "/" + Math.Round(s_KH_Amount2).ToString("#,##0");

                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
    }
}