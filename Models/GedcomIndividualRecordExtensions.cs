// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using System.Text.RegularExpressions;

namespace KekuleHtml.Models
{
    /// <summary>
    /// Extension methods on different <see cref="GedcomRecord"/> objects.
    /// </summary>
    internal static class GedcomIndividualRecordExtensions
    {
        #region GedcomIndividualRecord

        internal static string GetFormattedName(this GedcomIndividualRecord person)
        {
            var name = person.GetName();

            if (name == null)
                return "(unbekannt)";

            var surname = name.Surname?.Trim();
            var given = name.Given?.Replace(",", string.Empty).Replace("\"", string.Empty).Replace("\'", string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(surname) &&
                !string.IsNullOrWhiteSpace(given))
            {
                return $"{surname}, {given}";
            }

            var raw = name.Name ?? string.Empty;

            return raw.Replace("/", string.Empty).Trim();
        }

        internal static string GetFormattedDates(this GedcomIndividualRecord person)
        {
            var birth = FormatDate(person.Birth?.Date);

            var death = FormatDate(person.Death?.Date);

            if (string.IsNullOrWhiteSpace(birth) && string.IsNullOrWhiteSpace(death))
                return string.Empty;
            else if (!string.IsNullOrWhiteSpace(birth) && string.IsNullOrWhiteSpace(death))
                return $"* {birth}";
            else if (string.IsNullOrWhiteSpace(birth) && !string.IsNullOrWhiteSpace(death))
                return $"† {death}";
            else
                return $"*{birth} – †{death}";
        }

        internal static string GetFormattedNameWithDates(this GedcomIndividualRecord person) => $"{GetFormattedName(person)} ({GetFormattedDates(person)})";

        private static string? FormatDate(GedcomDate? date)
        {
            if (date == null)
                return null;

            if (date.DateTime1.HasValue)
            {
                var value = date.DateTime1.Value;

                if (value.Day != 1 || value.Month != 1 ||
                    !string.Equals(date.Date1, value.Year.ToString(), StringComparison.Ordinal))
                {
                    return value.ToString("d");
                }
            }

            if (!string.IsNullOrWhiteSpace(date.DateString))
                return date.DateString;

            return date.Date1;
        }

        #endregion

        #region GedcomDate

        /// <summary>
        /// Year between 1000 and 2199.
        /// </summary>
        private static readonly Regex _YearRegex = new(@"\b(1\d{3}|20\d{2}|21\d{2})\b");

        internal static bool TryGetYear1(this GedcomDate date, out int? year1)
        {
            year1 = null;
            if (date == null)
                return false;

            if (date.DateTime1.HasValue)
            {
                // well formed date in DateTime format
                year1 = date.DateTime1.Value.Year;
                return true;
            }
            else if (!string.IsNullOrEmpty(date.Date1))
            {
                // maybe only partly filled date (like 01.1900), allowed by GEDCOM spec.
                // try to parse a year from it.
                MatchCollection matches = _YearRegex.Matches(date.Date1);
                if (matches.Count > 0)
                {
                    // search for first matching year between 1000 and 2199
                    year1 = int.Parse(matches[0].Value);
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetYear2(this GedcomDate date, out int? year2)
        {
            year2 = null;
            if (date == null)
                return false;

            if (date.DateTime2.HasValue)
            {
                // well formed date in DateTime format
                year2 = date.DateTime2.Value.Year;
                return true;
            }
            else if (!string.IsNullOrEmpty(date.Date2))
            {
                // maybe only partly filled date (like 01.1900), allowed by GEDCOM spec.
                // try to parse a year from it.
                MatchCollection matches = _YearRegex.Matches(date.Date2);
                if (matches.Count > 0)
                {
                    // search for last matching year between 1000 and 2199
                    year2 = int.Parse(matches[^1].Value);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
