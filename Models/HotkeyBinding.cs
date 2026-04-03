namespace AdhdFocusOverlay.Models;

public sealed record HotkeyBinding(uint Modifiers, int VirtualKey)
{
    public override string ToString()
    {
        return $"{Modifiers}+0x{VirtualKey:X2}";
    }
}
