using System;
using System.Runtime.InteropServices;

namespace AdhdFocusOverlay.Infrastructure;

internal static class NativeMethods
{
    public const int GwlExStyle = -20;
    public const int HtTransparent = -1;
    public const int HtClient = 1;
    public const int WmNchitTest = 0x0084;
    public const int WmHotKey = 0x0312;
    public const int WsExTransparent = 0x20;
    public const int WsExToolWindow = 0x80;
    public const int WsExNoActivate = 0x08000000;
    public static readonly IntPtr HwndTopMost = new(-1);
    public const uint SwpNoActivate = 0x0010;
    public const uint SwpNoMove = 0x0002;
    public const uint SwpNoSize = 0x0001;
    public const uint SwpNoZOrder = 0x0004;
    public const uint SwpShowWindow = 0x0040;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    public static void MakeToolWindow(IntPtr handle)
    {
        var style = GetWindowStyle(handle);
        style |= WsExToolWindow | WsExNoActivate;
        SetWindowStyle(handle, style);
    }

    public static void SetClickThrough(IntPtr handle, bool enabled)
    {
        var style = GetWindowStyle(handle);
        if (enabled)
        {
            style |= WsExTransparent;
        }
        else
        {
            style &= ~WsExTransparent;
        }

        SetWindowStyle(handle, style);
    }

    private static int GetWindowStyle(IntPtr handle)
    {
        return unchecked((int)GetWindowLongPtr(handle, GwlExStyle).ToInt64());
    }

    private static void SetWindowStyle(IntPtr handle, int style)
    {
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style));
    }
}
