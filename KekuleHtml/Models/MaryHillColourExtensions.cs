// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Services;

namespace KekuleHtml.Models;

/// <summary>
/// Single source of truth for the hex colours of the four <see cref="MaryHillColour"/> lines.
/// Used for the map circles, the legend, the person borders and the research-focus bars, so the palette stays consistent and is defined in exactly one place.
/// </summary>
public static class MaryHillColourExtensions
{
    /// <summary>
    /// Gets the hex colour (e.g. <c>#005D8F</c>) for a <paramref name="colour"/>.
    /// </summary>
    public static string ToHex(this MaryHillColour colour) => colour switch
    {
        MaryHillColour.Blue => KekuleConsts.BlueHex,
        MaryHillColour.Green => KekuleConsts.GreenHex,
        MaryHillColour.Red => KekuleConsts.RedHex,
        MaryHillColour.Yellow => KekuleConsts.YellowHex,
        _ => throw new InvalidOperationException($"Unexpected colour {colour}!")
    };
}
