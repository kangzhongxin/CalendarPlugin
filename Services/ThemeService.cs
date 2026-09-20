using CalendarPlugin.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarPlugin.Services
{
    public static class ThemeService
    {
        private static readonly List<Theme> _themes = BuildThemes();

        public static IReadOnlyList<Theme> Themes => _themes;

        public static Theme GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return _themes[0];
            foreach (var t in _themes)
                if (t.Id == id) return t;
            return _themes[0];
        }

        public static Theme Default => _themes[0];

        private static List<Theme> BuildThemes()
        {
            return new List<Theme>
            {
                // ========== 浅色（默认） ==========
                new Theme
                {
                    Id = "light", Name = "浅色（默认）", IsDark = false,
                    FormBack = Color.White,
                    TitleBack = Color.FromArgb(58, 118, 200),
                    TitleFore = Color.White,
                    TodayButtonBack = Color.FromArgb(70, 255, 255, 255),
                    TodayButtonBorder = Color.FromArgb(180, 255, 255, 255),
                    NavButtonBack = Color.FromArgb(70, 255, 255, 255),
                    NavButtonBorder = Color.FromArgb(180, 255, 255, 255),

                    WeekBack = Color.FromArgb(245, 247, 250),
                    WeekFore = Color.FromArgb(90, 90, 90),
                    WeekendFore = Color.FromArgb(210, 80, 80),

                    CellBack = Color.White,
                    CellOtherBack = Color.FromArgb(248, 249, 251),
                    CellFore = Color.FromArgb(50, 50, 50),
                    CellOtherFore = Color.FromArgb(190, 190, 190),
                    GridLine = Color.FromArgb(228, 232, 238),
                    TodayBack = Color.FromArgb(255, 249, 224),
                    TodayBorder = Color.FromArgb(240, 173, 78),

                    HolidayFore = Color.FromArgb(210, 50, 50),
                    WorkdayFore = Color.FromArgb(120, 120, 120),
                    SubTextFore = Color.FromArgb(150, 150, 150),
                    WeekendBadgeFore = Color.FromArgb(220, 130, 130),

                    SelectionColor = Color.FromArgb(90, 74, 144, 226),
                },

                // ========== 深色 ==========
                new Theme
                {
                    Id = "dark", Name = "深色", IsDark = true,
                    FormBack = Color.FromArgb(30, 30, 30),
                    TitleBack = Color.FromArgb(37, 37, 38),
                    TitleFore = Color.FromArgb(240, 240, 240),
                    TodayButtonBack = Color.FromArgb(60, 255, 255, 255),
                    TodayButtonBorder = Color.FromArgb(120, 255, 255, 255),
                    NavButtonBack = Color.FromArgb(60, 255, 255, 255),
                    NavButtonBorder = Color.FromArgb(120, 255, 255, 255),

                    WeekBack = Color.FromArgb(45, 45, 48),
                    WeekFore = Color.FromArgb(200, 200, 200),
                    WeekendFore = Color.FromArgb(255, 130, 130),

                    CellBack = Color.FromArgb(30, 30, 30),
                    CellOtherBack = Color.FromArgb(24, 24, 24),
                    CellFore = Color.FromArgb(224, 224, 224),
                    CellOtherFore = Color.FromArgb(96, 96, 96),
                    GridLine = Color.FromArgb(51, 51, 51),
                    TodayBack = Color.FromArgb(60, 60, 40),
                    TodayBorder = Color.FromArgb(240, 173, 78),

                    HolidayFore = Color.FromArgb(255, 107, 107),
                    WorkdayFore = Color.FromArgb(160, 160, 160),
                    SubTextFore = Color.FromArgb(160, 160, 160),
                    WeekendBadgeFore = Color.FromArgb(255, 150, 150),

                    SelectionColor = Color.FromArgb(100, 74, 144, 226),
                },

                // ========== 蓝色海洋 ==========
                new Theme
                {
                    Id = "blue", Name = "蓝色海洋", IsDark = false,
                    FormBack = Color.FromArgb(240, 247, 255),
                    TitleBack = Color.FromArgb(30, 90, 180),
                    TitleFore = Color.White,
                    TodayButtonBack = Color.FromArgb(70, 255, 255, 255),
                    TodayButtonBorder = Color.FromArgb(180, 255, 255, 255),
                    NavButtonBack = Color.FromArgb(70, 255, 255, 255),
                    NavButtonBorder = Color.FromArgb(180, 255, 255, 255),

                    WeekBack = Color.FromArgb(220, 235, 252),
                    WeekFore = Color.FromArgb(30, 80, 160),
                    WeekendFore = Color.FromArgb(220, 100, 100),

                    CellBack = Color.FromArgb(248, 251, 255),
                    CellOtherBack = Color.FromArgb(232, 240, 250),
                    CellFore = Color.FromArgb(20, 60, 120),
                    CellOtherFore = Color.FromArgb(160, 180, 210),
                    GridLine = Color.FromArgb(200, 220, 245),
                    TodayBack = Color.FromArgb(210, 235, 255),
                    TodayBorder = Color.FromArgb(30, 90, 180),

                    HolidayFore = Color.FromArgb(200, 50, 50),
                    WorkdayFore = Color.FromArgb(100, 130, 170),
                    SubTextFore = Color.FromArgb(120, 150, 190),
                    WeekendBadgeFore = Color.FromArgb(200, 100, 100),

                    SelectionColor = Color.FromArgb(90, 30, 90, 180),
                },

                // ========== 森林绿 ==========
                new Theme
                {
                    Id = "green", Name = "森林绿", IsDark = false,
                    FormBack = Color.FromArgb(242, 250, 242),
                    TitleBack = Color.FromArgb(40, 130, 80),
                    TitleFore = Color.White,
                    TodayButtonBack = Color.FromArgb(70, 255, 255, 255),
                    TodayButtonBorder = Color.FromArgb(180, 255, 255, 255),
                    NavButtonBack = Color.FromArgb(70, 255, 255, 255),
                    NavButtonBorder = Color.FromArgb(180, 255, 255, 255),

                    WeekBack = Color.FromArgb(225, 242, 228),
                    WeekFore = Color.FromArgb(40, 100, 60),
                    WeekendFore = Color.FromArgb(210, 90, 90),

                    CellBack = Color.FromArgb(250, 254, 250),
                    CellOtherBack = Color.FromArgb(235, 245, 237),
                    CellFore = Color.FromArgb(30, 70, 45),
                    CellOtherFore = Color.FromArgb(170, 195, 175),
                    GridLine = Color.FromArgb(205, 228, 210),
                    TodayBack = Color.FromArgb(220, 245, 220),
                    TodayBorder = Color.FromArgb(40, 130, 80),

                    HolidayFore = Color.FromArgb(200, 50, 50),
                    WorkdayFore = Color.FromArgb(110, 140, 120),
                    SubTextFore = Color.FromArgb(130, 165, 140),
                    WeekendBadgeFore = Color.FromArgb(200, 100, 100),

                    SelectionColor = Color.FromArgb(90, 40, 130, 80),
                },

                // ========== 紫罗兰 ==========
                new Theme
                {
                    Id = "purple", Name = "紫罗兰", IsDark = false,
                    FormBack = Color.FromArgb(248, 244, 252),
                    TitleBack = Color.FromArgb(110, 60, 180),
                    TitleFore = Color.White,
                    TodayButtonBack = Color.FromArgb(70, 255, 255, 255),
                    TodayButtonBorder = Color.FromArgb(180, 255, 255, 255),
                    NavButtonBack = Color.FromArgb(70, 255, 255, 255),
                    NavButtonBorder = Color.FromArgb(180, 255, 255, 255),

                    WeekBack = Color.FromArgb(238, 228, 250),
                    WeekFore = Color.FromArgb(80, 40, 140),
                    WeekendFore = Color.FromArgb(220, 100, 120),

                    CellBack = Color.FromArgb(252, 250, 255),
                    CellOtherBack = Color.FromArgb(242, 235, 252),
                    CellFore = Color.FromArgb(60, 30, 100),
                    CellOtherFore = Color.FromArgb(180, 160, 210),
                    GridLine = Color.FromArgb(220, 205, 240),
                    TodayBack = Color.FromArgb(235, 220, 250),
                    TodayBorder = Color.FromArgb(110, 60, 180),

                    HolidayFore = Color.FromArgb(210, 60, 60),
                    WorkdayFore = Color.FromArgb(130, 110, 160),
                    SubTextFore = Color.FromArgb(150, 130, 180),
                    WeekendBadgeFore = Color.FromArgb(210, 110, 130),

                    SelectionColor = Color.FromArgb(90, 110, 60, 180),
                },

                // ========== 暖阳橙 ==========
                new Theme
                {
                    Id = "warm", Name = "暖阳橙", IsDark = false,
                    FormBack = Color.FromArgb(255, 250, 240),
                    TitleBack = Color.FromArgb(220, 120, 40),
                    TitleFore = Color.White,
                    TodayButtonBack = Color.FromArgb(70, 255, 255, 255),
                    TodayButtonBorder = Color.FromArgb(180, 255, 255, 255),
                    NavButtonBack = Color.FromArgb(70, 255, 255, 255),
                    NavButtonBorder = Color.FromArgb(180, 255, 255, 255),

                    WeekBack = Color.FromArgb(252, 240, 220),
                    WeekFore = Color.FromArgb(160, 80, 20),
                    WeekendFore = Color.FromArgb(220, 60, 60),

                    CellBack = Color.FromArgb(255, 253, 248),
                    CellOtherBack = Color.FromArgb(250, 240, 228),
                    CellFore = Color.FromArgb(90, 50, 20),
                    CellOtherFore = Color.FromArgb(200, 180, 160),
                    GridLine = Color.FromArgb(240, 225, 205),
                    TodayBack = Color.FromArgb(255, 235, 200),
                    TodayBorder = Color.FromArgb(220, 120, 40),

                    HolidayFore = Color.FromArgb(200, 40, 40),
                    WorkdayFore = Color.FromArgb(160, 120, 90),
                    SubTextFore = Color.FromArgb(180, 140, 110),
                    WeekendBadgeFore = Color.FromArgb(210, 90, 90),

                    SelectionColor = Color.FromArgb(90, 220, 120, 40),
                },
            };
        }
    }
}
