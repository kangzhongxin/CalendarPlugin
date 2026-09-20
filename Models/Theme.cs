using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Models
{
    /// <summary>日历主题：一组用于绘制的颜色。</summary>
    public class Theme
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDark { get; set; }

        // 窗体 & 标题栏
        public Color FormBack { get; set; }
        public Color TitleBack { get; set; }
        public Color TitleFore { get; set; }
        public Color TodayButtonBack { get; set; }
        public Color TodayButtonBorder { get; set; }
        public Color NavButtonBack { get; set; }
        public Color NavButtonBorder { get; set; }

        // 星期栏
        public Color WeekBack { get; set; }
        public Color WeekFore { get; set; }
        public Color WeekendFore { get; set; }

        // 单元格
        public Color CellBack { get; set; }
        public Color CellOtherBack { get; set; }
        public Color CellFore { get; set; }
        public Color CellOtherFore { get; set; }
        public Color GridLine { get; set; }
        public Color TodayBack { get; set; }
        public Color TodayBorder { get; set; }

        // 文字
        public Color HolidayFore { get; set; }
        public Color WorkdayFore { get; set; }
        public Color SubTextFore { get; set; }
        public Color WeekendBadgeFore { get; set; }

        // 选择高亮（带 alpha）
        public Color SelectionColor { get; set; }
    }
}
