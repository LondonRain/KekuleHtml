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
    // input, Date1, Date2, DateTime1 ("yyyy-MM-dd HH:mm:ss" or null), DateTime2, DateString, DatePeriod
    [TestMethod]
    // --- standard GEDCOM forms ---
    [DataRow("15 JUN 1900", "15 JUN 1900", "", "1900-06-15 00:00:00", null, "15 JUN 1900", "Exact")]
    [DataRow("JUN 1900", "JUN 1900", "", "1900-06-01 00:00:00", "1900-06-30 23:59:59", "FROM JUN 1900", "Range")]
    [DataRow("1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "FROM 1900", "Range")]
    // GeneGenie normalizes ABT -> Estimate (DateString "EST 1900"). CAL, by contrast, is kept verbatim.
    [DataRow("ABT 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "EST 1900", "Estimate")]
    [DataRow("BEF 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "BEF 1900", "Before")]
    [DataRow("AFT JUN 1900", "JUN 1900", "", "1900-06-01 00:00:00", "1900-06-30 23:59:59", "AFT JUN 1900", "After")]
    [DataRow("EST 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "EST 1900", "Estimate")]
    [DataRow("CAL 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "CAL 1900", "Calculated")]
    // Two-ended ranges: the whole range stays in Date1 (Date2 stays empty!), but both DateTime endpoints are populated.
    [DataRow("BET 1900 AND 1920", "1900 AND 1920", "", "1900-01-01 00:00:00", "1920-01-01 00:00:00", "BET 1900 AND 1920", "Between")]
    [DataRow("FROM 1900 TO 1920", "1900 TO 1920", "", "1900-01-01 00:00:00", "1920-01-01 00:00:00", "FROM 1900 TO 1920", "Range")]
    // Mixed precision "FROM JUN 1900 TO 1920": GeneGenie fails to populate *either* DateTime (the dee481f case).
    [DataRow("FROM JUN 1900 TO 1920", "JUN 1900 TO 1920", "", null, null, "FROM JUN 1900 TO 1920", "Range")]
    // Open-ended FROM/TO collapse to the same shape as a plain year/month (qualifier lost at parse time).
    [DataRow("FROM 1900", "1900", "", "1900-01-01 00:00:00", "1900-12-31 23:59:59", "FROM 1900", "Range")]
    [DataRow("TO JUN 1920", "JUN 1920", "", "1920-06-01 00:00:00", "1920-06-30 23:59:59", "FROM JUN 1920", "Range")]
    // Pure numeric years are parsed into a DateTime regardless of the 1000-2199 range our regex uses.
    [DataRow("999", "999", "", "0999-01-01 00:00:00", "0999-12-31 23:59:59", "FROM 999", "Range")]
    [DataRow("1000", "1000", "", "1000-01-01 00:00:00", "1000-12-31 23:59:59", "FROM 1000", "Range")]
    [DataRow("2199", "2199", "", "2199-01-01 00:00:00", "2199-12-31 23:59:59", "FROM 2199", "Range")]
    [DataRow("2200", "2200", "", "2200-01-01 00:00:00", "2200-12-31 23:59:59", "FROM 2200", "Range")]
    [DataRow("", "", "", null, null, "", "Exact")]
    // --- non-standard formats that actually occur in real GEDCOM exports (synthetic values) ---
    // German month name: not recognized -> month silently defaults to January (a lossy quirk).
    [DataRow("15 Juni 1900", "15 Juni 1900", "", "1900-01-15 00:00:00", null, "15 Juni 1900", "Exact")]
    [DataRow("Juni 1900", "Juni 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "FROM Juni 1900", "Range")]
    // Trailing dot on the day: not recognized -> day defaults to 1.
    [DataRow("15. JUN 1900", "15. JUN 1900", "", "1900-06-01 00:00:00", null, "15. JUN 1900", "Exact")]
    [DataRow("15.06.1900", "15.06.1900", "", "1900-06-15 00:00:00", null, "FROM 15.06.1900", "Range")]
    [DataRow("06.1900", "06.1900", "", null, null, "FROM 06.1900", "Range")]
    // Slash date "15/6/1900": mis-parsed as year 15.
    [DataRow("15/6/1900", "15/6/1900", "", "0015-01-01 00:00:00", "0015-12-31 23:59:59", "FROM 15/6/1900", "Range")]
    [DataRow("(06/1900)", "(06/1900)", "", null, null, "FROM (06/1900)", "Range")]
    // Dash ranges: mis-parsed to a DateTime around the *end* year.
    [DataRow("1900-1920", "1900-1920", "", "1920-01-31 00:00:00", "1920-02-28 23:59:59", "FROM 1900-1920", "Range")]
    [DataRow("1900 - 1920", "1900 - 1920", "", "1920-01-31 00:00:00", "1920-02-28 23:59:59", "FROM 1900 - 1920", "Range")]
    [DataRow("Vor 1900", "Vor 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "FROM Vor 1900", "Range")]
    [DataRow("Ca 1900", "Ca 1900", "", "1900-01-01 00:00:00", "1900-01-31 23:59:59", "FROM Ca 1900", "Range")]
    [DataRow("BF JUN 1900", "BF JUN 1900", "", "1900-06-01 00:00:00", null, "BF JUN 1900", "Exact")]
    [DataRow("1900  OR 1920", "1900  OR 1920", "", "1920-01-31 00:00:00", null, "1900  OR 1920", "Exact")]
    [DataRow("NN NNN 1900", "NN NNN 1900", "", "1900-01-01 00:00:00", null, "NN NNN 1900", "Exact")]
    [DataRow("ABT 06.1900", "06.1900", "", null, null, "EST 06.1900", "Estimate")]
    [DataRow("BEF 06.1900", "06.1900", "", null, null, "BEF 06.1900", "Before")]
    [DataRow("BET 10.1900 AND 1920", "10.1900 AND 1920", "", null, "1920-01-01 00:00:00", "BET 10.1900 AND 1920", "Between")]
    // --- data-less garbage / blind text ---
    [DataRow("NN NNN NNNN", "NN NNN NNNN", "", null, null, "NN NNN NNNN", "Exact")]
    [DataRow("(lorem)", "(lorem)", "", null, null, "FROM (lorem)", "Range")]
    [DataRow("(lorem ipsum)", "(lorem ipsum)", "", null, null, "FROM (lorem ipsum)", "Range")]
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
