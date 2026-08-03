// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Models;

/// <summary>
/// Single source of truth for the hex colours of the four <see cref="MaryHillColour"/> lines.
/// Used for the map circles, the legend, the person borders and the research-focus bars, so the palette stays consistent and is defined in exactly one place.
/// </summary>
public static class MaryHillColourExtensions
{
    public const string BlueHex = "#005D8F";
    public const string GreenHex = "#0A7050";
    public const string RedHex = "#BE2323";
    public const string YellowHex = "#F5AF00";

    /// <summary>
    /// Gets the hex colour (e.g. <c>#005D8F</c>) for a <paramref name="colour"/>.
    /// </summary>
    public static string ToHex(this MaryHillColour colour) => colour switch
    {
        MaryHillColour.Blue => BlueHex,
        MaryHillColour.Green => GreenHex,
        MaryHillColour.Red => RedHex,
        MaryHillColour.Yellow => YellowHex,
        _ => throw new InvalidOperationException($"Unexpected colour {colour}!")
    };
}
