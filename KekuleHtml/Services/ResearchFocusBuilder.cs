// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Models;

namespace KekuleHtml.Services;

/// <summary>
/// Aggregates the migration points and persons into research focus cards.
/// </summary>
public static class ResearchFocusBuilder
{
    #region Build

    /// <summary>
    /// Builds the research focus cards: one per Mary-Hill line plus a final "total" card.
    /// </summary>
    /// <remarks>
    /// Places are counted by <em>events</em> (a person can appear several times), surnames by <em>persons</em> (each person once).
    /// The total card carries only the key figures, since the detailed lists already appear on the four line cards.
    /// </remarks>
    public static IReadOnlyList<ResearchFocusCard> Build(FamilyTree familyTree, IEnumerable<MigrationPoint> points, int topListLength)
    {
        // "Ahnenschwund" duplicates would otherwise inflate person and surname counts.
        var persons = familyTree.AllPersons.Where(p => !p.IsDuplicate).ToList();

        // Build the cards for 4 the colour lines.
        var cards = new List<ResearchFocusCard>();

        foreach (var colour in Enum.GetValues<MaryHillColour>())
        {
            var linePersons = persons.Where(p => p.Colour == colour).ToList();
            var linePoints = points.Where(p => p.Person.Colour == colour).ToList();

            cards.Add(BuildCard(
                colour,
                GetSurnameForMaryHillLine(familyTree, colour),
                linePersons,
                linePoints,
                topListLength));
        }

        // Add total card over all lines: key figures only.
        cards.Add(BuildCard(null, null, persons, points, topListLength, true));

        return cards;
    }

    private static ResearchFocusCard BuildCard(
        MaryHillColour? colour,
        string? surnameForMaryHillLine,
        IEnumerable<Person> persons,
        IEnumerable<MigrationPoint> points,
        int topListLength,
        bool isTotalCard = false)
    {
        List<CountedItem> topPlaces = [];
        List<CountedItem> topSurnames = [];

        if (!isTotalCard)
        {
            topPlaces = points.GroupBy(p => p.PlaceName)
                              .Select(g => new CountedItem(g.Key, g.Count())) // events per place
                              .OrderByDescending(i => i.Count)
                              .ThenBy(i => i.Label)
                              .Take(topListLength)
                              .ToList();

            topSurnames = persons.Where(p => !string.IsNullOrWhiteSpace(p.Surname))
                                 .GroupBy(p => p.Surname)
                                 .Select(g => new CountedItem(g.Key, g.Count())) // persons per surname
                                 .OrderByDescending(i => i.Count)
                                 .ThenBy(i => i.Label)
                                 .Take(topListLength)
                                 .ToList();
        }

        return new ResearchFocusCard
        {
            Colour = colour,
            AncestorName = surnameForMaryHillLine,
            PersonCount = persons.Count(),
            PlaceCount = points.Select(p => p.PlaceName).Distinct().Count(),
            SurnameCount = persons.Select(p => p.Surname).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count(),
            TopPlaces = topPlaces,
            TopSurnames = topSurnames
        };
    }

    #endregion

    #region Helpers

    private static string? GetSurnameForMaryHillLine(FamilyTree familyTree, MaryHillColour colour)
    {
        ulong kekuleNumber = colour switch
        {
            MaryHillColour.Blue => 4,
            MaryHillColour.Green => 5,
            MaryHillColour.Red => 6,
            MaryHillColour.Yellow => 7,
            _ => throw new InvalidOperationException($"Unexpected colour {colour}!")
        };

        var surname = familyTree.GetPerson(kekuleNumber)?.Surname;

        return string.IsNullOrWhiteSpace(surname) ? null : surname;
    }

    #endregion
}
