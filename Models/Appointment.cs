using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Models
{
    public class Appointment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public string ColorHex { get; set; } = "#4A90E2";
        public bool IsAllDay { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public Color DisplayColor
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(ColorHex)
                        ? Color.CornflowerBlue
                        : ColorTranslator.FromHtml(ColorHex);
                }
                catch { return Color.CornflowerBlue; }
            }
        }

        [JsonIgnore]
        public int DurationDays => (EndDate.Date - StartDate.Date).Days + 1;
    }
}
