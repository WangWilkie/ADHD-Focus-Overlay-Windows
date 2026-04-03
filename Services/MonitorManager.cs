using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AdhdFocusOverlay.Models;
using Forms = System.Windows.Forms;

namespace AdhdFocusOverlay.Services;

public sealed class MonitorManager
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Forms.Screen.AllScreens
            .Select((screen, index) => new MonitorInfo(
                screen.DeviceName,
                index,
                screen.Primary,
                screen.Bounds,
                screen.WorkingArea,
                $"Monitor {index + 1}"))
            .ToArray();
    }

    public MonitorInfo GetPrimaryMonitor()
    {
        return GetMonitors().First(static monitor => monitor.IsPrimary);
    }

    public MonitorInfo ResolveActiveMonitor(string? monitorId)
    {
        var monitors = GetMonitors();
        if (!string.IsNullOrWhiteSpace(monitorId))
        {
            var saved = monitors.FirstOrDefault(monitor => monitor.Id == monitorId);
            if (saved is not null)
            {
                return saved;
            }
        }

        return monitors.First(monitor => monitor.IsPrimary);
    }

    public MonitorInfo ResolveMonitorForPoint(Point point)
    {
        var monitors = GetMonitors();
        var containing = monitors.FirstOrDefault(monitor => monitor.Bounds.Contains(point));
        if (containing is not null)
        {
            return containing;
        }

        return monitors
            .OrderBy(monitor => DistanceSquared(monitor.Bounds, point))
            .First();
    }

    private static long DistanceSquared(Rectangle bounds, Point point)
    {
        var dx = point.X < bounds.Left ? bounds.Left - point.X : point.X > bounds.Right ? point.X - bounds.Right : 0;
        var dy = point.Y < bounds.Top ? bounds.Top - point.Y : point.Y > bounds.Bottom ? point.Y - bounds.Bottom : 0;
        return ((long)dx * dx) + ((long)dy * dy);
    }
}
