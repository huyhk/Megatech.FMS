using ExcelDataReader;
using FMS.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace Megatech.FMS.Web
{
    public enum FlightFileType
    {
        MN,
        MB,
        SKYPEC
    }
    public enum FlightFileMode
    {
        Mode1,
        Mode2,
        Mode3,
        Mode4,
        Mode5
    }

    public class FlightImporter
    {
        public static IList<Flight> ImportFile(string fileName, FlightFileType fileType = FlightFileType.MN)
        {
            //if (fileType == FlightFileType.MN)
            //    return ImportFromXml(fileName);
            //else
            return ImportFromExcel(fileName);
        }

        public static IList<Flight> ImportFromExcel(string fileName, FlightFileMode fileMode = FlightFileMode.Mode1, string setPlanDay = "", string airportCode = "")
        {
            var list = new List<Flight>();
            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var depth = 0;
                    if (fileMode == FlightFileMode.Mode5)
                        depth = 6;
                    else if(fileMode == FlightFileMode.Mode3)
                        depth = 1;
                    var dataset = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true,
                            //ReadHeaderRow = (rowReader) =>
                            //{
                            //    // F.ex skip the first row and use the 2nd row as column headers:
                            //    rowReader.Read();
                            //},
                            //Skip the row headers
                            FilterRow = rowReader => rowReader.Depth > depth
                        }
                    });
                    var table = dataset.Tables[0];
                    foreach (DataRow row in table.Rows)
                    {
                        Flight f = new Flight();
                        //f.Aircraft = new Aircraft { AircraftType = row[1].ToString(), Code = row[2].ToString() };
                        try
                        {
                            if (fileMode == FlightFileMode.Mode1 && !string.IsNullOrEmpty(row[3].ToString()))
                            {
                                f.AircraftType = row[1].ToString();
                                f.AircraftCode = row[2].ToString();
                                f.Code = row[3].ToString();
                                f.RouteName = row[4].ToString();

                                if (row[5] is decimal || row[5] is double)
                                    f.EstimateAmount = Convert.ToDecimal(row[5]);

                                if (row[6] is DateTime)
                                    f.ArrivalScheduledTime = (DateTime)row[6];
                                //f.ArrivalScheduledTime = DateTime.Today.AddHours(f.ArrivalScheduledTime.Value.Hour).AddMinutes(f.ArrivalScheduledTime.Value.Minute);
                                if (row[7] is DateTime)
                                    f.DepartureScheduledTime = (DateTime)row[7];
                                //f.DepartureScheduledTime = DateTime.Today.AddHours(f.DepartureScheduledTime.Value.Hour).AddMinutes(f.DepartureScheduledTime.Value.Minute);
                                if (row[8] is DateTime)
                                    f.RefuelScheduledTime = (DateTime)row[8];
                                //f.RefuelScheduledTime = DateTime.Today.AddHours(f.RefuelScheduledTime.Value.Hour).AddMinutes(f.RefuelScheduledTime.Value.Minute);

                                f.Parking = row[9].ToString();
                                f.TruckName = row[10].ToString();
                                //f.DriverName = row[11].ToString();
                                //f.TechnicalerName = row[12].ToString();
                                //f.Shift = row[13].ToString();
                                //f.ShiftStartTime = (DateTime)row[14];
                                //f.ShiftEndTime = (DateTime)row[15];
                                //f.AirportName = row[16].ToString();
                            }
                            else if (fileMode == FlightFileMode.Mode2 && !string.IsNullOrEmpty(row[3].ToString()))
                            {
                                f.AircraftType = row[1].ToString();
                                f.AircraftCode = row[2].ToString();
                                f.Code = row[3].ToString();
                                f.RouteName = row[4].ToString();

                                if (row[5] is decimal || row[5] is double)
                                    f.EstimateAmount = Convert.ToDecimal(row[5]);

                                var h = "00";
                                var m = "00";
                                if (row[6] is DateTime && !string.IsNullOrEmpty(row[7].ToString()))
                                {
                                    var day = (DateTime)row[6];
                                    var time = row[7].ToString().Trim();

                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length == 4)
                                    {
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[2].ToString() + charArray[3].ToString();
                                    }
                                    else
                                    {
                                        h = "0" + charArray[0].ToString();
                                        m = charArray[1].ToString() + charArray[2].ToString();
                                    }
                                    f.ArrivalScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                if (row[8] is DateTime && !string.IsNullOrEmpty(row[9].ToString()))
                                {
                                    var day = (DateTime)row[8];
                                    var time = row[9].ToString().Trim();

                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length == 4)
                                    {
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[2].ToString() + charArray[3].ToString();
                                    }
                                    else
                                    {
                                        h = "0" + charArray[0].ToString();
                                        m = charArray[1].ToString() + charArray[2].ToString();
                                    }
                                    f.DepartureScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                if (row[10] is DateTime && !string.IsNullOrEmpty(row[11].ToString()))
                                {
                                    var day = (DateTime)row[10];
                                    var time = row[11].ToString().Trim();
                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length == 4)
                                    {
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[2].ToString() + charArray[3].ToString();
                                    }
                                    else
                                    {
                                        h = "0" + charArray[0].ToString();
                                        m = charArray[1].ToString() + charArray[2].ToString();
                                    }
                                    f.RefuelScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                f.Parking = row[12].ToString();
                            }
                            else if (fileMode == FlightFileMode.Mode3 && !string.IsNullOrEmpty(row[4].ToString()))
                            {
                                f.AircraftType = row[1].ToString();
                                f.AircraftCode = row[2].ToString();
                                f.Code = row[4].ToString();
                                if (!string.IsNullOrEmpty(airportCode))
                                    f.RouteName = airportCode + "-" + row[5].ToString();
                                else f.RouteName = row[5].ToString();

                                if (row[6] is decimal || row[6] is double)
                                    f.EstimateAmount = Convert.ToDecimal(row[6]);

                                var h = "00";
                                var m = "00";
                                if (!string.IsNullOrEmpty(row[8].ToString()))
                                {
                                    //var day = (DateTime)row[7];
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[8].ToString().Trim();

                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length == 4)
                                    {
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[2].ToString() + charArray[3].ToString();
                                    }
                                    else
                                    {
                                        h = "0" + charArray[0].ToString();
                                        m = charArray[1].ToString() + charArray[2].ToString();
                                    }
                                    f.ArrivalScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                if (!string.IsNullOrEmpty(row[10].ToString()))
                                {
                                    //var day = (DateTime)row[9];
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[10].ToString().Trim();

                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length == 4)
                                    {
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[2].ToString() + charArray[3].ToString();
                                    }
                                    else
                                    {
                                        h = "0" + charArray[0].ToString();
                                        m = charArray[1].ToString() + charArray[2].ToString();
                                    }
                                    f.DepartureScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                if (!string.IsNullOrEmpty(row[12].ToString()))
                                {
                                    //var day = (DateTime)row[11];
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[12].ToString().Trim();
                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length == 4)
                                    {
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[2].ToString() + charArray[3].ToString();
                                    }
                                    else
                                    {
                                        h = "0" + charArray[0].ToString();
                                        m = charArray[1].ToString() + charArray[2].ToString();
                                    }
                                    f.RefuelScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                f.Parking = row[16].ToString();
                            }
                            else if (fileMode == FlightFileMode.Mode4 && !string.IsNullOrEmpty(row[4].ToString()))
                            {
                                f.AircraftType = row[2].ToString();
                                f.AircraftCode = row[3].ToString();
                                f.Code = row[4].ToString();
                                f.RouteName = row[5].ToString();

                                var h = "00";
                                var m = "00";

                                if (!string.IsNullOrEmpty(row[9].ToString()))
                                {
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[9].ToString().Trim();
                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length >= 5)
                                    {
                                        if (charArray.Length == 6)
                                            day = day.AddDays(1);
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[3].ToString() + charArray[4].ToString();
                                    }
                                    f.ArrivalScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                    f.RefuelScheduledTime = f.ArrivalScheduledTime;
                                }
                                else if (!string.IsNullOrEmpty(row[6].ToString()))
                                {
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[6].ToString().Trim();
                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length >= 5)
                                    {
                                        if (charArray.Length == 6)
                                            day = day.AddDays(1);
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[3].ToString() + charArray[4].ToString();
                                    }
                                    //f.ArrivalScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                    //f.ArrivalScheduledTime = f.ArrivalScheduledTime.Value.AddHours(-1);
                                    var temp = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                    f.RefuelScheduledTime = temp.AddHours(-1);
                                }

                                if (!string.IsNullOrEmpty(row[6].ToString()))
                                {
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[6].ToString().Trim();

                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length >= 5)
                                    {
                                        if (charArray.Length == 6)
                                            day = day.AddDays(1);
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[3].ToString() + charArray[4].ToString();
                                    }
                                    f.DepartureScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                f.Parking = row[8].ToString();
                            }
                            else if (fileMode == FlightFileMode.Mode5 && !string.IsNullOrEmpty(row[3].ToString()))
                            {
                                f.AircraftType = row[1].ToString();
                                f.AircraftCode = "";
                                f.Code = row[3].ToString();
                                f.RouteName = row[4].ToString();

                                var h = "00";
                                var m = "00";

                                if (!string.IsNullOrEmpty(row[5].ToString()))
                                {
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[5].ToString().Trim();
                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length >= 5)
                                    {
                                        if (charArray.Length == 6)
                                            day = day.AddDays(1);
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[3].ToString() + charArray[4].ToString();
                                    }
                                    f.ArrivalScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }
                                
                                if (!string.IsNullOrEmpty(row[7].ToString()))
                                {
                                    var day = Convert.ToDateTime(setPlanDay);
                                    var time = row[7].ToString().Trim();

                                    char[] charArray = time.ToCharArray();
                                    if (charArray.Length >= 5)
                                    {
                                        if (charArray.Length == 6)
                                            day = day.AddDays(1);
                                        h = charArray[0].ToString() + charArray[1].ToString();
                                        m = charArray[3].ToString() + charArray[4].ToString();
                                    }
                                    f.DepartureScheduledTime = day.AddHours(Convert.ToInt32(h)).AddMinutes(Convert.ToInt32(m));
                                }

                                f.Parking = row[11].ToString();
                            }
                        }
                        catch (Exception e)
                        {
                            var mess = e.Message;
                        }
                        f.RepairDateTime();
                        list.Add(f);
                    }
                    
                }
            }
            return list;
        }

        private static IList<Flight> ImportFromXml(string fileName)
        {
            var list = new List<Flight>();


            var dataset = XMLtoDataTable.ImportExcelXML(fileName, true, 5, false);
            var table = dataset.Tables[0];
            foreach (DataRow row in table.Rows)
            {
                if (!string.IsNullOrEmpty(row[4].ToString()))
                {
                    Flight f = new Flight();
                    //f.Aircraft = new Aircraft { AircraftType = row[1].ToString(), Code = row[2].ToString() };
                    f.AircraftCode = row[2].ToString();
                    f.AircraftType = row[1].ToString();
                    f.Code = row[3].ToString();
                    f.RouteName = row[4].ToString();
                    f.EstimateAmount = Convert.ToDecimal(row[12]);
                    //f.ArrivalScheduledTime = ((DateTime)row[7]);
                    //f.ArrivalScheduledTime = DateTime.Today.AddHours(f.ArrivalScheduledTime.Hour).AddMinutes(f.ArrivalScheduledTime.Minute);
                    f.DepartureScheduledTime = DateTime.ParseExact(row[5].ToString(), "HH:mm", CultureInfo.InvariantCulture);
                    f.DepartureScheduledTime = DateTime.Today.AddHours(f.DepartureScheduledTime.Value.Hour).AddMinutes(f.DepartureScheduledTime.Value.Minute);
                    f.Parking = row[10].ToString();
                    f.RefuelTime = DateTime.ParseExact(row[6].ToString(), "HH:mm", CultureInfo.InvariantCulture);
                    list.Add(f);
                }
            }


            return list;
        }
        public static string ChangeDate(string date)
        {
            var temp = date.Split('/');
            if (temp.Length > 0)
                return temp[1] + "/" + temp[0] + "/" + temp[2];
            else return date;
        }
    }
}