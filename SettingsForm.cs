using CalendarPlugin.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin
{
    public partial class SettingsForm : Form
    {
        private CheckBox _chkAutoStart;
        private CheckBox _chkEmbed;
        private TrackBar _trackOpacity;
        private Label _lblOpacityValue;
        private Button _btnResetPosition;
        private Button _btnOk;
        private Button _btnCancel;

        public AppSettings Result { get; private set; }
        public bool ResetPositionRequested { get; private set; }

        public SettingsForm(AppSettings current)
        {
            Result = new AppSettings
            {
                AutoStart = current.AutoStart,
                EmbedToDesktop = current.EmbedToDesktop,
                WindowOpacity = current.WindowOpacity,
                WindowX = current.WindowX,
                WindowY = current.WindowY,
                WindowWidth = current.WindowWidth,
                WindowHeight = current.WindowHeight,
                ThemeId = current.ThemeId
            };
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "桌面日历设置";
            ClientSize = new Size(440, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("微软雅黑", 9.5f);
            BackColor = Color.White;

            var title = new Label
            {
                Text = "桌面日历设置",
                Font = new Font("微软雅黑", 13f, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(58, 118, 200)
            };
            Controls.Add(title);

            _chkAutoStart = new CheckBox
            {
                Text = "开机自动启动（写入注册表 HKCU\\Run）",
                Location = new Point(24, 65),
                AutoSize = true,
                Checked = Result.AutoStart
            };
            Controls.Add(_chkAutoStart);

            _chkEmbed = new CheckBox
            {
                Text = "嵌入桌面（作为桌面小部件显示，推荐）",
                Location = new Point(24, 100),
                AutoSize = true,
                Checked = Result.EmbedToDesktop
            };
            Controls.Add(_chkEmbed);

            var lblOpacity = new Label
            {
                Text = "窗口透明度：",
                Location = new Point(24, 145),
                AutoSize = true
            };
            Controls.Add(lblOpacity);

            _trackOpacity = new TrackBar
            {
                Minimum = 30,
                Maximum = 100,
                TickFrequency = 10,
                Location = new Point(120, 137),
                Width = 230,
                Value = Math.Max(30, Math.Min(100, Result.WindowOpacity))
            };
            _trackOpacity.ValueChanged += (s, e) =>
            {
                _lblOpacityValue.Text = _trackOpacity.Value + "%";
            };
            Controls.Add(_trackOpacity);

            _lblOpacityValue = new Label
            {
                Text = _trackOpacity.Value + "%",
                Location = new Point(355, 145),
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 90, 90)
            };
            Controls.Add(_lblOpacityValue);

            _btnResetPosition = new Button
            {
                Text = "重置窗口位置与大小",
                Location = new Point(24, 195),
                Size = new Size(180, 32),
                FlatStyle = FlatStyle.Flat
            };
            _btnResetPosition.Click += (s, e) =>
            {
                ResetPositionRequested = true;
                MessageBox.Show("已标记重置，确定后生效。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(_btnResetPosition);

            _btnOk = new Button
            {
                Text = "确定",
                Location = new Point(220, 280),
                Size = new Size(85, 32),
                BackColor = Color.FromArgb(58, 118, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += BtnOk_Click;
            Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(315, 280),
                Size = new Size(85, 32),
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            Result.AutoStart = _chkAutoStart.Checked;
            Result.EmbedToDesktop = _chkEmbed.Checked;
            Result.WindowOpacity = _trackOpacity.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
