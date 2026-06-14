using System.Diagnostics;

namespace KekuleHtml.Models;

public enum PointOrigin
{
    Birth,
    Marriage,
    Death,
    Residence
}

/// <summary>
/// One entry in time of a <see cref="Person"/> being at a <see cref="PlaceName"/> at a certain <see cref="YearFrom"/>.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public sealed class MigrationPoint
{

    public required Person Person { get; init; }

    public required PointOrigin PointOrigin { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required string PlaceName { get; init; }

    public required int YearFrom { get; init; }

    public int? YearTo { get; init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string DebuggerDisplay => $"{Person.Colour}, {Person.FormattedName}, {PointOrigin}, {YearFrom}-{YearTo}, {PlaceName}";
}