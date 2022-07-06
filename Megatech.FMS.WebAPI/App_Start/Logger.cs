using System;
using System.IO;
using System.Web.Hosting;


namespace Megatech.FMS.WebAPI.App_Start
{
    public class Logger
    {
        public static void AppendLog(string tag, string log)
        {
            AppendLog(tag, log, "post");
        }
        public static void AppendLog(string tag, string log, string fileName)
        {
            try
            {

                Logging.Logger.SetPath(HostingEnvironment.MapPath("~/logs"));
                Logging.Logger.AppendLog(tag, log, fileName);

            }
            catch (IOException ex)
            {
            }
        }

        internal static void SetPath(string v)
        {
            Logging.Logger.SetPath(v);
        }

        internal static void LogException(Exception ex, string v)
        {
            Logging.Logger.LogException(ex, v);
        }
    }
}