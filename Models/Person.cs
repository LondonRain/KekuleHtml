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

    public string SurName => GedcomRecord.GetName().Surname;

    /// <inheritdoc cref="MaryHillColour"/>
    public required MaryHillColour Color { get; init; }

    /// <summary>
    /// Used to track "Ahnenschwund"
    /// </summary>
    public int? FirstOccurrence { get; set; }

    /// <inheritdoc cref="FirstOccurrence"/>
    public bool IsDuplicate => FirstOccurrence.HasValue;

    public int Generation => (int)Math.Floor(Math.Log2(KekuleNumber));

    public int? BirthYear => GedcomRecord.Birth?.Date?.DateTime1?.Year;

    public int? DeathYear => GedcomRecord.Death?.Date?.DateTime1?.Year;

    public string FormattedDates => GedcomRecord.GetFormattedDates();
}