using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Megatech.FMS.DataExchange
{
    class Configuration
    {
        public string Name { get; set; }
        public string LoginUrl { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string ExecuteUrl { get; set; }
        public int Interval { get; set; }

        public static List<Configuration> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<List<Configuration>>(json);

        }

        public static List<Configuration> FromJsonFile(string filePath)
        {
            var json = System.IO.File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Configuration>>(json);

        }

        private static List<Configuration> _all;
        public static Configuration GetConfiguration(string name)
        {
            if (_all == null)
                _all = FromJsonFile("config.json");
            if (_all != null)
                return _all.FirstOrDefault(cf => cf.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            else return null;
        }
    }
}
