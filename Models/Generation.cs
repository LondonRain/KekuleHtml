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

    public string ExternalName => $"Generation {GenerationNumber}";

    public string Description
    {
        get
        {
            if (GenerationNumber == 0)
                return "Proband";
            else if (GenerationNumber == 1)
                return "Eltern";
            else if (GenerationNumber == 2)
                return "Großeltern";
            else if (GenerationNumber == 3)
                return "Urgroßeltern";
            else
                return $"{GenerationNumber - 2}x-Urgroßeltern";
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
}