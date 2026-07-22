// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Services;

/// <summary>
/// Parses command-line arguments into <see cref="AppOptions"/>.
/// Shared by the console application and the UI so both understand the same options.
/// </summary>
/// <remarks>
/// Adding a new option means adding one <c>case</c> below plus a property on <see cref="AppOptions"/>.
/// </remarks>
public static class CommandLineParser
{
    /// <summary>
    /// Name of the option that sets <see cref="AppOptions.MaxGenerations"/>.
    /// </summary>
    private const string MAX_GENERATIONS_OPTION = "-maxGenerations";

    /// <summary>
    /// Parses <paramref name="args"/>. Unknown options are ignored.
    /// Invalid or out-of-range values fall back to their defaults, so parsing never throws.
    /// </summary>
    public static AppOptions Parse(IReadOnlyList<string> args)
    {
        string? gedcomPath = null;
        int maxGenerations = KekuleDefaults.DefaultMaxGenerations;

        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case MAX_GENERATIONS_OPTION:
                    if (TryTakeValue(args, ref i, out var raw) &&
                        int.TryParse(raw, out var value))
                    {
                        maxGenerations = Math.Clamp(value, KekuleDefaults.MinGenerations, KekuleDefaults.MaxGenerations);
                    }
                    break;

                // Add further options here, e.g.:
                // case "-lang":
                //     if (TryTakeValue(args, ref i, out var lang)) language = lang;
                //     break;

                default:
                    // First non-option argument is treated as the GEDCOM file path.
                    if (!args[i].StartsWith('-') && gedcomPath is null)
                        gedcomPath = args[i];
                    break;
            }
        }

        return new AppOptions
        {
            GedcomPath = gedcomPath,
            MaxGenerations = maxGenerations
        };
    }

    /// <summary>
    /// Consumes the value following an option (advancing <paramref name="i"/>), if present.
    /// </summary>
    private static bool TryTakeValue(IReadOnlyList<string> args, ref int i, out string value)
    {
        if (i + 1 < args.Count)
        {
            value = args[++i];
            return true;
        }

        value = string.Empty;
        return false;
    }
}
