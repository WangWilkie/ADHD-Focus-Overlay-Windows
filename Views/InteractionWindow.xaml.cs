using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using AdhdFocusOverlay.Controllers;
using AdhdFocusOverlay.Infrastructure;
using AdhdFocusOverlay.Models;
using WpfPoint = System.Windows.Point;

namespace AdhdFocusOverlay.Views;

public partial class InteractionWindow : Window
{
    private const int HandleThickness = 18;

    private readonly FocusRegionController focusRegionController;
    private HwndSource? hwndSource;
    private bool interactionEnabled = true;
    private bool dragActive;
    private bool moveModeActive;
    private int deviceX;
    private int deviceY;
    private int deviceWidth;
    private int deviceHeight;

    public InteractionWindow(FocusRegionController focusRegionController)
    {
        InitializeComponent();
        this.focusRegionController = focusRegionController;
        SourceInitialized += InteractionWindow_SourceInitialized;
        focusRegionController.StateChanged += (_, _) => SynchronizeBounds();
    }

    public event EventHandler<bool>? InteractionStateChanged;

    public void SetInteractionEnabled(bool enabled)
    {
        if (interactionEnabled == enabled)
        {
            return;
        }

        interactionEnabled = enabled;
        if (hwndSource is not null)
        {
            NativeMethods.SetClickThrough(hwndSource.Handle, !enabled);
            if (enabled)
            {
                EnsureTopmost();
            }
        }
    }

    public void SynchronizeBounds()
    {
        var focus = focusRegionController.FocusRegion;
        if (deviceX == focus.X &&
            deviceY == focus.Y &&
            deviceWidth == focus.Width &&
            deviceHeight == focus.Height)
        {
            return;
        }

        deviceX = focus.X;
        deviceY = focus.Y;
        deviceWidth = focus.Width;
        deviceHeight = focus.Height;

        if (hwndSource is not null)
        {
            NativeMethods.SetWindowPos(
                hwndSource.Handle,
                IntPtr.Zero,
                deviceX,
                deviceY,
                deviceWidth,
                deviceHeight,
                NativeMethods.SwpNoActivate |
                NativeMethods.SwpNoZOrder |
                NativeMethods.SwpShowWindow);
            return;
        }

        var topLeft = DeviceToLogical(focus.X, focus.Y);
        var bottomRight = DeviceToLogical(focus.Right, focus.Bottom);
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = bottomRight.X - topLeft.X;
        Height = bottomRight.Y - topLeft.Y;
    }

    private void InteractionWindow_SourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            hwndSource = source;
            NativeMethods.MakeToolWindow(source.Handle);
            NativeMethods.SetClickThrough(source.Handle, false);
            source.AddHook(WndProc);
            EnsureTopmost();
        }

        SynchronizeBounds();
    }

    private void ResizeThumb_OnDragStarted(object sender, DragStartedEventArgs e)
    {
        SetDragActive(true);
    }

    private void ResizeThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!interactionEnabled ||
            sender is not Thumb thumb ||
            !Enum.TryParse(thumb.Tag?.ToString(), out ResizeHandle handle) ||
            handle == ResizeHandle.None)
        {
            return;
        }

        var delta = LogicalDeltaToDevice(e.HorizontalChange, e.VerticalChange);
        focusRegionController.Resize(handle, delta.dx, delta.dy);
    }

    private void ResizeThumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        SetDragActive(false);
    }

    private void MoveThumb_OnDragStarted(object sender, DragStartedEventArgs e)
    {
        moveModeActive = IsMoveModifierActive();
        if (moveModeActive)
        {
            SetDragActive(true);
        }
    }

    private void MoveThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!interactionEnabled || !moveModeActive)
        {
            return;
        }

        var delta = LogicalDeltaToDevice(e.HorizontalChange, e.VerticalChange);
        focusRegionController.Move(delta.dx, delta.dy);
    }

    private void MoveThumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        moveModeActive = false;
        SetDragActive(false);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!interactionEnabled || msg != NativeMethods.WmNchitTest)
        {
            return IntPtr.Zero;
        }

        var screenX = unchecked((short)(lParam.ToInt32() & 0xFFFF));
        var screenY = unchecked((short)((lParam.ToInt32() >> 16) & 0xFFFF));
        var localX = screenX - deviceX;
        var localY = screenY - deviceY;
        var insideCenter =
            localX > HandleThickness &&
            localX < deviceWidth - HandleThickness &&
            localY > HandleThickness &&
            localY < deviceHeight - HandleThickness;

        handled = true;
        if (insideCenter)
        {
            return new IntPtr(IsMoveModifierActive() ? NativeMethods.HtClient : NativeMethods.HtTransparent);
        }

        return new IntPtr(NativeMethods.HtClient);
    }

    private (int dx, int dy) LogicalDeltaToDevice(double dx, double dy)
    {
        if (hwndSource?.CompositionTarget is null)
        {
            return ((int)Math.Round(dx), (int)Math.Round(dy));
        }

        var matrix = hwndSource.CompositionTarget.TransformToDevice;
        return (
            (int)Math.Round(dx * matrix.M11),
            (int)Math.Round(dy * matrix.M22));
    }

    private WpfPoint DeviceToLogical(double x, double y)
    {
        if (hwndSource?.CompositionTarget is null)
        {
            return new WpfPoint(x, y);
        }

        return hwndSource.CompositionTarget.TransformFromDevice.Transform(new WpfPoint(x, y));
    }

    private void EnsureTopmost()
    {
        if (hwndSource is null)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            hwndSource.Handle,
            NativeMethods.HwndTopMost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoActivate |
            NativeMethods.SwpNoMove |
            NativeMethods.SwpNoSize |
            NativeMethods.SwpShowWindow);
    }

    private static bool IsMoveModifierActive()
    {
        var altPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VkMenu) & 0x8000) != 0;
        var shiftPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VkShift) & 0x8000) != 0;
        return altPressed && shiftPressed;
    }

    private void SetDragActive(bool active)
    {
        if (dragActive == active)
        {
            return;
        }

        dragActive = active;
        InteractionStateChanged?.Invoke(this, active);
    }
}
