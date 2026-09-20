using CalendarPlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Services
{
    public class AppointmentService
    {
        private List<Appointment> _appointments;
        private readonly JsonStorageService _storage;

        public AppointmentService(JsonStorageService storage)
        {
            _storage = storage;
            _appointments = _storage.LoadAppointments();
        }

        public List<Appointment> GetAll()
        {
            return _appointments.OrderBy(a => a.StartDate).ToList();
        }

        public List<Appointment> GetByDate(DateTime date)
        {
            return _appointments
                .Where(a => date.Date >= a.StartDate.Date && date.Date <= a.EndDate.Date)
                .OrderBy(a => a.StartDate)
                .ToList();
        }

        public void Add(Appointment apt)
        {
            _appointments.Add(apt);
            Save();
        }

        public void Delete(string id)
        {
            _appointments.RemoveAll(a => a.Id == id);
            Save();
        }

        public void Update(Appointment apt)
        {
            var index = _appointments.FindIndex(a => a.Id == apt.Id);
            if (index >= 0)
            {
                _appointments[index] = apt;
                Save();
            }
            else
            {
                _appointments.Add(apt);
                Save();
            }
        }

        private void Save() => _storage.SaveAppointments(_appointments);
    }
}
