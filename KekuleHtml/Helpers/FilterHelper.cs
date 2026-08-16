// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Helpers;

/// <summary>
/// Shared name-list filtering used by both the console and the SuggestionComboBox.
/// Every word in the search text must appear somewhere in the text (case-insensitive).
/// </summary>
public static class FilterHelper
{
    /// <summary>
    /// Search for every word in <paramref name="searchText"/> (AND) but it can be anywhere in <paramref name="text"/>.
    /// </summary>
    public static bool Matches(string? text, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        if (string.IsNullOrEmpty(text))
            return false;

        return searchText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
