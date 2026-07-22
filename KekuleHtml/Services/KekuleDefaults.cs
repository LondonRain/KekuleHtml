// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Services;

/// <summary>
/// Shared defaults and limits for the Kekule list generation, used by both the console
/// application and the UI so they stay in sync.
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
}
