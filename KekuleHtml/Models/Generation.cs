// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Properties;
using System.Diagnostics;

namespace KekuleHtml.Models;

/// <summary>
/// One generation holding all its <see cref="Person"/>s.
/// </summary>
[DebuggerDisplay("{Description}, GenerationNumber: {GenerationNumber}, Count: {Count}")]
public sealed class Generation
{
    public required int GenerationNumber { get; init; }

    public string ShortName => $"G{GenerationNumber}";

    public string ExternalName => string.Format(Resources.GenerationExternalName, GenerationNumber);

    public string Description
    {
        get
        {
            if (GenerationNumber == 0)
                return Resources.GenerationProband;
            else if (GenerationNumber == 1)
                return Resources.GenerationParents;
            else if (GenerationNumber == 2)
                return Resources.GenerationGrandparents;
            else if (GenerationNumber == 3)
                return Resources.GenerationGreatGrandparents;
            else
                return string.Format(Resources.GenerationNthGreatGrandparents, GenerationNumber - 2);
        }
    }

    public required IReadOnlyList<Person> Persons { get; init; }

    /// <summary>
    /// Number of people in the generation
    /// </summary>
    public int Count => Persons.Count;

    public int? BirthMinYear { get; init; }
    public int? BirthMaxYear { get; init; }
    public int? BirthAverageYear { get; init; }
    public int? BirthMedianYear { get; init; }

    public int? DeathMinYear { get; init; }
    public int? DeathMaxYear { get; init; }
    public int? DeathAverageYear { get; init; }
    public int? DeathMedianYear { get; init; }

    /// <summary>
    /// Whether generation contains people that did not die already and aren't older than 110 years.
    /// </summary>
    /// <remarks>
    /// If there is at least one of those persons assume that generation could still be alive.
    /// </remarks>
    public bool MightStillBeLiving { get; init; }
}