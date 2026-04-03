using System.Drawing;

namespace AdhdFocusOverlay.Models;

public sealed record MonitorInfo(
    string Id,
    int Index,
    bool IsPrimary,
    Rectangle Bounds,
    Rectangle WorkingArea,
    string DisplayName);
