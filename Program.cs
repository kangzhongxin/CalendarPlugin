using CalendarPlugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin
{
    internal static class Program
    {
        private const string MutexName = "DesktopCalendarPlugin_SingleInstance";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    string logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "CalendarPlugin", "crash.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    System.IO.File.AppendAllText(logPath,
                        DateTime.Now + "  " + e.ExceptionObject + Environment.NewLine);
                }
                catch { }

                MessageBox.Show("程序发生异常：\n" + e.ExceptionObject,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show("UI 线程异常：\n" + e.Exception,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            // 单实例：避免自启动时重复弹窗
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("桌面日历已在运行中（请查看系统托盘图标）。",
                        "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                bool autoStart = args != null
                    && args.Any(a => a.Equals("--autostart",
                        StringComparison.OrdinalIgnoreCase));

                var settingsService = new SettingsService();
                var settings = settingsService.Load();
                bool safeMode = args != null
&& args.Any(a => a.Equals("--safe", StringComparison.OrdinalIgnoreCase));

                if (safeMode)
                {
                    settings.EmbedToDesktop = false;
                }
                // 自启动时强制进入嵌入模式
                if (autoStart) settings.EmbedToDesktop = true;

                Application.Run(new MainForm(settings, settingsService));
            }
        }
    }
}
