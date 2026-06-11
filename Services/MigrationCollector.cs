using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Globalization;

namespace KekuleHtml.Services;

public class MigrationCollector(GedcomAdapter adapter)
{
    private readonly GedcomAdapter _Adapter = adapter;

    #region Points

    public IReadOnlyList<MigrationPoint> GetMigrationPoins(FamilyTree familyTree)
    {
        var points = new HashSet<MigrationPoint>();

        foreach (var person in familyTree.AllPersons.Where(p => !p.IsDuplicate))
        {
            CollectBirth(points, person);
            CollectDeath(points, person);
            CollectResidence(points, person);
            CollectMarriage(points, person);
        }

        return points.OrderBy(p => p.MaryHillColour).ThenBy(p => p.Year).ThenBy(p => p.PlaceName).ToList();
    }

    private static void CollectBirth(ISet<MigrationPoint> points, Person person)
    {
        var birth = person.GedcomRecord.Birth;

        if (birth?.Date?.DateTime1 == null)
            return;

        AddPlace(points, birth.Place, birth.Date.DateTime1.Value.Year, person.Color);
    }

    private static void CollectDeath(ISet<MigrationPoint> points, Person person)
    {
        var death = person.GedcomRecord.Death;

        if (death?.Date?.DateTime1 == null)
            return;

        AddPlace(points, death.Place, death.Date.DateTime1.Value.Year, person.Color);
    }

    private static void CollectResidence(ISet<MigrationPoint> points, Person person)
    {
        foreach (var evt in person.GedcomRecord.Attributes)
        {
            if (evt.GedcomTag != "RESI")
                continue;

            if (evt.Date?.DateTime1 == null)
                continue;

            AddPlace(points, evt.Place, evt.Date.DateTime1.Value.Year, person.Color);
        }
    }

    private void CollectMarriage(ISet<MigrationPoint> points, Person person)
    {
        foreach (var familyLink in person.GedcomRecord.SpouseIn)
        {
            var family = _Adapter.GetFamily(familyLink.Family);

            if (family?.Marriage?.Date?.DateTime1 == null)
                continue;

            // will be added from both marriage sides with a different color
            AddPlace(points, family.Marriage.Place, family.Marriage.Date.DateTime1.Value.Year, person.Color);
        }
    }

    private static void AddPlace(ISet<MigrationPoint> points, GedcomPlace? place, int year, MaryHillColour color)
    {
        if (place == null)
            return;

        if (string.IsNullOrWhiteSpace(place.Latitude))
            return;

        if (string.IsNullOrWhiteSpace(place.Longitude))
            return;

        points.Add(
            new MigrationPoint
            {
                Latitude = ParseCoordinate(place.Latitude),
                Longitude = ParseCoordinate(place.Longitude),
                Year = year,
                MaryHillColour = color,
                PlaceName = place.Name
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
                p.MaryHillColour
            })
            .Select(g =>
            {
                var first = g.First();

                return new MigrationCluster
                {
                    Latitude = g.Key.Latitude,
                    Longitude = g.Key.Longitude,
                    MaryHillColour = g.Key.MaryHillColour,
                    Count = g.Count(),
                    MinYear = g.Min(x => x.Year),
                    MaxYear = g.Max(x => x.Year),
                    PlaceName = first.PlaceName
                };
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    #endregion
}