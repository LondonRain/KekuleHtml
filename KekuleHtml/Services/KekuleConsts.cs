// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Services;

/// <summary>
/// Shared defaults, limits and constants for the Kekule list generation, used by both the console application and the UI so they stay in sync.
/// </summary>
public static class KekuleConsts
{
    #region File extensions

    /// <summary>
    /// Primary GEDCOM file extension (including the leading dot).
    /// </summary>
    public const string GedcomExtension = ".ged";

    /// <summary>
    /// Alternative GEDCOM file extension (including the leading dot).
    /// </summary>
    public const string GedcomExtensionAlternative = ".gedcom";

    #endregion

    #region Parameters

    /// <summary>
    /// Default number of generations to traverse (excluding the proband, i.e. G1..G{value}).
    /// </summary>
    public const int DefaultMaxGenerations = 20;

    /// <summary>
    /// Smallest sensible number of generations (allow to visualize one person's history).
    /// </summary>
    public const int MinGenerations = 0;

    /// <summary>
    /// Highest number of generations that can be represented with the Kekule number's data
    /// type (<see cref="ulong"/>): generation 63's largest Kekule number is 2^64-1 = ulong.MaxValue.
    /// </summary>
    public const int MaxGenerations = 63;

    /// <summary>
    /// Number of entries shown in each "research focus" top list (top places / top surnames).
    /// </summary>
    public const int TopListLength = 5;

    #endregion

    #region Mary Hill colours

    public const string BlueHex = "#133FB9";
    public const string GreenHex = "#137C2D";
    public const string RedHex = "#BE2323";
    public const string YellowHex = "#F5AF00";

    #endregion
}
