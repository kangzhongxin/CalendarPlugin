using CalendarPlugin.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Services
{
    public class HolidayService
    {
        private readonly Dictionary<string, HolidayInfo> _holidays
            = new Dictionary<string, HolidayInfo>();
        private readonly HashSet<int> _loadedYears = new HashSet<int>();

        private static readonly string[] WeekNames =
            { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };

        public async Task LoadHolidaysAsync(int year)
        {
            if (_loadedYears.Contains(year)) return;
            _loadedYears.Add(year);

            string url = $"https://cdn.jsdelivr.net/gh/NateScarlet/holiday-cn@master/{year}.json";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string json = await client.GetStringAsync(url);
                    var root = JObject.Parse(json);
                    var days = root["days"] as JArray;
                    if (days != null)
                    {
                        foreach (var d in days)
                        {
                            string date = d["date"]?.ToString();
                            if (string.IsNullOrEmpty(date)) continue;

                            DateTime dt;
                            if (!DateTime.TryParse(date, out dt)) continue;

                            bool isOffDay = d["isOffDay"]?.Value<bool>() ?? false;
                            _holidays[date] = new HolidayInfo
                            {
                                Date = date,
                                Holiday = d["name"]?.ToString() ?? "",
                                Rest = isOffDay,
                                DayType = isOffDay ? 1 : 3,
                                WeekDescCn = WeekNames[(int)dt.DayOfWeek]
                            };
                        }
                    }
                }
            }
            catch
            {
                LoadFallbackHolidays(year);
            }
        }

        private void LoadFallbackHolidays(int year)
        {
            // 网络不可用时的最小内置数据（元旦、劳动节、国庆节）
            AddHoliday(year, 1, 1, "元旦节", true);
            AddHoliday(year, 5, 1, "劳动节", true);
            AddHoliday(year, 10, 1, "国庆节", true);
            AddHoliday(year, 10, 2, "国庆节", true);
            AddHoliday(year, 10, 3, "国庆节", true);
        }

        private void AddHoliday(int year, int month, int day, string name, bool rest)
        {
            var dt = new DateTime(year, month, day);
            string key = dt.ToString("yyyy-MM-dd");
            _holidays[key] = new HolidayInfo
            {
                Date = key,
                Holiday = name,
                Rest = rest,
                DayType = rest ? 1 : 0,
                WeekDescCn = WeekNames[(int)dt.DayOfWeek]
            };
        }

        public HolidayInfo GetHoliday(DateTime date)
        {
            string key = date.ToString("yyyy-MM-dd");
            HolidayInfo info;
            return _holidays.TryGetValue(key, out info) ? info : null;
        }
    }
}
