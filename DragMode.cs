using System;

namespace AdhdFocusOverlay
{
    [Flags]
    internal enum DragMode
    {
        None = 0,
        Move = 1,
        Left = 2,
        Right = 4,
        Top = 8,
        Bottom = 16
    }
}
