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
using EntityFramework.DynamicFilters;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Megatech.FMS.Web.Controllers
{
    public class ReportsController : Controller
    {
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        private DataContext db = new DataContext();
        private User user
        {
            get
            {
                var user = db.Users.Include(u => u.Airports).FirstOrDefault(u => u.UserName == User.Identity.Name);
                return user;
            }
        }
        private int currentUserId
        {
            get
            {
                if (user != null)
                    return user.Id;
                else return 0;
            }
        }

        public string ChangeDate(string date)
        {
            var temp = date.Split('/');
            if (temp.Length > 0)
                return temp[1] + "/" + temp[0] + "/" + temp[2];
            else return date;
        }
        // GET: Report
        public ActionResult Index()
        {
            return View();
        }
        [FMSAuthorize]
        //[Authorize(Roles = "Super Admin, Admins, Administrators,Quản lý miền, Quản lý chi nhánh, Điều hành, Tra nạp")]
        public ActionResult Export(string dr, string a)
        {
            var name = "Congtactranap";
            string fileName = name + ".xlsx";

            var flights = db.Flights
                .Include(f => f.ParkingLot)
                .Include(f => f.Airport)
                .Include(f => f.Airline)
                .Include(f => f.RefuelItems.Select(r => r.Truck))
                .Include(f => f.RefuelItems.Select(r => r.Driver))
                .Include(f => f.RefuelItems.Select(r => r.Operator))
                .Include(f => f.RefuelItems.Select(r => r.InvoiceForm))
                .Where(f => f.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).AsNoTracking() as IQueryable<Flight>;
            var fl_temp = flights;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                //var arp = db.Airports.FirstOrDefault(ar => ar.Id == user.AirportId);
                //if (arp != null)
                var arps = user.Airports.Select(u => u.Id);
                flights = flights.Where(f => arps.Contains((int)f.AirportId));
            }

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (!string.IsNullOrEmpty(dr))
            {
                var range = dr.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(a))
            {
                int aID = 0;
                int.TryParse(a, out aID);

                flights = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                && f.AirportId == aID);
            }
            else
            {
                flights = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0));
            }
            var list = flights.OrderBy(f => f.RefuelScheduledTime).ToList();

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
                CreateHeader(ws1);

                int rowIndexBegin = 2;
                int rowIndexCurrent = rowIndexBegin;

                GenerateData(ws1, rowIndexCurrent, list);
                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        private ExcelWorksheet CreateHeader(ExcelWorksheet ws1)
        {
            var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());

            using (var rng = ws1.Cells["A:XFD"])
            {
                rng.Style.Font.SetFromFont(new Font("Arial", 10));
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            ///
            /// header property
            /// 
            ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws1.Row(1).Style.Fill.BackgroundColor.SetColor(Color.White);
            ws1.Row(1).Style.Font.Bold = true;
            ws1.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;

            ws1.View.FreezePanes(2, 1);
            var rowfix = 1;
            var colfix = 1;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix++].Value = "Stt";
            ws1.Column(colfix++).Width = 5;


            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix++].Value = "Thời gian";
            ws1.Column(colfix++).Width = 10;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Loại tàu bay";
            ws1.Column(colfix++).Width = 20;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Số hiệu tàu bay";
            ws1.Column(colfix++).Width = 20;

            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Số hiệu chuyến bay";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Hãng hàng không";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Chặng bay";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Số hóa đơn";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws1.Cells[1, rowfix++].Value = "Số phiếu hóa nghiệm";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Giờ tra nạp dự kiến";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Giờ bắt đầu tra nạp";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Giờ kết thúc tra nạp";
            ws1.Column(colfix++).Width = 20;


            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Xe tra nạp";
            ws1.Column(colfix++).Width = 25;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Lái xe";
            ws1.Column(colfix++).Width = 25;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Nhân viên kỹ thuật tra nạp";
            ws1.Column(colfix++).Width = 25;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Số đồng hồ bắt đầu";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Số đồng hồ kết thúc";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Lượng nhiên liệu đã nạp";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Nhiệt độ thực tế (độ C)";
            ws1.Column(colfix++).Width = 20;

            if (User.IsInRole("Super Admin"))
            {
                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[1, rowfix++].Value = "Mã sân bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[1, rowfix++].Value = "Sân bay";
                ws1.Column(colfix++).Width = 20;

                ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[1, rowfix++].Value = "Nhiệt độ 2";
                ws1.Column(colfix++).Width = 20;
            }

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Lit thực tế";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Tỷ trọng thực tế (kg/m3)";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Khối lượng (kg)";
            ws1.Column(colfix++).Width = 20;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Đơn giá";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Loại tiền tệ";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Tiền hàng";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "VAT%";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Tiền thuế VAT";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Mẫu số (phiếu, hóa đơn)";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Ký hiệu (phiếu, hóa đơn)";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Cách tạo";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws1.Cells[1, rowfix++].Value = "Kết thúc";
            ws1.Column(colfix++).Width = 10;

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Tổng tiền thanh toán";

            ws1.Cells[1, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws1.Cells[1, rowfix++].Value = "Ghi chú";

            ws1.Column(colfix++).Width = 20;

            return ws1;
        }
        private ExcelWorksheet GenerateData(ExcelWorksheet ws1, int rowIndexCurrent, List<Flight> list)
        {
            var html = new HtmlHelper(new ViewContext(ControllerContext, new WebFormView(ControllerContext, "empty"), new ViewDataDictionary(), new TempDataDictionary(), new System.IO.StringWriter()), new ViewPage());
            var index = 1;
            foreach (var item in list)
            {
                foreach (var ritem in item.RefuelItems.Where(r => r.Status == REFUEL_ITEM_STATUS.DONE))
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime.Value.ToString("dd/MM/yyyy");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftType;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftCode;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Code;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Airline != null ? item.Airline.Name : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RouteName;
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.InvoiceNumber;
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.QCNo;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelScheduledTime.Value.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.StartTime.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.EndTime.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Truck != null ? ritem.Truck.Code : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Driver != null ? ritem.Driver.FullName : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Operator != null ? ritem.Operator.FullName : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.StartNumber).ToString();
                    //ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.EndNumber).ToString();
                    ws1.Cells[rowIndexCurrent, col++].Value = (Math.Round(ritem.StartNumber) + Math.Round(ritem.Amount)).ToString();
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.Amount).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.ManualTemperature.ToString("#,##0.00");

                    if (User.IsInRole("Super Admin"))
                    {
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Airport != null ? item.Airport.Code : "---";
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Airport != null ? item.Airport.Name : "---";
                        ws1.Cells[rowIndexCurrent, col++].Value = ritem.Temperature.ToString("#,##0.00");
                    }

                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.Volume).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Density.ToString("#,##0.0000");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(ritem.Weight).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.PrintTemplate == PRINT_TEMPLATE.BILL ? "---" : (ritem.Currency == REFUEL_CURRENCY.USD ? ritem.Price.ToString("#,##0.00") : Math.Round(ritem.Price).ToString("#,##0"));

                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Currency;
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.PrintTemplate == PRINT_TEMPLATE.BILL ? "---" : (ritem.Currency == REFUEL_CURRENCY.USD ? ritem.SaleAmount.ToString("#,##0.00") : Math.Round(ritem.SaleAmount).ToString("#,##0"));
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.PrintTemplate == PRINT_TEMPLATE.BILL ? "---" : ritem.TaxRate.ToString();
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.PrintTemplate == PRINT_TEMPLATE.BILL ? "---" : (ritem.Currency == REFUEL_CURRENCY.USD ? ritem.VATAmount.ToString("#,##0.00") : Math.Round(ritem.VATAmount).ToString("#,##0"));

                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.InvoiceForm != null ? ritem.InvoiceForm.FormNo : "---";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.InvoiceForm != null ? ritem.InvoiceForm.Sign : "---";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.CreatedLocation == ITEM_CREATED_LOCATION.APP ? "Tự động" : "Từ web";
                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.Completed ? "Thủ công" : "Tự động";

                    ws1.Cells[rowIndexCurrent, col++].Value = ritem.PrintTemplate == PRINT_TEMPLATE.BILL ? "---" : (ritem.Currency == REFUEL_CURRENCY.USD ? ritem.TotalSalesAmount.ToString("#,##0.00") : Math.Round(ritem.TotalSalesAmount).ToString("#,##0"));
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Note;
                    rowIndexCurrent++;
                }
            }
            return ws1;
        }
        [FMSAuthorize]
        public ActionResult TimekeepingReport()
        {
            string daterange = "";
            if (Request["daterange"] != null)
                daterange = Request["daterange"];

            string a = "";
            if (Request["a"] != null)
                a = Request["a"];

            string g = "";
            if (Request["g"] != null)
                g = Request["g"];
            return View(TimekeepingList(daterange, a, g));
        }
        [FMSAuthorize]
        public ActionResult TimekeepingExportExcel(string daterange, string a, string g)
        {
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            var temp = TimekeepingList(daterange, a, g);
            var name = "Baocaochamcong";
            string fileName = name + ".xlsx";

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
                ws1.Cells[1, 3].Value = "Tổng hợp số chuyến tra nạp của nhân viên tra nạp \n" + " Từ ngày: " + fd.ToString("dd/MM/yyyy") + " - Đến ngày: " + td.ToString("dd/MM/yyyy");
                ws1.Column(1).Width = 35;

                var rowfix = 1;
                var colfix = 1;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Cells[2, rowfix++].Value = "TT";
                ws1.Column(colfix++).Width = 10;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[2, rowfix++].Value = "Họ tên";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "Chức danh";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "Nhóm 1";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "Nhóm 2";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "Nhóm 3";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "Nhóm 4";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Cells[2, rowfix++].Value = "Tổng cộng";
                ws1.Column(colfix++).Width = 20;

                rowfix = 2;
                colfix = 1;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Bình thường";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Chuyên cơ";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Hút dầu";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Bình thường";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Chuyên cơ";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Hút dầu";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Bình thường";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Chuyên cơ";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Hút dầu";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Bình thường";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Chuyên cơ";
                ws1.Column(colfix++).Width = 20;

                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Column(colfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Cells[3, rowfix++].Value = "Hút dầu";
                ws1.Column(colfix++).Width = 20;

                int rowIndexBegin = 4;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                foreach (var item in temp)
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.FullName;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Role;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelBT;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelCC;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Extract;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelBT_2;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelCC_2;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Extract_2;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelBT_3;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelCC_3;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Extract_3;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelBT_4;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.RefuelCC_4;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Extract_4;

                    ws1.Cells[rowIndexCurrent, col++].Value = item.Extract + item.RefuelCC + item.RefuelBT + item.Extract_2 + item.RefuelCC_2 + item.RefuelBT_2 + item.Extract_3 + item.RefuelCC_3 + item.RefuelBT_3 + item.Extract_4 + item.RefuelCC_4 + item.RefuelBT_4;

                    rowIndexCurrent++;
                }

                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        public List<TimekeepingReport> TimekeepingList(string daterange, string a, string g)
        {
            var flights = db.Flights
                .Include(f => f.RefuelItems)
                .Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                ).OrderByDescending(f => f.RefuelScheduledTime);


            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);

            var context = new ApplicationDbContext();
            var driver_id = (from u in context.Users
                             where u.Roles.Any(ua => ua.RoleId == "a7661639-6d36-4b29-a8e3-0f425afcd955")
                             select u.UserId).ToArray();

            var operator_id = (from u in context.Users
                               where u.Roles.Any(ua => ua.RoleId == "32008037-b18f-422c-897d-2328c968285b")
                               select u.UserId).ToArray();

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var u_airportIds = user.Airports.Select(ar => ar.Id);
                //if (User.IsInRole("Quản lý miền") && user.Airport != null)
                //{
                //    var ids = db.Airports.Where(ap => ap.Branch == user.Airport.Branch).Select(ap => ap.Id);
                //    flights = flights.Where(f => ids.Contains((int)f.AirportId));
                //    ViewBag.Airports = db.Airports.Where(ar => ids.Contains((int)ar.Id)).OrderBy(ap => ap.Name).ToList();
                //}
                //else
                //{
                flights = flights.Where(f => u_airportIds.Contains((int)f.AirportId));
                ViewBag.Airports = db.Airports.Where(ar => u_airportIds.Contains(ar.Id)).OrderBy(ap => ap.Name).ToList();
                //}
            }
            else
                ViewBag.Airports = db.Airports.OrderBy(ap => ap.Name).ToList();

            if (!string.IsNullOrEmpty(a))
            {
                var airportId = 0;
                int.TryParse(Request["a"], out airportId);
                if (airportId > 0)
                    flights = flights.Where(f => f.AirportId == airportId);
            }

            var air_c = db.Aircrafts;
            var aircraftTypes = air_c.Select(ar => ar.AircraftType);
            var refuelItems = db.RefuelItems.Where(r => flights.Select(f => f.Id).Contains(r.FlightId) && r.Status == REFUEL_ITEM_STATUS.DONE);
            if (!string.IsNullOrEmpty(g))
            {
                if (g == "d")
                {
                    var driver_lst = refuelItems.Where(r => driver_id.Contains((int)r.DriverId)).GroupBy(r => new { r.Driver }).Select(t =>
             new TimekeepingReport
             {
                 FullName = t.Key.Driver != null ? t.Key.Driver.FullName : "",
                 Role = t.Key.Driver != null ? "LXTN" : "",
                 RefuelBT = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType)) || !aircraftTypes.Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),
             }).OrderBy(i => i.FullName).ToList();
                    return driver_lst;
                }
                else
                {
                    var operator_list1 = refuelItems.Where(r => operator_id.Contains((int)r.OperatorId)).GroupBy(r => new { r.Operator }).Select(t =>
             new TimekeepingReport
             {
                 FullName = t.Key.Operator != null ? t.Key.Operator.FullName : "",
                 Role = t.Key.Operator != null ? "NVKT" : "",
                 RefuelBT = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType)) || !aircraftTypes.Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),
             }).OrderBy(i => i.FullName).ToList();
                    return operator_list1;
                }
            }
            else
            {
                var temp = refuelItems.Where(r => driver_id.Contains((int)r.DriverId)).GroupBy(r => new { r.Driver }).Select(t =>
             new TimekeepingReport
             {
                 FullName = t.Key.Driver != null ? t.Key.Driver.FullName : "",
                 Role = t.Key.Driver != null ? "LXTN" : "",
                 RefuelBT = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                 RefuelBT_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 RefuelCC_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                 Extract_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType)) || !aircraftTypes.Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),
             }).ToList();

                var operator_list = refuelItems.Where(r => operator_id.Contains((int)r.OperatorId)).GroupBy(r => new { r.Operator }).Select(t =>
                 new TimekeepingReport
                 {
                     FullName = t.Key.Operator != null ? t.Key.Operator.FullName : "",
                     Role = t.Key.Operator != null ? "NVKT" : "",
                     RefuelBT = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     RefuelCC = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     Extract = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "1").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                     RefuelBT_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     RefuelCC_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     Extract_2 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "2").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                     RefuelBT_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     RefuelCC_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     Extract_3 = t.Where(r => flights.Where(f => air_c.Where(ar => ar.Code == "3").Select(ar => ar.AircraftType).Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),

                     RefuelBT_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && (f.FlightCarry == FlightCarry.PAX || f.FlightCarry == FlightCarry.CGO)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     RefuelCC_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType) || !aircraftTypes.Contains(f.AircraftType)) && f.FlightCarry == FlightCarry.CCO).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL).Count(),
                     Extract_4 = t.Where(r => flights.Where(f => (air_c.Where(ar => ar.Code == "4").Select(ar => ar.AircraftType).Contains(f.AircraftType)) || !aircraftTypes.Contains(f.AircraftType)).Select(f => f.Id).Contains(r.FlightId) && r.RefuelItemType == REFUEL_ITEM_TYPE.EXTRACT).Count(),
                 });

                temp.AddRange(operator_list);
                var list = temp.OrderBy(i => i.FullName).ToList();
                return list;
            }
        }
        [FMSAuthorize]
        public ActionResult TotalAmountReport()
        {
            var tf = db.Flights
                .Include(f => f.Airport)
                .Include(f => f.RefuelItems)
                //.Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            var flights = tf;

            //var count = flights.Count();
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
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

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.ToList().GroupBy(f => f.AirportId)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   //TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => Math.Round(Math.Round(r.Amount) * 3.7854M) * r.Density)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
                   Branch = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Branch : Branch.NONE
               });

            var list = temp.ToList();
            flights = tf;

            fd = DateTime.Today.AddHours(0).AddMinutes(0);
            td = DateTime.Today.AddHours(23).AddMinutes(59);
            daterange = Request["daterange2"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                //fd = Convert.ToDateTime(ChangeDate(range[0]));
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                //td = Convert.ToDateTime(ChangeDate(range[1]));
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0));

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            temp = flights.ToList().GroupBy(f => f.AirportId)
               .Select(
               g => new AmountReport
               {
                   KH_Number2 = g.Count(),
                   //TH_Number2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   TH_Number2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount2 = g.Sum(s => s.EstimateAmount),
                   //TH_Amount2 = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   TH_Amount2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => Math.Round(Math.Round(r.Amount) * 3.7854M) * r.Density)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
                   Branch = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Branch : Branch.NONE
               });
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
                    Branch = g.FirstOrDefault() != null ? g.FirstOrDefault().Branch : Branch.NONE
                }
                );
            return View(tlist.OrderBy(t => t.Branch).ToList());
        }
        [FMSAuthorize]
        public ActionResult AmountReport()
        {
            var flights = db.Flights
                .Include(f => f.Airport)
                .Include(f => f.RefuelItems)
                //.Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
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

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.ToList().GroupBy(f => f.AirportId)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   TH_Number = g.Where(s => s.Status == FlightStatus.REFUELED).Count(),
                   //TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   //TH_Amount = g.Where(s => s.Status == FlightStatus.REFUELED).Count() > 0 ? g.Where(s => s.Status == FlightStatus.REFUELED).Sum(s => s.RefuelItems.Sum(r => r.Amount)) : 0,
                   //TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => (Math.Round(r.Amount) * 3.7854M) / 1000)) : 0,
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => Math.Round(Math.Round(r.Amount) * 3.7854M) * r.Density)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   Branch = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Branch : Branch.NONE,
               });

            var list = temp.OrderBy(t => t.Branch).ToList();
            return View(list);
        }
        [FMSAuthorize]
        public ActionResult RefuelHistory(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var flights = db.Flights
                .Include(f => f.ParkingLot)
                .Include(f => f.Airport)
                .Include(f => f.Airline)
                .Include(f => f.RefuelItems.Select(r => r.Truck))
                .Include(f => f.RefuelItems.Select(r => r.Driver))
                .Include(f => f.RefuelItems.Select(r => r.Operator))
                .Include(f => f.RefuelItems.Select(r => r.InvoiceForm))
                .Where(f => f.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).AsNoTracking() as IQueryable<Flight>;
            var count = flights.Count();
            var fl_temp = flights;
            ViewBag.Airports = db.Airports.ToList();

            if (user != null)
            {
                var u_airportIds = user.Airports.Select(a => a.Id);
                if (u_airportIds.Count() > 0)
                {
                    flights = flights.Where(f => u_airportIds.Contains((int)f.AirportId));
                    count = flights.Count();
                }
                ViewBag.Airports = db.Airports.Where(ar => u_airportIds.Contains(ar.Id)).ToList();
            }

            //if (User.IsInRole("Quản lý miền") && user.Airport != null)
            //{
            //    var ids = db.Airports.Where(a => a.Branch == user.Airport.Branch).Select(a => a.Id);
            //    flights = fl_temp.Where(f => ids.Contains((int)f.AirportId));
            //    count = flights.Count();
            //    ViewBag.Airports = db.Airports.Where(a => ids.Contains(a.Id)).ToList();
            //}

            //if (Request["fd"] != null && Request["td"] != null)
            //{
            //var fd = Convert.ToDateTime(ChangeDate(Request["fd"]));
            //var td = Convert.ToDateTime(ChangeDate(Request["td"]));

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                int a = 0;
                int.TryParse(Request["a"], out a);

                ViewBag.Total = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                && f.AirportId == a).Select(f => f.RefuelItems.Sum(r => r.Amount)).DefaultIfEmpty().Sum();

                flights = flights.Where(f =>
                 DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                && f.AirportId == a);
                count = flights.Count();

                //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                //{
                //    ViewBag.Total = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year && f.AirportId == a).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year && f.AirportId == a).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
                //else
                //{
                //    td = td.AddHours(23);
                //    ViewBag.Total = flights.Where(f => f.RefuelScheduledTime >= fd && f.RefuelScheduledTime <= td && f.AirportId == a).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => f.RefuelScheduledTime >= fd && f.RefuelScheduledTime <= td && f.AirportId == a).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
            }
            else
            {
                ViewBag.Total = flights.Where(f =>
                DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                ).ToList().Select(f => f.RefuelItems.Sum(r => r.Amount)).DefaultIfEmpty().Sum();

                flights = flights.Where(f =>
                 DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );
                count = flights.Count();

                //if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                //{
                //    ViewBag.Total = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => f.RefuelScheduledTime.Value.Day == fd.Day && f.RefuelScheduledTime.Value.Month == fd.Month && f.RefuelScheduledTime.Value.Year == fd.Year).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
                //else
                //{
                //    td = td.AddHours(23);
                //    ViewBag.Total = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).ToList().Sum(f => f.TotalAmount);
                //    flights = flights.Where(f => (DateTime)f.RefuelScheduledTime >= fd && (DateTime)f.RefuelScheduledTime <= td).OrderByDescending(f => f.RefuelScheduledTime);
                //    count = flights.Count();
                //}
            }
            //}
            flights = flights.OrderBy(f => f.RefuelScheduledTime).Skip((p - 1) * pageSize).Take(pageSize);
            var list = flights.ToList();
            ViewBag.ItemCount = count;

            ViewBag.Trucks = db.Trucks.ToList();
            return View(list);
        }
        [FMSAuthorize]
        public ActionResult ExportCheckTrucks(string dr, int airport_id = -1)
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
        [FMSAuthorize]
        public ActionResult ExportAmountReport(string dr)
        {
            var name = "Baocaochuyenbaysanluong";
            string fileName = name + ".xlsx";

            var flights = db.Flights
               .Include(f => f.Airport)
               .Include(f => f.RefuelItems)
               //.Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
               .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (dr != null)
            {
                var range = dr.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
               >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
               && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
               <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
               ).OrderByDescending(f => f.RefuelScheduledTime);

            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.ToList().GroupBy(f => f.AirportId)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => Math.Round(Math.Round(r.Amount) * 3.7854M) * r.Density)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   Branch = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Branch : Branch.NONE
               });


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

                ws1.Column(1).Style.WrapText = true;
                ws1.Cells[1, 1, 1, 5].Merge = true;
                ws1.Cells[1, 1, 1, 5].Value = "CÔNG TY TNHH MỘT THÀNH VIÊN \r\n NHIÊN LIỆU HÀNG KHÔNG VIỆT NAM (SKYPEC) \r\n ĐƠN VỊ.........";
                ws1.Cells[1, 6, 1, 9].Merge = true;
                ws1.Cells[1, 6, 1, 9].Value = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\r\n Độc lập - Tự Do - Hạnh Phúc";
                ws1.Cells[2, 5].Value = "BÁO CÁO CHUYẾN BAY,SẢN LƯỢNG";
                ws1.Cells[3, 5].Value = "Từ ngày: " + fd.ToString("dd/MM/yyyy") + " - Đến ngày: " + td.ToString("dd/MM/yyyy");
                ws1.Cells[4, 3, 4, 5].Merge = true;
                ws1.Cells[4, 3, 4, 5].Value = "CHUYẾN BAY";
                ws1.Cells[4, 6, 4, 8].Merge = true;
                ws1.Cells[4, 6, 4, 8].Value = "SẢN LƯỢNG (KG)";
                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(2).Style.Font.Bold = true;
                ws1.Row(3).Style.Font.Bold = true;
                ws1.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Row(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;


                ws1.Column(1).Width = 10;
                ws1.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(5).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(8).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;


                var rowfix = 5;
                var colfix = 1;
                ws1.Row(rowfix).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[rowfix, colfix++].Value = "TT";
                ws1.Cells[rowfix, colfix++].Value = "ĐƠN VỊ";
                ws1.Cells[rowfix, colfix++].Value = "KH";
                ws1.Cells[rowfix, colfix++].Value = "TH";
                ws1.Cells[rowfix, colfix++].Value = "TH/KH";
                ws1.Cells[rowfix, colfix++].Value = "KH";
                ws1.Cells[rowfix, colfix++].Value = "TH";
                ws1.Cells[rowfix, colfix++].Value = "TH/KH";
                ws1.Cells[rowfix, colfix++].Value = "GHI CHÚ";


                int rowIndexBegin = 6;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                var branch = Branch.NONE;
                foreach (var item in temp.OrderBy(t => t.Branch))
                {
                    var col = 1;
                    if (item.Branch != branch)
                    {
                        branch = item.Branch;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 9].Style.Font.Bold = true;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 9].Merge = true;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 9].Value = (item.Branch == Branch.MB ? "CNKVMB" : item.Branch == Branch.MT ? "CNKVMT" : "CNKVMN");
                        rowIndexCurrent++;
                    }

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
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
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

                rowIndexCurrent++;
                ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Merge = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Value = "BM70.07/NLHK";

                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        [FMSAuthorize]
        public ActionResult ExportTotalAmountReport(string dr1, string dr2)
        {
            var name = "Baocaotonghopchuyenbaysanluong";
            string fileName = name + ".xlsx";

            var tf = db.Flights
                .Include(f => f.Airport)
                .Include(f => f.RefuelItems)
                //.Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            var flights = tf;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (dr1 != null)
            {
                var range = dr1.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
            );

            var fd1 = fd;
            var td1 = td;
            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            var temp = flights.ToList().GroupBy(f => f.AirportId)
               .Select(
               g => new AmountReport
               {
                   KH_Number = g.Count(),
                   TH_Number = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount = g.Sum(s => s.EstimateAmount),
                   TH_Amount = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => Math.Round(Math.Round(r.Amount) * 3.7854M) * r.Density)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
                   Branch = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Branch : Branch.NONE
               });

            var list = temp.ToList();
            flights = tf;

            fd = DateTime.Today.AddHours(0).AddMinutes(0);
            td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (dr2 != null)
            {
                var range = dr2.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            flights = flights.Where(f =>
            DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
            && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
            <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0));


            var fd2 = fd;
            var td2 = td;
            flights = flights.OrderByDescending(f => f.RefuelScheduledTime);
            temp = flights.GroupBy(f => f.Airport)
               .Select(
               g => new AmountReport
               {
                   KH_Number2 = g.Count(),
                   TH_Number2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count(),
                   KH_Amount2 = g.Sum(s => s.EstimateAmount),
                   TH_Amount2 = g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Count() > 0 ? g.Where(s => s.RefuelItems.Any(r => r.Status == REFUEL_ITEM_STATUS.DONE)).Sum(s => s.RefuelItems.Sum(r => Math.Round(Math.Round(r.Amount) * 3.7854M) * r.Density)) : 0,
                   AirportName = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Name : "",
                   AirportId = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Id : 0,
                   Branch = g.FirstOrDefault().Airport != null ? g.FirstOrDefault().Airport.Branch : Branch.NONE
               });

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
                    Branch = g.FirstOrDefault() != null ? g.FirstOrDefault().Branch : Branch.NONE
                }
                );

            var lists = tlist.OrderBy(t => t.Branch).ToList();

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
                ws1.Column(1).Width = 10;
                ws1.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Column(3).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(5).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(9).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(10).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Column(11).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Column(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(8).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(12).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(13).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws1.Column(14).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws1.Column(1).Style.WrapText = true;
                ws1.Cells[1, 1, 1, 8].Merge = true;
                ws1.Cells[1, 1, 1, 8].Value = "CÔNG TY TNHH MỘT THÀNH VIÊN \r\n NHIÊN LIỆU HÀNG KHÔNG VIỆT NAM (SKYPEC) \r\n ĐƠN VỊ.........";
                ws1.Cells[1, 9, 1, 19].Merge = true;
                ws1.Cells[1, 9, 1, 19].Value = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\r\n Độc lập - Tự Do - Hạnh Phúc";
                ws1.Cells[2, 1, 2, 19].Merge = true;
                ws1.Cells[2, 1, 2, 19].Value = "BÁO CÁO TỔNG HỢP CHUYẾN BAY,SẢN LƯỢNG";

                ws1.Cells[3, 3, 3, 8].Merge = true;
                ws1.Cells[3, 3, 3, 8].Value = "Từ ngày: " + fd1.ToString("dd/MM/yyyy") + " - Đến ngày: " + td1.ToString("dd/MM/yyyy");
                ws1.Cells[3, 3, 3, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[3, 9, 3, 14].Merge = true;
                ws1.Cells[3, 9, 3, 14].Value = "Từ ngày: " + fd2.ToString("dd/MM/yyyy") + " - Đến ngày: " + td2.ToString("dd/MM/yyyy");
                ws1.Cells[3, 9, 3, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[3, 15, 3, 16].Merge = true;
                ws1.Cells[3, 15, 3, 16].Value = "So sánh.../...thực hiện";
                ws1.Cells[3, 15, 3, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[3, 17, 3, 19].Merge = true;
                ws1.Cells[3, 17, 3, 19].Value = "Sản lượng TB/ngày";
                ws1.Cells[3, 17, 3, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[4, 3, 4, 5].Merge = true;
                ws1.Cells[4, 3, 4, 5].Value = "CHUYẾN BAY";
                ws1.Cells[4, 6, 4, 8].Merge = true;
                ws1.Cells[4, 6, 4, 8].Value = "SẢN LƯỢNG (KG)";

                ws1.Cells[4, 9, 4, 11].Merge = true;
                ws1.Cells[4, 9, 4, 11].Value = "CHUYẾN BAY";
                ws1.Cells[4, 12, 4, 14].Merge = true;
                ws1.Cells[4, 12, 4, 14].Value = "SẢN LƯỢNG (KG)";


                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(2).Style.Font.Bold = true;
                ws1.Row(3).Style.Font.Bold = true;
                ws1.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws1.Row(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var rowfix = 5;
                var colfix = 1;

                ws1.Cells[rowfix, colfix++].Value = "TT";
                ws1.Cells[rowfix, colfix++].Value = "ĐƠN VỊ";
                ws1.Cells[rowfix, colfix++].Value = "KH";
                ws1.Cells[rowfix, colfix++].Value = "TH";
                ws1.Cells[rowfix, colfix++].Value = "TH/KH";
                ws1.Cells[rowfix, colfix++].Value = "KH";
                ws1.Cells[rowfix, colfix++].Value = "TH";
                ws1.Cells[rowfix, colfix++].Value = "TH/KH";
                ws1.Cells[rowfix, colfix++].Value = "KH";
                ws1.Cells[rowfix, colfix++].Value = "TH";
                ws1.Cells[rowfix, colfix++].Value = "TH/KH";
                ws1.Cells[rowfix, colfix++].Value = "KH";
                ws1.Cells[rowfix, colfix++].Value = "TH";
                ws1.Cells[rowfix, colfix++].Value = "TH/KH";
                ws1.Cells[rowfix, colfix++].Value = "C.BAY";
                ws1.Cells[rowfix, colfix++].Value = "S.LƯỢNG";
                ws1.Cells[rowfix, colfix++].Value = "TUẦN...";
                ws1.Cells[rowfix, colfix++].Value = "TUẦN...";
                ws1.Cells[rowfix, colfix++].Value = ".../...";

                int rowIndexBegin = 6;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                var branch = Branch.NONE;
                foreach (var item in lists.OrderBy(t => t.Branch))
                {
                    var col = 1;
                    if (item.Branch != branch)
                    {
                        branch = item.Branch;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 19].Style.Font.Bold = true;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 19].Merge = true;
                        ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 19].Value = (item.Branch == Branch.MB ? "CNKVMB" : item.Branch == Branch.MT ? "CNKVMT" : "CNKVMN");
                        rowIndexCurrent++;
                    }
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

                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
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

                rowIndexCurrent++;
                ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 14].Merge = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 14].Value = "BM70.08/NLHK";

                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public IQueryable<Flight> BM2501Query(string daterange, int airportId = -1)
        {
            var flights = db.Flights
                .Include(f => f.Airport)
                .Include(f => f.RefuelItems)
                .Include(f => f.RefuelItems.Select(r => r.Truck))
                .Include(f => f.RefuelItems.Select(r => r.Driver))
                .Include(f => f.RefuelItems.Select(r => r.Operator))
                .Include(f => f.RefuelItems.Select(r => r.User))
                .Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
                .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                ).OrderByDescending(f => f.RefuelScheduledTime);

            if (airportId > 0)
                flights = flights.Where(f => f.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(f => ids.Contains((int)f.AirportId));
            }
            return flights;
        }
        [FMSAuthorize]
        public ActionResult BM2501(int p = 1)
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            int pageSize = 20;

            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var flights = BM2501Query(Request["daterange"], airportId);

            var arports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
            }
            ViewBag.Airports = arports;
            ViewBag.ItemCount = flights.Count();
            flights = flights.Skip((p - 1) * pageSize).Take(pageSize);
            var list = flights.ToList();
            return View(list);
        }
        [FMSAuthorize]
        public ActionResult ExportBM2501(string dr, int airportId = -1)
        {
            var name = "BM2501";
            string fileName = name + ".xlsx";

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (dr != null)
            {
                var range = dr.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            var temp = BM2501Query(dr, airportId);
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
                    rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    //rng.AutoFitColumns();
                    rng.Style.WrapText = true;
                    //rng.Style.Border.BorderAround(ExcelBorderStyle.Medium, System.Drawing.Color.Black);
                }



                ws1.Cells[1, 1, 1, 15].Merge = true;
                ws1.Cells[1, 1, 1, 15].Value = "CÔNG TY TNHH MTV NHIÊN LIỆU HÀNG KHÔNG VIỆT NAM (SKYPEC)";

                ws1.Cells[2, 2, 2, 15].Merge = true;
                ws1.Cells[2, 2, 2, 15].Value = "ĐƠN VỊ:........................";

                ws1.Cells[3, 1, 3, 15].Merge = true;
                ws1.Cells[3, 1, 3, 15].Value = "KẾ HOẠCH TRA NẠP NHIÊN LIỆU CHO TÀU BAY";
                ws1.Cells[4, 1, 4, 15].Merge = true;
                ws1.Cells[4, 1, 4, 15].Value = "Từ ngày: " + fd.ToString("dd/MM/yyyy HH:mm") + " - Đến ngày: " + td.ToString("dd/MM/yyyy HH:mm");

                ws1.Cells[5, 2, 5, 5].Merge = true;
                ws1.Cells[5, 2, 5, 5].Value = "THÔNG TIN TÀU BAY";

                ws1.Cells[5, 7, 5, 9].Merge = true;
                ws1.Cells[5, 7, 5, 9].Value = "THỜI GIAN DỰ KIẾN";

                ws1.Cells[5, 12, 5, 14].Merge = true;
                ws1.Cells[5, 12, 5, 14].Value = "NGƯỜI THỰC HIỆN";

                ws1.Row(2).Style.Font.Bold = true;
                ws1.Row(3).Style.Font.Bold = true;
                ws1.Row(4).Style.Font.Bold = true;
                ws1.Row(6).Style.Font.Bold = true;

                ws1.Column(1).Width = 10;
                ws1.Column(2).Width = ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = 16;
                ws1.Column(11).Width = ws1.Column(12).Width = ws1.Column(13).Width = ws1.Column(14).Width = 35;
                var rowfix = 6;
                var colfix = 1;
                ws1.Row(rowfix + 2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws1.Cells[rowfix, colfix++].Value = "Số TT \r\n chuyến bay";
                ws1.Cells[rowfix, colfix++].Value = "Loại \r\n tàu bay";
                ws1.Cells[rowfix, colfix++].Value = "Số hiệu \r\n tàu bay";
                ws1.Cells[rowfix, colfix++].Value = "Số hiệu \r\n chuyến bay";
                ws1.Cells[rowfix, colfix++].Value = "Đường bay";
                ws1.Cells[rowfix, colfix++].Value = "Số lượng \r\n dự kiến(kg)";
                ws1.Cells[rowfix, colfix++].Value = "Hạ cánh";
                ws1.Cells[rowfix, colfix++].Value = "Cất cánh";
                ws1.Cells[rowfix, colfix++].Value = "Thời gian \r\n bắt đầu \r\n tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Vị trí \r\n bãi đậu";
                ws1.Cells[rowfix, colfix++].Value = "Số hiệu \r\n xe tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Lái xe";
                ws1.Cells[rowfix, colfix++].Value = "Nhân viên \r\n tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Trực \r\n chỉ huy";
                ws1.Cells[rowfix, colfix++].Value = "Ghi chú";


                int rowIndexBegin = 7;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                var date = DateTime.MinValue;

                var db = new DataContext();
                db.DisableFilter("IsNotDeleted");
                var db_Trucks = db.Trucks.Where(t => t.IsDeleted).ToList();
                var db_Users = db.Users.Where(t => t.IsDeleted).ToList();
                db.EnableFilter("IsNotDeleted");


                foreach (var item in temp)
                {
                    var refuelItems = item.RefuelItems.Where(r => r.RefuelItemType == REFUEL_ITEM_TYPE.REFUEL);
                    foreach (var ri in refuelItems)
                    {
                        var col = 1;
                        if (item.RefuelScheduledTime.Value.Date != date)
                        {
                            date = item.RefuelScheduledTime.Value.Date;
                            ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Style.Font.Bold = true;
                            ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Merge = true;
                            ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Value = item.RefuelScheduledTime.Value.ToString("dd/MM/yyyy");
                            rowIndexCurrent++;
                        }
                        ws1.Cells[rowIndexCurrent, col++].Value = index++;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftType;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftCode;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Code;
                        ws1.Cells[rowIndexCurrent, col++].Value = item.RouteName;
                        ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.EstimateAmount).ToString("#,##0");
                        ws1.Cells[rowIndexCurrent, col++].Value = item.ArrivalScheduledTime != null && item.ArrivalScheduledTime.Value.Year != DateTime.MaxValue.Year ? Convert.ToDateTime(item.ArrivalScheduledTime).ToString("HH:mm") : "---";
                        ws1.Cells[rowIndexCurrent, col++].Value = Convert.ToDateTime(item.DepartureScheduledTime).ToString("HH:mm");
                        ws1.Cells[rowIndexCurrent, col++].Value = Convert.ToDateTime(item.RefuelScheduledTime).ToString("HH:mm");
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Parking;

                        if (ri.Truck != null)
                            ws1.Cells[rowIndexCurrent, col++].Value = ri.Truck.Code;
                        else
                        {
                            var truck_d = db_Trucks.FirstOrDefault(t => t.Id == ri.TruckId);
                            if (truck_d != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = truck_d.Code;
                            else
                                ws1.Cells[rowIndexCurrent, col++].Value = "---";
                        }

                        if (ri.Driver != null)
                            ws1.Cells[rowIndexCurrent, col++].Value = ri.Driver.FullName;
                        else
                        {
                            var user_d = db_Users.FirstOrDefault(u => u.Id == ri.DriverId);
                            if (user_d != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                            else
                                ws1.Cells[rowIndexCurrent, col++].Value = "---";
                        }

                        if (ri.Operator != null)
                            ws1.Cells[rowIndexCurrent, col++].Value = ri.Operator.FullName;
                        else
                        {
                            var user_d = db_Users.FirstOrDefault(u => u.Id == ri.OperatorId);
                            if (user_d != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                            else
                                ws1.Cells[rowIndexCurrent, col++].Value = "---";
                        }

                        if (ri.User != null)
                            ws1.Cells[rowIndexCurrent, col++].Value = ri.User.FullName;
                        else
                        {
                            var user_d = db_Users.FirstOrDefault(u => u.Id == ri.UserId);
                            if (user_d != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                            else
                                ws1.Cells[rowIndexCurrent, col++].Value = "---";
                        }
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Note;

                        rowIndexCurrent++;
                    }
                }

                ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Merge = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Value = "BM25.01/NLHK";

                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Top.Color.SetColor(Color.Black);
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Left.Color.SetColor(Color.Black);
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Right.Color.SetColor(Color.Black);
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Bottom.Color.SetColor(Color.Black);
                //ws1.Cells[6, 1, rowIndexCurrent, 15].AutoFitColumns();

                package.SaveAs(newFile);
            }
            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult CreateBM2501(string url, int airportId = -1)
        {
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            var uri = new Uri("http://fms.skypec.com.vn" + url);
            var query = uri.Query;
            if (!string.IsNullOrEmpty(query))
            {
                var daterange = HttpUtility.ParseQueryString(query).Get("daterange");
                if (!string.IsNullOrEmpty(daterange))
                {
                    var range = daterange.Split('-');
                    fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    if (range.Length > 1)
                        td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var report = new Report();
            report.ReportType = REPORT_TYPE.BM2501;
            report.Url = url;
            if (user != null)
                report.UserCreatedId = user.Id;
            report.FromTime = fd;
            report.ToTime = td;

            if (airportId > 0)
                report.AirportId = airportId;
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var u_airportIds = user.Airports.Select(a => a.Id).ToArray();
                if (u_airportIds.Count() == 1)
                    report.AirportId = u_airportIds[0];
                //report.AirportIds = string.Join(",", u_airportIds);
            }

            db.Reports.Add(report);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã lưu báo cáo này" });
        }
        [FMSAuthorize]
        public ActionResult BM2501List(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var daterange = Request["daterange"];
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var reports = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Where(r => r.ReportType == REPORT_TYPE.BM2501)
                .AsNoTracking() as IQueryable<Report>;

            reports = reports.Where(f => DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );

            var arports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                reports = reports.Where(a => ids.Contains((int)a.AirportId));
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
            }

            if (airportId > 0)
                reports = reports.Where(r => r.AirportId == airportId);

            ViewBag.Airports = arports;
            ViewBag.ItemCount = reports.Count();
            reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            var list = reports.ToList();
            return View(list);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult ActiveBM2501(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);
            report.IsActive = true;
            if (user != null)
                report.UserActivedId = user.Id;
            report.DateActived = DateTime.Now;

            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã duyệt" });
        }
        [FMSAuthorize]
        public ActionResult BM2501Detail(int id, int p = 1)
        {
            var airportId = -1;
            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                if (report.AirportId != null)
                    airportId = (int)report.AirportId;

                int pageSize = 20;
                if (Request["pageSize"] != null)
                    pageSize = Convert.ToInt32(Request["pageSize"]);
                if (Request["pageIndex"] != null)
                    p = Convert.ToInt32(Request["pageIndex"]);

                ViewBag.Id = id;
                ViewBag.CreatedName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                ViewBag.FromTime = report.FromTime;
                ViewBag.ToTime = report.ToTime;
                ViewBag.DateCreated = report.DateCreated;
                ViewBag.ActivedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var flights = BM2501Query(dr, airportId);

                ViewBag.ItemCount = flights.Count();
                flights = flights.Skip((p - 1) * pageSize).Take(pageSize);
                list = flights.ToList();
            }
            return View(list);
        }
        [FMSAuthorize]
        public ActionResult ExportBM2501Detail(int id)
        {
            int airportId = -1;
            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                if (report.AirportId != null)
                    airportId = (int)report.AirportId;

                var createdName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                var fromTime = report.FromTime;
                var toTime = report.ToTime;
                var dateCreated = report.DateCreated;
                var activedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                var name = "BM2501";
                string fileName = name + ".xlsx";

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var temp = BM2501Query(dr, airportId);

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
                        rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                        rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        //rng.AutoFitColumns();
                        rng.Style.WrapText = true;
                    }

                    ws1.Cells[1, 1, 1, 15].Merge = true;
                    ws1.Cells[1, 1, 1, 15].Value = "Người tạo: " + createdName;
                    ws1.Cells[2, 1, 2, 15].Merge = true;
                    ws1.Cells[2, 1, 2, 15].Value = "Người duyệt: " + activedName;
                    //"Người tạo: " + createdName + "\r\n" + "Người duyệt: " + activedName;
                    //ws1.Cells[1, 6, 1, 9].Merge = true;
                    //ws1.Cells[1, 6, 1, 9].Value = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\r\n Độc lập - Tự Do - Hạnh Phúc";
                    ws1.Cells[3, 1, 3, 15].Merge = true;
                    ws1.Cells[3, 1, 3, 15].Value = "KẾ HOẠCH TRA NẠP NHIÊN LIỆU CHO TÀU BAY";
                    ws1.Cells[4, 1, 4, 15].Merge = true;
                    ws1.Cells[4, 1, 4, 15].Value = "Từ ngày: " + fd.ToString("dd/MM/yyyy HH:mm") + " - Đến ngày: " + td.ToString("dd/MM/yyyy HH:mm");

                    ws1.Cells[5, 2, 5, 5].Merge = true;
                    ws1.Cells[5, 2, 5, 5].Value = "THÔNG TIN TÀU BAY";

                    ws1.Cells[5, 7, 5, 9].Merge = true;
                    ws1.Cells[5, 7, 5, 9].Value = "THỜI GIAN DỰ KIẾN";

                    ws1.Cells[5, 12, 5, 14].Merge = true;
                    ws1.Cells[5, 12, 5, 14].Value = "NGƯỜI THỰC HIỆN";

                    ws1.Row(3).Style.Font.Bold = true;
                    ws1.Row(4).Style.Font.Bold = true;
                    ws1.Row(6).Style.Font.Bold = true;

                    ws1.Column(1).Width = 10;
                    ws1.Column(2).Width = ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = 16;
                    ws1.Column(11).Width = ws1.Column(12).Width = ws1.Column(13).Width = ws1.Column(14).Width = 35;
                    var rowfix = 6;
                    var colfix = 1;

                    ws1.Cells[rowfix, colfix++].Value = "Số TT \r\n chuyến bay";
                    ws1.Cells[rowfix, colfix++].Value = "Loại \r\n tàu bay";
                    ws1.Cells[rowfix, colfix++].Value = "Số hiệu \r\n tàu bay";
                    ws1.Cells[rowfix, colfix++].Value = "Số hiệu \r\n chuyến bay";
                    ws1.Cells[rowfix, colfix++].Value = "Đường bay";
                    ws1.Cells[rowfix, colfix++].Value = "Số lượng \r\n dự kiến(kg)";
                    ws1.Cells[rowfix, colfix++].Value = "Hạ cánh";
                    ws1.Cells[rowfix, colfix++].Value = "Cất cánh";
                    ws1.Cells[rowfix, colfix++].Value = "Thời gian \r\n bắt đầu \r\n tra nạp";
                    ws1.Cells[rowfix, colfix++].Value = "Vị trí \r\n bãi đậu";
                    ws1.Cells[rowfix, colfix++].Value = "Số hiệu \r\n xe tra nạp";
                    ws1.Cells[rowfix, colfix++].Value = "Lái xe";
                    ws1.Cells[rowfix, colfix++].Value = "Nhân viên \r\n tra nạp";
                    ws1.Cells[rowfix, colfix++].Value = "Trực \r\n chỉ huy";
                    ws1.Cells[rowfix, colfix++].Value = "Ghi chú";


                    int rowIndexBegin = 7;
                    int rowIndexCurrent = rowIndexBegin;
                    var index = 1;
                    var date = DateTime.MinValue;

                    var db = new DataContext();
                    db.DisableFilter("IsNotDeleted");
                    var db_Trucks = db.Trucks.Where(t => t.IsDeleted).ToList();
                    var db_Users = db.Users.Where(t => t.IsDeleted).ToList();
                    db.EnableFilter("IsNotDeleted");

                    foreach (var item in temp)
                    {
                        foreach (var ri in item.RefuelItems)
                        {
                            var col = 1;

                            if (item.RefuelScheduledTime.Value.Date != date)
                            {
                                date = item.RefuelScheduledTime.Value.Date;
                                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Style.Font.Bold = true;
                                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Merge = true;
                                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Value = item.RefuelScheduledTime.Value.ToString("dd/MM/yyyy");
                                rowIndexCurrent++;
                            }

                            ws1.Cells[rowIndexCurrent, col++].Value = index++;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftType;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.AircraftCode;
                            ws1.Cells[rowIndexCurrent, col++].Value = item.Code;

                            ws1.Cells[rowIndexCurrent, col++].Value = item.RouteName;
                            ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.EstimateAmount).ToString("#,##0");
                            ws1.Cells[rowIndexCurrent, col++].Value = item.ArrivalScheduledTime != null && item.ArrivalScheduledTime.Value.Year != DateTime.MaxValue.Year ? Convert.ToDateTime(item.ArrivalScheduledTime).ToString("HH:mm") : "---";
                            ws1.Cells[rowIndexCurrent, col++].Value = Convert.ToDateTime(item.DepartureScheduledTime).ToString("HH:mm");
                            ws1.Cells[rowIndexCurrent, col++].Value = Convert.ToDateTime(item.RefuelScheduledTime).ToString("HH:mm");
                            ws1.Cells[rowIndexCurrent, col++].Value = item.Parking;

                            if (ri.Truck != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = ri.Truck.Code;
                            else
                            {
                                var truck_d = db_Trucks.FirstOrDefault(t => t.Id == ri.TruckId);
                                if (truck_d != null)
                                    ws1.Cells[rowIndexCurrent, col++].Value = truck_d.Code;
                                else
                                    ws1.Cells[rowIndexCurrent, col++].Value = "---";
                            }


                            if (ri.Driver != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = ri.Driver.FullName;
                            else
                            {
                                var user_d = db_Users.FirstOrDefault(u => u.Id == ri.DriverId);
                                if (user_d != null)
                                    ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                                else
                                    ws1.Cells[rowIndexCurrent, col++].Value = "---";
                            }

                            if (ri.Operator != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = ri.Operator.FullName;
                            else
                            {
                                var user_d = db_Users.FirstOrDefault(u => u.Id == ri.OperatorId);
                                if (user_d != null)
                                    ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                                else
                                    ws1.Cells[rowIndexCurrent, col++].Value = "---";
                            }

                            if (ri.User != null)
                                ws1.Cells[rowIndexCurrent, col++].Value = ri.User.FullName;
                            else
                            {
                                var user_d = db_Users.FirstOrDefault(u => u.Id == ri.UserId);
                                if (user_d != null)
                                    ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                                else
                                    ws1.Cells[rowIndexCurrent, col++].Value = "---";
                            }
                            ws1.Cells[rowIndexCurrent, col++].Value = item.Note;
                            rowIndexCurrent++;
                        }
                    }

                    ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 15].Value = "BM25.01";

                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Top.Color.SetColor(Color.Black);
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Left.Color.SetColor(Color.Black);
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Right.Color.SetColor(Color.Black);
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[5, 1, rowIndexCurrent, 15].Style.Border.Bottom.Color.SetColor(Color.Black);
                    //ws1.Cells[6, 1, rowIndexCurrent, 15].AutoFitColumns();
                    package.SaveAs(newFile);
                }


                var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
                if (!System.IO.File.Exists(readFile))
                    return HttpNotFound();
                return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            return View();
        }
        //BM7009
        [FMSAuthorize]
        public ActionResult BM7009()
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var airports = db.Airports.ToList();


            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id)).ToList();
            }

            if (airportId > 0)
                ViewBag.Branch = db.Airports.FirstOrDefault(a => a.Id == airportId).Branch;
            else if (user != null && user.Airports.Count() == 1)
                ViewBag.Branch = user.Airports.FirstOrDefault().Branch;

            ViewBag.Airports = airports;
            return View(BM7009Query(Request["daterange"], airportId));
        }
        public BM7009 BM7009Query(string daterange, int airportId = -1, int reportId = -1)
        {
            var flights = db.Flights
               .Include(f => f.RefuelItems)
               .Include(f => f.Airport)
               .Include(f => f.Airline)
               .Include(f => f.Properties)
               .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );

            if (airportId > 0)
                flights = flights.Where(f => f.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(f => ids.Contains((int)f.AirportId));
            }

            var report = new Report();
            if (reportId > 0)
                report = db.Reports
                    .FirstOrDefault(r => r.Id == reportId);

            var g = flights.ToList();
            var temp = new BM7009
            {
                CNKVMB1 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "1" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT1 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "1" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN1 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "1" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB2 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT2 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN2 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB21 = g.Where(s => s.Airline != null && s.Airline.Code == "VN" && s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT21 = g.Where(s => s.Airline != null && s.Airline.Code == "VN" && s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN21 = g.Where(s => s.Airline != null && s.Airline.Code == "VN" && s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB22 = g.Where(s => s.Airline != null && s.Airline.Code == "VJ" && s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT22 = g.Where(s => s.Airline != null && s.Airline.Code == "VJ" && s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN22 = g.Where(s => s.Airline != null && s.Airline.Code == "VJ" && s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB23 = g.Where(s => s.Airline != null && s.Airline.Code == "BL" && s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT23 = g.Where(s => s.Airline != null && s.Airline.Code == "BL" && s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN23 = g.Where(s => s.Airline != null && s.Airline.Code == "BL" && s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB24 = g.Where(s => (s.Airline == null || (s.Airline != null && s.Airline.Code != "BL" && s.Airline.Code != "VJ" && s.Airline.Code != "VN")) && s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT24 = g.Where(s => (s.Airline == null || (s.Airline != null && s.Airline.Code != "BL" && s.Airline.Code != "VJ" && s.Airline.Code != "VN")) && s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN24 = g.Where(s => (s.Airline == null || (s.Airline != null && s.Airline.Code != "BL" && s.Airline.Code != "VJ" && s.Airline.Code != "VN")) && s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "2" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB3 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Status == FlightStatus.CANCELED).Count(),
                CNKVMT3 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Status == FlightStatus.CANCELED).Count(),
                CNKVMN3 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Status == FlightStatus.CANCELED).Count(),

                CNKVMB4 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Status == FlightStatus.NOTREFUEL).Count(),
                CNKVMT4 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Status == FlightStatus.NOTREFUEL).Count(),
                CNKVMN4 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Status == FlightStatus.NOTREFUEL).Count(),

                CNKVMB5 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "5")).Count(),
                CNKVMT5 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "5")).Count(),
                CNKVMN5 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "5")).Count(),

                CNKVMB6 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.FlightCarry == FlightCarry.CCO).Count(),
                CNKVMT6 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.FlightCarry == FlightCarry.CCO).Count(),
                CNKVMN6 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.FlightCarry == FlightCarry.CCO).Count(),

                CNKVMB7 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "7" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT7 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "7" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN7 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "7" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB8 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "8" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT8 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "8" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN8 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "8" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB9 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "9" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT9 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "9" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN9 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "9" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB10 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.Properties.Any(p => p.Code == "10" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMT10 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.Properties.Any(p => p.Code == "10" && p.ReportType == REPORT_TYPE.BM7009)).Count(),
                CNKVMN10 = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.Properties.Any(p => p.Code == "10" && p.ReportType == REPORT_TYPE.BM7009)).Count(),

                CNKVMB_CB = (report != null && !string.IsNullOrEmpty(report.Value1)) ? Convert.ToInt32(report.Value1) : 0,
                CNKVMB_L = (report != null && !string.IsNullOrEmpty(report.Value2)) ? Convert.ToDecimal(report.Value2) : 0,

                CNKVMT_CB = (report != null && !string.IsNullOrEmpty(report.Value3)) ? Convert.ToInt32(report.Value3) : 0,
                CNKVMT_L = (report != null && !string.IsNullOrEmpty(report.Value4)) ? Convert.ToDecimal(report.Value4) : 0,

                CNKVMN_CB = (report != null && !string.IsNullOrEmpty(report.Value5)) ? Convert.ToInt32(report.Value5) : 0,
                CNKVMN_L = (report != null && !string.IsNullOrEmpty(report.Value6)) ? Convert.ToDecimal(report.Value6) : 0,

                //CNKVMB_CB = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.RefuelItems.Any(p => p.Techlog > 0)).Count(),
                //CNKVMB_L = (decimal)g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MB && s.RefuelItems.Any(p => p.Techlog > 0)).Sum(s => s.RefuelItems.Sum(r => r.Techlog)),

                //CNKVMT_CB = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.RefuelItems.Any(p => p.Techlog > 0)).Count(),
                //CNKVMT_L = (decimal)g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MT && s.RefuelItems.Any(p => p.Techlog > 0)).Sum(s => s.RefuelItems.Sum(r => r.Techlog)),

                //CNKVMN_CB = g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.RefuelItems.Any(p => p.Techlog > 0)).Count(),
                //CNKVMN_L = (decimal)g.Where(s => s.Airport != null && s.Airport.Branch == Branch.MN && s.RefuelItems.Any(p => p.Techlog > 0)).Sum(s => s.RefuelItems.Sum(r => r.Techlog)),
            };
            return temp;
        }
        [FMSAuthorize]
        public ActionResult BM7009List(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var daterange = Request["daterange"];
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var reports = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Where(r => r.ReportType == REPORT_TYPE.BM7009)
                .AsNoTracking() as IQueryable<Report>;

            reports = reports.Where(f => DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );


            var arports = db.Airports.ToList();
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                reports = reports.Where(a => ids.Contains((int)a.AirportId));
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
            }

            if (airportId > 0)
                reports = reports.Where(r => r.AirportId == airportId);

            ViewBag.Airports = arports;
            ViewBag.ItemCount = reports.Count();
            reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            var list = reports.ToList();
            return View(list);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult CreateBM7009([Bind(Include = "Value1,Value2,Value3,Value4,Value5,Value6")] Report report, string url, int airportId = -1)
        {
            if (Request.IsAjaxRequest() && ModelState.IsValid)
            {

                var fd = DateTime.Today.AddHours(0).AddMinutes(0);
                var td = DateTime.Today.AddHours(23).AddMinutes(59);

                var uri = new Uri("http://fms.skypec.com.vn" + url);
                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var daterange = HttpUtility.ParseQueryString(query).Get("daterange");
                    if (!string.IsNullOrEmpty(daterange))
                    {
                        var range = daterange.Split('-');
                        fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                        if (range.Length > 1)
                            td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    }
                }

                report.ReportType = REPORT_TYPE.BM7009;
                report.Url = url;
                if (user != null)
                    report.UserCreatedId = user.Id;
                report.FromTime = fd;
                report.ToTime = td;

                if (airportId > 0)
                    report.AirportId = airportId;
                else if (!User.IsInRole("Super Admin") && user != null)
                {
                    var u_airportIds = user.Airports.Select(a => a.Id).ToArray();
                    if (u_airportIds.Count() == 1)
                        report.AirportId = u_airportIds[0];
                    //report.AirportIds = string.Join(",", u_airportIds);
                }

                db.Reports.Add(report);
                db.SaveChanges();
                return Json(new { result = "OK" });
            }
            return View(report);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult ActiveBM7009(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);
            report.IsActive = true;
            if (user != null)
                report.UserActivedId = user.Id;
            report.DateActived = DateTime.Now;

            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã duyệt" });
        }
        [FMSAuthorize]
        public ActionResult BM7009Detail(int id, int p = 1)
        {
            var airportId = -1;

            var bm7009 = new BM7009();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                if (report.AirportId != null)
                    airportId = (int)report.AirportId;

                int pageSize = 20;
                if (Request["pageSize"] != null)
                    pageSize = Convert.ToInt32(Request["pageSize"]);
                if (Request["pageIndex"] != null)
                    p = Convert.ToInt32(Request["pageIndex"]);

                ViewBag.Id = id;
                ViewBag.CreatedName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                ViewBag.FromTime = report.FromTime;
                ViewBag.ToTime = report.ToTime;
                ViewBag.DateCreated = report.DateCreated;
                ViewBag.ActivedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                bm7009 = BM7009Query(dr, airportId, report.Id);
            }
            return View(bm7009);
        }
        [FMSAuthorize]
        public ActionResult ExportBM7009Detail(int id)
        {
            int airportId = -1;
            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                airportId = report.AirportId != null ? (int)report.AirportId : -1;
                var createdName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                var fromTime = report.FromTime;
                var toTime = report.ToTime;
                var dateCreated = report.DateCreated;
                var activedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                var name = "BM7009";
                string fileName = name + ".xlsx";

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var temp = BM7009Query(dr, airportId, report.Id);

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
                        rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                        rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        //rng.AutoFitColumns();
                        rng.Style.WrapText = true;
                    }

                    ws1.Cells[1, 1, 1, 9].Merge = true;
                    ws1.Cells[1, 1, 1, 9].Value = "Người tạo: " + createdName;
                    ws1.Cells[2, 1, 2, 9].Merge = true;
                    ws1.Cells[2, 1, 2, 9].Value = "Người duyệt: " + activedName;

                    ws1.Cells[3, 1, 3, 9].Merge = true;
                    ws1.Cells[3, 1, 3, 9].Value = "BÁO CÁO TÌNH HÌNH KHAI THÁC TRÊN SÂN ĐỖ";

                    ws1.Cells[4, 2].Value = "Tuần";
                    ws1.Cells[4, 3, 4, 8].Merge = true;
                    ws1.Cells[4, 3, 4, 8].Value = "Tuần... từ " + fd.ToString("dd/MM/yyyy HH:mm") + " đến " + td.ToString("dd/MM/yyyy HH:mm");

                    ws1.Row(3).Style.Font.Bold = true;
                    ws1.Row(4).Style.Font.Bold = true;
                    ws1.Row(5).Style.Font.Bold = true;
                    var rowfix = 5;

                    ws1.Cells[rowfix, 1].Value = "TT";
                    ws1.Cells[rowfix, 2].Value = "Đơn vị";

                    ws1.Cells[rowfix, 3, rowfix, 4].Merge = true;
                    ws1.Cells[rowfix, 3, rowfix, 4].Value = "CNKVMB";

                    ws1.Cells[rowfix, 5, rowfix, 6].Merge = true;
                    ws1.Cells[rowfix, 5, rowfix, 6].Value = "CNKVMT";

                    ws1.Cells[rowfix, 7, rowfix, 8].Merge = true;
                    ws1.Cells[rowfix, 7, rowfix, 8].Value = "CNKVMN";

                    ws1.Cells[rowfix, 9].Value = "Ghi chú";

                    int rowIndexBegin = 6;
                    int rowIndexCurrent = rowIndexBegin;

                    if (temp != null)
                    {

                        ws1.Cells[rowIndexCurrent, 1].Value = "1";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay chậm do SKYPEC";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB1;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT1;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN1;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "2";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay chậm so với kế hoạch";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB2;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT2;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN2;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "2.1";
                        ws1.Cells[rowIndexCurrent, 2].Value = "VNA";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB21;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT21;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN21;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "2.2";
                        ws1.Cells[rowIndexCurrent, 2].Value = "VJ";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB22;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT22;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN22;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "2.3";
                        ws1.Cells[rowIndexCurrent, 2].Value = "BL";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB23;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT23;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN23;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "2.4";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Các hãng hàng không khác";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB24;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT24;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN24;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "3";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Hủy chuyến bay";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB3;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT3;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN3;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "4";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay không lấy dầu";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB4;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT4;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN4;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "5";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay ngoài kế hoạch ngày";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB5;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT5;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN5;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "6";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyên cơ";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB6;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT6;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN6;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "7";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Sự cố / vụ việc trong tra nạp";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB7;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT7;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN7;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "8";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Sự cố / vụ việc bị lập biên bản";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB8;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT8;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN8;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "9";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Vi phạm việc tuân thủ quy trình,quy định bị phản hồi";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB9;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT9;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN9;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "10";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Thông tin phản hồi từ khách hàng";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB10;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT10;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN10;
                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "11";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Hệ số sẵn sàng xe tra nạp (%)";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = temp.CNKVMB11;

                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Merge = true;
                        ws1.Cells[rowIndexCurrent, 5, rowIndexCurrent, 6].Value = temp.CNKVMT11;

                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 7, rowIndexCurrent, 8].Value = temp.CNKVMN11;

                        ws1.Cells[rowIndexCurrent, 9].Value = "";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "12";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chênh lệch đồng hồ tàu bay và xe tra nạp";
                        ws1.Cells[rowIndexCurrent, 3].Value = "Chuyến bay \r\n" + temp.CNKVMB_CB;
                        ws1.Cells[rowIndexCurrent, 4].Value = "Sản lượng (kg) \r\n" + temp.CNKVMB_L.ToString("#,##0");
                        ws1.Cells[rowIndexCurrent, 5].Value = "Chuyến bay \r\n" + temp.CNKVMT_CB;
                        ws1.Cells[rowIndexCurrent, 6].Value = "Sản lượng (kg) \r\n" + temp.CNKVMT_L.ToString("#,##0");
                        ws1.Cells[rowIndexCurrent, 7].Value = "Chuyến bay \r\n" + temp.CNKVMN_CB;
                        ws1.Cells[rowIndexCurrent, 8].Value = "Sản lượng (kg) \r\n" + temp.CNKVMN_L.ToString("#,##0");
                        ws1.Cells[rowIndexCurrent, 9].Value = "";

                    }

                    rowIndexCurrent++;
                    ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 9].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 9].Value = "BM70.09/NLHK";

                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Top.Color.SetColor(Color.Black);
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Left.Color.SetColor(Color.Black);
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Right.Color.SetColor(Color.Black);
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 9].Style.Border.Bottom.Color.SetColor(Color.Black);

                    ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = ws1.Column(6).Width = ws1.Column(7).Width = ws1.Column(8).Width = 20;
                    ws1.Column(2).Width = 50;
                    ws1.Column(2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Row(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws1.Row(5).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    package.SaveAs(newFile);
                }


                var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
                if (!System.IO.File.Exists(readFile))
                    return HttpNotFound();
                return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            return View();
        }
        [FMSAuthorize]
        public ActionResult EditBM7009(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Report report = db.Reports.Find(id);
            if (report == null)
                return HttpNotFound();

            if (report.AirportId != null)
                ViewBag.Branch = db.Airports.FirstOrDefault(a => a.Id == report.AirportId).Branch;

            return View(report);
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM7009([Bind(Include = "Id,Value1,Value2,Value3,Value4,Value5,Value6")] Report report)
        {
            if (ModelState.IsValid)
            {
                var model = db.Reports.FirstOrDefault(r => r.Id == report.Id);
                if (model != null)
                {
                    model.Value1 = report.Value1;
                    model.Value2 = report.Value2;
                    model.Value3 = report.Value3;
                    model.Value4 = report.Value4;
                    model.Value5 = report.Value5;
                    model.Value6 = report.Value6;
                }
                db.SaveChanges();
                return RedirectToAction("BM7009List");
            }
            return View(report);
        }
        /// BM10002 
        public BM10002 BM10002Query(string daterange, int airportId = -1, int reportId = -1)
        {
            var flights = db.Flights
               .Include(f => f.RefuelItems)
               .Include(f => f.Airport)
               .Include(f => f.Airline)
               .Include(f => f.Properties)
               .Include(f => f.PropertyFlightValues)
               .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );

            if (airportId > 0)
                flights = flights.Where(f => f.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(f => ids.Contains((int)f.AirportId));
            }
            var report = new Report();
            if (reportId > 0)
                report = db.Reports
                    .Include(r => r.User1)
                    .Include(r => r.User2)
                    .FirstOrDefault(r => r.Id == reportId);

            var g = flights.ToList();

            var temp = new BM10002
            {
                Flights = g.Where(f => !string.IsNullOrEmpty(f.Note)).OrderByDescending(f => f.RefuelScheduledTime).ToList(),
                Quantity1 = g.Count(),
                Description1 = report != null ? report.Value1 : "",

                Quantity2 = g.Where(s => s.FlightCarry == FlightCarry.CCO).Count(),
                Description2 = report != null ? report.Value2 : "",

                Quantity3 = g.Where(s => s.Properties.Any(p => p.Code == "3" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description3 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "3" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description3 = report != null ? report.Value3 : "",

                Quantity4 = g.Where(s => s.Properties.Any(p => p.Code == "4" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description4 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "4" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description4 = report != null ? report.Value4 : "",

                Quantity5 = g.Where(s => s.Properties.Any(p => p.Code == "5" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description5 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "5" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description5 = report != null ? report.Value5 : "",

                Quantity6 = g.Where(s => s.Status == FlightStatus.CANCELED).Count(),
                Description6 = report != null ? report.Value6 : "",

                Quantity7 = g.Where(s => s.Properties.Any(p => p.Code == "7" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description7 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "7" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description7 = report != null ? report.Value7 : "",

                Quantity8 = g.Where(s => s.Properties.Any(p => p.Code == "8" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description8 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "8" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description8 = report != null ? report.Value8 : "",

                Quantity9 = g.Where(s => s.Properties.Any(p => p.Code == "9" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description9 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "9" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description9 = report != null ? report.Value9 : "",

                Quantity10 = g.Where(s => s.Properties.Any(p => p.Code == "10" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description10 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "10" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description10 = report != null ? report.Value10 : "",

                Quantity11 = g.Where(s => s.Properties.Any(p => p.Code == "11" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description11 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "11" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description11 = report != null ? report.Value11 : "",

                Quantity12 = g.Where(s => s.Properties.Any(p => p.Code == "12" && p.ReportType == REPORT_TYPE.BM10002)).Count(),
                Description12 = string.Join("; ", g.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "12" && pr.ReportType == REPORT_TYPE.BM10002).Id).Select(pf => pf.Note)).ToArray()),
                //Description12 = report != null ? report.Value12 : "",

                Description = report != null ? report.Description : "",
                User1Name = (report != null && report.User1 != null) ? report.User1.FullName : "---",
                User2Name = (report != null && report.User2 != null) ? report.User2.FullName : "---",

            };
            return temp;
        }
        [FMSAuthorize]
        public ActionResult BM10002()
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var users = db.Users.AsNoTracking() as IQueryable<User>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                users = users.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Users = users.ToList();
            ViewBag.Airports = airports.ToList();
            return View(BM10002Query(Request["daterange"], airportId));
        }
        [FMSAuthorize]
        public ActionResult BM10002Detail(int id, int p = 1)
        {
            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;
            ViewBag.CurrentUserId = currentUserId;

            var item = new BM10002();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                int pageSize = 20;
                if (Request["pageSize"] != null)
                    pageSize = Convert.ToInt32(Request["pageSize"]);
                if (Request["pageIndex"] != null)
                    p = Convert.ToInt32(Request["pageIndex"]);

                ViewBag.IsActive = report.IsActive;
                ViewBag.Id = id;
                ViewBag.CreatedName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                ViewBag.FromTime = report.FromTime;
                ViewBag.ToTime = report.ToTime;
                ViewBag.DateCreated = report.DateCreated;
                ViewBag.ActivedName = report.UserActived != null ? report.UserActived.FullName : "sa";
                ViewBag.Report = report;

                var fd = report.FromTime;
                var td = report.ToTime;

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                item = BM10002Query(dr, (int)report.AirportId, id);
            }
            return View(item);
        }
        [FMSAuthorize]
        public ActionResult BM10002List(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var daterange = Request["daterange"];
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var reports = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .Where(r => r.ReportType == REPORT_TYPE.BM10002)
                .AsNoTracking() as IQueryable<Report>;

            reports = reports.Where(f => DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );


            var arports = db.Airports.ToList();
            if (airportId > 0)
                reports = reports.Where(r => r.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                reports = reports.Where(a => ids.Contains((int)a.AirportId));
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
            }

            ViewBag.Airports = arports;
            ViewBag.ItemCount = reports.Count();
            reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            var list = reports.ToList();
            return View(list);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult CreateBM10002([Bind(Include = "Value1,Value2,Value3,Value4,Value5,Value6,Value7,Value8,Value9,Value10,Value11,Value12,Description,User1Id,User2Id")] Report report, string url, int airportId = -1)
        {
            if (Request.IsAjaxRequest() && ModelState.IsValid)
            {
                var fd = DateTime.Today.AddHours(0).AddMinutes(0);
                var td = DateTime.Today.AddHours(23).AddMinutes(59);

                var uri = new Uri("http://fms.skypec.com.vn" + url);
                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var daterange = HttpUtility.ParseQueryString(query).Get("daterange");
                    if (!string.IsNullOrEmpty(daterange))
                    {
                        var range = daterange.Split('-');
                        fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                        if (range.Length > 1)
                            td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    }
                }

                report.ReportType = REPORT_TYPE.BM10002;
                report.Url = url;
                if (user != null)
                    report.UserCreatedId = user.Id;
                report.FromTime = fd;
                report.ToTime = td;

                if (airportId > 0)
                    report.AirportId = airportId;
                else if (!User.IsInRole("Super Admin") && user != null)
                {
                    var u_airportIds = user.Airports.Select(a => a.Id).ToArray();
                    if (u_airportIds.Count() == 1)
                        report.AirportId = u_airportIds[0];
                }

                db.Reports.Add(report);
                db.SaveChanges();
                return Json(new { result = "OK" });
            }
            return View(report);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult ActiveBM10002(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);

            report.IsActive = true;
            if (user != null)
                report.UserActivedId = user.Id;
            report.DateActived = DateTime.Now;

            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã duyệt" });

        }
        [HttpPost]
        public ActionResult Active2BM10002(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);

            report.IsActive2 = true;
            if (user != null)
                report.UserActived2Id = user.Id;
            report.DateActived2 = DateTime.Now;

            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xác nhận" });

        }
        [FMSAuthorize]
        public ActionResult ExportBM10002Detail(int id)
        {
            int airportId = -1;
            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                airportId = (int)report.AirportId;
                var isactive = report.IsActive ? "Đã duyệt" : "Chưa duyệt";
                var createdName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                var fromTime = report.FromTime;
                var toTime = report.ToTime;
                var dateCreated = report.DateCreated;
                var activedName = "";
                if (report.IsActive)
                    activedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var actived2Name = "";
                if (report.IsActive2 != null && (bool)report.IsActive2)
                    actived2Name = report.UserActived2 != null ? report.UserActived2.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                var name = "BM10001";
                string fileName = name + ".xlsx";

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var temp = BM10002Query(dr, airportId, id);

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
                        rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                        rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        //rng.AutoFitColumns();
                        rng.Style.WrapText = true;
                    }

                    ws1.Cells[1, 1, 1, 4].Merge = true;
                    ws1.Cells[1, 1, 1, 4].Value = "BÁO CÁO CA TRỰC ĐIỀU PHỐI";

                    ws1.Cells[2, 1, 2, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[2, 1, 2, 4].Merge = true;
                    ws1.Cells[2, 1, 2, 4].Value = "Thời gian trực: Từ " + fd.ToString("dd/MM/yyyy HH:mm") + " đến " + td.ToString("dd/MM/yyyy HH:mm");

                    ws1.Cells[3, 1, 3, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[3, 1, 3, 4].Merge = true;
                    ws1.Cells[3, 1, 3, 4].Value = "Người báo cáo: " + createdName;

                    ws1.Cells[4, 1, 4, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[4, 1, 4, 4].Merge = true;
                    ws1.Cells[4, 1, 4, 4].Value = "Trạng thái duyệt: " + isactive;

                    ws1.Cells[5, 1, 5, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[5, 1, 5, 4].Merge = true;
                    ws1.Cells[5, 1, 5, 4].Value = "Người duyệt: " + activedName;

                    ws1.Cells[6, 1, 6, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[6, 1, 6, 4].Merge = true;
                    ws1.Cells[6, 1, 6, 4].Value = "Người nhận bàn giao: " + actived2Name;

                    ws1.Cells[7, 1, 7, 4].Style.Font.Bold = true;
                    ws1.Cells[7, 1, 7, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[7, 1, 7, 4].Merge = true;
                    ws1.Cells[7, 1, 7, 4].Value = "I.NỘI DUNG GHI NHẬN";

                    ws1.Row(1).Style.Font.Bold = ws1.Row(2).Style.Font.Bold = ws1.Row(3).Style.Font.Bold = ws1.Row(4).Style.Font.Bold = ws1.Row(5).Style.Font.Bold = ws1.Row(6).Style.Font.Bold = true;
                    var rowfix = 8;

                    ws1.Row(rowfix).Style.Font.Bold = true;
                    ws1.Cells[rowfix, 1].Value = "TT";
                    ws1.Cells[rowfix, 2].Value = "Tiêu chí";
                    ws1.Cells[rowfix, 3].Value = "Số lượng";
                    ws1.Cells[rowfix, 4].Value = "Diễn giải";

                    int rowIndexBegin = 9;
                    int rowIndexCurrent = rowIndexBegin;

                    if (temp != null)
                    {
                        //int total = (temp.Quantity2 + temp.Quantity3 + temp.Quantity4 + temp.Quantity5 + temp.Quantity6 + temp.Quantity7 + temp.Quantity8 + temp.Quantity9 + temp.Quantity10 + temp.Quantity11 + temp.Quantity12);
                        ws1.Cells[rowIndexCurrent, 1].Value = "1";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Số chuyến bay phục vụ trong ca trực";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity1;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description1;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "2";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay chuyên cơ";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity2;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description2;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "3";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay chậm phục vụ tra nạp";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity3;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description3;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "4";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Sự cố bất thường (an ninh, an toàn, chênh lệch)";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity4;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description4;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "5";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay Charter/HĐ Charter";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity5;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description5;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "6";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay hủy";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity6;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description6;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "7";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay tăng chuyến ngoài kế hoạch ";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity7;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description7;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "8";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay đã nạp dầu nhưng thay đổi đường bay";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity8;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description8;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "9";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Nạp dầu kỹ thuật";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity9;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description9;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "10";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay hút dầu";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity10;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description10;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "11";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Thông tin khách hàng (phản hồi, yêu cầu dịch vụ, khách hàng mới…) ";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity11;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description11;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1].Value = "12";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Sự cố liên quan trang thiết bị (hệ thống mạng, bộ đàm, điện thoại, máy in…) ";
                        ws1.Cells[rowIndexCurrent, 3].Value = temp.Quantity12;
                        ws1.Cells[rowIndexCurrent, 4].Value = temp.Description12;
                        ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "II.THÔNG TIN CHUYẾN BAY";
                        rowIndexCurrent++;

                        ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                        ws1.Cells[rowIndexCurrent, 1].Value = "TT";
                        ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay";
                        ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 3].Value = "Ghi nhận";

                        ws1.Cells[rowIndexBegin, 2, rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexBegin, 4, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                        var index = 1;
                        var rowIndexBegin2 = rowIndexCurrent++;
                        foreach (var item in temp.Flights)
                        {
                            ws1.Cells[rowIndexCurrent, 1].Value = index++;
                            ws1.Cells[rowIndexCurrent, 2].Value = item.Code;
                            ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                            ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = item.Note;
                            rowIndexCurrent++;
                        }
                        ws1.Cells[rowIndexBegin2, 2, rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexBegin2, 3, rowIndexCurrent, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "III.BÀN GIAO CA TRỰC";
                        rowIndexCurrent++;

                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = temp.Description;
                        rowIndexCurrent++;

                        ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Merge = true;
                        ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Value = "BM100.01/CNMN";

                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Top.Color.SetColor(Color.Black);
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Left.Color.SetColor(Color.Black);
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Right.Color.SetColor(Color.Black);
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        ws1.Cells[7, 1, rowIndexCurrent, 4].Style.Border.Bottom.Color.SetColor(Color.Black);
                        ws1.Column(2).Width = 70; ws1.Column(4).Width = 50;
                        //rowIndexCurrent++;

                        //ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 2].Merge = true;
                        //ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 2].Value = "NGƯỜI NHẬN BÀN GIAO \r\n" + temp.User1Name;
                        //ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Merge = true;
                        //ws1.Cells[rowIndexCurrent, 3, rowIndexCurrent, 4].Value = "NGƯỜI BÁO CÁO \r\n" + temp.User2Name;
                    }
                    package.SaveAs(newFile);
                }


                var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
                if (!System.IO.File.Exists(readFile))
                    return HttpNotFound();
                return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            return View();
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult JDeleteBM10002(int id)
        {
            Report item = db.Reports.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        [FMSAuthorize]
        public ActionResult EditBM10002(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Report report = db.Reports.Find(id);
            if (report == null)
                return HttpNotFound();

            var users = db.Users.AsNoTracking() as IQueryable<User>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                users = users.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.User1Id = new SelectList(users.ToList(), "Id", "FullName", report.User1Id);
            ViewBag.User2Id = new SelectList(users.ToList(), "Id", "FullName", report.User2Id);
            return View(report);
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM10002([Bind(Include = "Id,Description,Value1,Value2,Value3,Value4,Value5,Value6,Value7,Value8,Value9,Value10,Value11,Value12,User1Id,User2Id")] Report report)
        {
            if (ModelState.IsValid)
            {
                var model = db.Reports.FirstOrDefault(r => r.Id == report.Id);
                if (model != null)
                {
                    model.Value1 = report.Value1;
                    model.Value2 = report.Value2;
                    model.Value3 = report.Value3;
                    model.Value4 = report.Value4;
                    model.Value5 = report.Value5;
                    model.Value6 = report.Value6;
                    model.Value7 = report.Value7;
                    model.Value8 = report.Value8;
                    model.Value9 = report.Value9;
                    model.Value10 = report.Value10;
                    model.Value11 = report.Value11;
                    model.Value12 = report.Value12;
                    model.Description = report.Description;
                    model.User2Id = report.User2Id;
                    model.User1Id = report.User1Id;
                }
                db.SaveChanges();
                return RedirectToAction("BM10002List");
            }
            return View(report);
        }
        ///BM10103
        ///
        public IQueryable<Report> BM10103Query(string daterange, int airportId = -1)
        {
            var reports = db.Reports
                .Include(r => r.Flight)
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .Where(r => r.ReportType == REPORT_TYPE.BM10103 && (r.IsGroup == null || r.IsGroup == false))
                .AsNoTracking() as IQueryable<Report>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            reports = reports.Where(f => DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );

            if (airportId > 0)
                reports = reports.Where(f => f.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                reports = reports.Where(f => ids.Contains((int)f.AirportId));
            }
            return reports;
        }
        [FMSAuthorize]
        public ActionResult CreateBM10103Group(int p = 1)
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            int pageSize = 100;

            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var reports = BM10103Query(Request["daterange"], airportId);

            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id).ToArray();
                arports = arports.Where(a => ids.Contains(a.Id));
            }
            ViewBag.Airports = arports.ToList();
            ViewBag.ItemCount = reports.Count();
            reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            var list = reports.ToList();
            return View(list);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult CreateBM10103Group(string url, string value8, string value9, string value10, int airportId = -1)
        {
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            var uri = new Uri("http://fms.skypec.com.vn" + url);
            var query = uri.Query;
            if (!string.IsNullOrEmpty(query))
            {
                var daterange = HttpUtility.ParseQueryString(query).Get("daterange");
                if (!string.IsNullOrEmpty(daterange))
                {
                    var range = daterange.Split('-');
                    fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    if (range.Length > 1)
                        td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var report = new Report();
            report.ReportType = REPORT_TYPE.BM10103;
            report.IsGroup = true;
            report.Value8 = value8;
            report.Value9 = value9;
            report.Value10 = value10;

            report.Url = url;
            if (user != null)
                report.UserCreatedId = user.Id;
            report.FromTime = fd;
            report.ToTime = td;

            if (airportId > 0)
                report.AirportId = airportId;
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var u_airportIds = user.Airports.Select(a => a.Id).ToArray();
                if (u_airportIds.Count() == 1)
                    report.AirportId = u_airportIds[0];
                //report.AirportIds = string.Join(",", u_airportIds);
            }

            db.Reports.Add(report);
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã lưu báo cáo này" });
        }
        [FMSAuthorize]
        public ActionResult BM10103List(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var daterange = Request["daterange"];
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var reports = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .Where(r => r.ReportType == REPORT_TYPE.BM10103 && r.IsGroup == true)
                .AsNoTracking() as IQueryable<Report>;

            reports = reports.Where(f => DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );


            var arports = db.Airports.ToList();
            if (airportId > 0)
                reports = reports.Where(r => r.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                reports = reports.Where(a => ids.Contains((int)a.AirportId));
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
            }

            ViewBag.Airports = arports;
            ViewBag.ItemCount = reports.Count();
            reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            var list = reports.ToList();
            return View(list);
        }
        [FMSAuthorize]
        public ActionResult CreateBM10103(int flightId = -1)
        {
            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var users = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id));
                users = users.Where(u => ids.Contains((int)u.AirportId));
            }

            string truck_name = "";
            var refuelItem = db.RefuelItems.Include(r => r.Truck).Where(r => r.FlightId == flightId);
            if (refuelItem.Count() > 0)
            {
                truck_name = string.Join(", ", refuelItem.Select(t => t.Truck.Code).ToArray());
            }

            ViewBag.Airports = arports.ToList();
            ViewBag.Users = users.ToList();
            ViewBag.FlightId = flightId;
            ViewBag.TruckName = truck_name;
            return View();
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBM10103([Bind(Include = "AirportId,FlightId,User1Id,FromTime,Value2,BValue1,BValue2,BValue3,BValue4,Value3,Value4,Value5,BValue7,BValue8,BValue9,BValue10,BValue11,Value6,BValue12,BValue13,BValue14,BValue15,BValue16,BValue17,Value7,BValue18,BValue19,Value8,Value9,Value10, Value11, Value12, Value13, Value14, Value15, Value16, Value17, Value18, Value19, Value20, Value21, Value22, Value23, Value24, Value25, Value26, Value27")] Report report)
        {
            if (ModelState.IsValid)
            {
                var o_report = db.Reports.FirstOrDefault(r => r.FlightId == report.FlightId && r.ReportType == REPORT_TYPE.BM10103 && r.IsGroup != true);
                if (o_report == null)
                {
                    if (string.IsNullOrEmpty(report.Value2))
                        report.Value2 = DateTime.Now.ToString("hh:mm");

                    int airportId = -1;
                    if (report.AirportId > 0)
                        airportId = (int)report.AirportId;

                    //var fd = DateTime.Today.AddHours(0).AddMinutes(0);
                    //var td = DateTime.Today.AddHours(23).AddMinutes(59);

                    //if (!string.IsNullOrEmpty(Request["daterange"]))
                    //{
                    //    var daterange = Request["daterange"];
                    //    var range = daterange.Split('-');
                    //    fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    //    if (range.Length > 1)
                    //        td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    //}

                    report.ReportType = REPORT_TYPE.BM10103;
                    report.Url = "";
                    if (user != null)
                        report.UserCreatedId = user.Id;

                    //report.FromTime = fd;
                    report.ToTime = DateTime.Now;

                    if (airportId > 0)
                        report.AirportId = airportId;
                    else if (!User.IsInRole("Super Admin") && user != null)
                    {
                        var u_airportIds = user.Airports.Select(a => a.Id).ToArray();
                        if (u_airportIds.Count() == 1)
                            report.AirportId = u_airportIds[0];
                    }

                    db.Reports.Add(report);
                    db.SaveChanges();
                }
                Response.Write("<script>window.open('" + (Request["url"] != null ? Request["url"].ToString() : "~/Flights/Index") + "','_parent');</script>");
                //return RedirectToAction("BM10103List");
            }

            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var users = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id));
                users = users.Where(u => ids.Contains((int)u.AirportId));
            }
            ViewBag.Airports = arports.ToList();
            ViewBag.Users = users.ToList();

            return View();
        }
        [HttpPost]
        public ActionResult ActiveBM10103(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);
            report.IsActive = true;
            if (user != null)
                report.UserActivedId = user.Id;
            report.DateActived = DateTime.Now;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã duyệt" });
        }
        [HttpPost]
        public ActionResult Active2BM10103(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);

            report.IsActive2 = true;
            if (user != null)
                report.UserActived2Id = user.Id;
            report.DateActived2 = DateTime.Now;

            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xác nhận" });
        }
        [FMSAuthorize]
        public ActionResult EditBM10103(int? id, int flightId = -1)
        {
            if (id == null && flightId == -1)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Report report = db.Reports.Find(id);
            if (report == null)
                report = db.Reports.FirstOrDefault(r => r.FlightId == flightId);
            if (report == null)
                return HttpNotFound();

            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var users = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id));
                users = users.Where(u => ids.Contains((int)u.AirportId));
            }
            ViewBag.Airports = arports.ToList();
            ViewBag.Users = users.ToList();

            return View(report);
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM10103([Bind(Include = "Id, AirportId, User1Id, Value1, Value2, BValue1, BValue2, BValue3, BValue4, Value3, Value4, Value5, BValue7, BValue11, Value6, BValue12, BValue13, BValue14, BValue15, BValue16, BValue17, Value7, BValue18, BValue19, Value8, Value9, Value10, Value11, Value12, Value13, Value14, Value15, Value16, Value17, Value18, Value19, Value20, Value21, Value22, Value23, Value24, Value25, Value26, Value27")] Report report)
        {
            if (ModelState.IsValid)
            {
                var model = db.Reports.FirstOrDefault(r => r.Id == report.Id);
                if (model != null)
                {

                    var fd = DateTime.Today.AddHours(0).AddMinutes(0);
                    var td = DateTime.Today.AddHours(23).AddMinutes(59);

                    if (!string.IsNullOrEmpty(Request["daterange"]))
                    {
                        var daterange = Request["daterange"];
                        var range = daterange.Split('-');
                        fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                        if (range.Length > 1)
                            td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    }


                    model.DateUpdated = DateTime.Now;
                    model.FromTime = fd;
                    model.ToTime = td;
                    model.AirportId = report.AirportId;
                    model.User1Id = report.User1Id;
                    model.Value1 = report.Value1;
                    if (string.IsNullOrEmpty(report.Value2))
                        model.Value2 = DateTime.Now.ToString("hh:mm tt");
                    else
                        model.Value2 = report.Value2;
                    model.BValue1 = report.BValue1;
                    model.BValue2 = report.BValue2;
                    model.BValue3 = report.BValue3;
                    model.BValue4 = report.BValue4;
                    model.Value3 = report.Value3;
                    model.Value4 = report.Value4;
                    model.Value5 = report.Value5;
                    model.BValue7 = report.BValue7;
                    model.BValue8 = report.BValue8;
                    model.BValue9 = report.BValue9;
                    model.BValue10 = report.BValue10;
                    model.BValue11 = report.BValue11;
                    model.Value6 = report.Value6;
                    model.BValue12 = report.BValue12;
                    model.BValue13 = report.BValue13;
                    model.BValue14 = report.BValue14;
                    model.BValue15 = report.BValue15;
                    model.BValue16 = report.BValue16;
                    model.BValue17 = report.BValue17;
                    model.Value7 = report.Value7;
                    model.BValue18 = report.BValue18;
                    model.BValue19 = report.BValue19;
                    model.Value8 = report.Value8;
                    model.Value9 = report.Value9;
                    model.Value10 = report.Value10;
                    model.Value11 = report.Value11;
                    model.Value12 = report.Value12;
                    model.Value13 = report.Value13;
                    model.Value14 = report.Value14;
                    model.Value15 = report.Value15;
                    model.Value16 = report.Value16;
                    model.Value17 = report.Value17;
                    model.Value18 = report.Value18;
                    model.Value19 = report.Value19;
                    model.Value20 = report.Value20;
                    model.Value21 = report.Value21;
                    model.Value22 = report.Value22;
                    model.Value23 = report.Value23;
                    model.Value24 = report.Value24;
                    model.Value25 = report.Value25;
                    model.Value26 = report.Value26;
                    model.Value27 = report.Value27;
                }
                db.SaveChanges();
                if (string.IsNullOrEmpty(Request["nl"]))
                    Response.Write("<script>window.open('" + (Request["url"] != null ? Request["url"].ToString() : "~/Flights/Index") + "','_parent');</script>");
                else
                    return RedirectToAction("BM10103List");
            }
            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var users = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id));
                users = users.Where(u => ids.Contains((int)u.AirportId));
            }
            ViewBag.Airports = arports.ToList();
            ViewBag.Users = users.ToList();
            return View(report);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult JDeleteBM10103(int id)
        {
            Report item = db.Reports.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        [FMSAuthorize]
        public ActionResult BM10103Detail(int id, int p = 1)
        {
            var airportId = -1;
            var list = new List<Report>();
            var report = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.Id == id);

            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;
            ViewBag.Report = report;
            ViewBag.CurrentUserId = currentUserId;

            if (report != null)
            {
                if (report.AirportId != null)
                    airportId = (int)report.AirportId;

                int pageSize = 20;
                if (Request["pageSize"] != null)
                    pageSize = Convert.ToInt32(Request["pageSize"]);
                if (Request["pageIndex"] != null)
                    p = Convert.ToInt32(Request["pageIndex"]);

                var fd = report.FromTime;
                var td = report.ToTime;

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var reports = BM10103Query(dr, airportId);

                ViewBag.ItemCount = reports.Count();
                reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
                list = reports.ToList();
            }
            return View(list);
        }
        public ActionResult BM10103Detail2(int flightId)
        {
            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;
            ViewBag.CurrentUserId = currentUserId;

            var report = db.Reports
                .Include(r => r.Flight)
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.FlightId == flightId);
            return View(report);
        }
        [FMSAuthorize]
        public ActionResult ExportBM10103Detail(int id)
        {
            int airportId = -1;
            var report = db.Reports
                .Include(r => r.Flight)
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.User2)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                airportId = report.AirportId != null ? (int)report.AirportId : -1;

                var createdName = report.UserCreated != null ? report.UserCreated.FullName : "sa";

                var fromTime = report.FromTime;
                var toTime = report.ToTime;
                var dateCreated = report.DateCreated;
                var activedName = "";
                if (report.IsActive)
                    activedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var actived2Name = "";
                if (report.IsActive2 != null && (bool)report.IsActive2)
                    actived2Name = report.UserActived2 != null ? report.UserActived2.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                var name = "BM10103";
                string fileName = name + ".xlsx";

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var temp = BM10103Query(dr, airportId);

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
                        rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                        rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        //rng.AutoFitColumns();
                        rng.Style.WrapText = true;
                    }

                    ws1.Cells[1, 1, 1, 21].Merge = true;
                    ws1.Cells[1, 1, 1, 21].Value = "Người tạo: " + createdName;
                    ws1.Cells[2, 1, 2, 21].Merge = true;
                    ws1.Cells[2, 1, 2, 21].Value = "Người duyệt: " + activedName;
                    ws1.Cells[3, 1, 3, 21].Merge = true;
                    ws1.Cells[3, 1, 3, 21].Value = "Người nhận bàn giao: " + actived2Name;
                    ws1.Cells[4, 1, 4, 21].Merge = true;
                    ws1.Cells[4, 1, 4, 21].Value = "Thời gian trực: từ " + fd.ToString("dd/MM/yyyy HH:mm") + " đến " + td.ToString("dd/MM/yyyy HH:mm");

                    ws1.Cells[5, 1, 5, 21].Merge = true;
                    ws1.Cells[5, 1, 5, 21].Value = "BÁO CÁO GIÁM SÁT CLDV TẠI SÂN ĐỖ";

                    ws1.Cells[6, 2].Value = "Thông tin chuyến bay";

                    ws1.Cells[6, 3, 6, 5].Merge = true;
                    ws1.Cells[6, 3, 6, 5].Value = "1. Về DVKH";

                    ws1.Cells[6, 6, 6, 7].Merge = true;
                    ws1.Cells[6, 6, 6, 7].Value = "2. Chỉ tiêu đúng giờ";

                    ws1.Cells[6, 8, 6, 12].Merge = true;
                    ws1.Cells[6, 8, 6, 12].Value = "3. Tuân thủ qui trình tra nạp";

                    ws1.Cells[6, 13, 6, 18].Merge = true;
                    ws1.Cells[6, 13, 6, 18].Value = "4. Phương tiện, trang thiết bị tra nạp";

                    ws1.Cells[6, 19, 6, 20].Merge = true;
                    ws1.Cells[6, 19, 6, 20].Value = "5. Xử lý bất thường/nhận diện rủi ro";

                    ws1.Row(5).Style.Font.Bold = true;
                    ws1.Row(6).Style.Font.Bold = true;
                    ws1.Row(7).Style.Font.Bold = true;
                    ws1.Column(1).Width = ws1.Column(2).Width = ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = ws1.Column(6).Width = ws1.Column(7).Width = ws1.Column(8).Width
                        = ws1.Column(9).Width = ws1.Column(10).Width = ws1.Column(11).Width = ws1.Column(12).Width = ws1.Column(13).Width = ws1.Column(14).Width
                        = ws1.Column(15).Width = ws1.Column(16).Width = ws1.Column(17).Width = ws1.Column(18).Width = ws1.Column(19).Width = ws1.Column(20).Width = ws1.Column(21).Width = 20;

                    var rowfix = 7;

                    ws1.Cells[rowfix, 1].Value = "Chuyến bay/Giờ giám sát";
                    ws1.Cells[rowfix, 2].Value = "Số hiệu tàu bay, đường bay/ Loại tàu bay";
                    ws1.Cells[rowfix, 3].Value = "1.1 Trang phục, bảo hộ lao động, giấy tờ chứng chỉ, hóa đơn";
                    ws1.Cells[rowfix, 4].Value = "1.2 Tác phong (không vi phạm các qui định cơ bản trong tra nạp), thái độ phục vụ, thông tin cung cấp";
                    ws1.Cells[rowfix, 5].Value = "1.3 Việc tuân thủ qui định an ninh, an toàn của nhân viên trên sân đỗ";
                    ws1.Cells[rowfix, 6].Value = "2.1 Sự hiện diện đúng giờ";
                    ws1.Cells[rowfix, 7].Value = "2.2 Tra nạp cánh:Trái/Phải/Cả hai/Giờ bắt đầu/ Giờ kết thúc";
                    ws1.Cells[rowfix, 8].Value = "3.1 Điều khiển xe và tiếp cận vị trí tra nạp (Tiếp cận đúng vị trí ,…)/ ";
                    ws1.Cells[rowfix, 9].Value = "3.2 Các biện pháp đảm bảo an toàn chung (kẹp tiếp mát với tàu bay, hãng yêu cầu vửa tra nạp vừa Boarding có gọi xe cứu hỏa..)";
                    ws1.Cells[rowfix, 10].Value = "3.3 Kiểm tra chất lượng nhiên liệu";
                    ws1.Cells[rowfix, 11].Value = "3.4 Đảm bảo an toàn trong quá trình tra nạp: Áp suất đầu vòi tra nạp ≤ 45 PSI, chênh lệch áp suất bầu lọc ≤ 15 PSI";
                    ws1.Cells[rowfix, 12].Value = "3.5 Đảm bảo an toàn khi kết thúc/Tra nạp: tháo Coupling, đóng nắp cover/panel của tàu bay,tháo kẹp tiếp mát, thu thang tra nạp (nếu có),xác nhận số lượng, đi kiểm tra 360°.,…";
                    ws1.Cells[rowfix, 13].Value = "Xe tra nạp";
                    ws1.Cells[rowfix, 14].Value = "4.1 Tình trạng kỹ thuật phương tiện, trang thiết bị tra nạp";
                    ws1.Cells[rowfix, 15].Value = "4.2 Nhận diện thương hiệu";
                    ws1.Cells[rowfix, 16].Value = "4.3 Các biểu tượng cảnh báo, tình trạng tem và thời hạn kiểm định của trang thiết bị";
                    ws1.Cells[rowfix, 17].Value = "4.4 Vệ sinh (sạch đẹp)";
                    ws1.Cells[rowfix, 18].Value = "4.5 Tốc độ, luồng tuyến lưu thông";
                    ws1.Cells[rowfix, 19].Value = "5.1 Bất thường (Có/Không) \r\n - Có: lập biên bản BM101.06/1/CNMN";
                    ws1.Cells[rowfix, 20].Value = "5.2 Nhận diện rủi ro (Có/Không) - \r\n Nếu có báo cáo theo SMS-SKYPEC-HIRA";
                    ws1.Cells[rowfix, 21].Value = "6. Giám sát chênh lệch nhiên liệu";


                    int rowIndexBegin = 8;
                    int rowIndexCurrent = rowIndexBegin;

                    foreach (var item in temp)
                    {
                        ws1.Cells[rowIndexCurrent, 1].Value = (item.Flight != null ? item.Flight.Code : "---") + "\r\n" + item.FromTime.ToString("HH:mm") + "\r\n" + item.ToTime.ToString("HH:mm");
                        ws1.Cells[rowIndexCurrent, 2].Value = (item.Flight != null ? item.Flight.AircraftCode : "---") + "\r\n" + (item.Flight != null ? item.Flight.RouteName : "---") + "\r\n" + (item.Flight != null ? item.Flight.AircraftType : "---");
                        ws1.Cells[rowIndexCurrent, 3].Value = item.BValue1 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value11;
                        ws1.Cells[rowIndexCurrent, 4].Value = item.BValue2 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value12;
                        ws1.Cells[rowIndexCurrent, 5].Value = item.BValue3 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value13;
                        ws1.Cells[rowIndexCurrent, 6].Value = item.BValue4 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value14;
                        ws1.Cells[rowIndexCurrent, 7].Value = item.Value3 + "\r\n" + item.Value4 + "\r\n" + item.Value5;
                        ws1.Cells[rowIndexCurrent, 8].Value = item.BValue7 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value15;
                        ws1.Cells[rowIndexCurrent, 9].Value = item.BValue8 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value16;
                        ws1.Cells[rowIndexCurrent, 10].Value = item.BValue9 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value17;
                        ws1.Cells[rowIndexCurrent, 11].Value = item.BValue10 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value18;
                        ws1.Cells[rowIndexCurrent, 12].Value = item.BValue11 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value19;
                        ws1.Cells[rowIndexCurrent, 13].Value = item.Value6;
                        ws1.Cells[rowIndexCurrent, 14].Value = item.BValue12 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value20;
                        ws1.Cells[rowIndexCurrent, 15].Value = item.BValue13 == true ? "Có" : "Không" + "\r\n" + item.Value21;
                        ws1.Cells[rowIndexCurrent, 16].Value = item.BValue14 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value22;
                        ws1.Cells[rowIndexCurrent, 17].Value = item.BValue15 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value23;
                        ws1.Cells[rowIndexCurrent, 18].Value = item.BValue16 == true ? "Đạt" : "Không đạt" + "\r\n" + item.Value24;
                        ws1.Cells[rowIndexCurrent, 19].Value = item.BValue17 == true ? "Có" : "Không" + "\r\n" + (item.BValue17 == true ? item.Value7 : "") + "\r\n" + item.Value25;
                        ws1.Cells[rowIndexCurrent, 20].Value = item.BValue18 == true ? "Có" : "Không" + "\r\n" + item.Value26;
                        ws1.Cells[rowIndexCurrent, 21].Value = item.BValue19 == true ? "Có" : "Không" + "\r\n" + item.Value27;
                        rowIndexCurrent++;
                    }

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Value = "*Báo cáo kết quả ca trực: " + report.Value8;
                    rowIndexCurrent++;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Value = "*Kiến nghị / đề xuất: " + report.Value9;

                    rowIndexCurrent++;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 21].Value = "*Bàn giao ca: " + report.Value10;

                    rowIndexCurrent++;
                    ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Value = "BM101.03/CNMN";

                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Top.Color.SetColor(Color.Black);
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Left.Color.SetColor(Color.Black);
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Right.Color.SetColor(Color.Black);
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    ws1.Cells[4, 1, rowIndexCurrent, 21].Style.Border.Bottom.Color.SetColor(Color.Black);

                    package.SaveAs(newFile);
                }


                var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
                if (!System.IO.File.Exists(readFile))
                    return HttpNotFound();
                return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            return View();
        }
        //BM2508
        [FMSAuthorize]
        public ActionResult CreateBM2508(int p = 1)
        {
            int pageSize = 20;

            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var flights = db.Flights
                .Include(f => f.RefuelItems)
                .Include(f => f.RefuelItems.Select(r => r.Driver))
                .Include(f => f.RefuelItems.Select(r => r.Operator))
                .Where(f => f.Status == FlightStatus.REFUELED).AsNoTracking() as IQueryable<Flight>;
            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            var daterange = Request["daterange"];
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                ).OrderByDescending(f => f.RefuelScheduledTime);

            if (airportId > 0)
                flights = flights.Where(f => f.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id).ToArray();
                arports = arports.Where(a => ids.Contains(a.Id));
                flights = flights.Where(f => ids.Contains((int)f.AirportId));
            }
            ViewBag.Airports = arports.ToList();
            ViewBag.Flights = flights.ToList();

            //var item = new Flight();
            if (!string.IsNullOrEmpty(Request["f"]))
            {
                var fid = Convert.ToInt32(Request["f"]);
                flights = flights.Where(f => f.Id == fid);
            }
            //ViewBag.ItemCount = flights.Count();
            //flights = flights.Skip((p - 1) * pageSize).Take(pageSize);
            var list = flights.ToList();


            return View(flights);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult CreateBM2508(string json)
        {
            using (DataContext db = new DataContext())
            {
                JObject jsonData = JObject.Parse("{'items':" + json + "}");
                var refuelItems = (from d in jsonData["items"]
                                   select new RefuelItem { Id = d["id"].Value<int>(), BondingCable = d["bondingCable"].Value<bool>(), FuelingHose = d["fuelingHose"].Value<bool>(), FuelingCap = d["fuelingCap"].Value<bool>(), Ladder = d["ladder"].Value<bool>() }
                           ).ToList();
                var ids = refuelItems.Select(a => a.Id).ToList();
                var list = db.RefuelItems.Where(c => ids.Contains(c.Id)).ToList();
                list.ForEach(a =>
                {
                    var item = refuelItems.FirstOrDefault(d => d.Id == a.Id);
                    if (item != null)
                    {
                        a.BondingCable = item.BondingCable;
                        a.FuelingHose = item.FuelingHose;
                        a.FuelingCap = item.FuelingCap;
                        a.Ladder = item.Ladder;
                    }
                });

                db.SaveChanges();
                return Json(new { Status = 0, Message = "Đã lưu" });
            }
        }
        public List<RefuelItem> BM2508Query(string daterange, int airportId = -1, int truckId = -1)
        {
            var refuelItems = db.RefuelItems
                .Include(f => f.Flight)
                .Include(f => f.Truck)
                .Include(f => f.Driver)
                .Include(f => f.Operator)
                .Where(f => f.Status == REFUEL_ITEM_STATUS.DONE && f.BM2508Result != null).AsNoTracking() as IQueryable<RefuelItem>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            refuelItems = refuelItems.Where(f => DbFunctions.CreateDateTime(f.EndTime.Value.Year, f.EndTime.Value.Month, f.EndTime.Value.Day, f.EndTime.Value.Hour, f.EndTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.EndTime.Value.Year, f.EndTime.Value.Month, f.EndTime.Value.Day, f.EndTime.Value.Hour, f.EndTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );

            if (airportId > 0)
            {
                //var fids = db.Flights.Where(f => f.AirportId == airportId).Select(f => f.Id).ToArray();
                //refuelItems = refuelItems.Where(f => fids.Contains(f.FlightId));
                refuelItems = refuelItems.Where(f => f.Flight.AirportId == airportId);
            }
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id).ToArray();
                //var fids = db.Flights.Where(f => ids.Contains((int)f.AirportId)).Select(f => f.Id).ToArray();
                //refuelItems = refuelItems.Where(f => fids.Contains((int)f.FlightId));
                refuelItems = refuelItems.Where(f => ids.Contains((int)f.Flight.AirportId));
            }
            if (truckId > 0)
                refuelItems = refuelItems.Where(r => r.TruckId == truckId);

            return refuelItems.ToList();
        }
        [FMSAuthorize]
        public ActionResult BM2508List()
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var truckId = -1;
            if (!string.IsNullOrEmpty(Request["t"]))
                truckId = Convert.ToInt32(Request["t"]);

            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            var daterange = Request["daterange"];
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }
            if (airportId > 0)
                trucks = trucks.Where(t => t.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id).ToArray();
                arports = arports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(t => ids.Contains((int)t.AirportId));
            }

            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = arports.ToList();

            var refuelItems = BM2508Query(daterange, airportId, truckId);
            return View(refuelItems.OrderBy(f => f.EndTime).ToList());
        }
        [FMSAuthorize]
        public ActionResult BM2508Detail(int id)
        {
            var item = db.RefuelItems
                .Include(f => f.Flight)
                .Include(f => f.Truck)
                .Include(f => f.Driver)
                .Include(f => f.Operator)
                .FirstOrDefault(r => r.Id == id);
            return View(item);
        }
        [FMSAuthorize]
        public ActionResult EditBM2508(int id, string url)
        {
            ViewBag.UrlReturn = url;
            var item = db.RefuelItems
                   .Include(f => f.Flight)
                   .Include(f => f.Truck)
                   .Include(f => f.Driver)
                   .Include(f => f.Operator)
                   .FirstOrDefault(r => r.Id == id);
            return View(item);
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM2508([Bind(Include = "Id,BondingCable,FuelingHose,FuelingCap,Ladder")] RefuelItem refuelItem)
        {
            string url = Request["url"];
            if (ModelState.IsValid)
            {
                var model = db.RefuelItems.FirstOrDefault(r => r.Id == refuelItem.Id);
                model.BondingCable = refuelItem.BondingCable;
                model.FuelingHose = refuelItem.FuelingHose;
                model.FuelingCap = refuelItem.FuelingCap;
                model.Ladder = refuelItem.Ladder;
                db.SaveChanges();
                return Redirect(url);
            }
            return View(refuelItem);
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult JDeleteBM2508(int id)
        {
            var model = db.RefuelItems.FirstOrDefault(r => r.Id == id);
            model.BM2508Result = null;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        [FMSAuthorize]
        public ActionResult ExportBM2508(string daterange, int airportId = -1, int truckId = -1)
        {
            string truck_code = "";
            var truck = db.Trucks.FirstOrDefault(t => t.Id == truckId);
            if (truck != null)
                truck_code = truck.Code;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            var name = "BM2508";
            string fileName = name + ".xlsx";
            var temp = BM2508Query(daterange, airportId, truckId);

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
                    rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    //rng.AutoFitColumns();
                    rng.Style.WrapText = true;
                }

                ws1.Cells[1, 1, 1, 4].Merge = true;
                ws1.Cells[1, 1, 1, 4].Value = "CÔNG TY TNHH MỘT THÀNH VIÊN \r\n NHIÊN LIỆU HÀNG KHÔNG VIỆT NAM (SKYPEC)";
                ws1.Cells[2, 1, 2, 4].Merge = true;
                ws1.Cells[2, 1, 2, 4].Value = "CHI NHÁNH.............................";

                ws1.Cells[3, 1, 3, 8].Merge = true;
                ws1.Cells[3, 1, 3, 8].Value = "NỘI DUNG KIỂM TRA KHI KẾT THÚC TRA NẠP/HÚT CHO CHUYẾN BAY";
                ws1.Cells[4, 1, 4, 8].Merge = true;
                ws1.Cells[4, 1, 4, 8].Value = "Xe tra nạp: " + truck_code + " - Ngày kiểm tra: Từ " + fd.ToString("dd/MM/yyyy HH:mm") + " đến " + td.ToString("dd/MM/yyyy HH:mm");


                ws1.Row(3).Style.Font.Bold = true;
                ws1.Row(4).Style.Font.Bold = true;
                ws1.Row(5).Style.Font.Bold = true;

                ws1.Column(1).Width = 10;
                ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = ws1.Column(6).Width = 35;
                ws1.Column(2).Width = ws1.Column(7).Width = ws1.Column(8).Width = 20;
                var rowfix = 5;
                var colfix = 1;

                ws1.Cells[rowfix, colfix++].Value = "TT";
                ws1.Cells[rowfix, colfix++].Value = "Chuyến bay";
                ws1.Cells[rowfix, colfix++].Value = "Dây chuyền tĩnh điện đã \r\n được đưa về vị trí quy \r\n định trên xe tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Ống tra nạp đã được ngắt kết  \r\n với tàu bay và đưa về vị trí quy \r\n định";
                ws1.Cells[rowfix, colfix++].Value = "Nắp cửa nạp nhiên liệu \r\n của tàu bay đã \r\n được đóng";
                ws1.Cells[rowfix, colfix++].Value = "Thang tra nạp/giàn nâng \r\n đã được đưa về vị trí quy định trên \r\n xe tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Nhân viên lái xe";
                ws1.Cells[rowfix, colfix++].Value = "Nhân viên tra nạp";

                int rowIndexBegin = 6;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                var date = DateTime.MinValue;

                var db = new DataContext();
                db.DisableFilter("IsNotDeleted");
                var db_Trucks = db.Trucks.Where(t => t.IsDeleted).ToList();
                var db_Users = db.Users.Where(t => t.IsDeleted).ToList();
                db.EnableFilter("IsNotDeleted");

                foreach (var ri in temp)
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = ri.Flight.Code;
                    ws1.Cells[rowIndexCurrent, col++].Value = ri.BondingCable ? "P" : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ri.FuelingHose ? "P" : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ri.FuelingCap ? "P" : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = ri.Ladder ? "P" : "";

                    if (ri.Driver != null)
                        ws1.Cells[rowIndexCurrent, col++].Value = ri.Driver.FullName;
                    else
                    {
                        var user_d = db_Users.FirstOrDefault(u => u.Id == ri.DriverId);
                        if (user_d != null)
                            ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                        else
                            ws1.Cells[rowIndexCurrent, col++].Value = "---";
                    }

                    if (ri.Operator != null)
                        ws1.Cells[rowIndexCurrent, col++].Value = ri.Operator.FullName;
                    else
                    {
                        var user_d = db_Users.FirstOrDefault(u => u.Id == ri.OperatorId);
                        if (user_d != null)
                            ws1.Cells[rowIndexCurrent, col++].Value = user_d.FullName;
                        else
                            ws1.Cells[rowIndexCurrent, col++].Value = "---";
                    }
                    rowIndexCurrent++;
                }

                ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Merge = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 8].Value = "BM25.08/NLHK";

                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Top.Color.SetColor(Color.Black);
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Left.Color.SetColor(Color.Black);
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Right.Color.SetColor(Color.Black);
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws1.Cells[5, 1, rowIndexCurrent, 8].Style.Border.Bottom.Color.SetColor(Color.Black);

                package.SaveAs(newFile);
            }

            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();

            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        ///BM2507
        ///
        [FMSAuthorize]
        public ActionResult CreateBM2507()
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var users = db.Users.AsNoTracking() as IQueryable<User>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                users = users.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Users = users.ToList();
            ViewBag.Airports = airports.ToList();
            return View(BM2507Query(Request["daterange"], airportId));
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult CreateBM2507([Bind(Include = "Value1,Value2,Value3,Value4,Value5,Value6,Value7,Value8,Value9")] Report model, string url, int airportId, FormCollection form)
        {
            if (Request.IsAjaxRequest() && ModelState.IsValid)
            {
                var fd = DateTime.Today.AddHours(0).AddMinutes(0);
                var td = DateTime.Today.AddHours(23).AddMinutes(59);

                var uri = new Uri("http://fms.skypec.com.vn" + url);
                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var daterange = HttpUtility.ParseQueryString(query).Get("daterange");
                    if (!string.IsNullOrEmpty(daterange))
                    {
                        var range = daterange.Split('-');
                        fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                        if (range.Length > 1)
                            td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                    }
                }

                var report = new Report();
                report.Value1 = model.Value1;
                report.Value2 = model.Value2;
                report.Value3 = model.Value3;
                report.Value4 = model.Value4;
                report.Value5 = model.Value5;
                report.Value6 = model.Value6;
                report.Value7 = model.Value7;
                report.Value8 = model.Value8;
                report.Value9 = model.Value9;

                report.ReportType = REPORT_TYPE.BM2507;
                report.Url = url;
                if (user != null)
                    report.UserCreatedId = user.Id;
                report.FromTime = fd;
                report.ToTime = td;

                if (airportId > 0)
                    report.AirportId = airportId;
                else if (!User.IsInRole("Super Admin") && user != null)
                {
                    var u_airportIds = user.Airports.Select(a => a.Id).ToArray();
                    if (u_airportIds.Count() == 1)
                        report.AirportId = u_airportIds[0];
                }

                if (!string.IsNullOrEmpty(form["Rf_Ids"]))
                {
                    var rf_Ids = form["Rf_Ids"].Split(',');
                    for (int i = 0; i < rf_Ids.Count(); i++)
                    {
                        var reportDetail = new ReportDetail();
                        reportDetail.Type = REPORT_DETAIL_TYPE.DRIVER;
                        var tt = "Value_Rf_" + rf_Ids[i].Trim();
                        reportDetail.Value3 = form[tt.Trim()];
                        var ids = rf_Ids[i].Split('_');
                        reportDetail.Value1 = ids[0].Trim();
                        reportDetail.Value2 = ids[1].Trim();
                        report.ReportDetail.Add(reportDetail);
                    }
                }
                if (!string.IsNullOrEmpty(form["Opf_Ids"]))
                {
                    var opf_Ids = form["Opf_Ids"].Split(',');
                    for (int i = 0; i < opf_Ids.Count(); i++)
                    {
                        var reportDetail = new ReportDetail();
                        reportDetail.Type = REPORT_DETAIL_TYPE.OPERATOR;
                        var ids = opf_Ids[i].Split('_');
                        reportDetail.Value1 = ids[0].Trim();
                        reportDetail.Value2 = ids[1].Trim();
                        reportDetail.Value3 = form["Value_Opf_" + opf_Ids[i].Trim()];
                        report.ReportDetail.Add(reportDetail);
                    }
                }
                if (!string.IsNullOrEmpty(form["Order"]))
                {
                    int count = Convert.ToInt32(form["Order"]);
                    for (int i = 1; i <= count; i++)
                    {
                        if (form["Value1Detail_" + i] != null && form["Value1Detail_" + i] != "")
                        {
                            var reportDetail = new ReportDetail();
                            reportDetail.Value1 = form["Value1Detail_" + i];
                            reportDetail.Value2 = form["Value2Detail_" + i] != null ? "True" : "False";
                            reportDetail.Value3 = form["Value3Detail_" + i] != null ? "True" : "False";
                            reportDetail.Value4 = form["Value4Detail_" + i];
                            report.ReportDetail.Add(reportDetail);
                        }
                    }
                }

                db.Reports.Add(report);
                db.SaveChanges();
                return Json(new { result = "OK" });
            }
            return View();
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM2507([Bind(Include = "Id, Value1,Value2,Value3,Value4,Value5,Value6,Value7,Value8,Value9")] Report report, FormCollection form)
        {
            if (ModelState.IsValid)
            {
                var model = db.Reports.Include(r => r.ReportDetail).FirstOrDefault(r => r.Id == report.Id);
                TryUpdateModel(model);
                if (model != null)
                {
                    model.DateUpdated = DateTime.Now;
                    if (user != null)
                        model.UserCreatedId = user.Id;
                    model.Value1 = report.Value1;
                    model.Value2 = report.Value2;
                    model.Value3 = report.Value3;
                    model.Value4 = report.Value4;
                    model.Value5 = report.Value5;
                    model.Value6 = report.Value6;
                    model.Value7 = report.Value7;
                    model.Value8 = report.Value8;
                    model.Value9 = report.Value9;

                    db.ReportDetails.RemoveRange(model.ReportDetail);
                    db.SaveChanges();

                    List<ReportDetail> details = new List<ReportDetail>();

                    if (!string.IsNullOrEmpty(form["Rf_Ids"]))
                    {
                        var rf_Ids = form["Rf_Ids"].Split(',');
                        for (int i = 0; i < rf_Ids.Count(); i++)
                        {
                            var reportDetail = new ReportDetail();
                            reportDetail.Type = REPORT_DETAIL_TYPE.DRIVER;
                            var tt = "Value_Rf_" + rf_Ids[i].Trim();
                            reportDetail.Value3 = form[tt.Trim()];
                            var ids = rf_Ids[i].Split('_');
                            reportDetail.Value1 = ids[0].Trim();
                            reportDetail.Value2 = ids[1].Trim();
                            details.Add(reportDetail);
                        }
                    }

                    if (!string.IsNullOrEmpty(form["Opf_Ids"]))
                    {
                        var opf_Ids = form["Opf_Ids"].Split(',');
                        for (int i = 0; i < opf_Ids.Count(); i++)
                        {
                            var reportDetail = new ReportDetail();
                            reportDetail.Type = REPORT_DETAIL_TYPE.OPERATOR;
                            var ids = opf_Ids[i].Split('_');
                            reportDetail.Value1 = ids[0].Trim();
                            reportDetail.Value2 = ids[1].Trim();
                            reportDetail.Value3 = form["Value_Opf_" + opf_Ids[i].Trim()];
                            details.Add(reportDetail);
                        }
                    }

                    if (!string.IsNullOrEmpty(form["Order"]))
                    {
                        int count = Convert.ToInt32(form["Order"]);
                        for (int i = 1; i <= count; i++)
                        {
                            if (form["Value1Detail_" + i] != null && form["Value1Detail_" + i] != "")
                            {
                                var reportDetail = new ReportDetail();
                                reportDetail.Value1 = form["Value1Detail_" + i];
                                reportDetail.Value2 = form["Value2Detail_" + i] != null ? "True" : "False";
                                reportDetail.Value3 = form["Value3Detail_" + i] != null ? "True" : "False";
                                reportDetail.Value4 = form["Value4Detail_" + i];
                                details.Add(reportDetail);
                            }
                        }
                    }
                    model.ReportDetail = details;
                }
                db.SaveChanges();
                return RedirectToAction("BM2507List");
            }
            var arports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var users = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                arports = arports.Where(a => ids.Contains(a.Id));
                users = users.Where(u => ids.Contains((int)u.AirportId));
            }
            ViewBag.Airports = arports.ToList();
            ViewBag.Users = users.ToList();
            return View(report);
        }
        public List<Flight> BM2507Query(string daterange, int airportId = -1, int reportId = -1)
        {
            var flights = db.Flights
               .Include(f => f.RefuelItems)
               .Include(f => f.RefuelItems.Select(r => r.Driver))
               .Include(f => f.RefuelItems.Select(r => r.Operator))
               .Include(f => f.RefuelItems.Select(r => r.Truck))
               .Include(f => f.Properties)
               .Include(f => f.PropertyFlightValues)
               //.Where(f => f.Status == FlightStatus.REFUELED || f.Status == FlightStatus.REFUELING)
               .AsNoTracking() as IQueryable<Flight>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            flights = flights.Where(f => DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.RefuelScheduledTime.Value.Year, f.RefuelScheduledTime.Value.Month, f.RefuelScheduledTime.Value.Day, f.RefuelScheduledTime.Value.Hour, f.RefuelScheduledTime.Value.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );

            if (airportId > 0)
                flights = flights.Where(f => f.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(f => ids.Contains((int)f.AirportId));
            }

            return flights.ToList();
        }
        [FMSAuthorize]
        public ActionResult BM2507List(int p = 1)
        {
            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;

            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var daterange = Request["daterange"];
            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);
            if (!string.IsNullOrEmpty(daterange))
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            var reports = db.Reports
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .Include(r => r.UserActived3)
                .Include(r => r.UserActived4)
                .Include(r => r.UserActived5)
                .Where(r => r.ReportType == REPORT_TYPE.BM2507)
                .AsNoTracking() as IQueryable<Report>;

            reports = reports.Where(f => DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.DateCreated.Year, f.DateCreated.Month, f.DateCreated.Day, f.DateCreated.Hour, f.DateCreated.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );


            var arports = db.Airports.ToList();
            if (airportId > 0)
                reports = reports.Where(r => r.AirportId == airportId);
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                reports = reports.Where(a => ids.Contains((int)a.AirportId));
                arports = arports.Where(a => ids.Contains(a.Id)).ToList();
            }

            ViewBag.Airports = arports;
            ViewBag.ItemCount = reports.Count();
            reports = reports.OrderByDescending(r => r.DateCreated).Skip((p - 1) * pageSize).Take(pageSize);
            var list = reports.ToList();
            return View(list);
        }
        [FMSAuthorize]
        public ActionResult BM2507Detail(int id, int p = 1)
        {
            var airportId = -1;
            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.ReportDetail)
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived2)
                .Include(r => r.UserActived3)
                .Include(r => r.UserActived4)
                .Include(r => r.UserActived5)
                .FirstOrDefault(r => r.Id == id);

            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;
            ViewBag.Report = report;
            ViewBag.CurrentUserId = currentUserId;

            if (report != null)
            {
                if (report.AirportId != null)
                    airportId = (int)report.AirportId;

                int pageSize = 20;
                if (Request["pageSize"] != null)
                    pageSize = Convert.ToInt32(Request["pageSize"]);
                if (Request["pageIndex"] != null)
                    p = Convert.ToInt32(Request["pageIndex"]);

                var fd = report.FromTime;
                var td = report.ToTime;

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var flights = BM2507Query(dr, airportId);

                ViewBag.ItemCount = flights.Count();

                list = flights.ToList();
            }
            return View(list);
        }
        [HttpPost]
        public ActionResult ActiveBM2507(int id)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);
            report.IsActive = true;
            if (user != null)
                report.UserActivedId = user.Id;
            report.DateActived = DateTime.Now;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã duyệt" });
        }
        [HttpPost]
        public ActionResult Active2BM2507(int id, int type)
        {
            var report = db.Reports.FirstOrDefault(r => r.Id == id);
            if (type == 2)
            {
                report.IsActive2 = true;
                if (user != null)
                    report.UserActived2Id = user.Id;
                report.DateActived2 = DateTime.Now;
            }
            else if (type == 3)
            {
                report.IsActive3 = true;
                if (user != null)
                    report.UserActived3Id = user.Id;
                report.DateActived3 = DateTime.Now;
            }
            else if (type == 4)
            {
                report.IsActive4 = true;
                if (user != null)
                    report.UserActived4Id = user.Id;
                report.DateActived4 = DateTime.Now;
            }
            else if (type == 5)
            {
                report.IsActive5 = true;
                if (user != null)
                    report.UserActived5Id = user.Id;
                report.DateActived5 = DateTime.Now;
            }
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xác nhận" });
        }
        [FMSAuthorize]
        public ActionResult EditBM2507(int id)
        {
            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var users = db.Users.AsNoTracking() as IQueryable<User>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                users = users.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Users = users.ToList();
            ViewBag.Airports = airports.ToList();

            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.ReportDetail)
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .FirstOrDefault(r => r.Id == id);

            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;
            ViewBag.Report = report;
            ViewBag.CurrentUserId = currentUserId;

            if (report != null)
            {
                if (report.AirportId != null)
                    airportId = (int)report.AirportId;

                var fd = report.FromTime;
                var td = report.ToTime;

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var flights = BM2507Query(dr, airportId);
                ViewBag.ItemCount = flights.Count();
                list = flights.ToList();
            }
            return View(list);
        }
        [HttpPost]
        public ActionResult JDeleteBM2507(int id)
        {
            Report item = db.Reports.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        [FMSAuthorize]
        public ActionResult ExportBM2507Detail(int id)
        {
            int airportId = -1;
            var list = new List<Flight>();
            var report = db.Reports
                .Include(r => r.ReportDetail)
                .Include(r => r.UserCreated)
                .Include(r => r.UserActived)
                .Include(r => r.UserActived2)
                .Include(r => r.UserActived3)
                .Include(r => r.UserActived4)
                .Include(r => r.UserActived5)
                .FirstOrDefault(r => r.Id == id);
            if (report != null)
            {
                airportId = (int)report.AirportId;
                var isactive = report.IsActive ? "Đã duyệt" : "Chưa duyệt";
                var createdName = report.UserCreated != null ? report.UserCreated.FullName : "sa";
                var fromTime = report.FromTime;
                var toTime = report.ToTime;
                var dateCreated = report.DateCreated;
                var activedName = "";
                if (report.IsActive)
                    activedName = report.UserActived != null ? report.UserActived.FullName : "sa";

                var actived2Name = "";
                if (report.IsActive2 != null && (bool)report.IsActive2)
                    actived2Name = report.UserActived2 != null ? report.UserActived2.FullName : "sa";

                var actived3Name = "";
                if (report.IsActive3 != null && (bool)report.IsActive3)
                    actived3Name = report.UserActived3 != null ? report.UserActived3.FullName : "sa";

                var actived4Name = "";
                if (report.IsActive4 != null && (bool)report.IsActive4)
                    actived4Name = report.UserActived4 != null ? report.UserActived4.FullName : "sa";

                var actived5Name = "";
                if (report.IsActive5 != null && (bool)report.IsActive5)
                    actived5Name = report.UserActived5 != null ? report.UserActived5.FullName : "sa";

                var fd = report.FromTime;
                var td = report.ToTime;

                var name = "BM2507";
                string fileName = name + ".xlsx";

                string dr = fd.ToString("dd/MM/yyyy HH:mm") + "-" + td.ToString("dd/MM/yyyy HH:mm");
                var temp = BM2507Query(dr, airportId, id);

                var rf_items = temp.SelectMany(f => f.RefuelItems).GroupBy(gf => new { gf.DriverId, gf.TruckId }).Select(sf => new RefuelItem { Driver = db.Users.FirstOrDefault(u => u.Id == sf.Key.DriverId), Truck = db.Trucks.FirstOrDefault(t => t.Id == sf.Key.TruckId) });
                var opf_items = temp.SelectMany(f => f.RefuelItems).GroupBy(gf => new { gf.OperatorId, gf.TruckId }).Select(sf => new RefuelItem { Operator = db.Users.FirstOrDefault(u => u.Id == sf.Key.OperatorId), Truck = db.Trucks.FirstOrDefault(t => t.Id == sf.Key.TruckId) });

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
                        rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                        rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        //rng.AutoFitColumns();
                        rng.Style.WrapText = true;
                    }

                    ws1.Cells[1, 1, 1, 4].Style.Font.Bold = true;
                    ws1.Cells[1, 1, 1, 4].Merge = true;
                    ws1.Cells[1, 1, 1, 4].Value = "TỔNG HỢP KẾT QUẢ VÀ BÀN GIAO CA TRỰC";

                    //ws1.Cells[2, 1, 2, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[2, 1, 2, 4].Merge = true;
                    //ws1.Cells[2, 1, 2, 4].Value = "Thời gian trực: Từ " + fd.ToString("dd/MM/yyyy HH:mm") + " đến " + td.ToString("dd/MM/yyyy HH:mm");

                    //ws1.Cells[3, 1, 3, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[3, 1, 3, 4].Merge = true;
                    //ws1.Cells[3, 1, 3, 4].Value = "Người báo cáo: " + createdName;

                    //ws1.Cells[4, 1, 4, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[4, 1, 4, 4].Merge = true;
                    //ws1.Cells[4, 1, 4, 4].Value = "Trạng thái duyệt: " + isactive;

                    //ws1.Cells[5, 1, 5, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[5, 1, 5, 4].Merge = true;
                    //ws1.Cells[5, 1, 5, 4].Value = "Người duyệt: " + activedName;

                    //ws1.Cells[6, 1, 6, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[6, 1, 6, 4].Merge = true;
                    //ws1.Cells[6, 1, 6, 4].Value = "Bên giao(CA TRƯỞNG): " + actived2Name;

                    //ws1.Cells[7, 1, 7, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[7, 1, 7, 4].Merge = true;
                    //ws1.Cells[7, 1, 7, 4].Value = "Bên nhận(CA TRƯỞNG): " + actived3Name;

                    //ws1.Cells[8, 1, 8, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[8, 1, 8, 4].Merge = true;
                    //ws1.Cells[8, 1, 8, 4].Value = "Bên giao(CÁN BỘ TRỰC): " + actived4Name;

                    //ws1.Cells[9, 1, 9, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    //ws1.Cells[9, 1, 9, 4].Merge = true;
                    //ws1.Cells[9, 1, 9, 4].Value = "Bên nhận(CÁN BỘ TRỰC): " + actived5Name;



                    ws1.Cells[2, 1, 2, 4].Style.Font.Bold = true;
                    ws1.Cells[2, 1, 2, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[2, 1, 2, 4].Merge = true;
                    ws1.Cells[2, 1, 2, 4].Value = "I.TỔNG HỢP KẾT QUẢ CỦA CA TRỰC";

                    ws1.Cells[3, 1, 3, 4].Style.Font.Bold = true;
                    ws1.Cells[3, 1, 3, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[3, 1, 3, 4].Merge = true;
                    ws1.Cells[3, 1, 3, 4].Value = "Từ " + fd.ToString("dd / MM / yyyy HH: mm") + " đến " + td.ToString("dd / MM / yyyy HH: mm");

                    ws1.Cells[4, 1, 4, 4].Style.Font.Bold = true;
                    ws1.Cells[4, 1, 4, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[4, 1, 4, 4].Merge = true;
                    ws1.Cells[4, 1, 4, 4].Value = "1.Tổng hợp nhân sự";

                    ws1.Column(2).Width = 40;
                    ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = 35;
                    var rowfix = 5;

                    ws1.Row(rowfix).Style.Font.Bold = true;
                    ws1.Cells[rowfix, 1].Value = "TT";
                    ws1.Cells[rowfix, 2].Value = "CHỨC DANH NHÂN VIÊN";
                    ws1.Cells[rowfix, 3].Value = "SỐ HIỆU XE TRA NẠP";
                    ws1.Cells[rowfix, 4].Value = "GHI CHÚ";

                    int rowIndexBegin = 6;
                    int rowIndexCurrent = rowIndexBegin;
                    var index = 1;

                    ws1.Cells[rowIndexCurrent, 1].Value = "I";
                    ws1.Cells[rowIndexCurrent, 2].Value = "NHÂN VIÊN LÁI XE";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 3].Value = "";
                    ws1.Cells[rowIndexCurrent, 4].Value = "";
                    rowIndexCurrent++;


                    foreach (var ri in rf_items.Where(r => r.Driver != null).OrderBy(r => r.Driver.FullName))
                    {
                        ws1.Cells[rowIndexCurrent, 1].Value = index;
                        ws1.Cells[rowIndexCurrent, 2].Value = ri.Driver.FullName;
                        ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 3].Value = ri.Truck != null ? ri.Truck.Code : "---";
                        ws1.Cells[rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        var not = report.ReportDetail.Where(r => r.Type == REPORT_DETAIL_TYPE.DRIVER && r.Value1 == ri.Driver.Id.ToString() && r.Value2 == ri.Truck.Id.ToString()).FirstOrDefault();
                        ws1.Cells[rowIndexCurrent, 4].Value = not != null ? not.Value3 : "";
                        rowIndexCurrent++;
                        index++;
                    }


                    ws1.Cells[rowIndexCurrent, 1].Value = "II";
                    ws1.Cells[rowIndexCurrent, 2].Value = "NHÂN VIÊN KỸ THUẬT TRA NẠP";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 3].Value = "";
                    ws1.Cells[rowIndexCurrent, 4].Value = "";
                    rowIndexCurrent++;

                    index = 1;

                    foreach (var ri in opf_items.Where(r => r.Operator != null).OrderBy(r => r.Operator.FullName))
                    {
                        ws1.Cells[rowIndexCurrent, 1].Value = index;
                        ws1.Cells[rowIndexCurrent, 2].Value = ri.Operator.FullName;
                        ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        ws1.Cells[rowIndexCurrent, 3].Value = ri.Truck != null ? ri.Truck.Code : "---";
                        ws1.Cells[rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        var not = report.ReportDetail.Where(r => r.Type == REPORT_DETAIL_TYPE.OPERATOR && r.Value1 == ri.Operator.Id.ToString() && r.Value2 == ri.Truck.Id.ToString()).FirstOrDefault();
                        ws1.Cells[rowIndexCurrent, 4].Value = not != null ? not.Value3 : "";
                        rowIndexCurrent++;
                        index++;
                    }

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "2.Kết quả hoạt động trong ca";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "TT";
                    ws1.Cells[rowIndexCurrent, 2].Value = "NỘI DUNG";
                    ws1.Cells[rowIndexCurrent, 3].Value = "KẾT QUẢ";
                    ws1.Cells[rowIndexCurrent, 4].Value = "GHI CHÚ";
                    ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "1";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Tổng số chuyến bay";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "1.1";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay chuyên cơ";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(f => f.FlightCarry == FlightCarry.CCO).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "1.2";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay ngoài kế hoạch ngày";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "5" && p.ReportType == REPORT_TYPE.BM7009)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "5" && pr.ReportType == REPORT_TYPE.BM7009).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "2";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Chuyến bay chậm do Skypec";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "1" && p.ReportType == REPORT_TYPE.BM7009)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "1" && pr.ReportType == REPORT_TYPE.BM7009).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "3";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Hủy chuyến bay";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(f => f.Status == FlightStatus.CANCELED).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "4";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Sự cố/vụ việc trong tra nạp";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "7" && p.ReportType == REPORT_TYPE.BM7009)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "7" && pr.ReportType == REPORT_TYPE.BM7009).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "5";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Sự cố/vụ việc bị lập biên bản";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "8" && p.ReportType == REPORT_TYPE.BM7009)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "8" && pr.ReportType == REPORT_TYPE.BM7009).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "6";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Vi phạm việc tuân thủ quy trình, quy định";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "9" && p.ReportType == REPORT_TYPE.BM7009)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "9" && pr.ReportType == REPORT_TYPE.BM7009).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "7";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Thông tin phản hồi từ khách hàng";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "10" && p.ReportType == REPORT_TYPE.BM7009)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "10" && pr.ReportType == REPORT_TYPE.BM7009).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "8";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Tình trạng kỹ thuật phương tiện, trang thiết bị";
                    ws1.Cells[rowIndexCurrent, 3].Value = temp.Where(s => s.Properties.Any(p => p.Code == "13" && p.ReportType == REPORT_TYPE.BM2507)).Count();
                    ws1.Cells[rowIndexCurrent, 4].Value = string.Join("; ", temp.SelectMany(s => s.PropertyFlightValues.Where(pf => pf.Property_Id == db.Properties.FirstOrDefault(pr => pr.Code == "13" && pr.ReportType == REPORT_TYPE.BM2507).Id).Select(pf => pf.Note)).ToArray());
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "9";
                    ws1.Cells[rowIndexCurrent, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 2].Value = "Các bất thường khác";
                    ws1.Cells[rowIndexCurrent, 3].Value = report != null ? report.Value1 : "";
                    ws1.Cells[rowIndexCurrent, 4].Value = "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "3.Nguyên nhân và giải pháp liên quan đến chất lượng dịch vụ tra nạp (số chuyến phục vụ chậm giờ bay; sự cố/vụ việc; tình trạng kỹ thuật phương tiện, trang thiết bị;  khiếu nại từ khách hàng)";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = report != null ? report.Value2 : "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "4.Kiến nghị,đề xuất";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = report != null ? report.Value3 : "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "II.NỘI DUNG BÀN GIAO CA TRỰC";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "Từ " + fd.ToString("dd / MM / yyyy HH: mm") + " đến " + td.ToString("dd / MM / yyyy HH: mm");
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "1.Kế hoạch tra nạp theo biểu mẫu BM25.01//NLHK (lưu ý các chuyến bay của ca trước chưa thực hiện phải được ghi chú  và thông báo cho ca sau). ";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = report != null ? report.Value4 : "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "2.Các phương tiện trang thiết bị";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1].Value = "TT";
                    ws1.Cells[rowIndexCurrent, 2].Value = "SỐ HIỆU XE TRA NẠP";
                    ws1.Cells[rowIndexCurrent, 3].Value = "TÌNH TRẠNG KỸ THUẬT XE";
                    ws1.Cells[rowIndexCurrent, 4].Value = "DỤNG CỤ HÓA NGHIỆM THEO XE";
                    ws1.Cells[rowIndexCurrent, 5].Value = "GHI CHÚ";
                    ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                    rowIndexCurrent++;

                    index = 1;
                    if (report != null)
                    {
                        foreach (var ri in report.ReportDetail)
                        {
                            ws1.Cells[rowIndexCurrent, 1].Value = index;
                            ws1.Cells[rowIndexCurrent, 2].Value = ri.Value1;
                            ws1.Cells[rowIndexCurrent, 3].Value = ri.Value2 == "True" ? "X" : "";
                            ws1.Cells[rowIndexCurrent, 4].Value = ri.Value3 == "True" ? "X" : "";
                            ws1.Cells[rowIndexCurrent, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            ws1.Cells[rowIndexCurrent, 5].Value = ri.Value4;
                            rowIndexCurrent++;
                            index++;
                        }
                    }

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = "3.Các nội dung khác";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 4].Value = report != null ? report.Value9 : "";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 3].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 3].Merge = true;
                    ws1.Cells[rowIndexCurrent, 2, rowIndexCurrent, 3].Value = "CA TRƯỞNG";

                    ws1.Cells[rowIndexCurrent, 4, rowIndexCurrent, 5].Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 4, rowIndexCurrent, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws1.Cells[rowIndexCurrent, 4, rowIndexCurrent, 5].Merge = true;
                    ws1.Cells[rowIndexCurrent, 4, rowIndexCurrent, 5].Value = "CÁN BỘ TRỰC";
                    rowIndexCurrent++;

                    ws1.Cells[rowIndexCurrent, 2].Value = "Bên giao \r\n" + actived2Name;
                    ws1.Cells[rowIndexCurrent, 3].Value = "Bên nhận \r\n" + actived3Name;
                    ws1.Cells[rowIndexCurrent, 4].Value = "Bên giao \r\n" + actived4Name;
                    ws1.Cells[rowIndexCurrent, 5].Value = "Bên nhận \r\n" + actived5Name;

                    //if (report != null)
                    //{
                    //    int uid = Convert.ToInt32(report.Value5);
                    //    var user = db.Users.FirstOrDefault(u => u.Id == uid);
                    //    if (user != null)
                    //        ws1.Cells[rowIndexCurrent, 2].Value = "Bên giao \r\n" + user.FullName;

                    //    uid = Convert.ToInt32(report.Value6);
                    //    user = db.Users.FirstOrDefault(u => u.Id == uid);
                    //    if (user != null)
                    //        ws1.Cells[rowIndexCurrent, 3].Value = "Bên nhận \r\n" + user.FullName;

                    //    uid = Convert.ToInt32(report.Value7);
                    //    user = db.Users.FirstOrDefault(u => u.Id == uid);
                    //    if (user != null)
                    //        ws1.Cells[rowIndexCurrent, 4].Value = "Bên giao \r\n" + user.FullName;

                    //    uid = Convert.ToInt32(report.Value8);
                    //    user = db.Users.FirstOrDefault(u => u.Id == uid);
                    //    if (user != null)
                    //        ws1.Cells[rowIndexCurrent, 5].Value = "Bên nhận \r\n" + user.FullName;
                    //}
                    rowIndexCurrent++;
                    ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 5].Merge = true;
                    ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 5].Value = "BM25.07/NLHK";

                    package.SaveAs(newFile);
                }


                var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
                if (!System.IO.File.Exists(readFile))
                    return HttpNotFound();
                return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            return View();
        }
        ///BM2502
        ///
        public List<TruckFuel> BM2502Query(string daterange, int airportId = -1, int truckId = -1)
        {
            var truckFuels = db.TruckFuels
                .Include(t => t.Truck)
                .Include(t => t.Operator)
                .AsNoTracking() as IQueryable<TruckFuel>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            truckFuels = truckFuels.Where(f => DbFunctions.CreateDateTime(f.Time.Year, f.Time.Month, f.Time.Day, f.Time.Hour, f.Time.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.Time.Year, f.Time.Month, f.Time.Day, f.Time.Hour, f.Time.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );


            if (airportId > 0)
            {
                var t_ids = db.Trucks.Where(t => t.AirportId == airportId).Select(t => t.Id);
                truckFuels = truckFuels.Where(f => t_ids.Contains(f.TruckId));
            }
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                var t_ids = db.Trucks.Where(t => ids.Contains((int)t.AirportId)).Select(t => t.Id);
                truckFuels = truckFuels.Where(f => t_ids.Contains(f.TruckId));
            }
            if (truckId > 0)
                truckFuels = truckFuels.Where(t => t.TruckId == truckId);

            return truckFuels.OrderBy(t => t.Time).ToList();
        }
        [FMSAuthorize]
        public ActionResult BM2502List()
        {
            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var truckId = -1;
            if (!string.IsNullOrEmpty(Request["t"]))
                truckId = Convert.ToInt32(Request["t"]);

            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
            }

            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();
            return View(BM2502Query(Request["daterange"], airportId, truckId));
        }
        [FMSAuthorize]
        public ActionResult CreateBM2502()
        {
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            return View();
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBM2502([Bind(Include = "Time,TruckId,TankNo,TicketNo,Amount,MaintenanceStaff")] TruckFuel truckFuel)
        {
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            if (ModelState.IsValid)
            {
                if (user != null)
                    truckFuel.UserCreatedId = user.Id;

                db.TruckFuels.Add(truckFuel);
                db.SaveChanges();
                Response.Write("<script>window.open('" + (Request["returnUrl"] != null ? Request["returnUrl"].ToString() : "/Reports/BM2502List") + "','_parent');</script>");
                //return RedirectToAction("BM2502List");
            }

            return View(truckFuel);
        }
        [FMSAuthorize]
        public ActionResult EditBM2502(int? id)
        {
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            TruckFuel truckFuel = db.TruckFuels.Find(id);
            if (truckFuel == null)
                return HttpNotFound();

            return View(truckFuel);
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM2502([Bind(Include = "Id,Time,TruckId,TankNo,TicketNo,Amount,MaintenanceStaff")] TruckFuel truckFuel)
        {
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
            }
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            if (ModelState.IsValid)
            {
                var model = db.TruckFuels.FirstOrDefault(p => p.Id == truckFuel.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = truckFuel.Id;
                entityLog.EntityName = "TruckFuel";
                entityLog.EntityDisplay = "BM2502";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = "";
                if (user != null)
                {
                    model.UserUpdatedId = user.Id;
                    model.DateUpdated = DateTime.Now;
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }

                if (model.Time != truckFuel.Time)
                {
                    entityLog.PropertyName = "Time";
                    entityLog.OldValues = model.Time.ToString("dd/MM/yyyy HH:mm");
                    entityLog.NewValues = truckFuel.Time.ToString("dd/MM/yyyy HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.TruckId != truckFuel.TruckId)
                {
                    entityLog.PropertyName = "Xe tra nạp";
                    entityLog.OldValues = model.TruckId.ToString();
                    entityLog.NewValues = truckFuel.TruckId.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.TankNo != truckFuel.TankNo)
                {
                    entityLog.PropertyName = "TankNo";
                    entityLog.OldValues = model.TankNo;
                    entityLog.NewValues = truckFuel.TankNo;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.TicketNo != truckFuel.TicketNo)
                {
                    entityLog.PropertyName = "TicketNo";
                    entityLog.OldValues = model.TicketNo;
                    entityLog.NewValues = truckFuel.TicketNo;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.Amount != truckFuel.Amount)
                {
                    entityLog.PropertyName = "Amount";
                    entityLog.OldValues = model.Amount.ToString();
                    entityLog.NewValues = truckFuel.Amount.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.MaintenanceStaff != truckFuel.MaintenanceStaff)
                {
                    entityLog.PropertyName = "MaintenanceStaff";
                    entityLog.OldValues = model.MaintenanceStaff;
                    entityLog.NewValues = truckFuel.MaintenanceStaff;
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }


                TryUpdateModel(model);

                model.Time = truckFuel.Time;
                model.TruckId = truckFuel.TruckId;
                model.TankNo = truckFuel.TankNo;
                model.TicketNo = truckFuel.TicketNo;
                model.Amount = truckFuel.Amount;
                model.MaintenanceStaff = truckFuel.MaintenanceStaff;

                db.SaveChanges();
                Response.Write("<script>window.open('" + (Request["returnUrl"] != null ? Request["returnUrl"].ToString() : "/Reports/BM2502List") + "','_parent');</script>");
                //return RedirectToAction("BM2502List");
            }
            return View(truckFuel);
        }
        [HttpPost]
        public ActionResult ActiveBM2502(int id)
        {
            var truckFuel = db.TruckFuels.FirstOrDefault(r => r.Id == id);
            if (user != null)
                truckFuel.OperatorId = user.Id;
            else
                truckFuel.OperatorId = 0;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xác nhận" });

        }
        //[FMSAuthorize]
        //public ActionResult BM2502Detail(int? id)
        //{
        //    if (id == null)
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    TruckFuel truckFuel = db.TruckFuels.Find(id);
        //    if (truckFuel == null)
        //        return HttpNotFound();
        //    return View(truckFuel);
        //}
        [FMSAuthorize]
        public ActionResult BM2502Detail(string daterange, int truckId = -1)
        {
            var truck = db.Trucks.Include(t => t.CurrentAirport).FirstOrDefault(t => t.Id == truckId);
            if (truck != null)
            {
                ViewBag.TruckCode = truck.Code;
                if (truck.CurrentAirport != null)
                    ViewBag.AirportCode = truck.CurrentAirport.Code;
            }
            return View(BM2502Query(daterange, -1, truckId));
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult JDeleteBM2502(int id)
        {
            TruckFuel item = db.TruckFuels.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        [FMSAuthorize]
        public ActionResult ExportBM2502Detail(string daterange, int truckId = -1)
        {
            var truck = db.Trucks.Include(t => t.CurrentAirport).FirstOrDefault(t => t.Id == truckId);
            var name = "BM2502";
            string fileName = name + ".xlsx";

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }
            var temp = BM2502Query(daterange, -1, truckId);

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
                    rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    //rng.AutoFitColumns();
                    rng.Style.WrapText = true;
                }

                ws1.Cells[1, 1, 1, 9].Merge = true;
                ws1.Cells[1, 1, 1, 9].Value = "PHIẾU ĐỀ NGHỊ NHẬP SỐ LƯỢNG NHIÊN LIÊU JET A-1 VÀO XE TRA NẠP";

                ws1.Cells[2, 1, 2, 9].Merge = true;
                ws1.Cells[2, 1, 2, 9].Value = "Ngày " + fd.Day + " tháng " + td.Month + " năm " + td.Year;

                ws1.Cells[3, 1, 3, 9].Merge = true;
                ws1.Cells[3, 1, 3, 9].Value = "Xe tra nạp: " + truck.Code + " - Sân bay: " + truck.CurrentAirport.Code;

                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(2).Style.Font.Bold = true;
                ws1.Row(3).Style.Font.Bold = true;
                ws1.Row(4).Style.Font.Bold = true;

                ws1.Column(1).Width = 10;
                ws1.Column(2).Width = ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = 25;
                ws1.Column(6).Width = ws1.Column(7).Width = ws1.Column(8).Width = ws1.Column(9).Width = 25;
                var rowfix = 4;
                var colfix = 1;

                ws1.Cells[rowfix, colfix++].Value = "TT";
                ws1.Cells[rowfix, colfix++].Value = "Thời gian";
                ws1.Cells[rowfix, colfix++].Value = "Xuất bể số";
                ws1.Cells[rowfix, colfix++].Value = "Số phiếu xuất";
                ws1.Cells[rowfix, colfix++].Value = "Dung tích \r\n thực của xe tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Số nhiên liệu \r\n đã xuất cho tàu bay(L)";
                ws1.Cells[rowfix, colfix++].Value = "Số lượng \r\n nhiên liệu nhập(L)";
                ws1.Cells[rowfix, colfix++].Value = "Nhân viên tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Nhân viên bảo quản";

                int rowIndexBegin = 5;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                var date = DateTime.MinValue;

                var db = new DataContext();
                db.DisableFilter("IsNotDeleted");
                var db_Trucks = db.Trucks.Where(t => t.IsDeleted).ToList();
                var db_Users = db.Users.Where(t => t.IsDeleted).ToList();
                db.EnableFilter("IsNotDeleted");


                foreach (var item in temp)
                {
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Time.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TankNo;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.TicketNo;
                    ws1.Cells[rowIndexCurrent, col++].Value = @Math.Round(item.TruckCapacity).ToString("#,##0");
                    ws1.Cells[rowIndexCurrent, col++].Value = (item.AccumulateRefuelAmount != null ? Math.Round((decimal)item.AccumulateRefuelAmount).ToString("#,##0") : "0");
                    ws1.Cells[rowIndexCurrent, col++].Value = Math.Round(item.Amount).ToString("#,##0");
                    if (item.OperatorId >= 0)
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Operator != null ? item.Operator.FullName : "sa";
                    else
                        ws1.Cells[rowIndexCurrent, col++].Value = "";

                    ws1.Cells[rowIndexCurrent, col++].Value = item.MaintenanceStaff;

                    rowIndexCurrent++;
                }
                ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 9].Merge = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 9].Value = "BM25.02/NLHK";

                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Top.Color.SetColor(Color.Black);
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Left.Color.SetColor(Color.Black);
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Right.Color.SetColor(Color.Black);
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 9].Style.Border.Bottom.Color.SetColor(Color.Black);
                //ws1.Cells[6, 1, rowIndexCurrent, 15].AutoFitColumns();
                package.SaveAs(newFile);
            }


            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        public ActionResult JGetTruckByAirportId(int id)
        {
            var lst = db.Trucks.Where(t => t.AirportId == id);
            return PartialView("_TruckList", lst);
        }

        ////BM2505
        public List<BM2505> BM2505Query(string daterange, int airportId = -1, int truckId = -1)
        {
            var lst = db.BM2505s
                .Include(t => t.Flight)
                .Include(t => t.Truck)
                .Include(t => t.Operator)
                .AsNoTracking() as IQueryable<BM2505>;

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }

            lst = lst.Where(f => DbFunctions.CreateDateTime(f.Time.Year, f.Time.Month, f.Time.Day, f.Time.Hour, f.Time.Minute, 0)
                >= DbFunctions.CreateDateTime(fd.Year, fd.Month, fd.Day, fd.Hour, fd.Minute, 0)
                && DbFunctions.CreateDateTime(f.Time.Year, f.Time.Month, f.Time.Day, f.Time.Hour, f.Time.Minute, 0)
                <= DbFunctions.CreateDateTime(td.Year, td.Month, td.Day, td.Hour, td.Minute, 0)
                );


            if (airportId > 0)
            {
                var t_ids = db.Trucks.Where(t => t.AirportId == airportId).Select(t => t.Id);
                lst = lst.Where(f => t_ids.Contains(f.TruckId));
            }
            else if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                var t_ids = db.Trucks.Where(t => ids.Contains((int)t.AirportId)).Select(t => t.Id);
                lst = lst.Where(f => t_ids.Contains(f.TruckId));
            }
            if (truckId > 0)
                lst = lst.Where(t => t.TruckId == truckId);

            return lst.OrderBy(t => t.Time).ToList();
        }
        [FMSAuthorize]
        public ActionResult BM2505List()
        {
            var name = "sa";
            if (user != null)
                name = user.FullName;
            ViewBag.Name = name;

            var airportId = -1;
            if (!string.IsNullOrEmpty(Request["a"]))
                airportId = Convert.ToInt32(Request["a"]);

            var truckId = -1;
            if (!string.IsNullOrEmpty(Request["t"]))
                truckId = Convert.ToInt32(Request["t"]);

            // var flights = db.Flights.AsNoTracking() as IQueryable<Flight>;
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                // flights = flights.Where(a => ids.Contains((int)a.AirportId));
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
            }

            //ViewBag.Flights = flights.ToList();
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();
            return View(BM2505Query(Request["daterange"], airportId, truckId));
        }
        [FMSAuthorize]
        public ActionResult CreateBM2505()
        {
            var today = DateTime.Today;
            var flights = db.Flights.Where(f => f.RefuelScheduledTime.Value.Day == today.Day && f.RefuelScheduledTime.Value.Month == today.Month && f.RefuelScheduledTime.Value.Year == today.Year).AsNoTracking() as IQueryable<Flight>;
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var operators = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(a => ids.Contains((int)a.AirportId));
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
                operators = operators.Where(a => ids.Contains((int)a.AirportId));
            }

            ViewBag.Operators = operators.ToList();
            ViewBag.Flights = flights.ToList();
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            return View();
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBM2505([Bind(Include = "Time,TruckId,FlightId,Temperature,Density,Density15,DensityCheck,AppearanceCheck,WaterCheck,PressureDiff,HosePressure,OperatorId,TankNo,RTCNo")] BM2505 item)
        {
            var today = DateTime.Today;
            var flights = db.Flights.Where(f => f.RefuelScheduledTime.Value.Day == today.Day && f.RefuelScheduledTime.Value.Month == today.Month && f.RefuelScheduledTime.Value.Year == today.Year).AsNoTracking() as IQueryable<Flight>;
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var operators = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(a => ids.Contains((int)a.AirportId));
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
                operators = operators.Where(a => ids.Contains((int)a.AirportId));
            }

            ViewBag.Operators = operators.ToList();
            ViewBag.Flights = flights.ToList();
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            if (item.OperatorId == null && user != null)
                item.OperatorId = user.Id;
            //if (ModelState.IsValid)
            //{
            if (user != null)
                item.UserCreatedId = user.Id;

            db.BM2505s.Add(item);
            db.SaveChanges();
            Response.Write("<script>window.open('" + (Request["returnUrl"] != null ? Request["returnUrl"].ToString() : "/Reports/BM2505List") + "','_parent');</script>");
            //return RedirectToAction("BM2502List");
            //}

            return View(item);
        }
        [FMSAuthorize]
        public ActionResult EditBM2505(int? id)
        {
            var today = DateTime.Today;
            var flights = db.Flights.Where(f => f.RefuelScheduledTime.Value.Day == today.Day && f.RefuelScheduledTime.Value.Month == today.Month && f.RefuelScheduledTime.Value.Year == today.Year).AsNoTracking() as IQueryable<Flight>;
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var operators = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(a => ids.Contains((int)a.AirportId));
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
                operators = operators.Where(a => ids.Contains((int)a.AirportId));
            }

            ViewBag.Operators = operators.ToList();
            ViewBag.Flights = flights.ToList();
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            BM2505 item = db.BM2505s.FirstOrDefault(b => b.Id == id);
            if (item == null)
                return HttpNotFound();

            return View(item);
        }
        [FMSAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBM2505([Bind(Include = "Id,Time,TruckId,FlightId,Temperature,Density,Density15,DensityCheck,AppearanceCheck,WaterCheck,PressureDiff,HosePressure,OperatorId,TankNo,RTCNo")] BM2505 bm2505)
        {
            var today = DateTime.Today;
            var flights = db.Flights.Where(f => f.RefuelScheduledTime.Value.Day == today.Day && f.RefuelScheduledTime.Value.Month == today.Month && f.RefuelScheduledTime.Value.Year == today.Year).AsNoTracking() as IQueryable<Flight>;
            var trucks = db.Trucks.AsNoTracking() as IQueryable<Truck>;
            var airports = db.Airports.AsNoTracking() as IQueryable<Airport>;
            var operators = db.Users.AsNoTracking() as IQueryable<User>;

            if (!User.IsInRole("Super Admin") && user != null)
            {
                var ids = user.Airports.Select(a => a.Id);
                flights = flights.Where(a => ids.Contains((int)a.AirportId));
                airports = airports.Where(a => ids.Contains(a.Id));
                trucks = trucks.Where(a => ids.Contains((int)a.AirportId));
                operators = operators.Where(a => ids.Contains((int)a.AirportId));
            }

            ViewBag.Operators = operators.ToList();
            ViewBag.Flights = flights.ToList();
            ViewBag.Trucks = trucks.ToList();
            ViewBag.Airports = airports.ToList();

            if (ModelState.IsValid)
            {
                var model = db.BM2505s.FirstOrDefault(p => p.Id == bm2505.Id);
                //Save ChangeLog
                var entityLog = new ChangeLog();
                entityLog.EntityId = bm2505.Id;
                entityLog.EntityName = "BM2505";
                entityLog.EntityDisplay = "BM2505";
                entityLog.DateChanged = DateTime.Now;
                entityLog.KeyValues = "";
                if (user != null)
                {
                    model.UserUpdatedId = user.Id;
                    model.DateUpdated = DateTime.Now;
                    entityLog.UserUpdatedId = user.Id;
                    entityLog.UserUpdatedName = user.FullName;
                }

                if (model.Time != bm2505.Time)
                {
                    entityLog.PropertyName = "Time";
                    entityLog.OldValues = model.Time.ToString("dd/MM/yyyy HH:mm");
                    entityLog.NewValues = bm2505.Time.ToString("dd/MM/yyyy HH:mm");
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.TruckId != bm2505.TruckId)
                {
                    entityLog.PropertyName = "Xe tra nạp";
                    entityLog.OldValues = model.TruckId.ToString();
                    entityLog.NewValues = bm2505.TruckId.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }

                if (model.FlightId != bm2505.FlightId)
                {
                    entityLog.PropertyName = "Chuyến bay";
                    entityLog.OldValues = model.FlightId != null ? model.FlightId.ToString() : "";
                    entityLog.NewValues = bm2505.FlightId != null ? bm2505.FlightId.ToString() : "";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Temperature != bm2505.Temperature)
                {
                    entityLog.PropertyName = "Nhiệt độ thực tế";
                    entityLog.OldValues = model.Temperature.ToString();
                    entityLog.NewValues = bm2505.Temperature.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Density != bm2505.Density)
                {
                    entityLog.PropertyName = "Khối lượng riêng thực tế";
                    entityLog.OldValues = model.Density.ToString();
                    entityLog.NewValues = bm2505.Density.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.Density15 != bm2505.Density15)
                {
                    entityLog.PropertyName = "Khối lượng riêng 15 độ C";
                    entityLog.OldValues = model.Density15.ToString();
                    entityLog.NewValues = bm2505.Density15.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.DensityCheck != bm2505.DensityCheck)
                {
                    entityLog.PropertyName = "Đánh giá";
                    entityLog.OldValues = model.DensityCheck ? "P" : "F";
                    entityLog.NewValues = bm2505.DensityCheck ? "P" : "F";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.AppearanceCheck != bm2505.AppearanceCheck)
                {
                    entityLog.PropertyName = "Kiểm tra ngoại quan";
                    entityLog.OldValues = model.AppearanceCheck.ToString();
                    entityLog.NewValues = bm2505.AppearanceCheck.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.WaterCheck != bm2505.WaterCheck)
                {
                    entityLog.PropertyName = "Viên thử nước";
                    entityLog.OldValues = model.WaterCheck ? "P" : "F";
                    entityLog.NewValues = bm2505.WaterCheck ? "P" : "F";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.PressureDiff != bm2505.PressureDiff)
                {
                    entityLog.PropertyName = "Chênh lệch áp suất bầu lọc";
                    entityLog.OldValues = model.AppearanceCheck.ToString();
                    entityLog.NewValues = bm2505.PressureDiff.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.HosePressure != bm2505.HosePressure)
                {
                    entityLog.PropertyName = "Áp suất đầu vòi tra nạp";
                    entityLog.OldValues = model.HosePressure.ToString();
                    entityLog.NewValues = bm2505.HosePressure.ToString();
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                if (model.OperatorId != bm2505.OperatorId)
                {
                    entityLog.PropertyName = "Người kiểm tra";
                    entityLog.OldValues = model.OperatorId != null ? model.OperatorId.ToString() : "";
                    entityLog.NewValues = bm2505.OperatorId != null ? bm2505.OperatorId.ToString() : "";
                    db.ChangeLogs.Add(entityLog);
                    db.SaveChanges();
                }
                TryUpdateModel(model);

                model.Time = bm2505.Time;
                model.TruckId = bm2505.TruckId;
                model.FlightId = bm2505.FlightId;
                model.Temperature = bm2505.Temperature;
                model.Density = bm2505.Density;
                model.Density15 = bm2505.Density15;
                model.DensityCheck = bm2505.DensityCheck;
                model.AppearanceCheck = bm2505.AppearanceCheck;
                model.WaterCheck = bm2505.WaterCheck;
                model.PressureDiff = bm2505.PressureDiff;
                model.HosePressure = bm2505.HosePressure;
                model.TankNo = bm2505.TankNo;
                model.RTCNo = bm2505.RTCNo;

                if (bm2505.OperatorId == null && user != null)
                    model.OperatorId = user.Id;
                else
                    model.OperatorId = bm2505.OperatorId;

                db.SaveChanges();
                Response.Write("<script>window.open('" + (Request["returnUrl"] != null ? Request["returnUrl"].ToString() : "/Reports/BM2505List") + "','_parent');</script>");
                //return RedirectToAction("BM2502List");
            }
            return View(bm2505);
        }
        [FMSAuthorize]
        public ActionResult BM2505Detail(string daterange, int truckId = -1)
        {
            var truck = db.Trucks.Include(t => t.CurrentAirport).FirstOrDefault(t => t.Id == truckId);
            if (truck != null)
            {
                ViewBag.TruckCode = truck.Code;
                if (truck.CurrentAirport != null)
                    ViewBag.AirportCode = truck.CurrentAirport.Code;
            }
            return View(BM2505Query(daterange, -1, truckId));
        }
        [FMSAuthorize]
        [HttpPost]
        public ActionResult JDeleteBM2505(int id)
        {
            BM2505 item = db.BM2505s.Find(id);
            item.IsDeleted = true;
            item.DateDeleted = DateTime.Now;
            if (user != null)
                item.UserDeletedId = user.Id;
            db.SaveChanges();
            return Json(new { Status = 0, Message = "Đã xóa" });
        }
        [FMSAuthorize]
        public ActionResult ExportBM2505Detail(string daterange, int truckId = -1)
        {
            var truck = db.Trucks.Include(t => t.CurrentAirport).FirstOrDefault(t => t.Id == truckId);
            var name = "BM2505";
            string fileName = name + ".xlsx";

            var fd = DateTime.Today.AddHours(0).AddMinutes(0);
            var td = DateTime.Today.AddHours(23).AddMinutes(59);

            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy H:mm", CultureInfo.InvariantCulture);
                }
            }
            var temp = BM2505Query(daterange, -1, truckId);

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
                    rng.Style.Font.SetFromFont(new Font("Times New Roman", 13));
                    rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    //rng.AutoFitColumns();
                    rng.Style.WrapText = true;
                }

                ws1.Cells[1, 1, 1, 9].Merge = true;
                ws1.Cells[1, 1, 1, 9].Value = "PHIẾU KIỂM TRA CHẤT LƯỢNG NHIÊN LIÊU JET A-1 TRÊN XE TRA NẠP VÀ ĐỒNG HỒ ÁP SUẤT";

                ws1.Cells[2, 1, 2, 9].Merge = true;
                ws1.Cells[2, 1, 2, 9].Value = "Ngày " + fd.Day + " tháng " + td.Month + " năm " + td.Year;

                ws1.Cells[3, 1, 3, 9].Merge = true;
                ws1.Cells[3, 1, 3, 9].Value = "Xe tra nạp: " + truck.Code + " - Sân bay: " + truck.CurrentAirport.Code;

                ws1.Cells[4, 5, 4, 11].Merge = true;
                ws1.Cells[4, 5, 4, 11].Value = "Kết quả kiểm tra chất lượng nhiên liệu Jet A-1";

                ws1.Cells[5, 6, 5, 9].Merge = true;
                ws1.Cells[5, 6, 5, 9].Value = "Kiểm tra khối lượng riêng ";

                ws1.Cells[5, 10, 5, 11].Merge = true;
                ws1.Cells[5, 10, 5, 11].Value = "Kiểm tra khối lượng riêng ";

                ws1.Row(1).Style.Font.Bold = true;
                ws1.Row(2).Style.Font.Bold = true;
                ws1.Row(3).Style.Font.Bold = true;
                ws1.Row(4).Style.Font.Bold = true;
                ws1.Row(5).Style.Font.Bold = true;
                ws1.Row(6).Style.Font.Bold = true;

                ws1.Column(1).Width = 10;
                ws1.Column(2).Width = ws1.Column(3).Width = ws1.Column(4).Width = ws1.Column(5).Width = ws1.Column(6).Width = ws1.Column(7).Width = ws1.Column(8).Width = ws1.Column(9).Width = ws1.Column(10).Width = ws1.Column(11).Width = ws1.Column(12).Width = ws1.Column(13).Width = 20;
                ws1.Column(14).Width = 25;
                var rowfix = 6;
                var colfix = 1;

                ws1.Cells[rowfix, colfix++].Value = "TT";
                ws1.Cells[rowfix, colfix++].Value = "Thời gian";
                ws1.Cells[rowfix, colfix++].Value = "Chuyến bay/ \r\n bể cấp ";
                ws1.Cells[rowfix, colfix++].Value = "Tàu bay/ \r\n số CNKTCL";


                ws1.Cells[rowfix, colfix++].Value = "Nhiệt độ thực tế";
                ws1.Cells[rowfix, colfix++].Value = "Thực tế";
                ws1.Cells[rowfix, colfix++].Value = "Ở 15 độ C";
                ws1.Cells[rowfix, colfix++].Value = "Chênh lệch";
                ws1.Cells[rowfix, colfix++].Value = "Đánh giá";

                ws1.Cells[rowfix, colfix++].Value = "Kiểm tra ngoại quan";
                ws1.Cells[rowfix, colfix++].Value = "Viên thử nước";

                ws1.Cells[rowfix, colfix++].Value = "Chênh lệch áp \r\n suất bầu lọc";
                ws1.Cells[rowfix, colfix++].Value = "Áp suất đầu \r\n vòi tra nạp";
                ws1.Cells[rowfix, colfix++].Value = "Người kiểm tra";

                int rowIndexBegin = 7;
                int rowIndexCurrent = rowIndexBegin;
                var index = 1;
                var date = DateTime.MinValue;

                var db = new DataContext();
                db.DisableFilter("IsNotDeleted");
                var db_Trucks = db.Trucks.Where(t => t.IsDeleted).ToList();
                var db_Users = db.Users.Where(t => t.IsDeleted).ToList();
                db.EnableFilter("IsNotDeleted");


                foreach (var item in temp)
                {
                    var t = item.Density15 - item.Density;
                    var col = 1;
                    ws1.Cells[rowIndexCurrent, col++].Value = index++;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Time.ToString("HH:mm");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Flight != null ? item.Flight.Code : "---" + "\r\n" + item.TankNo;
                    ws1.Cells[rowIndexCurrent, col++].Value = (item.Flight != null && !string.IsNullOrEmpty(item.Flight.AircraftType)) ? item.Flight.AircraftCode : "---" + "\r\n" + item.RTCNo;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Temperature.ToString("#,##0.00");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Density.ToString("#,##0.0000");
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Density15 > 0 ? item.Density15.ToString("#,##0.0000") : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = item.Density15 > 0 ? t.ToString("#,##0.0000") : "";
                    ws1.Cells[rowIndexCurrent, col++].Value = item.DensityCheck ? "P" : "F";
                    ws1.Cells[rowIndexCurrent, col++].Value = item.AppearanceCheck;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.WaterCheck ? "P" : "F";
                    ws1.Cells[rowIndexCurrent, col++].Value = item.PressureDiff;
                    ws1.Cells[rowIndexCurrent, col++].Value = item.HosePressure;

                    if (item.OperatorId >= 0)
                        ws1.Cells[rowIndexCurrent, col++].Value = item.Operator != null ? item.Operator.FullName : "sa";
                    else
                        ws1.Cells[rowIndexCurrent, col++].Value = "";

                    rowIndexCurrent++;
                }

                ws1.Row(rowIndexCurrent).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                ws1.Row(rowIndexCurrent).Style.Font.Bold = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 14].Merge = true;
                ws1.Cells[rowIndexCurrent, 1, rowIndexCurrent, 14].Value = "BM25.05/NLHK";

                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Top.Color.SetColor(Color.Black);
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Left.Color.SetColor(Color.Black);
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Right.Color.SetColor(Color.Black);
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws1.Cells[rowIndexBegin, 1, rowIndexCurrent, 14].Style.Border.Bottom.Color.SetColor(Color.Black);
                //ws1.Cells[6, 1, rowIndexCurrent, 15].AutoFitColumns();
                package.SaveAs(newFile);
            }


            var readFile = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(filePath, fileName));
            if (!System.IO.File.Exists(readFile))
                return HttpNotFound();
            return File(readFile, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
    }
}