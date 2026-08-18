// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using System.Collections.Frozen;

namespace KekuleHtml.Services;

/// <summary>
/// Shared defaults and limits for the Kekule list generation, used by both the console application and the UI so they stay in sync.
/// </summary>
public static class KekuleDefaults
{
    /// <summary>
    /// Default number of generations to traverse (excluding the proband, i.e. G1..G{value}).
    /// </summary>
    public const int DefaultMaxGenerations = 20;

    /// <summary>
    /// Smallest sensible number of generations (just the proband's parents).
    /// </summary>
    public const int MinGenerations = 1;

    /// <summary>
    /// Highest number of generations that can be represented with the Kekule number's data
    /// type (<see cref="ulong"/>): generation 63's largest Kekule number is 2^64-1 = ulong.MaxValue.
    /// </summary>
    public const int MaxGenerations = 63;

    /// <summary>
    /// Number of entries shown in each "research focus" top list (top places / top surnames).
    /// </summary>
    public const int TopListLength = 5;

    /// <summary>
    /// Placeholder surnames that stand for an unknown family name rather than a real one.
    /// GEDCOM has no official placeholder, but these are the widely documented conventions.
    /// </summary>
    /// <remarks>
    /// Values are stored already normalized (see <see cref="IsUnknownSurname(string?)"/>).
    /// </remarks>
    private static readonly FrozenSet<string> _UnknownSurnamePlaceholders = new[]
    {
        // Nomen Nescio
        "NN",
        // Last Name Unknown
        "LNU",
        // First Name Unknown
        "FNU",
        // Maiden Name Unknown
        "MNU",
        "UNK",
        // Do Not Know
        "DNK",
        "UNKNOWN",
        "UNBEKANNT"
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="surname"/> does not denote a real family name:
    /// either it is empty/whitespace or it is one of the documented "unknown surname" placeholders.
    /// Matching is case-insensitive and ignores dots and spaces, so <c>N.N.</c>, <c>n. n.</c> and <c>NN</c> all match.
    /// </summary>
    public static bool IsUnknownSurname(string? surname)
    {
        if (string.IsNullOrWhiteSpace(surname))
            return true;

        var normalized = surname.Replace(".", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

        return _UnknownSurnamePlaceholders.Contains(normalized);
    }
}
