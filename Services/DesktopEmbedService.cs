using CalendarPlugin.Helpers;
using CalendarPlugin.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin.Services
{
    /// <summary>
    /// 将窗口挂载到桌面壁纸层（WorkerW）。
    /// 原理：向 Progman 发送 0x052C 消息，触发系统生成承载壁纸的 WorkerW 窗口；
    /// 随后找到 SHELLDLL_DefView 的兄弟 WorkerW，把目标窗口 SetParent 到它下面。
    /// 这样窗口就"长"在桌面上，位于图标层之下、壁纸之上，且不显示在任务栏/Alt+Tab。
    /// </summary>
    public static class DesktopEmbedService
    {
        private static bool _isEmbedded;
        public static bool IsEmbedded => _isEmbedded;

        public static bool Embed(Form form, Point location)
        {
            Log("========== Embed 开始 ==========");
            Log("form.Handle = " + form.Handle);
            Log("location = " + location);
            Log("form.Size = " + form.Size);

            if (_isEmbedded) { Log("已经嵌入，直接返回 true"); return true; }

            try
            {
                IntPtr progman = NativeMethods.FindWindow("Progman", null);
                Log("Progman = " + progman);
                if (progman == IntPtr.Zero) { Log("找不到 Progman，嵌入失败"); return false; }

                // 触发 WorkerW 分裂（在部分系统上会创建出 WorkerW）
                IntPtr result;
                NativeMethods.SendMessageTimeout(progman, 0x052C,
                    new IntPtr(0xD), new IntPtr(0x1),
                    NativeMethods.SMTO_NORMAL, 1000, out result);
                Log("已发送 0x052C 消息");

                // ========== 收集所有候选父窗口 ==========
                IntPtr defView = IntPtr.Zero;       // SHELLDLL_DefView
                IntPtr defViewParent = IntPtr.Zero; // DefView 的父窗口
                IntPtr workerw = IntPtr.Zero;       // DefView 父窗口的兄弟 WorkerW

                NativeMethods.EnumWindows((tophandle, l) =>
                {
                    IntPtr sv = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero,
                        "SHELLDLL_DefView", null);
                    if (sv != IntPtr.Zero)
                    {
                        defView = sv;
                        defViewParent = tophandle;
                        string cls = GetClassName(tophandle);
                        Log($"找到 SHELLDLL_DefView={sv}，父窗口={tophandle} 类名={cls}");

                        workerw = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle,
                            "WorkerW", null);
                        Log("兄弟 WorkerW = " + workerw);
                    }
                    return true;
                }, IntPtr.Zero);

                // ========== 选择目标父窗口（按优先级） ==========
                // 优先级：
                //   1. 兄弟 WorkerW（标准做法，Win7~Win10）
                //   2. SHELLDLL_DefView 本身（Win11 24H2+，无独立 WorkerW 时）
                //   3. DefView 的父窗口（Progman）
                //   4. Progman 兜底
                IntPtr targetParent;
                string parentKind;

                if (workerw != IntPtr.Zero)
                {
                    targetParent = workerw;
                    parentKind = "WorkerW";
                }
                else if (defView != IntPtr.Zero)
                {
                    targetParent = defView;
                    parentKind = "SHELLDLL_DefView";
                    Log("未找到 WorkerW，改用 SHELLDLL_DefView 作为父窗口");
                }
                else if (defViewParent != IntPtr.Zero)
                {
                    targetParent = defViewParent;
                    parentKind = "DefViewParent";
                    Log("未找到 WorkerW 和 DefView，改用 DefView 父窗口");
                }
                else
                {
                    targetParent = progman;
                    parentKind = "Progman";
                    Log("全部未找到，回退到 Progman");
                }

                Log($"最终目标父窗口: {targetParent} (类型={parentKind})");

                // ========== 修改窗口样式 ==========
                int exStyle = NativeMethods.GetWindowLong(form.Handle, NativeMethods.GWL_EXSTYLE);
                int style = NativeMethods.GetWindowLong(form.Handle, NativeMethods.GWL_STYLE);
                Log("原 exStyle=0x" + exStyle.ToString("X") + ", style=0x" + style.ToString("X"));

                //exStyle &= ~NativeMethods.WS_EX_LAYERED;
                exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                NativeMethods.SetWindowLong(form.Handle, NativeMethods.GWL_EXSTYLE, exStyle);

                style &= ~NativeMethods.WS_POPUP;
                style |= NativeMethods.WS_CHILD;
                NativeMethods.SetWindowLong(form.Handle, NativeMethods.GWL_STYLE, style);

                // ========== SetParent ==========
                Log("SetParent 之前，当前 GetParent=" + NativeMethods.GetParent(form.Handle));
                IntPtr prevParent = NativeMethods.SetParent(form.Handle, targetParent);
                int lastErr = Marshal.GetLastWin32Error();
                Log("SetParent 返回 prevParent=" + prevParent + ", lastErr=" + lastErr);

                // ========== 位置与 Z 序 ==========
                // 关键：挂到 DefView 时，用 HWND_TOP 让窗口在 SysListView32 之上；
                //       挂到 WorkerW 或 Progman 时，用 HWND_BOTTOM 让窗口沉入壁纸层。
                IntPtr insertAfter;
                if (parentKind == "SHELLDLL_DefView")
                {
                    // DefView 下：放最顶层，图标层(SysListView32)默认也是子窗口，
                    // 视觉上图标仍然可见（SysListView32 是透明的）
                    insertAfter = NativeMethods.HWND_TOP;
                }
                else
                {
                    insertAfter = NativeMethods.HWND_BOTTOM;
                }

                NativeMethods.SetWindowPos(form.Handle, insertAfter,
                    location.X, location.Y, form.Width, form.Height,
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

                // ========== 强制显示 ==========
                NativeMethods.ShowWindow(form.Handle, NativeMethods.SW_SHOW);

                // ========== 校验 ==========
                IntPtr actualParent = NativeMethods.GetParent(form.Handle);
                bool visible = NativeMethods.IsWindowVisible(form.Handle);
                Log("挂载后 GetParent=" + actualParent + ", IsVisible=" + visible);

                if (actualParent != targetParent)
                {
                    Log("父窗口不匹配，嵌入失败，回退");
                    RestoreTopLevel(form, style, exStyle);
                    return false;
                }

                _isEmbedded = true;
                Log($"========== Embed 成功 (父类型={parentKind}) ==========");
                return true;
            }
            catch (Exception ex)
            {
                Log("Embed 异常：" + ex);
                return false;
            }
        }

        public static void Detach(Form form)
        {
            if (!_isEmbedded) return;
            try
            {
                Log("开始 Detach");
                NativeMethods.SetParent(form.Handle, IntPtr.Zero);

                int style = NativeMethods.GetWindowLong(form.Handle, NativeMethods.GWL_STYLE);
                style &= ~NativeMethods.WS_CHILD;
                style |= NativeMethods.WS_POPUP;
                NativeMethods.SetWindowLong(form.Handle, NativeMethods.GWL_STYLE, style);

                int exStyle = NativeMethods.GetWindowLong(form.Handle, NativeMethods.GWL_EXSTYLE);
                exStyle &= ~NativeMethods.WS_EX_TOOLWINDOW;
                NativeMethods.SetWindowLong(form.Handle, NativeMethods.GWL_EXSTYLE, exStyle);

                _isEmbedded = false;
                Log("Detach 完成");
            }
            catch (Exception ex)
            {
                Log("Detach 异常：" + ex);
            }
        }

        private static void RestoreTopLevel(Form form, int style, int exStyle)
        {
            try
            {
                NativeMethods.SetParent(form.Handle, IntPtr.Zero);
                style &= ~NativeMethods.WS_CHILD;
                style |= NativeMethods.WS_POPUP;
                NativeMethods.SetWindowLong(form.Handle, NativeMethods.GWL_STYLE, style);
                exStyle &= ~NativeMethods.WS_EX_TOOLWINDOW;
                NativeMethods.SetWindowLong(form.Handle, NativeMethods.GWL_EXSTYLE, exStyle);
            }
            catch { }
            _isEmbedded = false;
        }

        private static string GetClassName(IntPtr hwnd)
        {
            try
            {
                var sb = new System.Text.StringBuilder(256);
                NativeMethods.GetClassName(hwnd, sb, sb.Capacity);
                return sb.ToString();
            }
            catch { return "?"; }
        }

        /// <summary>
        /// 同时写入 %APPDATA% 和 exe 所在目录，保证一定能找到。
        /// </summary>
        private static void Log(string msg)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + msg + Environment.NewLine;

            // 位置 1：%APPDATA%\CalendarPlugin\embed.log
            TryWrite(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CalendarPlugin", "embed.log"), line);

            // 位置 2：exe 所在目录\embed.log
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                if (!string.IsNullOrEmpty(exeDir))
                    TryWrite(Path.Combine(exeDir, "embed.log"), line);
            }
            catch { }
        }

        private static void TryWrite(string path, string line)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText(path, line);
            }
            catch { }
        }
    }
}
