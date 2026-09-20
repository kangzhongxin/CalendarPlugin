using CalendarPlugin.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Services
{
    public class JsonStorageService
    {
        private readonly string _dataPath;

        public JsonStorageService()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CalendarPlugin");
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);
            _dataPath = Path.Combine(appData, "appointments.json");
        }

        public string DataPath => _dataPath;

        public List<Appointment> LoadAppointments()
        {
            if (!File.Exists(_dataPath)) return new List<Appointment>();
            try
            {
                string json = File.ReadAllText(_dataPath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<Appointment>>(json)
                    ?? new List<Appointment>();
            }
            catch
            {
                return new List<Appointment>();
            }
        }

        public void SaveAppointments(List<Appointment> appointments)
        {
            string json = JsonConvert.SerializeObject(appointments, Formatting.Indented);
            string tempPath = _dataPath + ".tmp";
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(_dataPath))
                File.Replace(tempPath, _dataPath, null);
            else
                File.Move(tempPath, _dataPath);
        }
    }
}
