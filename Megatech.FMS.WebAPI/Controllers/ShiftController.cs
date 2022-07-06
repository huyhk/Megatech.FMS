using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class ShiftController : ApiController
    {
        private DataContext db = new DataContext();
        public IHttpActionResult GetShift(int direction = 0)
        {
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;

            var userName = ClaimsPrincipal.Current.Identity.Name;

            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            var airportId = user != null ? user.AirportId : 0;

            // DbFunctions.CreateTime(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            var date = DateTime.Now;

            var shiftModel = GetShift(date, airportId);
            /*if (direction != 0)
            {
                if (direction == -1)
                {
                    date = shiftModel.StartTime.AddMinutes(-10);
                }
                else if (direction == 1)
                    date = shiftModel.EndTime.AddMinutes(10);

                shiftModel = GetShift(date, airportId);

            }*/

            if (shiftModel != null)
            {
                var pre = db.Shifts.Where(s => s.AirportId == (int)airportId && DbFunctions.CreateDateTime(shiftModel.StartTime.Year, shiftModel.StartTime.Month, shiftModel.StartTime.Day, s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) <= shiftModel.StartTime).OrderByDescending(s => s.EndTime).FirstOrDefault();
                if (pre == null)
                {
                    var prevDay = shiftModel.StartTime.AddDays(-1);

                    pre = db.Shifts.Where(s => s.AirportId == (int)airportId && DbFunctions.CreateDateTime(prevDay.Year, prevDay.Month, prevDay.Day, s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) <= shiftModel.StartTime).OrderByDescending(s => s.EndTime).FirstOrDefault();
                }
                var next = db.Shifts.Where(s => s.AirportId == (int)airportId && DbFunctions.CreateDateTime(shiftModel.StartTime.Year, shiftModel.StartTime.Month, shiftModel.StartTime.Day, s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) >= shiftModel.EndTime).OrderBy(s => s.StartTime).FirstOrDefault();
                if (next == null)
                {
                    var nextDay = shiftModel.StartTime.AddDays(1);
                    next = db.Shifts.Where(s => s.AirportId == (int)airportId && DbFunctions.CreateDateTime(nextDay.Year, nextDay.Month, nextDay.Day, s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) >= shiftModel.EndTime).OrderBy(s => s.StartTime).FirstOrDefault();
                }
                if (pre != null)
                {
                    if (pre.StartTime.TimeOfDay >= shiftModel.StartTime.TimeOfDay)
                        pre.StartTime = shiftModel.StartTime.Date.AddDays(-1).Add(pre.StartTime.TimeOfDay);
                    else
                        pre.StartTime = shiftModel.StartTime.Date.Add(pre.StartTime.TimeOfDay);

                    if (pre.EndTime.TimeOfDay >= shiftModel.EndTime.TimeOfDay)
                        pre.EndTime = shiftModel.EndTime.Date.AddDays(-1).Add(pre.EndTime.TimeOfDay);
                    else
                        pre.EndTime = shiftModel.EndTime.Date.Add(pre.EndTime.TimeOfDay);

                    shiftModel.PrevShift = new ShiftViewModel
                    {
                        Name = pre.Name,
                        StartTime = pre.StartTime,
                        EndTime = pre.EndTime,
                        AiportId = pre.AirportId,
                        Date = DateTime.Today

                    };
                }
                else
                {
                    shiftModel.PrevShift = new ShiftViewModel { StartTime = shiftModel.StartTime.AddDays(-1), EndTime = shiftModel.EndTime.AddDays(-1), Name = shiftModel.Name };
                }
                if (next != null)
                {

                    if (next.StartTime.TimeOfDay <= shiftModel.StartTime.TimeOfDay)
                        next.StartTime = shiftModel.StartTime.Date.AddDays(1).Add(next.StartTime.TimeOfDay);

                    else
                        next.StartTime = shiftModel.StartTime.Date.Add(next.StartTime.TimeOfDay);

                    if (next.EndTime.TimeOfDay <= shiftModel.EndTime.TimeOfDay)
                        next.EndTime = shiftModel.EndTime.Date.AddDays(1).Add(next.EndTime.TimeOfDay);

                    else
                        next.EndTime = shiftModel.EndTime.Date.Add(next.EndTime.TimeOfDay);


                    shiftModel.NextShift = new ShiftViewModel
                    {
                        Name = next.Name,
                        StartTime = next.StartTime,
                        EndTime = next.EndTime,
                        AiportId = next.AirportId,
                        Date = DateTime.Today

                    };
                }
                else
                {
                    shiftModel.NextShift = new ShiftViewModel { StartTime = shiftModel.StartTime.AddDays(1), EndTime = shiftModel.EndTime.AddDays(1), Name = shiftModel.Name };
                }
            }

            return Ok(shiftModel);
        }

        private ShiftViewModel GetShift(DateTime date, int? airportId)
        {
            var now = date.TimeOfDay;


            var qshift = db.Shifts.Where(s => (s.StartTime < s.EndTime && DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)
                                            || (s.StartTime > s.EndTime && (DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) <= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) <= now
                                                                            || DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) >= now && DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) >= now)));

            qshift = qshift.Where(s => s.AirportId == airportId);


            var shift = qshift.FirstOrDefault();
            var start = date.Date;
            var end = date.Date;
            string name = "N/A";
            if (shift != null)
            {
                start = start.Add(shift.StartTime.TimeOfDay);
                if (start.TimeOfDay > now)
                    start = start.AddDays(-1);
                end = end.Add(shift.EndTime.TimeOfDay);
                if (end < start)
                    end = end.AddDays(1);
                name = shift.Name;
                var shiftModel = new ShiftViewModel
                {
                    Name = name,
                    StartTime = start,
                    EndTime = end,
                    AiportId = airportId,
                    Date = DateTime.Today

                };
                return shiftModel;
            }
            else
            {
                //get prev end as start
                var startT = db.Shifts.Where(s => s.AirportId == airportId)
                    .Where(s => DbFunctions.CreateTime(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second) < now)
                    .OrderByDescending(s => s.EndTime).FirstOrDefault();

                if (startT == null)
                    startT = db.Shifts.Where(s => s.AirportId == airportId)
                    .OrderByDescending(s => s.EndTime).FirstOrDefault();

                var endT = db.Shifts.Where(s => s.AirportId == airportId)
                    .Where(s => DbFunctions.CreateTime(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second) > now)
                    .OrderBy(s => s.StartTime).FirstOrDefault();

                if (endT == null)
                    endT = db.Shifts.Where(s => s.AirportId == airportId)
                    .OrderByDescending(s => s.StartTime).FirstOrDefault();
                if (startT != null && endT != null)
                {
                    end = date.Date.Add(endT.StartTime.TimeOfDay);
                    start = date.Date.Add(startT.EndTime.TimeOfDay);
                    if (start > date)
                        start = start.AddDays(-1);
                    if (end < date)
                        end = end.AddDays(1);
                }
                return new ShiftViewModel
                {
                    Name = name,
                    StartTime = start,
                    EndTime = end,
                    AiportId = airportId,
                    Date = DateTime.Today

                };
            }


        }
    }
}
