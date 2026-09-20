using CalendarPlugin.Controls;
using CalendarPlugin.Helpers;
using CalendarPlugin.Models;
using CalendarPlugin.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin
{
    public partial class MainForm : Form
    {
        private CalendarControl _calendar;
        private AppointmentService _aptService;
        private HolidayService _holidayService;
        private JsonStorageService _storage;
        private SettingsService _settingsService;
        private AppSettings _settings;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _miShowHide;
        private ToolStripMenuItem _miAutoStart;
        private ToolStripMenuItem _miEmbedMode;
        private ToolStripMenuItem _miFloatMode;
        private ToolStripMenuItem _miTopMost;
        private ToolStripMenuItem _miThemeRoot;   // "切换主题"子菜单

        private bool _reallyExit;
        private bool _settingsDialogOpen;         // ★ 防重入锁

        // 拖动/调整大小 命中区域
        private const int ResizeBorder = 8;
        private const int TitleBarHeight = 44;

        // 拖动状态
        private enum DragMode
        {
            None, Move,
            ResizeLeft, ResizeRight, ResizeTop, ResizeBottom,
            ResizeTopLeft, ResizeTopRight, ResizeBottomLeft, ResizeBottomRight
        }
        private DragMode _dragMode = DragMode.None;
        private Point _dragStartScreen;
        private Point _dragStartLocation;
        private Size _dragStartSize;

        public MainForm(AppSettings settings, SettingsService settingsService)
        {
            _settings = settings;
            _settingsService = settingsService;
            InitializeUI();
            ApplySettings();
            InitializeServices();
            InitializeTray();
            WireEvents();
            ApplyTheme();
        }

        private void InitializeUI()
        {
            Text = "桌面日历";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            MinimumSize = new Size(480, 400);
            Font = new Font("微软雅黑", 9f);

            _calendar = new CalendarControl { Dock = DockStyle.Fill };
            Controls.Add(_calendar);
        }

        private void ApplySettings()
        {
            var screen = Screen.PrimaryScreen.WorkingArea;
            int w = Math.Min(_settings.WindowWidth, screen.Width);
            int h = Math.Min(_settings.WindowHeight, screen.Height);

            int x = _settings.WindowX >= 0 ? _settings.WindowX : screen.Right - w - 30;
            int y = _settings.WindowY >= 0 ? _settings.WindowY : screen.Top + 30;

            x = Math.Max(screen.Left, Math.Min(x, screen.Right - w));
            y = Math.Max(screen.Top, Math.Min(y, screen.Bottom - h));

            Location = new Point(x, y);
            Size = new Size(w, h);
        }

        private async void InitializeServices()
        {
            _storage = new JsonStorageService();
            _aptService = new AppointmentService(_storage);
            _holidayService = new HolidayService();
            _calendar.HolidayService = _holidayService;
            _calendar.SetAppointments(_aptService.GetAll());

            try
            {
                await _holidayService.LoadHolidaysAsync(DateTime.Today.Year);
                await _holidayService.LoadHolidaysAsync(DateTime.Today.Year + 1);
                _calendar.RefreshHolidays();
            }
            catch { _calendar.RefreshHolidays(); }
        }

        // ====================================================================
        //  托盘菜单
        // ====================================================================
        private void InitializeTray()
        {
            _trayMenu = new ContextMenuStrip();

            // 显示/隐藏
            _miShowHide = new ToolStripMenuItem("隐藏日历", null, (s, e) => ToggleVisibility());
            _trayMenu.Items.Add(_miShowHide);

            _trayMenu.Items.Add(new ToolStripSeparator());

            // 嵌入 / 浮动
            _miEmbedMode = new ToolStripMenuItem("嵌入桌面模式", null, (s, e) => SwitchToEmbedMode());
            _miFloatMode = new ToolStripMenuItem("浮动窗口模式", null, (s, e) => SwitchToFloatMode());
            _trayMenu.Items.Add(_miEmbedMode);
            _trayMenu.Items.Add(_miFloatMode);

            _miTopMost = new ToolStripMenuItem("窗口置顶", null, (s, e) =>
            {
                _miTopMost.Checked = !_miTopMost.Checked;
                TopMost = _miTopMost.Checked && !DesktopEmbedService.IsEmbedded;
            });
            _trayMenu.Items.Add(_miTopMost);

            _trayMenu.Items.Add(new ToolStripSeparator());

            // ★ 切换主题（子菜单，动态填充）
            _miThemeRoot = new ToolStripMenuItem("切换主题");
            BuildThemeMenuItems();
            // 每次展开时刷新选中状态（防止外部修改后未同步）
            _miThemeRoot.DropDownOpening += (s, e) => RefreshThemeChecks();
            _trayMenu.Items.Add(_miThemeRoot);

            _trayMenu.Items.Add(new ToolStripSeparator());

            // 自启动
            _miAutoStart = new ToolStripMenuItem("开机自启动", null, (s, e) =>
            {
                _miAutoStart.Checked = !_miAutoStart.Checked;
                SetAutoStart(_miAutoStart.Checked);
            });
            _miAutoStart.Checked = AutoStartService.IsEnabled;
            _trayMenu.Items.Add(_miAutoStart);

            // 设置
            _trayMenu.Items.Add(new ToolStripMenuItem("设置…", null, (s, e) => ShowSettings()));

            _trayMenu.Items.Add(new ToolStripSeparator());

            _trayMenu.Items.Add(new ToolStripMenuItem("退出", null, (s, e) =>
            {
                _reallyExit = true;
                Close();
            }));

            _trayIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                Text = "桌面日历插件",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            _trayIcon.DoubleClick += (s, e) => ToggleVisibility();
        }

        /// <summary>根据 ThemeService.Themes 生成主题菜单项</summary>
        private void BuildThemeMenuItems()
        {
            _miThemeRoot.DropDownItems.Clear();
            foreach (var t in ThemeService.Themes)
            {
                var item = new ToolStripMenuItem(t.Name);
                item.Tag = t.Id;
                item.CheckOnClick = false;
                item.Click += (s, e) => SwitchTheme((string)((ToolStripMenuItem)s).Tag);
                _miThemeRoot.DropDownItems.Add(item);
            }
            RefreshThemeChecks();
        }

        /// <summary>刷新主题菜单勾选状态</summary>
        private void RefreshThemeChecks()
        {
            if (_miThemeRoot == null) return;
            foreach (ToolStripItem it in _miThemeRoot.DropDownItems)
            {
                var mi = it as ToolStripMenuItem;
                if (mi == null) continue;
                mi.Checked = ((string)mi.Tag) == _settings.ThemeId;
            }
        }

        /// <summary>切换主题，立即生效并持久化</summary>
        private void SwitchTheme(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return;
            if (_settings.ThemeId == themeId) return;

            _settings.ThemeId = themeId;
            ApplyTheme();
            RefreshThemeChecks();
            SaveSettingsQuietly();
        }

        /// <summary>把当前主题应用到窗体、日历、托盘菜单</summary>
        private void ApplyTheme()
        {
            var theme = ThemeService.GetById(_settings.ThemeId);

            if (_calendar != null)
                _calendar.Theme = theme;

            BackColor = theme.FormBack;

            if (_trayMenu != null)
            {
                _trayMenu.BackColor = theme.IsDark
                    ? Color.FromArgb(45, 45, 48)
                    : Color.FromArgb(250, 250, 250);
                _trayMenu.ForeColor = theme.IsDark
                    ? Color.FromArgb(220, 220, 220)
                    : Color.FromArgb(30, 30, 30);
                foreach (ToolStripItem it in _trayMenu.Items)
                    it.ForeColor = _trayMenu.ForeColor;
            }
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
        /// <summary>
        /// 生成托盘图标：圆角日历 + 红色标题条 + 日期点阵。
        /// 使用 Clone 方式返回 Icon，随后立即释放 HICON，避免句柄泄漏。
        /// </summary>
        private static Icon CreateTrayIcon()
        {
            const int S = 32;
            using (var bmp = new Bitmap(S, S))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                var body = new Rectangle(3, 5, 26, 24);   // 日历主体区域

                // —— 1. 底部投影 ——
                using (var shadow = RoundedRect(new Rectangle(4, 7, 26, 24), 4))
                using (var b = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
                    g.FillPath(b, shadow);

                // —— 2. 白色纸面 ——
                using (var path = RoundedRect(body, 4))
                using (var b = new LinearGradientBrush(body,
                           Color.FromArgb(255, 255, 255),
                           Color.FromArgb(226, 236, 250), 90f))
                    g.FillPath(b, path);

                // —— 3. 顶部红色标题条 ——
                var head = new Rectangle(3, 5, 26, 8);
                using (var path = TopRoundedRect(head, 4))
                using (var b = new LinearGradientBrush(head,
                           Color.FromArgb(238, 92, 82),
                           Color.FromArgb(196, 48, 44), 90f))
                    g.FillPath(b, path);

                // —— 4. 两个挂环 ——
                using (var pen = new Pen(Color.FromArgb(120, 132, 148), 2.6f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 10.5f, 2.5f, 10.5f, 7f);
                    g.DrawLine(pen, 21.5f, 2.5f, 21.5f, 7f);
                }

                // —— 5. 纸面外描边 ——
                using (var path = RoundedRect(body, 4))
                using (var pen = new Pen(Color.FromArgb(160, 60, 90, 150), 1f))
                    g.DrawPath(pen, path);

                // —— 6. 日期点阵（今天用红点突出） ——
                using (var dot = new SolidBrush(Color.FromArgb(105, 140, 190)))
                {
                    for (int r = 0; r < 2; r++)
                        for (int c = 0; c < 3; c++)
                            g.FillEllipse(dot, 7.5f + c * 7f, 16.5f + r * 5.5f, 3f, 3f);
                }
                using (var today = new SolidBrush(Color.FromArgb(230, 78, 68)))
                    g.FillEllipse(today, 14.5f, 16.5f, 3.4f, 3.4f);

                // —— 7. 转成 Icon 并释放 HICON ——
                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    using (var tmp = Icon.FromHandle(hIcon))
                        return (Icon)tmp.Clone();   // Clone 出来的 Icon 拥有独立句柄
                }
                finally
                {
                    DestroyIcon(hIcon);             // 原始 HICON 立刻销毁
                }
            }
        }

        /// <summary>四角圆角矩形路径</summary>
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>仅顶部两角圆角的矩形路径（底部平直）</summary>
        private static GraphicsPath TopRoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            p.CloseFigure();
            return p;
        }

        private void WireEvents()
        {
            _calendar.AddAppointmentRequested += OnAddAppointmentRequested;
            _calendar.AppointmentDoubleClick += OnEditAppointment;
            _calendar.AppointmentDeleteRequested += OnDeleteAppointment;
            _calendar.MonthChanged += OnMonthChanged;
            _calendar.BorderHitTest += OnCalendarBorderHitTest;
            _calendar.CursorResolver = ResolveCursor;
        }

        // ====================================================================
        //  生命周期
        // ====================================================================
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyTheme();
            ApplyOpacitySafe();
            _miEmbedMode.Checked = _settings.EmbedToDesktop;
            _miFloatMode.Checked = !_settings.EmbedToDesktop;
        }

        private void ApplyOpacitySafe()
        {
            if (_settings == null) return;
            double op = Math.Max(0.3, Math.Min(1.0, _settings.WindowOpacity / 100.0));
            bool ok = WindowOpacity.Set(this, op);
            if (!ok) { try { Opacity = 1.0; } catch { } }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!Visible) Show();
            BringToFront();

            if (_settings.EmbedToDesktop)
                BeginInvoke(new Action(TryEmbedAfterShown));
        }

        private void TryEmbedAfterShown()
        {
            if (!Visible) Show();

            if (!DesktopEmbedService.Embed(this, Location))
            {
                _settings.EmbedToDesktop = false;
                _miEmbedMode.Checked = false;
                _miFloatMode.Checked = true;
                TopMost = false;
                ShowInTaskbar = false;
                BringToFront();
                Activate();
                ApplySettings();
                ApplyOpacitySafe();
                SaveSettingsQuietly();
                MessageBox.Show("无法嵌入桌面，已自动切换为浮动窗口模式。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _miEmbedMode.Checked = true;
            _miFloatMode.Checked = false;
            TopMost = false;
            ApplyOpacitySafe();
        }

        private void SwitchToEmbedMode()
        {
            if (DesktopEmbedService.IsEmbedded) return;
            if (!Visible) Show();

            if (!DesktopEmbedService.Embed(this, Location))
            {
                _settings.EmbedToDesktop = false;
                _miEmbedMode.Checked = false;
                _miFloatMode.Checked = true;
                ApplySettings();
                MessageBox.Show("嵌入桌面失败，已切换为浮动模式。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _settings.EmbedToDesktop = true;
            _miEmbedMode.Checked = true;
            _miFloatMode.Checked = false;
            TopMost = false;
            ApplyOpacitySafe();
            SaveSettingsQuietly();
        }

        private void SwitchToFloatMode()
        {
            if (!DesktopEmbedService.IsEmbedded) return;
            DesktopEmbedService.Detach(this);
            _settings.EmbedToDesktop = false;
            _miEmbedMode.Checked = false;
            _miFloatMode.Checked = true;
            ApplySettings();
            ApplyOpacitySafe();
            BringToFront();
            SaveSettingsQuietly();
        }

        private void SetAutoStart(bool enable)
        {
            if (enable)
            {
                if (!AutoStartService.Enable())
                {
                    MessageBox.Show("写入注册表失败，可能没有权限。", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _miAutoStart.Checked = false;
                    return;
                }
            }
            else AutoStartService.Disable();

            _settings.AutoStart = enable;
            SaveSettingsQuietly();
        }

        // ====================================================================
        //  设置对话框（带防重入锁）
        // ====================================================================
        private void ShowSettings()
        {
            // ★ 防重入：双击菜单或消息重入时只弹一次
            if (_settingsDialogOpen) return;
            _settingsDialogOpen = true;
            try
            {
                using (var dlg = new SettingsForm(_settings))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    var newSettings = dlg.Result;
                    bool embedChanged = newSettings.EmbedToDesktop != _settings.EmbedToDesktop;
                    bool autoStartChanged = newSettings.AutoStart != _settings.AutoStart;

                    if (autoStartChanged)
                    {
                        if (newSettings.AutoStart) AutoStartService.Enable();
                        else AutoStartService.Disable();
                        _miAutoStart.Checked = newSettings.AutoStart;
                    }

                    _settings.WindowOpacity = newSettings.WindowOpacity;
                    ApplyOpacitySafe();

                    if (dlg.ResetPositionRequested)
                    {
                        _settings.WindowX = -1;
                        _settings.WindowY = -1;
                        _settings.WindowWidth = 760;
                        _settings.WindowHeight = 600;
                        ApplySettings();
                    }

                    if (embedChanged)
                    {
                        _settings.EmbedToDesktop = newSettings.EmbedToDesktop;
                        if (_settings.EmbedToDesktop) SwitchToEmbedMode();
                        else SwitchToFloatMode();
                    }

                    _settings.AutoStart = newSettings.AutoStart;
                    SaveSettingsQuietly();
                }
            }
            finally
            {
                _settingsDialogOpen = false;
            }
        }

        private void SaveSettingsQuietly()
        {
            try { _settingsService.Save(_settings); } catch { }
        }

        private void ToggleVisibility()
        {
            if (Visible)
            {
                Hide();
                _miShowHide.Text = "显示日历";
            }
            else
            {
                Show();
                BringToFront();
                _miShowHide.Text = "隐藏日历";
            }
        }

        // ====================================================================
        //  日程事件
        // ====================================================================
        private async void OnMonthChanged(object sender, MonthChangedEventArgs e)
        {
            try
            {
                await _holidayService.LoadHolidaysAsync(e.Month.Year);
                _calendar.RefreshHolidays();
            }
            catch { }
        }

        private void OnAddAppointmentRequested(object sender, Models.DateRangeEventArgs e)
        {
            using (var form = new AppointmentEditForm(null, e.Start, e.End))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _aptService.Add(form.Result);
                    RefreshCalendar();
                }
            }
        }

        private void OnEditAppointment(object sender, AppointmentEventArgs e)
        {
            using (var form = new AppointmentEditForm(e.Appointment))
            {
                var dr = form.ShowDialog(this);
                if (dr != DialogResult.OK) return;
                if (form.DeleteRequested) _aptService.Delete(e.Appointment.Id);
                else _aptService.Update(form.Result);
                RefreshCalendar();
            }
        }

        private void OnDeleteAppointment(object sender, AppointmentEventArgs e)
        {
            if (MessageBox.Show(
                string.Format("确定要删除日程「{0}」吗？", e.Appointment.Title),
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _aptService.Delete(e.Appointment.Id);
                RefreshCalendar();
            }
        }

        private void RefreshCalendar()
        {
            _calendar.SetAppointments(_aptService.GetAll());
        }

        // ====================================================================
        //  手动拖动 / 调整大小
        // ====================================================================
        private void OnCalendarBorderHitTest(object sender, BorderHitEventArgs e)
        {
            var mode = DetermineDragMode(e.Location);
            if (mode == DragMode.None) return;

            e.Handled = true;
            _dragMode = mode;
            _dragStartScreen = Cursor.Position;
            _dragStartLocation = Location;
            _dragStartSize = Size;
            Capture = true;
        }

        private Cursor ResolveCursor(Point client)
        {
            if (_dragMode != DragMode.None) return null;

            var mode = DetermineDragMode(client);
            switch (mode)
            {
                case DragMode.ResizeTopLeft:
                case DragMode.ResizeBottomRight:
                    return Cursors.SizeNWSE;
                case DragMode.ResizeTopRight:
                case DragMode.ResizeBottomLeft:
                    return Cursors.SizeNESW;
                case DragMode.ResizeLeft:
                case DragMode.ResizeRight:
                    return Cursors.SizeWE;
                case DragMode.ResizeTop:
                case DragMode.ResizeBottom:
                    return Cursors.SizeNS;
                case DragMode.Move:
                    return Cursors.SizeAll;
                default:
                    return null;
            }
        }

        private DragMode DetermineDragMode(Point client)
        {
            int w = Width;
            int h = Height;
            int r = ResizeBorder;

            bool left = client.X >= 0 && client.X < r;
            bool right = client.X <= w && client.X > w - r;
            bool top = client.Y >= 0 && client.Y < r;
            bool bottom = client.Y <= h && client.Y > h - r;

            if (left && top) return DragMode.ResizeTopLeft;
            if (right && top) return DragMode.ResizeTopRight;
            if (left && bottom) return DragMode.ResizeBottomLeft;
            if (right && bottom) return DragMode.ResizeBottomRight;
            if (left) return DragMode.ResizeLeft;
            if (right) return DragMode.ResizeRight;
            if (top) return DragMode.ResizeTop;
            if (bottom) return DragMode.ResizeBottom;

            if (client.Y >= r && client.Y < TitleBarHeight
                && client.X > 110 && client.X < w - 50)
                return DragMode.Move;

            return DragMode.None;
        }

        private void DoDrag(Point screenPos)
        {
            int dx = screenPos.X - _dragStartScreen.X;
            int dy = screenPos.Y - _dragStartScreen.Y;

            int newW = _dragStartSize.Width;
            int newH = _dragStartSize.Height;
            int offsetX = 0;
            int offsetY = 0;

            switch (_dragMode)
            {
                case DragMode.Move:
                    offsetX = dx; offsetY = dy;
                    break;
                case DragMode.ResizeRight: newW += dx; break;
                case DragMode.ResizeBottom: newH += dy; break;
                case DragMode.ResizeLeft: newW -= dx; offsetX = dx; break;
                case DragMode.ResizeTop: newH -= dy; offsetY = dy; break;
                case DragMode.ResizeTopLeft:
                    newW -= dx; offsetX = dx;
                    newH -= dy; offsetY = dy; break;
                case DragMode.ResizeTopRight:
                    newW += dx;
                    newH -= dy; offsetY = dy; break;
                case DragMode.ResizeBottomLeft:
                    newW -= dx; offsetX = dx;
                    newH += dy; break;
                case DragMode.ResizeBottomRight:
                    newW += dx; newH += dy; break;
            }

            if (newW < MinimumSize.Width)
            {
                if (_dragMode == DragMode.ResizeLeft ||
                    _dragMode == DragMode.ResizeTopLeft ||
                    _dragMode == DragMode.ResizeBottomLeft)
                    offsetX = _dragStartSize.Width - MinimumSize.Width;
                newW = MinimumSize.Width;
            }
            if (newH < MinimumSize.Height)
            {
                if (_dragMode == DragMode.ResizeTop ||
                    _dragMode == DragMode.ResizeTopLeft ||
                    _dragMode == DragMode.ResizeTopRight)
                    offsetY = _dragStartSize.Height - MinimumSize.Height;
                newH = MinimumSize.Height;
            }

            if (newW != Width || newH != Height)
                Size = new Size(newW, newH);

            if (offsetX != 0 || offsetY != 0)
                Location = new Point(_dragStartLocation.X + offsetX,
                                     _dragStartLocation.Y + offsetY);
        }

        // ====================================================================
        //  窗口消息
        // ====================================================================
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTCLIENT;
                return;
            }

            if (m.Msg == WM_MOUSEMOVE)
            {
                if (_dragMode != DragMode.None)
                {
                    DoDrag(Cursor.Position);
                    return;
                }
            }

            if (m.Msg == WM_LBUTTONUP)
            {
                if (_dragMode != DragMode.None)
                {
                    _dragMode = DragMode.None;
                    Capture = false;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        // ====================================================================
        //  关闭
        // ====================================================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _miShowHide.Text = "显示日历";
                return;
            }

            try
            {
                _settings.WindowX = Location.X;
                _settings.WindowY = Location.Y;
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
                _settingsService.Save(_settings);
            }
            catch { }

            try { DesktopEmbedService.Detach(this); } catch { }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            base.OnFormClosing(e);
        }
    }
}
