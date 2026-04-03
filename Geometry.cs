using System;
using System.Drawing;

namespace AdhdFocusOverlay
{
    internal static class Geometry
    {
        public const int MinFocusWidth = 180;
        public const int MinFocusHeight = 140;

        public static Rectangle ClampToScreen(Rectangle rect, Rectangle screen)
        {
            var width = Math.Max(MinFocusWidth, Math.Min(rect.Width, screen.Width));
            var height = Math.Max(MinFocusHeight, Math.Min(rect.Height, screen.Height));
            var x = rect.X;
            var y = rect.Y;

            if (x < screen.Left) x = screen.Left;
            if (y < screen.Top) y = screen.Top;
            if (x + width > screen.Right) x = screen.Right - width;
            if (y + height > screen.Bottom) y = screen.Bottom - height;

            return new Rectangle(x, y, width, height);
        }

        public static Rectangle Resize(Rectangle original, DragMode mode, int deltaX, int deltaY, Rectangle screen)
        {
            var rect = original;

            if ((mode & DragMode.Left) == DragMode.Left)
            {
                rect.X += deltaX;
                rect.Width -= deltaX;
            }

            if ((mode & DragMode.Right) == DragMode.Right) rect.Width += deltaX;

            if ((mode & DragMode.Top) == DragMode.Top)
            {
                rect.Y += deltaY;
                rect.Height -= deltaY;
            }

            if ((mode & DragMode.Bottom) == DragMode.Bottom) rect.Height += deltaY;

            if (rect.Width < MinFocusWidth)
            {
                if ((mode & DragMode.Left) == DragMode.Left) rect.X -= (MinFocusWidth - rect.Width);
                rect.Width = MinFocusWidth;
            }

            if (rect.Height < MinFocusHeight)
            {
                if ((mode & DragMode.Top) == DragMode.Top) rect.Y -= (MinFocusHeight - rect.Height);
                rect.Height = MinFocusHeight;
            }

            return ClampToScreen(rect, screen);
        }

        public static Rectangle Move(Rectangle original, int deltaX, int deltaY, Rectangle screen)
        {
            return ClampToScreen(
                new Rectangle(original.X + deltaX, original.Y + deltaY, original.Width, original.Height),
                screen);
        }
    }
}
