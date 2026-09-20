using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Services
{
    public class AppSettings
    {
        /// <summary>主题 Id，见 ThemeService.Themes</summary>
        public string ThemeId { get; set; } = "light";
        public bool AutoStart { get; set; } = false;
        public bool EmbedToDesktop { get; set; } = true;
        public int WindowOpacity { get; set; } = 88;   // 30~100
        public int WindowX { get; set; } = -1;         // -1 表示使用默认
        public int WindowY { get; set; } = -1;
        public int WindowWidth { get; set; } = 760;
        public int WindowHeight { get; set; } = 600;
    }
}
