// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using KekuleHtml.Helpers;
using KekuleHtml.Models;

namespace KekuleHtml.Services;

/// <summary>
/// Parses the gedcom file to get create a data structure for a Kekule list for the selected person.
/// </summary>
public sealed class KekuleListBuilder(GedcomAdapter adapter)
{
    private readonly GedcomAdapter _Adapter = adapter;

    private readonly Dictionary<string, int> _Seen = [];

    private readonly List<Person> _Entries = [];

    public IReadOnlyList<Person> GetPersons(GedcomIndividualRecord root)
    {
        Traverse(root, 1);

        // Now that every occurrence is known, link all occurrences of the same individual to each other ("Ahnenschwund"). The shared, sorted list is handed to each entry.
        foreach (var group in _Entries.GroupBy(e => e.GedcomRecord.XRefID))
        {
            var occurrences = group.Select(e => e.KekuleNumber).Order().ToList();

            foreach (var entry in group)
                entry.Occurrences = occurrences;
        }

        return _Entries.OrderBy(e => e.KekuleNumber).ToList();
    }

    private void Traverse(GedcomIndividualRecord? person, int number) => Traverse(person, number, []);

    private void Traverse(GedcomIndividualRecord? person, int number, HashSet<string> ancestryPath)
    {
        if (person == null)
            return;

        var entry = new Person
        {
            KekuleNumber = number,
            GedcomRecord = person,
            Colour = GetColourFromKekule(number)
        };

        // Remember the first occurrence so duplicates ("Ahnenschwund") can be marked and cross-linked,
        // but keep traversing either way so every occurrence's ancestors are written out as well.
        if (!_Seen.TryGetValue(person.XRefID, out var firstNumber))
            _Seen[person.XRefID] = number;
        else
            entry.FirstOccurrence = firstNumber;

        _Entries.Add(entry);

        // Guard against cyclic data (a person being their own ancestor); a normal implex is not a cycle
        // because the repeated individual sits on a different, already-finished branch, not on this path.
        if (!ancestryPath.Add(person.XRefID))
            return;

        var father = _Adapter.GetFather(person);
        var mother = _Adapter.GetMother(person);

        try
        {
            Traverse(father, number * 2, ancestryPath);
            Traverse(mother, number * 2 + 1, ancestryPath);
        }
        catch (ArgumentException ex)
        {
            throw new KekuleHtmlException($"Failed to build Kekule list for person K:{number} \"{person.GetFormattedNameWithDates()}\".", ex);
        }

        ancestryPath.Remove(person.XRefID);
    }

    public static MaryHillColour GetColourFromKekule(int number)
    {
        if (number < 1)
            throw new ArgumentException($"Must be 1 or greater, but was {number}.", nameof(number));

        // fixed colour for first 2 generations (0 and 1).
        if (number == 1 || number == 2)
            return MaryHillColour.Blue;
        else if (number == 3)
            return MaryHillColour.Red;

        // fall back to one of the 4 people of generation 2
        long baseAncestor = number;
        while (baseAncestor > 7)
        {
            // bit shift right (division by 2)
            baseAncestor >>= 1;
        }

        // 4 colours for 4 different lines from generation 2 onwards
        return baseAncestor switch
        {
            4 => MaryHillColour.Blue,
            5 => MaryHillColour.Green,
            6 => MaryHillColour.Red,
            7 => MaryHillColour.Yellow,
            _ => throw new InvalidOperationException("Could not resolve Marry Hill colour.")
        };
    }
}