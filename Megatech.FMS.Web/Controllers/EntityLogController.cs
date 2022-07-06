using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using FMS.Data;
using System.Globalization;
using System.Collections.Generic;
using EntityFramework.DynamicFilters;

namespace Megatech.FMS.Web.Controllers
{
    public class EntityLogController : Controller
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
        public ActionResult Index(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var logs = db.ChangeLogs.AsNoTracking() as IQueryable<ChangeLog>;
            var count = logs.Count();

            if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
            {
                if (User.IsInRole("Quản lý chi nhánh"))
                {
                    var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                    logs = logs.Where(l => ids.Contains((int)l.UserUpdatedId));
                    count = logs.Count();
                }
                else
                {
                    logs = logs.Where(l => l.UserUpdatedId == user.Id);
                    count = logs.Count();
                }
            }

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var a = Request["a"].ToString();

                if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                {
                    logs = logs.Where(f => f.DateChanged.Day == fd.Day && f.DateChanged.Month == fd.Month && f.DateChanged.Year == fd.Year && f.EntityName == a).OrderByDescending(f => f.DateChanged);
                    count = logs.Count();
                }
                else
                {
                    td = td.AddHours(23);
                    logs = logs.Where(f => f.DateChanged >= fd && f.DateChanged <= td && f.EntityName == a).OrderByDescending(f => f.DateChanged);
                    count = logs.Count();
                }
            }
            else
            {
                if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                {
                    logs = logs.Where(f => f.DateChanged.Day == fd.Day && f.DateChanged.Month == fd.Month && f.DateChanged.Year == fd.Year).OrderByDescending(f => f.DateChanged);
                    count = logs.Count();
                }
                else
                {
                    td = td.AddHours(23);
                    logs = logs.Where(f => (DateTime)f.DateChanged >= fd && (DateTime)f.DateChanged <= td).OrderByDescending(f => f.DateChanged);
                    count = logs.Count();
                }
            }
            if (!string.IsNullOrEmpty(Request["keyword"]))
            {
                var key = Request["keyword"];
                logs = logs.Where(f => f.OldValues.Contains(key)
                || f.KeyValues.Contains(key)
                || f.NewValues.Contains(key));
                count = logs.Count();
            }
            logs = logs.OrderByDescending(f => f.DateChanged).Skip((p - 1) * pageSize).Take(pageSize);
            var list = logs.ToList();
            ViewBag.ItemCount = count;

            return View(list);
        }
        public ActionResult LogCreate(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var count = 0;
            var logs = new List<ChangeLogCreate>();

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }
            var key = "";
            if (!string.IsNullOrEmpty(Request["keyword"]))
                key = Request["keyword"];

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var a = Request["a"].ToString();
                if (a == "flight")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => (f.CreateType == 0 || f.CreateType == null) && f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "airport")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airports.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airports.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sân bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airports.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airports.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sân bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airports.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airports.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sân bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airports.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airports.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sân bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airports.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airports.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sân bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airports.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airports.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sân bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "airline")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airlines.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airlines.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Hãng bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airlines.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airlines.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Hãng bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airlines.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airlines.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Hãng bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airlines.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airlines.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Hãng bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airlines.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airlines.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Hãng bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airlines.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Airlines.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Hãng bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "aircraft")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Aircrafts.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Aircrafts.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Tàu bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Aircrafts.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Aircrafts.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Tàu bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Aircrafts.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Aircrafts.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Tàu bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Aircrafts.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Aircrafts.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Tàu bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Aircrafts.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Aircrafts.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Tàu bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Aircrafts.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Aircrafts.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Tàu bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "truck")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Trucks.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Trucks.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Xe tra nạp",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Trucks.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Trucks.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Xe tra nạp",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Trucks.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Trucks.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Xe tra nạp",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Trucks.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Trucks.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Xe tra nạp",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Trucks.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Trucks.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Xe tra nạp",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Trucks.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Trucks.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Xe tra nạp",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "parkinglot")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.ParkingLots.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.ParkingLots.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Bãi đỗ",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.ParkingLots.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.ParkingLots.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Bãi đỗ",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.ParkingLots.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.ParkingLots.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Bãi đỗ",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.ParkingLots.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.ParkingLots.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Bãi đỗ",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.ParkingLots.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.ParkingLots.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Bãi đỗ",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.ParkingLots.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.ParkingLots.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Bãi đỗ",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "product")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }

                }
                else if (a == "productprice")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Giá sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Giá sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Giá sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Giá sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Giá sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Products.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Giá sản phẩm",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
            }
            else
            {
                if (User.IsInRole("Quản lý chi nhánh") && user != null)
                {
                    var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
                else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
                else
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.DateCreated >= fd && f.DateCreated <= td)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
            }

            logs = logs.OrderByDescending(f => f.DateChanged).Skip((p - 1) * pageSize).Take(pageSize).ToList();
            var list = logs.ToList();
            ViewBag.ItemCount = count;

            return View(list);
        }
        public ActionResult LogImport(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var count = 0;
            var logs = new List<ChangeLogCreate>();

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }
            var key = "";
            if (!string.IsNullOrEmpty(Request["keyword"]))
                key = Request["keyword"];

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var a = Request["a"].ToString();
                if (a == "flight")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }      
            }
            else
            {
                if (User.IsInRole("Quản lý chi nhánh") && user != null)
                {
                    var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
                else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
                else
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
            }

            logs = logs.OrderByDescending(f => f.DateChanged).Skip((p - 1) * pageSize).Take(pageSize).ToList();
            var list = logs.ToList();
            ViewBag.ItemCount = count;

            return View(list);
        }
        public ActionResult LogCopy(int p = 1)
        {
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var count = 0;
            var logs = new List<ChangeLogCreate>();

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }
            var key = "";
            if (!string.IsNullOrEmpty(Request["keyword"]))
                key = Request["keyword"];

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var a = Request["a"].ToString();
                if (a == "flight")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                            if (!string.IsNullOrEmpty(key))
                                logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                        select new ChangeLogCreate
                                        {
                                            DateChanged = f.DateCreated,
                                            EntityDisplay = "Chuyến bay",
                                            KeyValues = f.Code,
                                            UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                        }).ToList();
                            count = logs.Count();
                        }
                    }
                }
            }
            else
            {
                if (User.IsInRole("Quản lý chi nhánh") && user != null)
                {
                    var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.CreateType == 1 && f.DateCreated >= fd && f.DateCreated <= td && ids.Contains((int)f.UserCreatedId) && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
                else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && f.UserCreatedId == user.Id && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
                else
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated.Day == fd.Day && f.DateCreated.Month == fd.Month && f.DateCreated.Year == fd.Year && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateCreated,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                }).ToList();
                        if (!string.IsNullOrEmpty(key))
                            logs = (from f in db.Flights.Where(f => f.CreateType == 2 && f.DateCreated >= fd && f.DateCreated <= td && f.Code.Contains(key))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateCreated,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserCreatedId).UserName : ""
                                    }).ToList();
                        count = logs.Count();
                    }
                }
            }

            logs = logs.OrderByDescending(f => f.DateChanged).Skip((p - 1) * pageSize).Take(pageSize).ToList();
            var list = logs.ToList();
            ViewBag.ItemCount = count;

            return View(list);
        }
        public ActionResult LogDelete(int p = 1)
        {
            db.DisableFilter("IsNotDeleted");
            int pageSize = 20;
            if (Request["pageSize"] != null)
                pageSize = Convert.ToInt32(Request["pageSize"]);
            if (Request["pageIndex"] != null)
                p = Convert.ToInt32(Request["pageIndex"]);

            var count = 0;
            var logs = new List<ChangeLogCreate>();

            var fd = DateTime.Today;
            var td = DateTime.Today;
            var daterange = Request["daterange"];
            if (daterange != null)
            {
                var range = daterange.Split('-');
                fd = DateTime.ParseExact(range[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (range.Length > 1)
                {
                    td = DateTime.ParseExact(range[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrEmpty(Request["a"]))
            {
                var a = Request["a"].ToString();
                if (a == "flight")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted >= fd && f.DateDeleted <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Chuyến bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "airport")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airports.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airports.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airports.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airports.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airports.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airports.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sân bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "airline")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airlines.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airlines.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airlines.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airlines.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Airlines.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Airlines.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Hãng bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "aircraft")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Aircrafts.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Aircrafts.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Aircrafts.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Aircrafts.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Aircrafts.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Aircrafts.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Tàu bay",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "truck")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Trucks.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Trucks.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Trucks.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Trucks.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Trucks.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Trucks.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Xe tra nạp",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "parkinglot")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.ParkingLots.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.ParkingLots.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.ParkingLots.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.ParkingLots.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.ParkingLots.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.ParkingLots.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Bãi đỗ",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
                else if (a == "product")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }

                }
                else if (a == "productprice")
                {
                    if (User.IsInRole("Quản lý chi nhánh") && user != null)
                    {
                        var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                    else
                    {
                        if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                        {
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                        else
                        {
                            td = td.AddHours(23);
                            logs = (from f in db.Products.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                    select new ChangeLogCreate
                                    {
                                        DateChanged = f.DateDeleted.Value,
                                        EntityDisplay = "Giá sản phẩm",
                                        KeyValues = f.Code,
                                        UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                    }).ToList();
                            count = logs.Count();
                        }
                    }
                }
            }
            else
            {
                if (User.IsInRole("Quản lý chi nhánh") && user != null)
                {
                    var ids = db.Users.Where(u => u.AirportId == user.AirportId).Select(u => u.Id);
                    var t = ids.ToArray();
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && ids.Contains((int)f.UserDeletedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateDeleted.Value,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && ids.Contains((int)f.UserDeletedId))
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateDeleted.Value,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                }).ToList();
                        count = logs.Count();
                    }
                }
                else if (!User.IsInRole("Administrators") && !User.IsInRole("Super Admin") && !User.IsInRole("Quản lý tổng công ty") && user != null)
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year && f.UserDeletedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateDeleted.Value,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td && f.UserDeletedId == user.Id)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateDeleted.Value,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                }).ToList();
                        count = logs.Count();
                    }
                }
                else
                {
                    if (fd.Day == td.Day && fd.Month == td.Month && fd.Year == td.Year)
                    {
                        logs = (from f in db.Flights.Where(f => f.IsDeleted && f.DateDeleted.Value.Day == fd.Day && f.DateDeleted.Value.Month == fd.Month && f.DateDeleted.Value.Year == fd.Year)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateDeleted.Value,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                }).ToList();
                        count = logs.Count();
                    }
                    else
                    {
                        td = td.AddHours(23);
                        logs = (from f in db.Flights.Where(f => f.DateDeleted.Value >= fd && f.DateDeleted.Value <= td)
                                select new ChangeLogCreate
                                {
                                    DateChanged = f.DateDeleted.Value,
                                    EntityDisplay = "Chuyến bay",
                                    KeyValues = f.Code,
                                    UserUpdatedName = db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId) != null ? db.Users.FirstOrDefault(u => u.Id == (int)f.UserDeletedId).UserName : ""
                                }).ToList();
                        count = logs.Count();
                    }
                }
            }

            logs = logs.OrderByDescending(f => f.DateChanged).Skip((p - 1) * pageSize).Take(pageSize).ToList();
            var list = logs.ToList();
            ViewBag.ItemCount = count;

            return View(list);
        }
    }
}