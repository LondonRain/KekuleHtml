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

        internal static string? FormatDate(GedcomDate? date)
        {
            if (date == null)
                return null;

            /* A genuine two-ended range ("FROM x TO y", "BET x AND y"). GeneGenie keeps the whole range text in Date1 (leaving Date2 empty) but
             * populates DateTime1/DateTime2 with the two *distinct* endpoints. Without this guard the token-count heuristic below would mistake a
             * Date1 like "1900 TO 1920" (three whitespace tokens) for a full day-month-year date and collapse the range to a single invented day
             * ("01.01.1900"). We therefore detect the two distinct endpoint years and return the honest qualified range text instead. A single
             * year/month value ("FEB 1837", "1837") has both endpoints in the *same* year (DateTime2 = period end) and is not affected. */
            if (date.DateTime1.HasValue && date.DateTime2.HasValue &&
                date.DateTime1.Value.Year != date.DateTime2.Value.Year &&
                !string.IsNullOrWhiteSpace(date.DateString))
            {
                return date.DateString;
            }

            /* GeneGenie always parses a *complete* DateTime, even when the GEDCOM source only specifies a year ("1837") or a month and a year ("FEB 1837").
             * Missing components are silently filled with 1, so e.g. "FEB 1837" becomes 1837 - 02 - 01 and would be rendered as "01.02.1837" – a day-level
             * precision the record never claimed. Likewise a bare "1837" becomes 1837 - 01 - 01. We therefore only trust the parsed DateTime for the nicely
             * localized "d" format when the raw first date part actually contains all three components (day, month and year). Date1 is kept in GEDCOM form
             * ("1 FEB 1837", "FEB 1837", "1837"), so the number of whitespace - separated tokens reflects the real precision. */
            if (date.DateTime1.HasValue && GetDatePartCount(date.Date1) >= 3)
                return date.DateTime1.Value.ToString("d");

            /* GeneGenie represents an *unqualified* year-only ("1837") or month+year ("FEB 1837") GEDCOM date internally as a range that spans the whole
             * year/month: it sets DatePeriod to Range with an empty second date (DateTime2 is pushed to the end of the year/month) and its DateString getter
             * then prefixes a synthetic "FROM" – so a source value of "FEB 1837" comes back as "FROM FEB 1837". That qualifier is not in the GEDCOM source.
             * For such an open range (Range without an explicit second date) we therefore return the raw first date part, which carries the honest, original
             * precision without the invented qualifier ("FEB 1837", "1837"). A genuine two-ended "FROM x TO y" range has a non-empty Date2 and is preserved
             * by the DateString branch below.
             *
             * Known, accepted limitation: a genuinely open-ended source date like "FROM 1978" (born in or after 1978) is parsed by GeneGenie into the exact
             * same object as a plain "1978" – same Date1, empty Date2, same DateTime1/DateTime2 (year end), DatePeriod=Range, DateString="FROM 1978". The
             * distinction is destroyed at parse time and cannot be recovered from the GedcomDate; the only way to tell them apart would be to re-read the raw
             * GEDCOM source text ourselves. We deliberately optimize for the overwhelmingly common plain year/month case and render "1978", accepting that a
             * (rare, and on a vital event semantically dubious) open-ended "FROM <year>" loses its "FROM". */
            if (date.DatePeriod == GedcomDatePeriod.Range && string.IsNullOrWhiteSpace(date.Date2))
                return date.Date1;

            /* Fallback to the original textual representation for everything that carries a *real* qualifier the source did specify and that a DateTime
             * cannot express: approximate/relative dates (e.g. "ABT 1850", "BEF 1900", "AFT JUL 1887", "EST 1854") and ranges whose endpoints GeneGenie
             * could not both turn into a DateTime (e.g. "BET 10.1790 AND 1791"). Fully parsed two-ended ranges are already handled by the branch above. */
            if (!string.IsNullOrWhiteSpace(date.DateString))
                return date.DateString;

            // Last resort: the raw first date part.
            return date.Date1;
        }

        /// <summary>
        /// Counts the whitespace-separated components of a raw GEDCOM date part (e.g. "1 FEB 1837" → 3, "FEB 1837" → 2, "1837" → 1).
        /// Used to derive the precision of a date, since GeneGenie does not expose it publicly.
        /// </summary>
        private static int GetDatePartCount(string? date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return 0;

            return date.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
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
