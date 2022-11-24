using FMS.Data;
using Megatech.FMS.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Megatech.FMS.DataExchange
{
    public class FlightImporter
    {
        private static string IMPORT_FLIGHT_URL = ConfigurationManager.AppSettings["IMPORT_FLIGHT_URL"] ?? "http://fms.vietnamairlines.com:8699/GetFlight.asmx";
        private static bool running = false;
        public static void Import(DateTime? date)
        {
            if (running) return;
            running = true;
            if (date == null)
                date = DateTime.Today;
            Logger.AppendLog("FLIGTH-IMPORT", "Started import", "flight-import");
            var url = IMPORT_FLIGHT_URL;
            var soapXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" >         
                    <soap:Body >          
                        <Get_Flight_SP xmlns=""http://tempuri.org/"">
                        <fDate>{date.Value.AddDays(-1):yyyy-MM-dd}</fDate>
                        <tDate>{date:yyyy-MM-dd}</tDate>
                        <User>SKYPEC01</User>
                        <Key>SKYPEC2021</Key>
                        </Get_Flight_SP>
                    </soap:Body>
                </soap:Envelope>";

            var result = PostSOAPRequestAsync(url, soapXml).ContinueWith(d =>
            {

                var content = d.Result;

                content = Regex.Replace(content, @"<\/?soap.*?>|<\/?Get_Flight_SPResponse.*?>|xsi.*?[\s]", "");

                XmlSerializer xml = new XmlSerializer(typeof(FlightImportModel));
                using (var db = new DataContext())
                using (TextReader reader = new StringReader(content))
                {
                    var data = (FlightImportModel)xml.Deserialize(reader);
                    Logger.AppendLog("FLIGHT-DATA", $"Row counts: {data.Items.Count}", "flight-import");

                    foreach (var item in data.Items)
                    {
                        item.ArrivalScheduledTime = item.ArrivalScheduledTime.ToLocalTime();
                        item.ArrivalTime = item.ArrivalTime.ToLocalTime();
                        item.DepartureScheduledTime = item.DepartureScheduledTime.ToLocalTime();
                        item.DepartureTime = item.DepartureTime.ToLocalTime();

                    }
                    var todayList = data.Items.Where(it => it.DepartureScheduledTime >= date).OrderBy(it => it.DepartureTime).ToList();
                    foreach (var item in todayList)
                    {
                        var ac = item.AircraftCode;
                        var lastF = data.Items.OrderByDescending(it => it.ArrivalTime).FirstOrDefault(it => it.AircraftCode == ac && it.ArrivalTime < item.ArrivalTime);
                        var flight = db.Flights.FirstOrDefault(f => f.AircraftCode == ac
                            && f.RefuelScheduledTime > lastF.ArrivalTime
                            && f.RefuelScheduledTime <= item.DepartureScheduledTime
                            && f.RouteName.EndsWith(item.FromAirport));
                        Logger.AppendLog("FLIGHT-DATA", $"Flight No: {item.FN_Carrier} {item.FN_Number} Aircraft No: {ac} Last arrive: {lastF.ArrivalTime} departure {item.DepartureScheduledTime}", "flight-import");
                        if (flight != null)
                        {
                            flight.LegNo = item.Leg_No;
                            flight.LegUpdateNo = item.Leg_Update_No;
                           
                        }
                    }
                }

                running = false;
            });
            Logger.AppendLog("FLIGTH-IMPORT", "Ended import", "flight-import");
        }
        private static async Task<string> PostSOAPRequestAsync(string url, string text)
        {
            try
            {

                using (HttpContent content = new StringContent(text, Encoding.UTF8, "text/xml"))
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                using (HttpClient client = new HttpClient())
                {
                    request.Headers.Add("SOAPAction", "http://tempuri.org/Get_Flight_SP");
                    request.Content = content;
                    using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        Logger.AppendLog("POST", $"Status code: {response.StatusCode}", "flight-import");
                        //response.EnsureSuccessStatusCode(); // throws an Exception if 404, 500, etc.
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "flight-import");
                throw;
            }
        }
    }
}
