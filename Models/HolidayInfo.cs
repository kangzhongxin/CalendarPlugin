using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Models
{
    public class HolidayInfo
    {
        public string Date { get; set; }        // "2026-01-01"
        public string Holiday { get; set; }     // "元旦节"
        public int DayType { get; set; }        // 0工作日 1节假日 2双休日 3调休日
        public bool Rest { get; set; }          // 是否休息
        public string WeekDescCn { get; set; }  // "星期一"
    }
}
