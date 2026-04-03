using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AdhdFocusOverlay.Services;

public sealed class CaptureService
{
    public CaptureService()
    {
        VirtualScreenBounds = SystemInformation.VirtualScreen;
        DisplayBounds = Screen.AllScreens
            .Select(screen => screen.Bounds)
            .ToArray();
        WorkingAreas = Screen.AllScreens
            .Select(screen => screen.WorkingArea)
            .ToArray();
        PrimaryWorkingArea = Screen.PrimaryScreen?.WorkingArea ?? VirtualScreenBounds;
    }

    public Rectangle VirtualScreenBounds { get; }

    public Rectangle[] DisplayBounds { get; }

    public Rectangle[] WorkingAreas { get; }

    public Rectangle PrimaryWorkingArea { get; }

    public string ModeDescription => "Transparent passthrough focus region for zero-latency live desktop preview.";
}
