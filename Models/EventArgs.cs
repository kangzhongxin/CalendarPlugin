using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Models
{
    public class AppointmentEventArgs : EventArgs
    {
        public Appointment Appointment { get; }
        public AppointmentEventArgs(Appointment apt) { Appointment = apt; }
    }

    public class DateRangeEventArgs : EventArgs
    {
        public DateTime Start { get; }
        public DateTime End { get; }
        public DateRangeEventArgs(DateTime start, DateTime end)
        {
            Start = start; End = end;
        }
    }

    public class MonthChangedEventArgs : EventArgs
    {
        public DateTime Month { get; }
        public MonthChangedEventArgs(DateTime month) { Month = month; }
    }
}
