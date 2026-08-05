using System.Runtime.InteropServices;

namespace ClockWidg;

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
