using System;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace AdhdFocusOverlay
{
    internal sealed class AppState
    {
        private const int DefaultOpacity = 170;
        private static readonly Color DefaultTint = Color.FromArgb(96, 96, 96);

        public Rectangle FocusRect { get; set; }
        public Color TintColor { get; set; }
        public int OverlayOpacity { get; set; }
        public bool StartupEnabled { get; set; }

        public static AppState CreateDefault(Rectangle virtualScreen)
        {
            var width = Math.Max(480, virtualScreen.Width / 3);
            var height = Math.Max(320, virtualScreen.Height / 2);
            var x = virtualScreen.Left + (virtualScreen.Width - width) / 2;
            var y = virtualScreen.Top + (virtualScreen.Height - height) / 2;

            return new AppState
            {
                FocusRect = new Rectangle(x, y, width, height),
                TintColor = DefaultTint,
                OverlayOpacity = DefaultOpacity,
                StartupEnabled = false
            };
        }

        public static string GetSettingsPath()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ADHDFocusOverlay");
            Directory.CreateDirectory(root);
            return Path.Combine(root, "settings.ini");
        }

        public static AppState Load(Rectangle virtualScreen)
        {
            var state = CreateDefault(virtualScreen);
            var path = GetSettingsPath();
            if (!File.Exists(path)) return state;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();
                switch (key)
                {
                    case "FocusRect":
                        Rectangle rect;
                        if (TryParseRect(value, out rect)) state.FocusRect = rect;
                        break;
                    case "TintColor":
                        Color color;
                        if (TryParseColor(value, out color)) state.TintColor = color;
                        break;
                    case "OverlayOpacity":
                        int opacity;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out opacity))
                        {
                            state.OverlayOpacity = Clamp(opacity, 20, 245);
                        }
                        break;
                    case "StartupEnabled":
                        bool startupEnabled;
                        if (bool.TryParse(value, out startupEnabled)) state.StartupEnabled = startupEnabled;
                        break;
                }
            }

            state.FocusRect = Geometry.ClampToScreen(state.FocusRect, virtualScreen);
            return state;
        }

        public void Save()
        {
            var lines = new[]
            {
                "# ADHDFocusOverlay settings",
                string.Format(CultureInfo.InvariantCulture, "FocusRect={0},{1},{2},{3}", FocusRect.X, FocusRect.Y, FocusRect.Width, FocusRect.Height),
                string.Format(CultureInfo.InvariantCulture, "TintColor={0},{1},{2}", TintColor.R, TintColor.G, TintColor.B),
                "OverlayOpacity=" + OverlayOpacity.ToString(CultureInfo.InvariantCulture),
                "StartupEnabled=" + StartupEnabled
            };

            File.WriteAllLines(GetSettingsPath(), lines);
        }

        private static bool TryParseRect(string value, out Rectangle rect)
        {
            rect = Rectangle.Empty;
            var parts = value.Split(',');
            if (parts.Length != 4) return false;

            int x, y, width, height;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out width) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out height))
            {
                return false;
            }

            rect = new Rectangle(x, y, width, height);
            return true;
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = DefaultTint;
            var parts = value.Split(',');
            if (parts.Length != 3) return false;

            int r, g, b;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out r) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out g) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
            {
                return false;
            }

            color = Color.FromArgb(Clamp(r, 0, 255), Clamp(g, 0, 255), Clamp(b, 0, 255));
            return true;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
