using CalendarPlugin.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalendarPlugin.Helpers
{
    /// <summary>
    /// 直接操作 WS_EX_LAYERED / SetLayeredWindowAttributes，
    /// 绕过 Form.Opacity 在句柄重建、子窗口场景下的 Win32Exception。
    /// </summary>
    public static class WindowOpacity
    {
        /// <summary>
        /// 设置窗口透明度。opacity 取值 0.0~1.0；>= 0.999 时恢复为完全不透明。
        /// 必须在句柄已创建后调用。
        /// </summary>
        public static bool Set(Form form, double opacity)
        {
            if (form == null || !form.IsHandleCreated) return false;

            try
            {
                double op = Math.Max(0.0, Math.Min(1.0, opacity));
                IntPtr hwnd = form.Handle;

                int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

                if (op >= 0.999)
                {
                    // 关闭分层
                    if ((exStyle & NativeMethods.WS_EX_LAYERED) != 0)
                    {
                        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                            exStyle & ~NativeMethods.WS_EX_LAYERED);
                    }
                    return true;
                }

                // 打开分层
                if ((exStyle & NativeMethods.WS_EX_LAYERED) == 0)
                {
                    NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                        exStyle | NativeMethods.WS_EX_LAYERED);
                }

                byte alpha = (byte)Math.Round(op * 255);
                if (!NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha,
                    NativeMethods.LWA_ALPHA))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
