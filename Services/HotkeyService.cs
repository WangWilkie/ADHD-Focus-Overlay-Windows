using System;
using System.Collections.Generic;
using System.Windows.Interop;
using AdhdFocusOverlay.Infrastructure;
using AdhdFocusOverlay.Models;

namespace AdhdFocusOverlay.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly HwndSource hwndSource;
    private readonly Dictionary<int, Action> actions = new();

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("FocusFrameHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };
        hwndSource = new HwndSource(parameters);
        hwndSource.AddHook(WndProc);
    }

    public bool Register(int id, HotkeyBinding binding, Action action)
    {
        Unregister(id);
        if (!NativeMethods.RegisterHotKey(hwndSource.Handle, id, binding.Modifiers, (uint)binding.VirtualKey))
        {
            return false;
        }

        actions[id] = action;
        return true;
    }

    public void UnregisterAll()
    {
        foreach (var id in actions.Keys)
        {
            NativeMethods.UnregisterHotKey(hwndSource.Handle, id);
        }

        actions.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        hwndSource.RemoveHook(WndProc);
        hwndSource.Dispose();
    }

    private void Unregister(int id)
    {
        if (!actions.Remove(id))
        {
            return;
        }

        NativeMethods.UnregisterHotKey(hwndSource.Handle, id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotKey && actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
