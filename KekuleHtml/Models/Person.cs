// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using System.Diagnostics;
using System.Numerics;

namespace KekuleHtml.Models;

/// <summary>
/// Colour coding by Mary Hill.
/// See <see href="http://www.genrootsorganizer.com/p/13-steps.html"/>.
/// </summary>
public enum MaryHillColour
{
    Blue,
    Green,
    Red,
    Yellow
}

[DebuggerDisplay("KekuleNumber: {KekuleNumber}, Name: {FormattedName}")]
public sealed class Person
{
    public required ulong KekuleNumber { get; init; }

    public required GedcomIndividualRecord GedcomRecord { get; init; }

    public string FormattedName => GedcomRecord.GetFormattedName();

    public string FormattedDates => GedcomRecord.GetFormattedDates();

    public string FormattedNameWithDates => GedcomRecord.GetFormattedNameWithDates();

    public string Surname => GedcomRecord.GetName().Surname;

    /// <inheritdoc cref="MaryHillColour"/>
    public required MaryHillColour Colour { get; init; }

    /// <inheritdoc cref="GenerationOf(ulong)"/>
    public int Generation => GenerationOf(KekuleNumber);

    /// <summary>
    /// Determines the generation (G0 = proband) from a <paramref name="kekuleNumber"/>.
    /// </summary>
    public static int GenerationOf(ulong kekuleNumber) => BitOperations.Log2(kekuleNumber);

    /// <summary>
    /// The Kekule number where this individual first appeared. Only set for duplicates.
    /// Set while traversing whenever the same individual is reached again ("Ahnenschwund").
    /// </summary>
    public ulong? FirstOccurrence { get; set; }

    /// <summary>
    /// Whether person was already found before. Not <see langword="true"/> for first match.
    /// </summary>
    public bool IsDuplicate => FirstOccurrence.HasValue;

    /// <summary>
    /// All Kekule numbers at which this same individual appears (including this one), sorted ascending.
    /// Only contains more than one entry in case of "Ahnenschwund".
    /// Filled in once the whole tree has been traversed.
    /// </summary>
    public IReadOnlyList<ulong> Occurrences { get; set; } = [];

    /// <summary>
    /// Whether person has any duplicates. <see langword="true"/> for every person of the duplicate group.
    /// </summary>
    public bool HasDuplicates => Occurrences.Count > 1;
}