// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Diagnostics;

namespace KekuleHtmlUi.Presenters;

/// <summary>
/// Represents a person from the GEDCOM file, as displayed and selected in the combobox.
/// </summary>
[DebuggerDisplay("{DisplayName}")]
public class PersonPresenter(GedcomIndividualRecord person)
{
    public GedcomIndividualRecord Person { get; init; } = person;

    /// <summary>
    /// Display name, e.g. "Max Mustermann (* 1900 - ✝1980)".
    /// </summary>
    public string DisplayName
    {
        get => Person.GetFormattedNameWithDates();
    }

    public override string ToString() => DisplayName;
}
