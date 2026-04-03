using System;
using AdhdFocusOverlay.Models;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace AdhdFocusOverlay.Controllers;

public sealed class VisualEffectController
{
    private MediaColor tintColor;
    private byte opacityByte;

    public VisualEffectController(OverlaySettings settings)
    {
        tintColor = ParseColor(settings.TintColor);
        opacityByte = settings.DimOpacity is > 15 and < 245 ? settings.DimOpacity : (byte)170;
    }

    public event EventHandler? VisualEffectChanged;

    public MediaColor TintColor => tintColor;

    public string TintHex => $"#{tintColor.R:X2}{tintColor.G:X2}{tintColor.B:X2}";

    public byte OpacityByte => opacityByte;

    public double DimOpacity => opacityByte / 255d;

    public void Update(MediaColor newTintColor, byte newOpacity)
    {
        if (newTintColor == tintColor && newOpacity == opacityByte)
        {
            return;
        }

        tintColor = newTintColor;
        opacityByte = newOpacity;
        VisualEffectChanged?.Invoke(this, EventArgs.Empty);
    }

    public static MediaColor ParseColor(string hex)
    {
        try
        {
            var parsed = (MediaColor)MediaColorConverter.ConvertFromString(hex)!;
            return MediaColor.FromRgb(parsed.R, parsed.G, parsed.B);
        }
        catch
        {
            return MediaColor.FromRgb(96, 96, 96);
        }
    }
}
