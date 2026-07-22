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

    private readonly Dictionary<string, ulong> _Seen = [];

    private readonly List<Person> _Entries = [];

    public IReadOnlyList<Person> GetPersons(GedcomIndividualRecord root, int maxGenerations)
    {
        Traverse(root, 1, maxGenerations);

        // Now that every occurrence is known, link all occurrences of the same individual to each other ("Ahnenschwund"). The shared, sorted list is handed to each entry.
        foreach (var group in _Entries.GroupBy(e => e.GedcomRecord.XRefID))
        {
            var occurrences = group.Select(e => e.KekuleNumber).Order().ToList();

            foreach (var entry in group)
                entry.Occurrences = occurrences;
        }

        return _Entries.OrderBy(e => e.KekuleNumber).ToList();
    }

    private void Traverse(GedcomIndividualRecord? person, ulong kekuleNumber, int maxGenerations) => Traverse(person, kekuleNumber, maxGenerations, []);

    private void Traverse(GedcomIndividualRecord? person, ulong kekuleNumber, int maxGenerations, HashSet<string> ancestryPath)
    {
        if (person == null)
            return;

        var entry = new Person
        {
            KekuleNumber = kekuleNumber,
            GedcomRecord = person,
            Colour = GetColourFromKekule(kekuleNumber)
        };

        // Remember the first occurrence so duplicates ("Ahnenschwund") can be marked and cross-linked,
        // but keep traversing either way so every occurrence's ancestors are written out as well.
        if (!_Seen.TryGetValue(person.XRefID, out var firstNumber))
            _Seen[person.XRefID] = kekuleNumber;
        else
            entry.FirstOccurrence = firstNumber;

        _Entries.Add(entry);

        // Guard against cyclic data (a person being their own ancestor); a normal implex is not a cycle
        // because the repeated individual sits on a different, already-finished branch, not on this path.
        if (!ancestryPath.Add(person.XRefID))
            return;

        var father = _Adapter.GetFather(person);
        var mother = _Adapter.GetMother(person);

        // Only descend while we are below the requested generation depth and while the parents' Kekule numbers still fit into their data type.
        if (Person.GenerationOf(kekuleNumber) < maxGenerations &&
            TryGetParentKekuleNumbers(kekuleNumber, out var fatherNumber, out var motherNumber))
        {
            try
            {
                Traverse(father, fatherNumber, maxGenerations, ancestryPath);
                Traverse(mother, motherNumber, maxGenerations, ancestryPath);
            }
            catch (Exception ex)
            {
                throw new KekuleHtmlException($"Failed to build Kekule list for person K:{kekuleNumber} \"{person.GetFormattedNameWithDates()}\".", ex);
            }
        }

        ancestryPath.Remove(person.XRefID);
    }

    /// <summary>
    /// Maps the given <paramref name="kekuleNumber"/> into a <see cref="MaryHillColour"/>.
    /// </summary>
    private static MaryHillColour GetColourFromKekule(ulong kekuleNumber)
    {
        if (kekuleNumber == 0)
            throw new ArgumentException($"Must be 1 or greater, but was {kekuleNumber}.", nameof(kekuleNumber));

        // fixed colour for first 2 generations (0 and 1).
        if (kekuleNumber == 1 || kekuleNumber == 2)
            return MaryHillColour.Blue;
        else if (kekuleNumber == 3)
            return MaryHillColour.Red;

        // fall back to one of the 4 people of generation 2
        ulong baseAncestor = kekuleNumber;
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

    /// <summary>
    /// <para>
    /// Computes the parents' Kekule numbers (father = <paramref name="number"/> * 2, mother = father + 1).
    /// </para>
    /// <para>
    /// Returns <see langword="false"/> when that calculation would overflow the data type of the Kekule number (currently <see cref="ulong"/>),
    /// so the traversal can stop instead of wrapping into an invalid number.
    /// </para>
    /// </summary>
    private static bool TryGetParentKekuleNumbers(ulong number, out ulong fatherNumber, out ulong motherNumber)
    {
        try
        {
            // Prevents overflows.
            checked
            {
                fatherNumber = number * 2;
                motherNumber = fatherNumber + 1;
            }

            return true;
        }
        catch (OverflowException)
        {
            fatherNumber = 0;
            motherNumber = 0;
            return false;
        }
    }
}