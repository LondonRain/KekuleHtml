// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
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
/// <remarks>
/// The base type carries no coordinates so places without a georeference still count towards the <see cref="ResearchFocusCard"/>s.
/// Only <seealso cref="MigrationPointWithCoordinates"/> is drawn on the map.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay}")]
public class MigrationPoint
{
    public required Person Person { get; init; }

    public required PointOrigin PointOrigin { get; init; }

    public required string PlaceName { get; init; }

    public required int YearFrom { get; init; }

    public int? YearTo { get; init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string DebuggerDisplay => $"{Person.Colour}, {Person.FormattedName}, {PointOrigin}, {YearFrom}-{YearTo}, {PlaceName}";
}

/// <summary>
/// A <see cref="MigrationPoint"/> that has an additional georeference and can be drawn on the migration map.
/// </summary>
public sealed class MigrationPointWithCoordinates : MigrationPoint
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }
}