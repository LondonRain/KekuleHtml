// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using System.Globalization;

namespace KekuleHtml.Helpers;

/// <summary>
/// Applies the UI language/culture. Shared by the console application and the UI so both the <c>-lang</c> command-line option and the <c>FORCE_ENGLISH</c> test build use the same logic.
/// </summary>
public static class CultureHelper
{
    /// <summary>
    /// Forces the English culture (used by the <c>FORCE_ENGLISH</c> test build, see FORCE_ENGLISH / Directory.Build.props).
    /// </summary>
    public static void ForceEnglish() => Apply(new CultureInfo("en"));

    /// <summary>
    /// Applies <paramref name="culture"/> to the current and default thread cultures.
    /// A <see langword="null"/> value leaves the current system culture unchanged.
    /// </summary>
    public static void Apply(CultureInfo? culture)
    {
        if (culture is null)
            return;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    /// <summary>
    /// Names of all cultures known to the runtime, used to reject made-up culture names.
    /// </summary>
    private static readonly HashSet<string> _KnownCultureNames = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                                                            .Select(culture => culture.Name)
                                                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves a language/culture name (e.g. "de", "en", "de-DE") to a <see cref="CultureInfo"/>, or <see langword="null"/> when <paramref name="language"/> is empty or not a known culture.
    /// </summary>
    /// <remarks>
    /// Under ICU, <see cref="CultureInfo.GetCultureInfo(string)"/> happily returns an empty, made-up  culture for an unknown name like "xx" instead of throwing,
    /// which would silently fall back to the neutral (English) resources. We therefore additionally verify the name against the runtime's known cultures,
    /// so an unknown code keeps the current system culture.
    /// </remarks>
    public static CultureInfo? TryGetCulture(string? language)
    {
        if (string.IsNullOrWhiteSpace(language) || !_KnownCultureNames.Contains(language))
            return null;

        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
