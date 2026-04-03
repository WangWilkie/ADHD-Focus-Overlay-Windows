using System.Drawing;

namespace AdhdFocusOverlay.Models;

public readonly record struct FocusRegion(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public Rectangle ToRectangle()
    {
        return new Rectangle(X, Y, Width, Height);
    }
}
