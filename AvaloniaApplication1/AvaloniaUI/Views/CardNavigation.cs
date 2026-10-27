using Avalonia.Input;

namespace AvaloniaUI.Views;

/// <summary>Where keyboard focus goes in a grid of cards laid out row by row.</summary>
public static class CardNavigation
{
    /// <summary>The index to focus after <paramref name="key"/>, or null when the key does not navigate.</summary>
    public static int? Target(int index, int count, int columns, int rowsPerPage, Key key)
    {
        if (count == 0) return null;
        columns = Math.Max(1, columns);
        int? target = key switch
        {
            Key.Left => index - 1,
            Key.Right => index + 1,
            Key.Up => index - columns,
            Key.Down => index + columns,
            Key.PageUp => index - columns * Math.Max(1, rowsPerPage),
            Key.PageDown => index + columns * Math.Max(1, rowsPerPage),
            Key.Home => 0,
            Key.End => count - 1,
            _ => null,
        };
        if (target is not { } t) return null;
        // Down from the last full row lands on the last card rather than doing nothing.
        if (key is Key.Down or Key.PageDown && t >= count && index / columns < (count - 1) / columns) return count - 1;
        return t < 0 || t >= count ? (key is Key.PageUp ? 0 : key is Key.PageDown ? count - 1 : index) : t;
    }
}
