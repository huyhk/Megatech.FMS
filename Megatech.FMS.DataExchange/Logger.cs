using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Megatech.FMS.DataExchange
{
    public class Logger
    {
        private static string folderPath;
        public static void SetPath(string path)
        {
            folderPath = path;
        }
        public static void AppendLog(string tag, string log)
        {
            AppendLog(tag, log, "post");
        }
        public static void AppendLog(string tag, string log, string fileName)
        {
            try
            {
                var filePath = Path.Combine(folderPath, fileName + ".log");
                FileInfo fi = new FileInfo(filePath);
                if (fi.Exists && fi.Length > 1024 * 1024)
                {
                    var archive = Path.Combine(folderPath, fileName + "-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                    fi.MoveTo(archive);
                }

                File.AppendAllText(filePath, string.Format("[{0:dd-MM-yyyy HH:mm:ss}] [{1}] {2}\n", DateTime.Now, tag, log));
            }
            catch (IOException ex)
            {
            }
        }

        internal static void LogException(Exception ex, string v)
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
    }
}
