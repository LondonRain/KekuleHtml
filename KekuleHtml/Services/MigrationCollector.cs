// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Diagnostics;
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

        Debug.WriteLine($"Found {points.Count} {nameof(MigrationPoint)}s of which {points.OfType<MigrationPointWithCoordinates>().Count()} have coordinates.");

        return points.OrderBy(p => p.Person.Colour).ThenBy(p => p.YearFrom).ThenBy(p => p.PlaceName).ToList();
    }

    private void CollectBirth(IList<MigrationPoint> points, Person person)
    {
        var birth = person.GedcomRecord.Birth;

        if (birth == null || !birth.Date.TryGetYear1(out var year1))
            return;

        AddPoint(
            points,
            person,
            PointOrigin.Birth,
            birth.Place,
            year1!.Value);
    }

    private void CollectDeath(IList<MigrationPoint> points, Person person)
    {
        var death = person.GedcomRecord.Death;

        if (death == null || !death.Date.TryGetYear1(out var year1))
            return;

        AddPoint(
            points,
            person,
            PointOrigin.Death,
            death.Place,
            year1!.Value);
    }

    private void CollectResidence(IList<MigrationPoint> points, Person person)
    {
        // residence from a single person
        foreach (var evt in person.GedcomRecord.Attributes)
        {
            if (evt.GedcomTag != "RESI")
                continue;

            if (!evt.Date.TryGetYear1(out var year1))
                continue;

            // a second date is optional
            _ = evt.Date.TryGetYear2(out var year2);

            AddPoint(
                points,
                person,
                PointOrigin.Residence,
                evt.Place,
                year1!.Value,
                year2);
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

                if (!evt.Date.TryGetYear1(out var year1))
                    continue;

                // a second date is optional
                _ = evt.Date.TryGetYear2(out var year2);

                // will be added for both partners with a different colour
                AddPoint(
                    points,
                    person,
                    PointOrigin.Residence,
                    evt.Place,
                    year1!.Value,
                    year2);
            }
        }
    }

    private void CollectMarriage(IList<MigrationPoint> points, Person person)
    {
        foreach (var familyLink in person.GedcomRecord.SpouseIn)
        {
            var family = _Adapter.GetFamily(familyLink.Family);

            if (family?.Marriage == null || !family.Marriage.Date.TryGetYear1(out var year1))
                continue;

            // will be added from both marriage sides with a different colour
            AddPoint(
                points,
                person,
                PointOrigin.Marriage,
                family.Marriage.Place,
                year1!.Value);
        }
    }

    private void AddPoint(
        IList<MigrationPoint> points,
        Person person,
        PointOrigin pointOrigin,
        GedcomPlace place,
        int yearFrom,
        int? yearTo = null)
    {
        // a place name is the minimum needed to attribute the event to a location
        if (place == null || string.IsNullOrWhiteSpace(place.Name))
            return;

        if (yearTo == yearFrom)
            yearTo = null;

        // points with a georeference can be drawn on the map. the others still count towards the research focus lists.
        var latitudeString = place.Latitude;
        var longitudeString = place.Longitude;

        if (!HasCoordinate(latitudeString, longitudeString))
        {
            // some events do not carry the coordinates themselves but only reference a location entry that does, so fall back to that to include those.
            latitudeString = place.Location?.Latitude;
            longitudeString = place.Location?.Longitude;
        }

        if (!HasCoordinate(latitudeString, longitudeString))
        {
            // some events do not even reference a location entry. we try to match a uniquely matching place name with coordinates as last fallback.
            if (_Adapter.Locations.TryGetValue(place.Name, out var locationRecord))
            {
                latitudeString = locationRecord.Latitude;
                longitudeString = locationRecord.Longitude;
            }
        }

        if (HasCoordinate(latitudeString, longitudeString) &&
            TryParseCoordinate(latitudeString!, out var latitude) &&
            TryParseCoordinate(longitudeString!, out var longitude))
        {
            points.Add(
                new MigrationPointWithCoordinates
                {
                    Person = person,
                    PointOrigin = pointOrigin,
                    PlaceName = place.Name,
                    YearFrom = yearFrom,
                    YearTo = yearTo,
                    Latitude = latitude,
                    Longitude = longitude
                });
        }
        else
        {
            points.Add(
                new MigrationPoint
                {
                    Person = person,
                    PointOrigin = pointOrigin,
                    PlaceName = place.Name,
                    YearFrom = yearFrom,
                    YearTo = yearTo
                });
        }
    }

    public static bool HasCoordinate(string? latitude, string? longitude) => !string.IsNullOrWhiteSpace(latitude) && !string.IsNullOrWhiteSpace(longitude);

    private static bool TryParseCoordinate(string coordinate, out double coordinateValue)
    {
        coordinate = coordinate.Trim();

        var sign = coordinate.StartsWith('S') || coordinate.StartsWith('W') ? -1 : 1;

        // skip hemisphere indicator
        coordinate = coordinate[1..];

        // handle GEDCOM invalid coordinates that contain a comma as decimal separator. we can heal that.
        coordinate = coordinate.Replace(',', '.');

        if (double.TryParse(coordinate, CultureInfo.InvariantCulture, out coordinateValue))
        {
            // simple guard against coordinates that would be out of range
            if (coordinateValue >= 0.0 && coordinateValue <= 180.0)
            {
                coordinateValue *= sign;
                return true;
            }
        }

        Debug.WriteLine($"Could not parse coordinate {coordinate}");

        return false;
    }

    #endregion

    #region Cluster

    public static IReadOnlyList<MigrationCluster> BuildMigrationClusters(IEnumerable<MigrationPoint> points)
    {
        var migrationClusters = points
            .OfType<MigrationPointWithCoordinates>()
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

        Debug.WriteLine($"Found {migrationClusters.Count} {nameof(MigrationCluster)}s.");

        return migrationClusters;
    }

    #endregion
}