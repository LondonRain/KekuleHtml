// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Globalization;

namespace KekuleHtml.Services;

public class MigrationCollector(GedcomAdapter adapter)
{
    private readonly GedcomAdapter _Adapter = adapter;

    #region Points

    public IReadOnlyList<MigrationPoint> GetMigrationPoints(FamilyTree familyTree)
    {
        var points = new List<MigrationPoint>();

        foreach (var person in familyTree.AllPersons.Where(p => !p.IsDuplicate))
        {
            CollectBirth(points, person);
            CollectDeath(points, person);
            CollectResidence(points, person);
            CollectMarriage(points, person);
        }

        return points.OrderBy(p => p.Person.Colour).ThenBy(p => p.YearFrom).ThenBy(p => p.PlaceName).ToList();
    }

    private static void CollectBirth(IList<MigrationPoint> points, Person person)
    {
        var birth = person.GedcomRecord.Birth;

        if (birth?.Date?.DateTime1 == null)
            return;

        AddPoint(
            points,
            person,
            PointOrigin.Birth,
            birth.Place,
            birth.Date.DateTime1.Value.Year);
    }

    private static void CollectDeath(IList<MigrationPoint> points, Person person)
    {
        var death = person.GedcomRecord.Death;

        if (death?.Date?.DateTime1 == null)
            return;

        AddPoint(
            points,
            person,
            PointOrigin.Death,
            death.Place,
            death.Date.DateTime1.Value.Year);
    }

    private void CollectResidence(IList<MigrationPoint> points, Person person)
    {
        // residence from a single person
        foreach (var evt in person.GedcomRecord.Attributes)
        {
            if (evt.GedcomTag != "RESI")
                continue;

            if (evt.Date?.DateTime1 == null)
                continue;

            AddPoint(
                points,
                person,
                PointOrigin.Residence,
                evt.Place,
                evt.Date.DateTime1.Value.Year,
                evt.Date.DateTime2?.Year);
        }

        // residences from marriages
        foreach (var familyLink in person.GedcomRecord.SpouseIn)
        {
            var family = _Adapter.GetFamily(familyLink.Family);

            if (family == null)
                continue;

            foreach (var evt in family.Events)
            {
                if (evt.GedcomTag != "RESI")
                    continue;

                if (evt.Date?.DateTime1 == null)
                    continue;

                // will be added for both partners with a different colour
                AddPoint(
                    points,
                    person,
                    PointOrigin.Residence,
                    evt.Place,
                    evt.Date.DateTime1.Value.Year,
                    evt.Date.DateTime2?.Year);
            }
        }
    }

    private void CollectMarriage(IList<MigrationPoint> points, Person person)
    {
        foreach (var familyLink in person.GedcomRecord.SpouseIn)
        {
            var family = _Adapter.GetFamily(familyLink.Family);

            if (family?.Marriage?.Date?.DateTime1 == null)
                continue;

            // will be added from both marriage sides with a different colour
            AddPoint(
                points,
                person,
                PointOrigin.Marriage,
                family.Marriage.Place,
                family.Marriage.Date.DateTime1.Value.Year);
        }
    }

    private static void AddPoint(
        IList<MigrationPoint> points,
        Person person,
        PointOrigin pointOrigin,
        GedcomPlace place,
        int yearFrom,
        int? yearTo = null)
    {
        if (string.IsNullOrWhiteSpace(place?.Latitude) || string.IsNullOrWhiteSpace(place?.Longitude))
            return;

        if (yearTo == yearFrom)
            yearTo = null;

        points.Add(
            new MigrationPoint
            {
                Person = person,
                PointOrigin = pointOrigin,
                Latitude = ParseCoordinate(place.Latitude),
                Longitude = ParseCoordinate(place.Longitude),
                PlaceName = place.Name,
                YearFrom = yearFrom,
                YearTo = yearTo
            });
    }

    private static double ParseCoordinate(string value)
    {
        value = value.Trim();

        var sign = value.StartsWith('S') || value.StartsWith('W') ? -1 : 1;

        value = value.Substring(1);

        return sign * double.Parse(value, CultureInfo.InvariantCulture);
    }

    #endregion

    #region Cluster

    public IReadOnlyList<MigrationCluster> BuildMigrationClusters(IEnumerable<MigrationPoint> points)
    {
        return points
            .GroupBy(p => new
            {
                p.Latitude,
                p.Longitude,
                p.Person.Colour
            })
            .Select(g =>
            {
                var first = g.First();

                return new MigrationCluster
                {
                    MigrationPoints = g.OrderBy(p => p.Person.Colour).ThenBy(p => p.Person.FormattedName).ThenBy(p => p.YearFrom).ToList(),
                    Latitude = g.Key.Latitude,
                    Longitude = g.Key.Longitude,
                    MaryHillColour = g.Key.Colour,
                    Count = g.Count(),
                    MinYear = g.Min(x => x.YearFrom),
                    MaxYear = Math.Max(g.Max(x => x.YearFrom), g.Max(x => x.YearTo).GetValueOrDefault()),
                    PlaceName = first.PlaceName
                };
            })
            .OrderBy(c => c.PlaceName)
            .ThenByDescending(x => x.Count)
            .ToList();
    }

    #endregion
}