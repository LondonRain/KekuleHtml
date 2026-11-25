// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using GeneGenie.Gedcom.Parser;
using KekuleHtml.Models;
using System.Globalization;

namespace KekuleHtml.Tests;

/// <summary>
/// <para>
/// Characterization / lock-in tests for the bundled <see cref="GeneGenie.Gedcom"/> library.
/// </para>
/// <para>
/// These pin the <b>observed</b> parse behaviour of <see cref="GedcomDate.ParseDateString(string)"/> so that
/// swapping the DLL for a different build makes any behavioural delta fail loudly instead of silently changing
/// our output.
/// </para>
/// <para>
/// The single-date in-memory parse reproduces every quirk we rely on in <see cref="GedcomRecordExtensions"/>.
/// A full <see cref="GedcomRecordReader"/> file is not required. All input values are synthetic
/// (years 1900/1920, uniform day 15 / month June); only the boundary cases use special years.
/// </para>
/// </summary>
[TestClass]
public sealed class GeneGenieDateParsingTests
{
    // Columns: input, Date1, Date2, DateTime1 ("yyyy-MM-dd HH:mm:ss" or null), DateTime2, DateString, DatePeriod.
    // Rows are split into four groups (identical order across all three test classes) so a value can be traced
    // from parse -> format -> year side by side. A value counts as a "known bug" when GeneGenie mis-parses it here;
    // FormatDate / TryGetYear may still hide or repair that downstream (see their per-row notes).
    [TestMethod]
    // ===== Group 1: standard GEDCOM forms (parsed as specified) =====
    [DataRow("15 JUN 1900", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "15 JUN 1900", "Exact")]
    // A year or month on its own is interpreted as a Range, but DateString keeps the text as written.
    [DataRow("JUN 1900", "JUN 1900", "", "1900-06-01 00:00:00", "1900-06-30 23:59:59", "JUN 1900", "Range")]
    [DataRow("1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "1900", "Range")]
    // ABT is interpreted as DatePeriod Estimate, DateString still says ABT.
    [DataRow("ABT 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "ABT 1900", "Estimate")]
    [DataRow("BEF 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "BEF 1900", "Before")]
    [DataRow("AFT JUN 1900", "JUN 1900", "", "1900-06-01 00:00:00", "1900-06-30 23:59:59", "AFT JUN 1900", "After")]
    [DataRow("EST 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "EST 1900", "Estimate")]
    [DataRow("CAL 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "CAL 1900", "Calculated")]
    // Two-ended ranges: the whole range stays in Date1 (Date2 stays empty!), but both DateTime endpoints are populated.
    [DataRow("BET 1900 AND 1920", "1900 AND 1920", "", "1900-01-01 00:00:00", "1920-01-01 00:00:00", "BET 1900 AND 1920", "Between")]
    [DataRow("FROM 1900 TO 1920", "1900 TO 1920", "", "1900-01-01 00:00:00", "1920-01-01 00:00:00", "FROM 1900 TO 1920", "Range")]
    // Mixed precision: GeneGenie leaves *both* DateTime endpoints null here, so the end year survives only in Date1.
    [DataRow("FROM JUN 1900 TO 1920", "JUN 1900 TO 1920", "", null, null, "FROM JUN 1900 TO 1920", "Range")]
    // Open-ended FROM/TO share Date1 and DateTimes with a plain year/month; only DateString keeps the qualifier.
    [DataRow("FROM 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "FROM 1900", "Range")]
    [DataRow("TO JUN 1920", "JUN 1920", "", "1920-06-01 00:00:00", "1920-06-30 23:59:59", "TO JUN 1920", "Range")]
    [DataRow("TO 1920", "1920", "", "1920-01-01 00:00:00", "1920-12-31 23:59:59", "TO 1920", "Range")]
    // Boundary years: parsed into a DateTime regardless of the 1000-2199 range our regex uses.
    [DataRow("999", "999", "", "0999-01-01 00:00:00", "0999-12-31 23:59:59", "999", "Range")]
    [DataRow("1000", "1000", "", "1000-01-01 00:00:00", "1000-12-31 23:59:59", "1000", "Range")]
    [DataRow("2199", "2199", "", "2199-01-01 00:00:00", "2199-12-31 23:59:59", "2199", "Range")]
    [DataRow("2200", "2200", "", "2200-01-01 00:00:00", "2200-12-31 23:59:59", "2200", "Range")]
    // Empty input.
    [DataRow("", "", "", null, null, "", "Exact")]
    // Qualified full-precision dates: DateTime1 is set although the date is not exact.
    [DataRow("ABT 15 JUN 1900", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "ABT 15 JUN 1900", "Estimate")]
    [DataRow("BEF 15 JUN 1900", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "BEF 15 JUN 1900", "Before")]
    [DataRow("AFT 15 JUN 1900", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "AFT 15 JUN 1900", "After")]
    [DataRow("FROM 15 JUN 1900", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "FROM 15 JUN 1900", "Range")]
    [DataRow("TO 15 JUN 1920", "15 JUN 1920", "", "1920-06-15 00:00:00", null, "TO 15 JUN 1920", "Range")]
    // Ranges with mixed or month precision: only the start gets a DateTime.
    [DataRow("BET 15 JUN 1900 AND 1920", "15 JUN 1900 AND 1920", "", "1900-06-15 00:00:00", null, "BET 15 JUN 1900 AND 1920", "Between")]
    [DataRow("BET JUN 1900 AND AUG 1920", "JUN 1900 AND AUG 1920", "", "1900-06-01 00:00:00", null, "BET JUN 1900 AND AUG 1920", "Between")]
    [DataRow("FROM JAN 1900 TO MAR 1900", "JAN 1900 TO MAR 1900", "", "1900-01-01 00:00:00", null, "FROM JAN 1900 TO MAR 1900", "Range")]
    // Interpreted dates: the phrase is part of DateString only, not of the parsed date.
    [DataRow("INT 15 JUN 1900 (lorem)", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "INT 15 JUN 1900 (lorem)", "Interpretation")]
    [DataRow("INT 1900 (lorem 1920)", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "INT 1900 (lorem 1920)", "Interpretation")]
    // Other calendars: DateTime1 is converted to Gregorian, DateString drops the calendar.
    [DataRow("JULIAN 15 JUN 1900", "15 JUN 1900", "", "1900-06-28 00:00:00", null, "15 JUN 1900", "Exact")]
    [DataRow("@#DHEBREW@ 15 TSH 5660", "15 TSH 5660", "", "1899-09-19 00:00:00", null, "15 TSH 5660", "Exact")]
    // Dual dating.
    [DataRow("15 JUN 1900/01", "15 JUN 1900/01", "", "1900-06-15 00:00:00", null, "15 JUN 1900/01", "Exact")]
    // ===== Group 2: non-standard forms that occur in real exports, but parsed correctly (no lost value) =====
    [DataRow("15.06.1900", "15.06.1900", "", "1900-06-15 00:00:00", null, "15.06.1900", "Range")]
    [DataRow("06.1900", "06.1900", "", null, null, "06.1900", "Range")]
    // German month+year: the internal DateTime month defaults to January, but the value is preserved in the text form.
    [DataRow("Juni 1900", "Juni 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "Juni 1900", "Range")]
    [DataRow("(06/1900)", "(06/1900)", "", null, null, "(06/1900)", "Range")]
    // Unrecognized German qualifiers: kept as text, year preserved.
    [DataRow("Vor 1900", "Vor 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "Vor 1900", "Range")]
    [DataRow("Ca 1900", "Ca 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "Ca 1900", "Range")]
    [DataRow("ABT 06.1900", "06.1900", "", null, null, "ABT 06.1900", "Estimate")]
    [DataRow("BEF 06.1900", "06.1900", "", null, null, "BEF 06.1900", "Before")]
    [DataRow("BET 10.1900 AND 1920", "10.1900 AND 1920", "", null, "1920-01-01 00:00:00", "BET 10.1900 AND 1920", "Between")]
    // Spelled-out qualifier and month names.
    [DataRow("ABOUT 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "ABOUT 1900", "Estimate")]
    [DataRow("15 JUNE 1900", "15 JUNE 1900", "", "1900-06-15 00:00:00", null, "15 JUNE 1900", "Exact")]
    [DataRow("15 SEPT 1900", "15 SEPT 1900", "", "1900-09-15 00:00:00", null, "15 SEPT 1900", "Exact")]
    // ===== Group 3: known GeneGenie bugs (value mis-parsed; frozen as golden master) =====
    // "BEFORE" is matched as "BEF" first, leaving "ORE" in the date.
    [DataRow("BEFORE 1900", "ORE 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "BEFORE 1900", "Before")]
    // German month name: not recognized -> month silently defaults to January.
    [DataRow("15 Juni 1900", "15 Juni 1900", "", "1900-01-15 00:00:00", null, "15 Juni 1900", "Exact")]
    // Trailing dot on the day: not recognized -> day defaults to 1.
    [DataRow("15. JUN 1900", "15. JUN 1900", "", "1900-06-01 00:00:00", null, "15. JUN 1900", "Exact")]
    // Slash date: mis-parsed as year 15.
    [DataRow("15/6/1900", "15/6/1900", "", "0015-01-01 00:00:00", "0015-12-31 23:59:59", "15/6/1900", "Range")]
    // Dash ranges: mis-parsed to a DateTime around the *end* year.
    [DataRow("1900-1920", "1900-1920", "", "1920-01-31 00:00:00", "1920-02-28 23:59:59", "1900-1920", "Range")]
    [DataRow("1900 - 1920", "1900 - 1920", "", "1920-01-31 00:00:00", "1920-02-28 23:59:59", "1900 - 1920", "Range")]
    // "BF" qualifier not recognized -> treated as an exact date.
    [DataRow("BF JUN 1900", "BF JUN 1900", "", "1900-06-01 00:00:00", null, "BF JUN 1900", "Exact")]
    // "OR" range not recognized -> mis-parsed to the end year.
    [DataRow("1900  OR 1920", "1900  OR 1920", "", "1920-01-31 00:00:00", null, "1900  OR 1920", "Exact")]
    // Placeholder day/month tokens -> silently default to 01.01.
    [DataRow("NN NNN 1900", "NN NNN 1900", "", "1900-01-01 00:00:00", null, "NN NNN 1900", "Exact")]
    // ===== Group 4: data-less garbage / blind text (no year to recover) =====
    [DataRow("NN NNN NNNN", "NN NNN NNNN", "", null, null, "NN NNN NNNN", "Exact")]
    [DataRow("(lorem)", "(lorem)", "", null, null, "(lorem)", "Range")]
    [DataRow("(lorem ipsum)", "(lorem ipsum)", "", null, null, "(lorem ipsum)", "Range")]
    public void ParseDateString_pins_GeneGenie_behaviour(
        string input,
        string date1,
        string date2,
        string? dateTime1,
        string? dateTime2,
        string dateString,
        string datePeriod)
    {
        var date = new GedcomDate();
        date.ParseDateString(input);

        Assert.AreEqual(date1, date.Date1, "Date1");
        Assert.AreEqual(date2, date.Date2, "Date2");
        Assert.AreEqual(ToDateTime(dateTime1), date.DateTime1, "DateTime1");
        Assert.AreEqual(ToDateTime(dateTime2), date.DateTime2, "DateTime2");
        Assert.AreEqual(dateString, date.DateString, "DateString");
        Assert.AreEqual(datePeriod, date.DatePeriod.ToString(), "DatePeriod");
    }

    private static DateTime? ToDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        else
            return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
