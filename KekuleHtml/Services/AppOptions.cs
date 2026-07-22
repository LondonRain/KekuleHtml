// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Services;

/// <summary>
/// Command-line / startup options shared by the console application and the UI.
/// Parsed via <see cref="CommandLineParser"/>.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// The GEDCOM file to load (first positional argument), or <see langword="null"/> if none was given.
    /// </summary>
    public string? GedcomPath { get; init; }

    /// <summary>
    /// Number of generations to traverse (excluding the proband). Defaults to <see cref="KekuleDefaults.DefaultMaxGenerations"/>.
    /// </summary>
    public int MaxGenerations { get; init; } = KekuleDefaults.DefaultMaxGenerations;
}
