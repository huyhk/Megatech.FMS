using System;
using System.IO;

namespace Megatech.FMS.Logging
{
    public class Logger
    {
        private static string logFolder;

        public static void SetPath(string folder)
        {
            logFolder = folder;
        }
        public static void AppendLog(string tag, string log)
        {
            AppendLog(tag, log, "post");
        }
        public static void AppendLog(string tag, string log, string fileName)
        {
            try
            {
                if (logFolder == null)
                {
                    logFolder = Directory.GetCurrentDirectory();
                }
                var filePath = Path.Combine(logFolder, fileName + ".log");
                FileInfo fi = new FileInfo(filePath);
                if (fi.Exists && fi.Length > 4 * 1024 * 1024)
                {
                    var i = 1;

                    var archive = Path.Combine(logFolder, fileName + "-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                    while (File.Exists(archive))
                    {
                        archive = Path.Combine(logFolder, fileName + "-" + DateTime.Now.ToString("yyyyMMdd") + "-" + i.ToString() + ".log");
                        i++;
                    }
                    fi.MoveTo(archive);
                }

                File.AppendAllText(filePath, string.Format("[{0:dd-MM-yyyy HH:mm:ss}] [{1}] {2}\n", DateTime.Now, tag, log));
            }
            catch (IOException ex)
            {
            }
        }

        public static void LogException(Exception ex, string v)
        {
            Logger.AppendLog("EXP", ex.StackTrace, v);
            Logger.AppendLog("EXP", ex.Message, v);
            var exp = ex.InnerException;
            while (exp != null)
            {
                Logger.AppendLog("EXP", exp.Message, v);
                exp = exp.InnerException;
            }
        }

        public static void WriteString(string log, string fileName)
        {
            if (logFolder == null)
            {
                logFolder = Directory.GetCurrentDirectory();
            }
            var filePath = Path.Combine(logFolder, fileName + ".log");
            FileInfo fi = new FileInfo(filePath);
            if (fi.Exists && fi.Length > 4 * 1024 * 1024)
            {
                var i = 1;

                var archive = Path.Combine(logFolder, fileName + "-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                while (File.Exists(archive))
                {
                    archive = Path.Combine(logFolder, fileName + "-" + DateTime.Now.ToString("yyyyMMdd") + "-" + i.ToString() + ".log");
                    i++;
                }
                fi.MoveTo(archive);
            }

            File.AppendAllText(filePath, string.Format("{0}\n", log));
        }
    }
}
