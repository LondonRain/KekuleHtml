using GeneGenie.Gedcom;
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

        return _Entries.OrderBy(e => e.KekuleNumber).ToList();
    }

    private void Traverse(GedcomIndividualRecord? person, int number)
    {
        if (person == null)
            return;

        var entry = new Person
        {
            KekuleNumber = number,
            GedcomRecord = person,
            Colour = GetColourFromKekule(number)
        };

        if (_Seen.TryGetValue(person.XRefID, out var firstNumber))
        {
            entry.FirstOccurrence = firstNumber;
            _Entries.Add(entry);
            return;
        }

        _Seen[person.XRefID] = number;

        _Entries.Add(entry);

        var father = _Adapter.GetFather(person);
        var mother = _Adapter.GetMother(person);

        Traverse(father, number * 2);
        Traverse(mother, number * 2 + 1);
    }

    public static MaryHillColour GetColourFromKekule(int number)
    {
        if (number < 1)
            throw new ArgumentException("Must be 1 or greater.");

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