using System.Windows;
using System.Windows.Interop;

namespace ClockWidg;

/// <summary>
/// Adds native edge/corner resizing to a borderless (WindowStyle=None) window
/// via WM_NCHITTEST, using raw physical pixels so it is DPI-correct.
/// </summary>
internal static class ResizeHook
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;
    private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
                      HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

    public static void Attach(Window window, double borderDip = 8.0)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var src = HwndSource.FromHwnd(hwnd);
            src?.AddHook((IntPtr h, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg != WM_NCHITTEST) return IntPtr.Zero;

                long lp = lParam.ToInt64();
                int mx = (short)(lp & 0xFFFF);
                int my = (short)((lp >> 16) & 0xFFFF);

                if (!NativeMethods.GetWindowRect(h, out var r)) return IntPtr.Zero;

                double dpi = src.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                int b = (int)Math.Ceiling(borderDip * dpi);

                bool left = mx <= r.Left + b, right = mx >= r.Right - b;
                bool top = my <= r.Top + b, bottom = my >= r.Bottom - b;

                int hit =
                    top && left ? HTTOPLEFT :
                    top && right ? HTTOPRIGHT :
                    bottom && left ? HTBOTTOMLEFT :
                    bottom && right ? HTBOTTOMRIGHT :
                    left ? HTLEFT :
                    right ? HTRIGHT :
                    top ? HTTOP :
                    bottom ? HTBOTTOM :
                    HTCLIENT;

                if (hit == HTCLIENT) return IntPtr.Zero;
                handled = true;
                return (IntPtr)hit;
            });
        };
    }
}
