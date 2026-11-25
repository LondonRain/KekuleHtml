// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using GeneGenie.Gedcom.Enums;
using KekuleHtml.Properties;
using System.Text.RegularExpressions;

namespace KekuleHtml.Models
{
    /// <summary>
    /// Extension methods on different <see cref="GedcomRecord"/> objects.
    /// </summary>
    public static class GedcomRecordExtensions
    {
        #region GedcomIndividualRecord

        public static string GetFormattedName(this GedcomIndividualRecord person)
        {
            var name = person.GetName();

            if (name == null)
                return Resources.NameUnknown;

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

        public static string GetFormattedDates(this GedcomIndividualRecord person)
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

        public static string GetFormattedNameWithDates(this GedcomIndividualRecord person) => $"{GetFormattedName(person)} ({GetFormattedDates(person)})";

        /// <summary>
        /// A well-formed day-month-year date: English (short or long) or Hebrew month names as GeneGenie understands them.
        /// </summary>
        private static readonly Regex _ExactDateRegex = new(
            @"^\s*\d{1,2}\s+" +
            @"(JAN(UARY)?|FEB(RUARY)?|MAR(CH)?|APR(IL)?|MAY|JUNE?|JULY?|AUG(UST)?|SEP(T|TEMBER)?|OCT(OBER)?|NOV(EMBER)?|DEC(EMBER)?|" +
            @"TSH|CSH|KSL|TVT|SHV|ADR|ADS|NSN|IYR|SVN|TMZ|AAV|ELL)" +
            @"\.?\s+\d{3,4}\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static string? FormatDate(GedcomDate? date)
        {
            if (date == null)
                return null;

            /* Only an unqualified, well-formed full date is shown in the localized "d" format. GeneGenie fills missing or unrecognized components
             * with 1 and still reports a DateTime for many qualified dates and ranges, so the DateTime alone would claim a precision or certainty
             * the record never had (e.g. "ABT 15 JUN 1900" or "BET JUN 1900 AND AUG 1920"). Dates in other calendars are converted to Gregorian. */
            if (date.DatePeriod == GedcomDatePeriod.Exact && date.DateTime1.HasValue && _ExactDateRegex.IsMatch(date.Date1))
                return date.DateTime1.Value.ToString("d");

            // Everything else is shown as written in the source (without a calendar escape or name).
            return date.DateString?.Trim();
        }

        #endregion

        #region GedcomDate

        /// <summary>
        /// Year between 1000 and 2199.
        /// </summary>
        private static readonly Regex _YearRegex = new(@"\b(1\d{3}|20\d{2}|21\d{2})\b");

        /// <summary>
        /// Extracts the start year from <paramref name="date"/>.
        /// </summary>
        public static bool TryGetYear1(this GedcomDate date, out int? year1)
        {
            year1 = null;
            if (date == null)
                return false;

            return TryGetYear(date.Date1, date.DateTime1, true, out year1);
        }

        /// <summary>
        /// Extracts the end year from <paramref name="date"/>.
        /// </summary>
        public static bool TryGetYear2(this GedcomDate date, out int? year2)
        {
            year2 = null;
            if (date == null)
                return false;

            /* GeneGenie keeps the whole "FROM x TO y" (and "BET x AND y") range text in Date1 and usually leaves Date2 empty. So we look at Date2
             * when it is populated, otherwise at Date1, and take the *last* year – for a range that is the end year (this also recovers the end year
             * for precision combinations like "FROM AUG 1900 TO 1920" where GeneGenie fails to populate DateTime2), for a single date it is simply the
             * start year again. */
            var text = !string.IsNullOrEmpty(date.Date2) ? date.Date2 : date.Date1;
            return TryGetYear(text, date.DateTime2, false, out year2);
        }

        /// <summary>
        /// Reads a year from <paramref name="text"/> (the first or last plausible year, 1000–2199), preferring it over
        /// <paramref name="dateTime"/>: for several non-standard inputs GeneGenie mis-parses the DateTime while the raw text still carries the
        /// correct year (e.g. "26/1/1920" is parsed as year 26, "1900-1920" as its end year). The regex only matches four-digit years, so a
        /// genuinely pre-1000 date – which GeneGenie *can* parse – falls through to the <paramref name="dateTime"/> fallback.
        /// </summary>
        private static bool TryGetYear(string? text, DateTime? dateTime, bool takeFirst, out int? year)
        {
            year = null;

            if (!string.IsNullOrEmpty(text))
            {
                MatchCollection matches = _YearRegex.Matches(text);
                if (matches.Count > 0)
                {
                    year = int.Parse(takeFirst ? matches[0].Value : matches[^1].Value);
                    return true;
                }
            }

            if (dateTime.HasValue)
            {
                year = dateTime.Value.Year;
                return true;
            }

            return false;
        }

        #endregion
    }
}
