using Avalonia.Input;
using AvaloniaUI.Views;
using AwesomeAssertions;

namespace RomManagerTests;

public class CardNavigationTests
{
    [Theory]
    [InlineData(0, Key.Right, 1)]
    [InlineData(1, Key.Left, 0)]
    [InlineData(0, Key.Left, 0)]       // stays at the first card
    [InlineData(1, Key.Down, 5)]
    [InlineData(5, Key.Up, 1)]
    [InlineData(2, Key.Up, 2)]         // first row: stays
    [InlineData(7, Key.Down, 9)]       // last full row → last card
    [InlineData(9, Key.Down, 9)]       // already on the last row
    [InlineData(6, Key.Home, 0)]
    [InlineData(1, Key.End, 9)]
    [InlineData(1, Key.PageDown, 9)]
    [InlineData(8, Key.PageUp, 0)]
    public void Target_MovesInAFourColumnGrid(int from, Key key, int expected) =>
        CardNavigation.Target(from, count: 10, columns: 4, rowsPerPage: 2, key).Should().Be(expected);

    [Fact]
    public void Target_IgnoresOtherKeys() => CardNavigation.Target(0, 10, 4, 2, Key.A).Should().BeNull();
}
