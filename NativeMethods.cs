using System;
using System.Runtime.InteropServices;

namespace AdhdFocusOverlay
{
    internal static class NativeMethods
    {
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x20;
        private const int WsExToolWindow = 0x80;
        private const int WsExNoActivate = 0x08000000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        public static void MakeOverlayWindow(IntPtr handle)
        {
            var style = GetWindowLong32(handle, GwlExStyle);
            style |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            SetWindowLong32(handle, GwlExStyle, style);
        }

        public static void MakeToolWindow(IntPtr handle)
        {
            var style = GetWindowLong32(handle, GwlExStyle);
            style |= WsExToolWindow | WsExNoActivate;
            SetWindowLong32(handle, GwlExStyle, style);
        }
    }
}
