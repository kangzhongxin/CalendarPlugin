using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin.Services
{
    /// <summary>
    /// 通过注册表 HKCU\Software\Microsoft\Windows\CurrentVersion\Run 实现自启动。
    /// 使用 HKCU 无需管理员权限。
    /// </summary>
    public static class AutoStartService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "DesktopCalendarPlugin";

        public static bool IsEnabled
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    {
                        if (key == null) return false;
                        var value = key.GetValue(AppName) as string;
                        return !string.IsNullOrEmpty(value);
                    }
                }
                catch { return false; }
            }
        }

        public static bool Enable()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return false;
                    key.SetValue(AppName, "\"" + exePath + "\" --autostart");
                }
                return true;
            }
            catch { return false; }
        }

        public static bool Disable()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return false;
                    if (key.GetValue(AppName) != null)
                        key.DeleteValue(AppName, false);
                }
                return true;
            }
            catch { return false; }
        }
    }
}
