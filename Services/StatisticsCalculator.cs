using KekuleHtml.Models;

namespace KekuleHtml.Services;

public static class StatisticsCalculator
{
    public static IReadOnlyDictionary<int, Generation> Calculate(IEnumerable<Person> entries)
    {
        return entries.GroupBy(e => e.Generation).ToDictionary(g => g.Key, CreateStatistics);
    }

    private static Generation CreateStatistics(IGrouping<int, Person> group)
    {
        var birthYears = group.Select(x => x.GedcomRecord.Birth?.Date?.DateTime1?.Year)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .OrderBy(x => x)
            .ToList();
        if (birthYears.Count == (0))
            birthYears = null;

        var deathYears = group.Select(x => x.GedcomRecord.Death?.Date?.DateTime1?.Year)
               .Where(x => x.HasValue)
               .Select(x => x!.Value)
               .OrderBy(x => x)
               .ToList();
        if (deathYears.Count == (0))
            deathYears = null;

        return new Generation
        {
            GenerationNumber = group.Key,
            Count = group.Count(),
            BirthMinYear = birthYears?.Min(),
            BirthMaxYear = birthYears?.Max(),
            BirthAverageYear = birthYears != null ? (int)Math.Round(birthYears.Average()) : null,
            BirthMedianYear = birthYears != null ? CalculateMedian(birthYears) : null,
            DeathMinYear = deathYears?.Min(),
            DeathMaxYear = deathYears?.Max(),
            DeathAverageYear = deathYears != null ? (int)Math.Round(deathYears.Average()) : null,
            DeathMedianYear = deathYears != null ? CalculateMedian(deathYears) : null
        };

        static int CalculateMedian(List<int> years)
        {
            return years.Count % 2 == 1 ?
                years[years.Count / 2] :
                (int)Math.Round((years[years.Count / 2 - 1] + years[years.Count / 2]) / 2.0);
        }
    }
}