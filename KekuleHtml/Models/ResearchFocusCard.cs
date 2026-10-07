// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Services;

namespace KekuleHtml.Models;

/// <summary>
/// A label with its count, used for the ranked "top places" / "top surnames" lists.
/// </summary>
public sealed record CountedItem(string Label, int Count);

/// <summary>
/// One "research focus" card: the key figures of a single Mary-Hill line plus its ranked top lists.
/// Or, when <see cref="Colour"/> is <see langword="null"/>, of the whole tree.
/// A pure data holder - the localized title and the HTML are built in <see cref="HtmlWriter"/>.
/// </summary>
public sealed class ResearchFocusCard
{
    /// <summary>
    /// The line's colour, or <see langword="null"/> for the aggregate "total" card.
    /// </summary>
    public required MaryHillColour? Colour { get; init; }

    /// <summary>
    /// Surname of the generation-2 ancestor of this line (Kekulé 4/5/6/7); may be <see langword="null"/>.
    /// </summary>
    public string? AncestorName { get; init; }

    /// <summary>
    /// Distinct persons in this line (duplicates from "Ahnenschwund" counted once).
    /// </summary>
    public required int PersonCount { get; init; }

    /// <summary>
    /// Distinct place names in this line.
    /// </summary>
    public required int PlaceCount { get; init; }

    /// <summary>
    /// Distinct surnames in this line.
    /// </summary>
    public required int SurnameCount { get; init; }

    /// <summary>
    /// Top places by number of <em>events</em> (empty for the total card).
    /// </summary>
    public required IReadOnlyList<CountedItem> TopPlaces { get; init; }

    /// <summary>
    /// Top surnames by number of <em>persons</em> (empty for the total card).
    /// </summary>
    public required IReadOnlyList<CountedItem> TopSurnames { get; init; }

    /// <summary>
    /// Whether this is the aggregate card over all lines (shows only the key figures).
    /// </summary>
    public bool IsTotal => Colour is null;
}
