using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AdhdFocusOverlay.Controllers;
using AdhdFocusOverlay.Infrastructure;
using AdhdFocusOverlay.Models;
using WpfPoint = System.Windows.Point;
using MediaColor = System.Windows.Media.Color;

namespace AdhdFocusOverlay.Views;

public partial class OverlayWindow : Window
{
    private HwndSource? hwndSource;
    private MonitorInfo? monitor;
    private FocusRegion? currentFocusRegion;
    private VisualEffectController? currentVisualEffectController;
    private System.Drawing.Rectangle? lastAppliedBounds;
    private FocusRegion? lastFocusRegion;
    private MediaColor? lastTint;
    private byte lastAlpha;
    private bool hasAppliedMask;
    private bool lastLightweightMode;
    private bool currentLightweightMode;
    private bool currentPickModeEnabled;
    private bool appliedPickModeEnabled;
    private int deviceX;
    private int deviceY;
    private int deviceWidth;
    private int deviceHeight;

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += OverlayWindow_SourceInitialized;
    }

    public event EventHandler<MonitorInfo>? MonitorClicked;

    public void UpdateState(
        MonitorInfo monitor,
        FocusRegion? focusRegion,
        VisualEffectController visualEffectController,
        bool pickModeEnabled,
        bool lightweightMode)
    {
        this.monitor = monitor;
        currentFocusRegion = focusRegion;
        currentVisualEffectController = visualEffectController;
        currentPickModeEnabled = pickModeEnabled;
        currentLightweightMode = lightweightMode;
        ApplyWindowBounds(monitor.Bounds);
        UpdateBrush(visualEffectController, lightweightMode);
        UpdateMask(focusRegion, lightweightMode);
        SetPickMode(pickModeEnabled);
    }

    public void SetPickMode(bool enabled)
    {
        currentPickModeEnabled = enabled;
        if (appliedPickModeEnabled == enabled)
        {
            return;
        }

        appliedPickModeEnabled = enabled;
        PickModeBadge.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        if (hwndSource is not null)
        {
            NativeMethods.SetClickThrough(hwndSource.Handle, !enabled);
        }
    }

    private void OverlayWindow_SourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            hwndSource = source;
            NativeMethods.MakeToolWindow(source.Handle);
            NativeMethods.SetClickThrough(source.Handle, true);
            EnsureTopmost();
        }

        ReapplyCurrentState();
    }

    private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!currentPickModeEnabled || monitor is null)
        {
            return;
        }

        MonitorClicked?.Invoke(this, monitor);
        e.Handled = true;
    }

    private void UpdateBrush(VisualEffectController visualEffectController, bool lightweightMode)
    {
        var tint = visualEffectController.TintColor;
        var baseAlpha = currentPickModeEnabled ? (byte)Math.Min(245, visualEffectController.OpacityByte + 25) : visualEffectController.OpacityByte;
        var alpha = lightweightMode
            ? (byte)Math.Max(30, baseAlpha - 15)
            : baseAlpha;
        if (lastTint == tint && lastAlpha == alpha)
        {
            return;
        }

        var brush = new SolidColorBrush(MediaColor.FromArgb(alpha, tint.R, tint.G, tint.B));
        brush.Freeze();
        MaskPath.Fill = brush;
        TopMask.Fill = brush;
        LeftMask.Fill = brush;
        RightMask.Fill = brush;
        BottomMask.Fill = brush;
        lastTint = tint;
        lastAlpha = alpha;
    }

    private void UpdateMask(FocusRegion? focusRegion, bool lightweightMode)
    {
        if (monitor is null)
        {
            return;
        }

        if (hasAppliedMask && lastFocusRegion == focusRegion && lastLightweightMode == lightweightMode)
        {
            return;
        }

        var monitorTopLeft = DeviceToLogical(monitor.Bounds.Left, monitor.Bounds.Top);
        var monitorBottomRight = DeviceToLogical(monitor.Bounds.Right, monitor.Bounds.Bottom);
        var outer = new Rect(0, 0, monitorBottomRight.X - monitorTopLeft.X, monitorBottomRight.Y - monitorTopLeft.Y);

        if (lightweightMode)
        {
            MaskPath.Visibility = Visibility.Collapsed;
            LightweightMaskCanvas.Visibility = Visibility.Visible;
            UpdateLightweightMask(outer, monitorTopLeft, focusRegion);
            lastFocusRegion = focusRegion;
            lastLightweightMode = true;
            hasAppliedMask = true;
            return;
        }

        MaskPath.Visibility = Visibility.Visible;
        LightweightMaskCanvas.Visibility = Visibility.Collapsed;

        if (focusRegion is null)
        {
            var rectangleGeometry = new RectangleGeometry(outer);
            rectangleGeometry.Freeze();
            MaskPath.Data = rectangleGeometry;
            lastFocusRegion = null;
            lastLightweightMode = false;
            hasAppliedMask = true;
            return;
        }

        var focus = focusRegion.Value;
        var focusTopLeft = DeviceToLogical(focus.X, focus.Y);
        var focusBottomRight = DeviceToLogical(focus.Right, focus.Bottom);
        var inner = new Rect(
            focusTopLeft.X - monitorTopLeft.X,
            focusTopLeft.Y - monitorTopLeft.Y,
            focusBottomRight.X - focusTopLeft.X,
            focusBottomRight.Y - focusTopLeft.Y);

        var geometry = new StreamGeometry
        {
            FillRule = FillRule.EvenOdd
        };

        using (var context = geometry.Open())
        {
            context.BeginFigure(outer.TopLeft, true, true);
            context.LineTo(outer.TopRight, true, false);
            context.LineTo(outer.BottomRight, true, false);
            context.LineTo(outer.BottomLeft, true, false);

            context.BeginFigure(inner.TopLeft, true, true);
            context.LineTo(inner.TopRight, true, false);
            context.LineTo(inner.BottomRight, true, false);
            context.LineTo(inner.BottomLeft, true, false);
        }

        geometry.Freeze();
        MaskPath.Data = geometry;
        lastFocusRegion = focusRegion;
        lastLightweightMode = false;
        hasAppliedMask = true;
    }

    private void ApplyWindowBounds(System.Drawing.Rectangle bounds)
    {
        if (lastAppliedBounds == bounds)
        {
            return;
        }

        deviceX = bounds.Left;
        deviceY = bounds.Top;
        deviceWidth = bounds.Width;
        deviceHeight = bounds.Height;

        if (hwndSource is not null)
        {
            NativeMethods.SetWindowPos(
                hwndSource.Handle,
                NativeMethods.HwndTopMost,
                deviceX,
                deviceY,
                deviceWidth,
                deviceHeight,
                NativeMethods.SwpNoActivate |
                NativeMethods.SwpShowWindow);
        lastAppliedBounds = bounds;
        lastFocusRegion = null;
        hasAppliedMask = false;
        lastLightweightMode = false;
        return;
        }

        var topLeft = DeviceToLogical(bounds.Left, bounds.Top);
        var bottomRight = DeviceToLogical(bounds.Right, bounds.Bottom);
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = bottomRight.X - topLeft.X;
        Height = bottomRight.Y - topLeft.Y;
        lastAppliedBounds = bounds;
        lastFocusRegion = null;
        hasAppliedMask = false;
        lastLightweightMode = false;
    }

    private WpfPoint DeviceToLogical(double x, double y)
    {
        if (hwndSource?.CompositionTarget is null)
        {
            return new WpfPoint(x, y);
        }

        return hwndSource.CompositionTarget.TransformFromDevice.Transform(new WpfPoint(x, y));
    }

    private void UpdateLightweightMask(Rect outer, WpfPoint monitorTopLeft, FocusRegion? focusRegion)
    {
        if (focusRegion is null)
        {
            SetMaskRectangle(TopMask, 0, 0, outer.Width, outer.Height);
            SetMaskRectangle(LeftMask, 0, 0, 0, 0);
            SetMaskRectangle(RightMask, 0, 0, 0, 0);
            SetMaskRectangle(BottomMask, 0, 0, 0, 0);
            return;
        }

        var focus = focusRegion.Value;
        var focusTopLeft = DeviceToLogical(focus.X, focus.Y);
        var focusBottomRight = DeviceToLogical(focus.Right, focus.Bottom);
        var innerX = focusTopLeft.X - monitorTopLeft.X;
        var innerY = focusTopLeft.Y - monitorTopLeft.Y;
        var innerWidth = focusBottomRight.X - focusTopLeft.X;
        var innerHeight = focusBottomRight.Y - focusTopLeft.Y;

        SetMaskRectangle(TopMask, 0, 0, outer.Width, Math.Max(0, innerY));
        SetMaskRectangle(LeftMask, 0, innerY, Math.Max(0, innerX), Math.Max(0, innerHeight));
        SetMaskRectangle(RightMask, innerX + innerWidth, innerY, Math.Max(0, outer.Width - innerX - innerWidth), Math.Max(0, innerHeight));
        SetMaskRectangle(BottomMask, 0, innerY + innerHeight, outer.Width, Math.Max(0, outer.Height - innerY - innerHeight));
    }

    private static void SetMaskRectangle(System.Windows.Shapes.Rectangle rectangle, double left, double top, double width, double height)
    {
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
        rectangle.Width = width;
        rectangle.Height = height;
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

    public void RefreshTopmost()
    {
        EnsureTopmost();
    }

    private void ReapplyCurrentState()
    {
        if (monitor is null || currentVisualEffectController is null)
        {
            return;
        }

        lastAppliedBounds = null;
        lastFocusRegion = null;
        hasAppliedMask = false;
        lastTint = null;
        lastAlpha = 0;
        lastLightweightMode = !currentLightweightMode;

        ApplyWindowBounds(monitor.Bounds);
        UpdateBrush(currentVisualEffectController, currentLightweightMode);
        UpdateMask(currentFocusRegion, currentLightweightMode);

        appliedPickModeEnabled = !currentPickModeEnabled;
        PickModeBadge.Visibility = currentPickModeEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (hwndSource is not null)
        {
            NativeMethods.SetClickThrough(hwndSource.Handle, !currentPickModeEnabled);
        }

        SetPickMode(currentPickModeEnabled);
    }
}
