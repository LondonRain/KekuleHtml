using System.Diagnostics;

namespace KekuleHtml.Models;

[DebuggerDisplay("{MaryHillColour}, {Year}, {PlaceName}")]
public sealed class MigrationPoint : IEquatable<MigrationPoint>
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required string PlaceName { get; init; }

    public required int Year { get; init; }


    public required MaryHillColour MaryHillColour { get; init; }

    public bool Equals(MigrationPoint? other)
    {
        if (other is null)
            return false;

        return Latitude.Equals(other.Latitude)
            && Longitude.Equals(other.Longitude)
            && PlaceName.Equals(other.PlaceName)
            && Year == other.Year
            && MaryHillColour == other.MaryHillColour;
    }

    public override bool Equals(object? obj) => Equals(obj as MigrationPoint);

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude, PlaceName, Year, MaryHillColour);
}