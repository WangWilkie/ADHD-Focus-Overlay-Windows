using System;
using System.Drawing;
using AdhdFocusOverlay.Models;
using AdhdFocusOverlay.Services;

namespace AdhdFocusOverlay.Controllers;

public sealed class FocusRegionController
{
    public const int MinWidth = 260;
    public const int MinHeight = 180;

    private readonly MonitorManager monitorManager;
    private FocusRegion focusRegion;

    public FocusRegionController(MonitorManager monitorManager, OverlaySettings settings)
    {
        this.monitorManager = monitorManager;
        ActiveMonitor = monitorManager.ResolveActiveMonitor(settings.ActiveMonitorId);
        focusRegion = Coerce(CreateInitialRegion(settings));
    }

    public event EventHandler? StateChanged;

    public MonitorInfo ActiveMonitor { get; private set; }

    public FocusRegion FocusRegion => focusRegion;

    public void ResetToDefault()
    {
        focusRegion = CreateDefault(ActiveMonitor.WorkingArea);
        RaiseChanged();
    }

    public void Resize(ResizeHandle handle, int deltaX, int deltaY)
    {
        var next = focusRegion;
        switch (handle)
        {
            case ResizeHandle.Left:
                next = next with { X = focusRegion.X + deltaX, Width = focusRegion.Width - deltaX };
                break;
            case ResizeHandle.Top:
                next = next with { Y = focusRegion.Y + deltaY, Height = focusRegion.Height - deltaY };
                break;
            case ResizeHandle.Right:
                next = next with { Width = focusRegion.Width + deltaX };
                break;
            case ResizeHandle.Bottom:
                next = next with { Height = focusRegion.Height + deltaY };
                break;
            case ResizeHandle.TopLeft:
                next = next with
                {
                    X = focusRegion.X + deltaX,
                    Y = focusRegion.Y + deltaY,
                    Width = focusRegion.Width - deltaX,
                    Height = focusRegion.Height - deltaY
                };
                break;
            case ResizeHandle.TopRight:
                next = next with
                {
                    Y = focusRegion.Y + deltaY,
                    Width = focusRegion.Width + deltaX,
                    Height = focusRegion.Height - deltaY
                };
                break;
            case ResizeHandle.BottomLeft:
                next = next with
                {
                    X = focusRegion.X + deltaX,
                    Width = focusRegion.Width - deltaX,
                    Height = focusRegion.Height + deltaY
                };
                break;
            case ResizeHandle.BottomRight:
                next = next with
                {
                    Width = focusRegion.Width + deltaX,
                    Height = focusRegion.Height + deltaY
                };
                break;
            default:
                return;
        }

        SetFocusRegion(next);
    }

    public void Move(int deltaX, int deltaY)
    {
        if (deltaX == 0 && deltaY == 0)
        {
            return;
        }

        SetFocusRegion(focusRegion with
        {
            X = focusRegion.X + deltaX,
            Y = focusRegion.Y + deltaY
        });
    }

    public void SwitchToMonitor(MonitorInfo targetMonitor)
    {
        if (targetMonitor.Id == ActiveMonitor.Id)
        {
            return;
        }

        var currentArea = ActiveMonitor.WorkingArea;
        var targetArea = targetMonitor.WorkingArea;
        var width = Math.Min(focusRegion.Width, targetArea.Width);
        var height = Math.Min(focusRegion.Height, targetArea.Height);

        var horizontalSpan = Math.Max(1, currentArea.Width - focusRegion.Width);
        var verticalSpan = Math.Max(1, currentArea.Height - focusRegion.Height);
        var relativeX = (double)(focusRegion.X - currentArea.Left) / horizontalSpan;
        var relativeY = (double)(focusRegion.Y - currentArea.Top) / verticalSpan;

        ActiveMonitor = targetMonitor;
        focusRegion = Coerce(new FocusRegion(
            targetArea.Left + (int)Math.Round(relativeX * Math.Max(0, targetArea.Width - width)),
            targetArea.Top + (int)Math.Round(relativeY * Math.Max(0, targetArea.Height - height)),
            width,
            height));
        RaiseChanged();
    }

    public void RefreshMonitorLayout()
    {
        var resolvedMonitor = monitorManager.ResolveActiveMonitor(ActiveMonitor.Id);
        var monitorChanged = resolvedMonitor.Id != ActiveMonitor.Id;
        ActiveMonitor = resolvedMonitor;

        var coerced = Coerce(focusRegion);
        if (!monitorChanged && coerced == focusRegion)
        {
            return;
        }

        focusRegion = coerced;
        RaiseChanged();
    }

    public void SelectMonitorAt(Point point)
    {
        SwitchToMonitor(monitorManager.ResolveMonitorForPoint(point));
    }

    public OverlaySettings CaptureState(OverlaySettings settings)
    {
        settings.ActiveMonitorId = ActiveMonitor.Id;
        settings.FocusX = focusRegion.X;
        settings.FocusY = focusRegion.Y;
        settings.FocusWidth = focusRegion.Width;
        settings.FocusHeight = focusRegion.Height;
        return settings;
    }

    private void SetFocusRegion(FocusRegion candidate)
    {
        var next = Coerce(candidate);
        if (next == focusRegion)
        {
            return;
        }

        focusRegion = next;
        RaiseChanged();
    }

    private FocusRegion Coerce(FocusRegion candidate)
    {
        var area = ActiveMonitor.WorkingArea;
        var right = candidate.Right;
        var bottom = candidate.Bottom;
        var width = Math.Max(MinWidth, Math.Min(candidate.Width, area.Width));
        var height = Math.Max(MinHeight, Math.Min(candidate.Height, area.Height));
        var x = candidate.X;
        var y = candidate.Y;

        if (candidate.X != focusRegion.X && candidate.Right == focusRegion.Right)
        {
            x = right - width;
        }

        if (candidate.Y != focusRegion.Y && candidate.Bottom == focusRegion.Bottom)
        {
            y = bottom - height;
        }

        x = Math.Max(area.Left, Math.Min(x, area.Right - width));
        y = Math.Max(area.Top, Math.Min(y, area.Bottom - height));
        width = Math.Max(MinWidth, Math.Min(width, area.Right - x));
        height = Math.Max(MinHeight, Math.Min(height, area.Bottom - y));
        x = Math.Max(area.Left, Math.Min(x, area.Right - width));
        y = Math.Max(area.Top, Math.Min(y, area.Bottom - height));

        return new FocusRegion(x, y, width, height);
    }

    private FocusRegion CreateInitialRegion(OverlaySettings settings)
    {
        if (settings.FocusWidth > 0 && settings.FocusHeight > 0)
        {
            return new FocusRegion(settings.FocusX, settings.FocusY, settings.FocusWidth, settings.FocusHeight);
        }

        return CreateDefault(ActiveMonitor.WorkingArea);
    }

    private static FocusRegion CreateDefault(Rectangle area)
    {
        var width = Math.Max(MinWidth, area.Width / 3);
        var height = Math.Max(MinHeight, area.Height / 2);
        return new FocusRegion(
            area.Left + ((area.Width - width) / 2),
            area.Top + ((area.Height - height) / 2),
            width,
            height);
    }

    private void RaiseChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
