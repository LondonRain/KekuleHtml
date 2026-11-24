// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using GeneGenie.Gedcom.Parser;
using System.Diagnostics.CodeAnalysis;

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

        // skip locations with no name or no coordinates and those where the name is not unique.
        Locations = _Database.Locations
            .Where(l => !string.IsNullOrWhiteSpace(l.Name) && MigrationCollector.HasCoordinate(l.Latitude, l.Longitude))
            .GroupBy(l => l.Name)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single());
    }

    #endregion

    #region Methods

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="path"/> has a supported GEDCOM extension.
    /// </summary>
    public static bool HasGedcomExtension([NotNullWhen(true)] string? path)
    {
        if (path is null)
            return false;

        var extension = Path.GetExtension(path);
        return extension.Equals(KekuleConsts.GedcomExtension, StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(KekuleConsts.GedcomExtensionAlternative, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether <paramref name="path"/> <see cref="HasGedcomExtension(string?)"/> and whether file exists.
    /// </summary>
    public static bool IsValidPath([NotNullWhen(true)] string? path) => HasGedcomExtension(path) && Path.Exists(path);

    public IReadOnlyCollection<GedcomIndividualRecord> Individuals => _Individuals.Values;

    public IReadOnlyList<GedcomIndividualRecord> IndividualsSorted
    {
        get
        {
            return Individuals
                .Where(i => i.Names.Any())
                .OrderBy(i => i.GetName().Surname)
                .ThenBy(i => i.GetName().Name)
                .ToList();
        }
    }

    /// <summary>
    /// Gets all the locations that have a valid coordinate and are uniquely identifiable by name.
    /// </summary>
    public Dictionary<string, GedcomLocationRecord> Locations { get; }

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

    internal GedcomFamilyRecord? GetFamily(string family)
    {
        return _Families.GetValueOrDefault(family);
    }

    #endregion
}