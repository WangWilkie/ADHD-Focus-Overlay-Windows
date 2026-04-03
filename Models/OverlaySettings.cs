namespace AdhdFocusOverlay.Models;

public sealed class OverlaySettings
{
    public string TintColor { get; set; } = "#606060";

    public byte DimOpacity { get; set; } = 170;

    public bool DesaturationEnabled { get; set; }

    public int FocusX { get; set; }

    public int FocusY { get; set; }

    public int FocusWidth { get; set; }

    public int FocusHeight { get; set; }

    public string ActiveMonitorId { get; set; } = string.Empty;

    public uint MovePickModifiers { get; set; } = 0x0005;

    public int MovePickVirtualKey { get; set; } = 0x51;

    public uint ToggleOverlayModifiers { get; set; } = 0x0005;

    public int ToggleOverlayVirtualKey { get; set; } = 0x53;

    public bool LaunchAtStartup { get; set; }
}
