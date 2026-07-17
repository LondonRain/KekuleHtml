// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using System.Diagnostics;

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
    public required int KekuleNumber { get; init; }

    public required GedcomIndividualRecord GedcomRecord { get; init; }

    public string FormattedName => GedcomRecord.GetFormattedName();

    public string FormattedDates => GedcomRecord.GetFormattedDates();

    public string FormattedNameWithDates => GedcomRecord.GetFormattedNameWithDates();

    public string Surname => GedcomRecord.GetName().Surname;

    /// <inheritdoc cref="MaryHillColour"/>
    public required MaryHillColour Colour { get; init; }

    public int Generation => (int)Math.Floor(Math.Log2(KekuleNumber));

    /// <summary>
    /// The Kekule number where this individual first appeared. Only set for duplicates.
    /// Set while traversing whenever the same individual is reached again ("Ahnenschwund").
    /// </summary>
    public int? FirstOccurrence { get; set; }

    /// <summary>
    /// Whether person was already found before. Not <see langword="true"/> for first match.
    /// </summary>
    public bool IsDuplicate => FirstOccurrence.HasValue;

    /// <summary>
    /// All Kekule numbers at which this same individual appears (including this one), sorted ascending.
    /// Only contains more than one entry in case of "Ahnenschwund".
    /// Filled in once the whole tree has been traversed.
    /// </summary>
    public IReadOnlyList<int> Occurrences { get; set; } = [];

    /// <summary>
    /// Whether person has any duplicates. <see langword="true"/> for every person of the duplicate group.
    /// </summary>
    public bool HasDuplicates => Occurrences.Count > 1;
}