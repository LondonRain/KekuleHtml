namespace KekuleHtml.Models;

public sealed class Generation
{
    public required int GenerationNumber { get; init; }

    public required int Count { get; init; }

    public int? BirthMinYear { get; init; }
    public int? BirthMaxYear { get; init; }
    public int? BirthAverageYear { get; init; }
    public int? BirthMedianYear { get; init; }

    public int? DeathMinYear { get; init; }
    public int? DeathMaxYear { get; init; }
    public int? DeathAverageYear { get; init; }
    public int? DeathMedianYear { get; init; }
}