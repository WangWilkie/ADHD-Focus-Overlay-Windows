using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AdhdFocusOverlay.Controllers;
using AdhdFocusOverlay.Models;

namespace AdhdFocusOverlay.Services;

internal sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public OverlaySettings Load(Rectangle initialMonitorBounds)
    {
        var settingsPath = GetSettingsPath();
        if (File.Exists(settingsPath))
        {
            try
            {
                var content = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<OverlaySettings>(content, JsonOptions);
                if (settings is not null)
                {
                    MigrateHotkeys(settings);
                    return settings;
                }
            }
            catch
            {
            }
        }

        var legacyPath = Path.Combine(Path.GetDirectoryName(settingsPath)!, "settings.ini");
        if (File.Exists(legacyPath))
        {
            return LoadLegacyIni(initialMonitorBounds, legacyPath);
        }

        return CreateDefault(initialMonitorBounds);
    }

    public void Save(OverlaySettings settings)
    {
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static void MigrateHotkeys(OverlaySettings settings)
    {
        var isLegacySwitchHotkey =
            (settings.MovePickModifiers == 0x0009 && settings.MovePickVirtualKey == 0x4D) ||
            (settings.MovePickModifiers == 0x0001 && settings.MovePickVirtualKey == 0x44) ||
            (settings.MovePickModifiers == 0x0005 && settings.MovePickVirtualKey == 0x53);

        if (isLegacySwitchHotkey)
        {
            settings.MovePickModifiers = 0x0005;
            settings.MovePickVirtualKey = 0x51;
        }

        if (settings.ToggleOverlayModifiers == 0 && settings.ToggleOverlayVirtualKey == 0)
        {
            settings.ToggleOverlayModifiers = 0x0005;
            settings.ToggleOverlayVirtualKey = 0x53;
        }

        if (settings.MovePickModifiers == settings.ToggleOverlayModifiers &&
            settings.MovePickVirtualKey == settings.ToggleOverlayVirtualKey)
        {
            settings.MovePickModifiers = 0x0005;
            settings.MovePickVirtualKey = 0x51;
        }
    }

    private static string GetSettingsPath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ADHDFocusOverlay");
        return Path.Combine(root, "settings.json");
    }

    private static OverlaySettings CreateDefault(Rectangle bounds)
    {
        var width = Math.Max(FocusRegionController.MinWidth, bounds.Width / 3);
        var height = Math.Max(FocusRegionController.MinHeight, bounds.Height / 2);
        return new OverlaySettings
        {
            TintColor = "#606060",
            DimOpacity = 170,
            FocusX = bounds.Left + (bounds.Width - width) / 2,
            FocusY = bounds.Top + (bounds.Height - height) / 2,
            FocusWidth = width,
            FocusHeight = height,
            DesaturationEnabled = false,
            ActiveMonitorId = string.Empty,
            MovePickModifiers = 0x0005,
            MovePickVirtualKey = 0x51,
            ToggleOverlayModifiers = 0x0005,
            ToggleOverlayVirtualKey = 0x53,
            LaunchAtStartup = false
        };
    }

    private static OverlaySettings LoadLegacyIni(Rectangle bounds, string path)
    {
        var settings = CreateDefault(bounds);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            switch (parts[0].Trim())
            {
                case "FocusRect":
                    var rectValues = parts[1].Split(',');
                    if (rectValues.Length == 4 &&
                        int.TryParse(rectValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                        int.TryParse(rectValues[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) &&
                        int.TryParse(rectValues[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) &&
                        int.TryParse(rectValues[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
                    {
                        settings.FocusX = x;
                        settings.FocusY = y;
                        settings.FocusWidth = width;
                        settings.FocusHeight = height;
                    }
                    break;
                case "TintColor":
                    var colorValues = parts[1].Split(',');
                    if (colorValues.Length == 3 &&
                        byte.TryParse(colorValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) &&
                        byte.TryParse(colorValues[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g) &&
                        byte.TryParse(colorValues[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
                    {
                        settings.TintColor = $"#{r:X2}{g:X2}{b:X2}";
                    }
                    break;
                case "OverlayOpacity":
                    if (byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var opacity))
                    {
                        settings.DimOpacity = opacity;
                    }
                    break;
                case "StartupEnabled":
                    if (bool.TryParse(parts[1], out var startup))
                    {
                        settings.LaunchAtStartup = startup;
                    }
                    break;
            }
        }

        return settings;
    }
}
