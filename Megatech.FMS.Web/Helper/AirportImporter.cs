using ExcelDataReader;
using FMS.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace Megatech.FMS.Web
{
    public class AirportImporter
    {
        public static IList<Airport> ImportFile(string fileName)
        {
            return ImportFromExcel(fileName);
        }

        private static IList<Airport> ImportFromExcel(string fileName)
        {
            var list = new List<Airport>();

            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                IExcelDataReader reader = null;
                if (fileName.EndsWith("xls"))
                    reader = ExcelReaderFactory.CreateReader(stream);
                else if (fileName.EndsWith("xml"))
                { }
                else
                    reader = ExcelReaderFactory.CreateCsvReader(stream);

                var dataset = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                    {

                    }


                });
                var table = dataset.Tables[0];
                foreach (DataRow row in table.Rows)
                {
                    if (!string.IsNullOrEmpty(row[1].ToString()))
                    {
                        Airport f = new Airport();
                        f.Name = row[0].ToString();
                        f.Code = row[1].ToString();

                        list.Add(f);
                    }
                }
            }


            return list;
        }
    }

}