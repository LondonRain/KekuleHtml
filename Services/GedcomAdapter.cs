using GeneGenie.Gedcom;
using GeneGenie.Gedcom.Parser;

namespace KekuleHtml.Services;

/// <summary>
/// Handles parsing of Gedcom files via GeneGenie.
/// </summary>
public sealed class GedcomAdapter
{
    #region Variables

    private readonly GedcomDatabase _Database;

    private readonly Dictionary<string, GedcomFamilyRecord> _Families;

    private readonly Dictionary<string, GedcomIndividualRecord> _Individuals;

    #endregion

    #region Constructor

    public GedcomAdapter(string fileName)
    {
        var reader = GedcomRecordReader.CreateReader(fileName);

        _Database = reader.Database;
        _Families = _Database.Families.ToDictionary(f => f.XRefID);
        _Individuals = _Database.Individuals.ToDictionary(i => i.XRefID);
    }

    #endregion

    #region Methods

    public IReadOnlyCollection<GedcomIndividualRecord> Individuals => _Individuals.Values;

    public GedcomIndividualRecord? GetFather(GedcomIndividualRecord person)
    {
        var family = GetParentFamily(person);

        if (family == null || string.IsNullOrWhiteSpace(family.Husband))
            return null;

        return _Individuals.GetValueOrDefault(family.Husband);
    }

    public GedcomIndividualRecord? GetMother(GedcomIndividualRecord person)
    {
        var family = GetParentFamily(person);

        if (family == null || string.IsNullOrWhiteSpace(family.Wife))
            return null;

        return _Individuals.GetValueOrDefault(family.Wife);
    }

    private GedcomFamilyRecord? GetParentFamily(GedcomIndividualRecord person)
    {
        var link = person.ChildIn.FirstOrDefault();

        if (link == null)
            return null;

        return _Families.GetValueOrDefault(link.Family);
    }

    #endregion
}