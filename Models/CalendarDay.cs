using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Models
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool IsWeekend { get; set; }
        public HolidayInfo Holiday { get; set; }
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
