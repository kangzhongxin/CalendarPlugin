using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Services
{
    public class SettingsService
    {
        private readonly string _path;

        public SettingsService()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CalendarPlugin");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "settings.json");
        }

        public string Paths => _path;

        public AppSettings Load()
        {
            if (!File.Exists(_path)) return new AppSettings();
            try
            {
                string json = File.ReadAllText(_path, Encoding.UTF8);
                var s = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
                return s ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    settings, Newtonsoft.Json.Formatting.Indented);
                string temp = _path + ".tmp";
                File.WriteAllText(temp, json, Encoding.UTF8);
                if (File.Exists(_path)) File.Replace(temp, _path, null);
                else File.Move(temp, _path);
            }
            catch { /* 忽略 */ }
        }
    }
}
