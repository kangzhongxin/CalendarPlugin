using CalendarPlugin.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin.Controls
{
    public partial class AppointmentEditForm : Form
    {
        private TextBox _txtTitle;
        private TextBox _txtDesc;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private FlowLayoutPanel _colorPanel;
        private Button _btnOk;
        private Button _btnCancel;
        private Button _btnDelete;
        private Label _lblRange;

        private string _selectedColorHex = "#4A90E2";
        private readonly bool _isNew;
        private Appointment _editing;
        private bool _suppressRangeSync;

        public Appointment Result { get; private set; }
        public bool DeleteRequested { get; private set; }

        private static readonly string[] PaletteColors =
        {
            "#4A90E2", // 蓝
            "#E74C3C", // 红
            "#27AE60", // 绿
            "#F39C12", // 橙
            "#9B59B6", // 紫
            "#16A085", // 青
            "#E91E63", // 粉
            "#34495E", // 深灰
            "#F1C40F", // 黄
            "#7F8C8D"  // 灰
        };

        public AppointmentEditForm(Appointment existing = null,
            DateTime? start = null, DateTime? end = null)
        {
            _isNew = existing == null;
            _editing = existing;

            InitializeUI();

            if (_isNew)
            {
                Text = "新建日程";
                _dtStart.Value = (start ?? DateTime.Today).Date;
                _dtEnd.Value = (end ?? start ?? DateTime.Today).Date;
                _selectedColorHex = PaletteColors[0];
                _btnDelete.Visible = false;
                Result = new Appointment();
            }
            else
            {
                Text = "编辑日程";
                _txtTitle.Text = existing.Title;
                _txtDesc.Text = existing.Description;
                _dtStart.Value = existing.StartDate.Date;
                _dtEnd.Value = existing.EndDate.Date;
                _selectedColorHex = existing.ColorHex ?? PaletteColors[0];
                _btnDelete.Visible = true;
                Result = existing;
            }

            UpdateColorSelection();
            SyncRangeLabel();
        }

        private void InitializeUI()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 360);
            Font = new Font("微软雅黑", 9f);
            BackColor = Color.White;

            var lblTitle = new Label
            {
                Text = "标题：",
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            _txtTitle = new TextBox
            {
                Location = new Point(80, 17),
                Width = 320,
                Font = new Font("微软雅黑", 10f)
            };
            Controls.Add(_txtTitle);

            var lblDesc = new Label
            {
                Text = "描述：",
                Location = new Point(20, 55),
                AutoSize = true
            };
            Controls.Add(lblDesc);

            _txtDesc = new TextBox
            {
                Location = new Point(80, 52),
                Width = 320,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(_txtDesc);

            var lblStart = new Label
            {
                Text = "开始：",
                Location = new Point(20, 130),
                AutoSize = true
            };
            Controls.Add(lblStart);

            _dtStart = new DateTimePicker
            {
                Location = new Point(80, 127),
                Width = 140,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd"
            };
            _dtStart.ValueChanged += (s, e) => OnStartChanged();
            Controls.Add(_dtStart);

            var lblEnd = new Label
            {
                Text = "结束：",
                Location = new Point(230, 130),
                AutoSize = true
            };
            Controls.Add(lblEnd);

            _dtEnd = new DateTimePicker
            {
                Location = new Point(280, 127),
                Width = 120,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd"
            };
            _dtEnd.ValueChanged += (s, e) => OnEndChanged();
            Controls.Add(_dtEnd);

            _lblRange = new Label
            {
                Location = new Point(80, 155),
                Size = new Size(320, 18),
                ForeColor = Color.Gray,
                Font = new Font("微软雅黑", 8f)
            };
            Controls.Add(_lblRange);

            var lblColor = new Label
            {
                Text = "颜色：",
                Location = new Point(20, 185),
                AutoSize = true
            };
            Controls.Add(lblColor);

            _colorPanel = new FlowLayoutPanel
            {
                Location = new Point(80, 182),
                Size = new Size(320, 90),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false
            };
            Controls.Add(_colorPanel);

            foreach (var hex in PaletteColors)
            {
                var btn = new Button
                {
                    Size = new Size(28, 28),
                    Margin = new Padding(2),
                    BackColor = ColorTranslator.FromHtml(hex),
                    FlatStyle = FlatStyle.Flat,
                    Tag = hex,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.Gainsboro;
                btn.Click += (s, e) =>
                {
                    _selectedColorHex = (string)((Button)s).Tag;
                    UpdateColorSelection();
                };
                _colorPanel.Controls.Add(btn);
            }

            _btnOk = new Button
            {
                Text = "确定",
                Location = new Point(220, 300),
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
                Location = new Point(315, 300),
                Size = new Size(85, 32),
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(_btnCancel);

            _btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(20, 300),
                Size = new Size(85, 32),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 80, 80),
                FlatStyle = FlatStyle.Flat
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += BtnDelete_Click;
            Controls.Add(_btnDelete);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void OnStartChanged()
        {
            if (_suppressRangeSync) return;
            _suppressRangeSync = true;
            if (_dtEnd.Value < _dtStart.Value)
                _dtEnd.Value = _dtStart.Value;
            _suppressRangeSync = false;
            SyncRangeLabel();
        }

        private void OnEndChanged()
        {
            if (_suppressRangeSync) return;
            _suppressRangeSync = true;
            if (_dtStart.Value > _dtEnd.Value)
                _dtStart.Value = _dtEnd.Value;
            _suppressRangeSync = false;
            SyncRangeLabel();
        }

        private void SyncRangeLabel()
        {
            int days = (_dtEnd.Value.Date - _dtStart.Value.Date).Days + 1;
            _lblRange.Text = string.Format("共 {0} 天", days);
        }

        private void UpdateColorSelection()
        {
            foreach (Control c in _colorPanel.Controls)
            {
                var btn = c as Button;
                if (btn == null) continue;
                bool selected = (string)btn.Tag == _selectedColorHex;
                btn.FlatAppearance.BorderSize = selected ? 3 : 1;
                btn.FlatAppearance.BorderColor = selected
                    ? Color.FromArgb(40, 40, 40)
                    : Color.Gainsboro;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            {
                MessageBox.Show("请输入日程标题。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTitle.Focus();
                return;
            }

            if (_isNew)
            {
                Result = new Appointment();
            }

            Result.Title = _txtTitle.Text.Trim();
            Result.Description = _txtDesc.Text.Trim();
            Result.StartDate = _dtStart.Value.Date;
            Result.EndDate = _dtEnd.Value.Date;
            Result.ColorHex = _selectedColorHex;
            Result.IsAllDay = true;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除该日程吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DeleteRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
