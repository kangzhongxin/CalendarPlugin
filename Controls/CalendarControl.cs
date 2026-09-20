using CalendarPlugin.Models;
using CalendarPlugin.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin.Controls
{
    public class BorderHitEventArgs : EventArgs
    {
        public Point Location { get; private set; }
        public bool Handled { get; set; }
        public BorderHitEventArgs(Point loc) { Location = loc; }
    }

    public partial class CalendarControl : Control
    {
        // ========== 布局常量 ==========
        private const int HeaderHeight = 44;
        private const int WeekDayHeight = 32;
        private const int DayNumberHeight = 26;
        private const int AppBarHeight = 16;
        private const int AppBarGap = 2;
        private const int BarRadius = 3;

        // ========== 主题 ==========
        private Theme _theme = ThemeService.Default;
        public Theme Theme
        {
            get { return _theme; }
            set
            {
                if (value == null) return;
                _theme = value;
                BackColor = _theme.FormBack;
                Invalidate();
            }
        }

        // ========== 数据 ==========
        private DateTime _displayMonth;
        private readonly List<CalendarDay> _days = new List<CalendarDay>();
        private List<Appointment> _appointments = new List<Appointment>();

        // ========== 拖拽选择 ==========
        private bool _isDragging;
        private DateTime _dragStartDate;
        private DateTime _dragCurrentDate;

        // ========== 悬停 ==========
        private Appointment _hoverAppointment;

        // ========== 字体 ==========
        private readonly Font _titleFont;
        private readonly Font _weekFont;
        private readonly Font _dayFont;
        private readonly Font _dayBoldFont;
        private readonly Font _subFont;
        private readonly Font _appFont;

        public HolidayService HolidayService { get; set; }

        public event EventHandler<AppointmentEventArgs> AppointmentDoubleClick;
        public event EventHandler<AppointmentEventArgs> AppointmentDeleteRequested;
        public event EventHandler<Models.DateRangeEventArgs> AddAppointmentRequested;
        public event EventHandler<MonthChangedEventArgs> MonthChanged;
        public event EventHandler<BorderHitEventArgs> BorderHitTest;
        public Func<Point, Cursor> CursorResolver { get; set; }

        public CalendarControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable |
                     ControlStyles.SupportsTransparentBackColor, true);

            _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            BackColor = _theme.FormBack;

            _titleFont = new Font("微软雅黑", 13f, FontStyle.Bold);
            _weekFont = new Font("微软雅黑", 9.5f, FontStyle.Regular);
            _dayFont = new Font("微软雅黑", 10.5f, FontStyle.Regular);
            _dayBoldFont = new Font("微软雅黑", 10.5f, FontStyle.Bold);
            _subFont = new Font("微软雅黑", 7.5f, FontStyle.Regular);
            _appFont = new Font("微软雅黑", 7.5f, FontStyle.Regular);

            RebuildDays();
        }

        public DateTime DisplayMonth
        {
            get { return _displayMonth; }
            set
            {
                var normalized = new DateTime(value.Year, value.Month, 1);
                if (normalized == _displayMonth) return;
                _displayMonth = normalized;
                RebuildDays();
                Invalidate();
                if (MonthChanged != null)
                    MonthChanged(this, new MonthChangedEventArgs(_displayMonth));
            }
        }

        public void SetAppointments(List<Appointment> appointments)
        {
            _appointments = appointments ?? new List<Appointment>();
            RebuildDays();
            Invalidate();
        }

        public void RefreshHolidays() { RebuildDays(); Invalidate(); }

        public void GotoToday()
        {
            DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        // ====================================================================
        //  日程条布局
        // ====================================================================
        private class BarInfo
        {
            public Appointment Apt;
            public int WeekRow;
            public int StartCol;
            public int EndCol;
            public int Lane;
            public bool IsRealStart;
            public bool IsRealEnd;
        }

        private List<BarInfo> CalculateBarLayout()
        {
            var result = new List<BarInfo>();
            for (int row = 0; row < 6; row++)
            {
                var weekDays = new List<CalendarDay>();
                for (int c = 0; c < 7; c++)
                {
                    int idx = row * 7 + c;
                    weekDays.Add(idx < _days.Count ? _days[idx] : null);
                }

                var seen = new HashSet<string>();
                var weekApts = new List<Appointment>();
                for (int c = 0; c < 7; c++)
                {
                    if (weekDays[c] == null) continue;
                    foreach (var apt in weekDays[c].Appointments)
                        if (seen.Add(apt.Id)) weekApts.Add(apt);
                }

                var bars = new List<BarInfo>();
                foreach (var apt in weekApts)
                {
                    int startCol = -1, endCol = -1;
                    for (int c = 0; c < 7; c++)
                    {
                        if (weekDays[c] == null) continue;
                        var d = weekDays[c].Date.Date;
                        if (d >= apt.StartDate.Date && d <= apt.EndDate.Date)
                        {
                            if (startCol < 0) startCol = c;
                            endCol = c;
                        }
                    }
                    if (startCol < 0) continue;

                    bars.Add(new BarInfo
                    {
                        Apt = apt,
                        WeekRow = row,
                        StartCol = startCol,
                        EndCol = endCol,
                        IsRealStart = weekDays[startCol].Date.Date <= apt.StartDate.Date,
                        IsRealEnd = weekDays[endCol].Date.Date >= apt.EndDate.Date
                    });
                }

                bars.Sort((a, b) =>
                {
                    int cmp = a.StartCol.CompareTo(b.StartCol);
                    if (cmp != 0) return cmp;
                    return (b.EndCol - b.StartCol).CompareTo(a.EndCol - a.StartCol);
                });

                var lanes = new List<List<BarInfo>>();
                foreach (var bar in bars)
                {
                    int assigned = -1;
                    for (int i = 0; i < lanes.Count; i++)
                    {
                        bool overlap = false;
                        foreach (var other in lanes[i])
                        {
                            if (!(bar.EndCol < other.StartCol || bar.StartCol > other.EndCol))
                            { overlap = true; break; }
                        }
                        if (!overlap) { assigned = i; break; }
                    }
                    if (assigned < 0) { lanes.Add(new List<BarInfo>()); assigned = lanes.Count - 1; }
                    bar.Lane = assigned;
                    lanes[assigned].Add(bar);
                }
                result.AddRange(bars);
            }
            return result;
        }

        // ====================================================================
        //  绘制
        // ====================================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int cellW = Width / 7;
            int cellH = (Height - HeaderHeight - WeekDayHeight) / 6;
            if (cellW <= 0 || cellH <= 0) return;

            DrawHeader(g);
            DrawWeekHeader(g, cellW);

            for (int i = 0; i < _days.Count && i < 42; i++)
            {
                int row = i / 7;
                int col = i % 7;
                Rectangle rect = new Rectangle(col * cellW,
                    HeaderHeight + WeekDayHeight + row * cellH, cellW, cellH);
                DrawDayCellBackground(g, _days[i], rect);
            }

            var layout = CalculateBarLayout();
            for (int row = 0; row < 6; row++)
                DrawWeekAppointments(g, row, cellW, cellH, layout);
        }

        private void DrawHeader(Graphics g)
        {
            using (var brush = new SolidBrush(_theme.TitleBack))
                g.FillRectangle(brush, 0, 0, Width, HeaderHeight);

            string title = string.Format("{0}年 {1}月", _displayMonth.Year, _displayMonth.Month);
            using (var brush = new SolidBrush(_theme.TitleFore))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(title, _titleFont, brush,
                    new RectangleF(0, 0, Width, HeaderHeight), sf);
            }

            DrawNavButton(g, new Rectangle(8, 8, 28, 28), "‹");
            DrawNavButton(g, new Rectangle(Width - 36, 8, 28, 28), "›");

            Rectangle todayRect = new Rectangle(44, 10, 52, 24);
            using (var brush = new SolidBrush(_theme.TodayButtonBack))
                g.FillRectangle(brush, todayRect);
            using (var pen = new Pen(_theme.TodayButtonBorder))
                g.DrawRectangle(pen, todayRect);
            using (var brush = new SolidBrush(_theme.TitleFore))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString("今天", _weekFont, brush, todayRect, sf);
            }
        }

        private void DrawNavButton(Graphics g, Rectangle rect, string text)
        {
            using (var brush = new SolidBrush(_theme.NavButtonBack))
                g.FillRectangle(brush, rect);
            using (var brush = new SolidBrush(_theme.TitleFore))
            using (var font = new Font("微软雅黑", 14f, FontStyle.Bold))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(text, font, brush, rect, sf);
            }
        }

        private void DrawWeekHeader(Graphics g, int cellW)
        {
            using (var brush = new SolidBrush(_theme.WeekBack))
                g.FillRectangle(brush, 0, HeaderHeight, Width, WeekDayHeight);

            string[] weekNames = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            for (int i = 0; i < 7; i++)
            {
                Rectangle rect = new Rectangle(i * cellW, HeaderHeight, cellW, WeekDayHeight);
                Color fore = (i >= 5) ? _theme.WeekendFore : _theme.WeekFore;
                using (var brush = new SolidBrush(fore))
                    g.DrawString(weekNames[i], _weekFont, brush, rect, sf);
            }

            using (var pen = new Pen(_theme.GridLine))
                g.DrawLine(pen, 0, HeaderHeight + WeekDayHeight - 1,
                    Width, HeaderHeight + WeekDayHeight - 1);
        }

        private void DrawDayCellBackground(Graphics g, CalendarDay day, Rectangle rect)
        {
            Color back;
            if (!day.IsCurrentMonth) back = _theme.CellOtherBack;
            else if (day.IsToday) back = _theme.TodayBack;
            else back = _theme.CellBack;

            using (var brush = new SolidBrush(back))
                g.FillRectangle(brush, rect);

            if (_isDragging && IsInDragRange(day.Date))
            {
                using (var brush = new SolidBrush(_theme.SelectionColor))
                    g.FillRectangle(brush, rect);
            }

            using (var pen = new Pen(_theme.GridLine))
            {
                g.DrawLine(pen, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom);
                g.DrawLine(pen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            }

            if (day.IsToday)
            {
                using (var pen = new Pen(_theme.TodayBorder, 1.5f))
                {
                    var r = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 3, rect.Height - 3);
                    g.DrawRectangle(pen, r);
                }
            }

            // 日期数字
            Color dayColor;
            if (!day.IsCurrentMonth) dayColor = _theme.CellOtherFore;
            else if (day.IsWeekend) dayColor = _theme.WeekendFore;
            else dayColor = _theme.CellFore;
            if (day.Holiday != null && day.Holiday.Rest && day.IsCurrentMonth)
                dayColor = _theme.HolidayFore;

            Font dayFont = day.IsToday ? _dayBoldFont : _dayFont;
            Rectangle dayTextRect = new Rectangle(rect.X + 6, rect.Y + 4, 40, 20);
            using (var brush = new SolidBrush(dayColor))
                g.DrawString(day.Date.Day.ToString(), dayFont, brush, dayTextRect);

            // 节假日/调休
            string subText = null;
            Color subColor = _theme.SubTextFore;

            if (day.Holiday != null && day.IsCurrentMonth)
            {
                subText = day.Holiday.Holiday;
                if (!day.Holiday.Rest && day.Holiday.DayType == 3)
                {
                    subText = "班";
                    subColor = _theme.WorkdayFore;
                }
                else subColor = _theme.HolidayFore;
            }
            else if (day.IsWeekend && day.IsCurrentMonth && !day.IsToday)
            {
                subText = "休";
                subColor = _theme.WeekendBadgeFore;
            }

            if (!string.IsNullOrEmpty(subText))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                RectangleF subRect = new RectangleF(rect.X + 6, rect.Y + 4, rect.Width - 12, 16);
                using (var brush = new SolidBrush(subColor))
                    g.DrawString(subText, _subFont, brush, subRect, sf);
            }
        }

        private void DrawWeekAppointments(Graphics g, int row, int cellW, int cellH,
                                          List<BarInfo> layout)
        {
            int weekTop = HeaderHeight + WeekDayHeight + row * cellH;
            int weekBottom = weekTop + cellH;
            int yStart = weekTop + DayNumberHeight;
            int availableH = cellH - DayNumberHeight - 4;
            if (availableH <= 0) return;

            int maxLanes = Math.Max(1,
                (availableH + AppBarGap) / (AppBarHeight + AppBarGap));

            var barsOfWeek = layout.Where(b => b.WeekRow == row).ToList();
            var hiddenByCol = new Dictionary<int, int>();

            foreach (var bar in barsOfWeek)
            {
                if (bar.Lane >= maxLanes)
                {
                    for (int c = bar.StartCol; c <= bar.EndCol; c++)
                    {
                        if (!hiddenByCol.ContainsKey(c)) hiddenByCol[c] = 0;
                        hiddenByCol[c]++;
                    }
                    continue;
                }

                int x1 = bar.StartCol * cellW + 3;
                int x2 = (bar.EndCol + 1) * cellW - 3;
                int y = yStart + bar.Lane * (AppBarHeight + AppBarGap);
                Rectangle barRect = new Rectangle(x1, y, x2 - x1, AppBarHeight);

                bool isHovered = _hoverAppointment != null
                              && _hoverAppointment.Id == bar.Apt.Id;

                DrawAppointmentBar(g, bar.Apt, barRect,
                    bar.IsRealStart, bar.IsRealEnd, isHovered);
            }

            foreach (var kv in hiddenByCol)
            {
                int col = kv.Key;
                int moreY = yStart + maxLanes * (AppBarHeight + AppBarGap);
                if (moreY + 14 > weekBottom - 2) moreY = weekBottom - 16;
                if (moreY < yStart) continue;

                Rectangle moreRect = new Rectangle(col * cellW + 4, moreY, cellW - 8, 14);
                using (var brush = new SolidBrush(_theme.SubTextFore))
                using (var sf = new StringFormat
                {
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                })
                {
                    g.DrawString("+" + kv.Value + " 更多", _appFont, brush, moreRect, sf);
                }
            }
        }

        private void DrawAppointmentBar(Graphics g, Appointment apt, Rectangle rect,
            bool roundLeft, bool roundRight, bool isHovered)
        {
            Color baseColor = apt.DisplayColor;
            Color fillColor = isHovered ? ControlPaint.Light(baseColor, 0.15f) : baseColor;

            using (var brush = new SolidBrush(fillColor))
                FillRoundedRectangle(g, brush, rect, BarRadius, roundLeft, roundRight);

            if (roundLeft)
            {
                using (var brush = new SolidBrush(ControlPaint.Dark(baseColor, 0.15f)))
                {
                    Rectangle leftStrip = new Rectangle(rect.X + 2, rect.Y + 3,
                        3, rect.Height - 6);
                    g.FillRectangle(brush, leftStrip);
                }
            }

            if (roundLeft)
            {
                using (var brush = new SolidBrush(GetContrastColor(fillColor)))
                using (var sf = new StringFormat
                {
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter,
                    LineAlignment = StringAlignment.Center
                })
                {
                    RectangleF textRect = new RectangleF(
                        rect.X + 8, rect.Y, rect.Width - 10, rect.Height);
                    g.DrawString(apt.Title, _appFont, brush, textRect, sf);
                }
            }

            if (isHovered)
            {
                using (var pen = new Pen(Color.FromArgb(180, 255, 255, 255), 1.5f))
                {
                    var innerRect = new Rectangle(rect.X + 1, rect.Y + 1,
                        rect.Width - 2, rect.Height - 2);
                    g.DrawRectangle(pen, innerRect);
                }
            }
        }

        private static Color GetContrastColor(Color color)
        {
            int b = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
            return b > 150 ? Color.FromArgb(40, 40, 40) : Color.White;
        }

        private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect,
            int radius, bool roundLeft, bool roundRight)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            int r = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            int d = r * 2;

            if (d <= 0 || (!roundLeft && !roundRight))
            {
                g.FillRectangle(brush, rect);
                return;
            }

            using (var path = new GraphicsPath())
            {
                if (roundLeft)
                {
                    path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                    if (roundRight) path.AddLine(rect.X + r, rect.Y, rect.Right - r, rect.Y);
                    else path.AddLine(rect.X + r, rect.Y, rect.Right, rect.Y);

                    if (roundRight)
                    {
                        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    }
                    else path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);

                    if (roundRight) path.AddLine(rect.Right - r, rect.Bottom, rect.X + r, rect.Bottom);
                    else path.AddLine(rect.Right, rect.Bottom, rect.X + r, rect.Bottom);

                    path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                }
                else
                {
                    path.AddLine(rect.X, rect.Y, rect.Right - r, rect.Y);
                    path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                    path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    path.AddLine(rect.Right - r, rect.Bottom, rect.X, rect.Bottom);
                    path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y);
                }
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        private bool IsInDragRange(DateTime date)
        {
            DateTime lo = _dragStartDate < _dragCurrentDate ? _dragStartDate : _dragCurrentDate;
            DateTime hi = _dragStartDate > _dragCurrentDate ? _dragStartDate : _dragCurrentDate;
            return date.Date >= lo.Date && date.Date <= hi.Date;
        }

        private void RebuildDays()
        {
            _days.Clear();
            DateTime firstDay = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
            int offset = ((int)firstDay.DayOfWeek + 6) % 7;
            DateTime start = firstDay.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                DateTime date = start.AddDays(i);
                var day = new CalendarDay
                {
                    Date = date,
                    IsCurrentMonth = date.Month == _displayMonth.Month,
                    IsToday = date.Date == DateTime.Today,
                    IsWeekend = date.DayOfWeek == DayOfWeek.Saturday
                             || date.DayOfWeek == DayOfWeek.Sunday
                };

                if (HolidayService != null) day.Holiday = HolidayService.GetHoliday(date);

                day.Appointments = _appointments
                    .Where(a => date.Date >= a.StartDate.Date && date.Date <= a.EndDate.Date)
                    .OrderBy(a => a.StartDate).ThenBy(a => a.Title).ToList();

                _days.Add(day);
            }
        }

        // ====================================================================
        //  命中测试
        // ====================================================================
        private CalendarDay HitTestDay(Point location)
        {
            int headerTotal = HeaderHeight + WeekDayHeight;
            if (location.Y < headerTotal) return null;

            int cellW = Width / 7;
            int cellH = (Height - headerTotal) / 6;
            if (cellW <= 0 || cellH <= 0) return null;
            if (location.Y >= headerTotal + cellH * 6) return null;

            int col = location.X / cellW;
            int row = (location.Y - headerTotal) / cellH;
            if (col < 0 || col > 6 || row < 0 || row > 5) return null;

            int index = row * 7 + col;
            if (index < 0 || index >= _days.Count) return null;
            return _days[index];
        }

        private Appointment HitTestAppointment(Point location)
        {
            int headerTotal = HeaderHeight + WeekDayHeight;
            if (location.Y < headerTotal) return null;

            int cellW = Width / 7;
            int cellH = (Height - headerTotal) / 6;
            if (cellW <= 0 || cellH <= 0) return null;

            int row = (location.Y - headerTotal) / cellH;
            if (row < 0 || row > 5) return null;

            int yStart = headerTotal + row * cellH + DayNumberHeight;
            int availableH = cellH - DayNumberHeight - 4;
            int maxLanes = Math.Max(1,
                (availableH + AppBarGap) / (AppBarHeight + AppBarGap));

            var layout = CalculateBarLayout();
            foreach (var bar in layout)
            {
                if (bar.WeekRow != row) continue;
                if (bar.Lane >= maxLanes) continue;

                int x1 = bar.StartCol * cellW + 3;
                int x2 = (bar.EndCol + 1) * cellW - 3;
                int y = yStart + bar.Lane * (AppBarHeight + AppBarGap);
                Rectangle barRect = new Rectangle(x1, y, x2 - x1, AppBarHeight);
                if (barRect.Contains(location)) return bar.Apt;
            }
            return null;
        }

        // ====================================================================
        //  鼠标交互
        // ====================================================================
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && BorderHitTest != null)
            {
                var args = new BorderHitEventArgs(e.Location);
                BorderHitTest(this, args);
                if (args.Handled) return;
            }

            base.OnMouseDown(e);

            if (e.Y < HeaderHeight) { HandleHeaderClick(e.Location); return; }

            var hitDay = HitTestDay(e.Location);
            if (e.Button == MouseButtons.Right)
            {
                if (hitDay != null) ShowContextMenu(e.Location, hitDay);
                return;
            }

            if (e.Button == MouseButtons.Left && hitDay != null)
            {
                _isDragging = true;
                _dragStartDate = hitDay.Date;
                _dragCurrentDate = hitDay.Date;
                Invalidate();
            }
        }

        private void HandleHeaderClick(Point location)
        {
            if (new Rectangle(8, 8, 28, 28).Contains(location))
                DisplayMonth = _displayMonth.AddMonths(-1);
            else if (new Rectangle(Width - 36, 8, 28, 28).Contains(location))
                DisplayMonth = _displayMonth.AddMonths(1);
            else if (new Rectangle(44, 10, 52, 24).Contains(location))
                GotoToday();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                var hitDay = HitTestDay(e.Location);
                if (hitDay != null && hitDay.Date != _dragCurrentDate)
                {
                    _dragCurrentDate = hitDay.Date;
                    Invalidate();
                }
                Cursor = Cursors.Hand;
                return;
            }

            if (CursorResolver != null)
            {
                var customCursor = CursorResolver(e.Location);
                if (customCursor != null)
                {
                    if (Cursor != customCursor) Cursor = customCursor;
                    if (_hoverAppointment != null)
                    {
                        _hoverAppointment = null;
                        Invalidate();
                    }
                    return;
                }
            }

            var hoverDay = HitTestDay(e.Location);
            if (hoverDay != null)
            {
                var apt = HitTestAppointment(e.Location);
                if (apt != _hoverAppointment)
                {
                    _hoverAppointment = apt;
                    Cursor = apt != null ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }
            else if (_hoverAppointment != null)
            {
                _hoverAppointment = null;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_isDragging) return;
            _isDragging = false;

            DateTime lo = _dragStartDate < _dragCurrentDate ? _dragStartDate : _dragCurrentDate;
            DateTime hi = _dragStartDate > _dragCurrentDate ? _dragStartDate : _dragCurrentDate;

            Invalidate();

            if (lo.Date != hi.Date && AddAppointmentRequested != null)
                AddAppointmentRequested(this, new Models.DateRangeEventArgs(lo, hi));
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (e.Y < HeaderHeight + WeekDayHeight) return;

            var hitDay = HitTestDay(e.Location);
            if (hitDay == null) return;

            var apt = HitTestAppointment(e.Location);
            if (apt != null)
            {
                if (AppointmentDoubleClick != null)
                    AppointmentDoubleClick(this, new AppointmentEventArgs(apt));
            }
            else
            {
                if (AddAppointmentRequested != null)
                    AddAppointmentRequested(this,
                        new Models.DateRangeEventArgs(hitDay.Date, hitDay.Date));
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0) DisplayMonth = _displayMonth.AddMonths(-1);
            else if (e.Delta < 0) DisplayMonth = _displayMonth.AddMonths(1);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverAppointment != null)
            {
                _hoverAppointment = null;
                Invalidate();
            }
            Cursor = Cursors.Default;
        }

        private void ShowContextMenu(Point location, CalendarDay day)
        {
            var menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;

            var addItem = new ToolStripMenuItem("➕  添加日程");
            addItem.Font = new Font(menu.Font, FontStyle.Bold);
            addItem.Click += (s, ev) =>
            {
                if (AddAppointmentRequested != null)
                    AddAppointmentRequested(this,
                        new Models.DateRangeEventArgs(day.Date, day.Date));
            };
            menu.Items.Add(addItem);

            if (day.Appointments.Count > 0)
            {
                menu.Items.Add(new ToolStripSeparator());
                var header = new ToolStripMenuItem("当日日程");
                header.Enabled = false;
                menu.Items.Add(header);

                foreach (var apt in day.Appointments)
                {
                    string label = apt.DurationDays > 1
                        ? string.Format("• {0}  ({1:MM/dd}-{2:MM/dd})",
                            apt.Title, apt.StartDate, apt.EndDate)
                        : "• " + apt.Title;

                    var item = new ToolStripMenuItem(label);
                    item.ForeColor = apt.DisplayColor;

                    var delSub = new ToolStripMenuItem("删除");
                    delSub.Click += (s, ev) =>
                    {
                        if (AppointmentDeleteRequested != null)
                            AppointmentDeleteRequested(this,
                                new AppointmentEventArgs(apt));
                    };
                    item.DropDownItems.Add(delSub);

                    item.Click += (s, ev) =>
                    {
                        if (AppointmentDoubleClick != null)
                            AppointmentDoubleClick(this,
                                new AppointmentEventArgs(apt));
                    };
                    menu.Items.Add(item);
                }
            }

            menu.Show(this, location);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont.Dispose();
                _weekFont.Dispose();
                _dayFont.Dispose();
                _dayBoldFont.Dispose();
                _subFont.Dispose();
                _appFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }    
}
