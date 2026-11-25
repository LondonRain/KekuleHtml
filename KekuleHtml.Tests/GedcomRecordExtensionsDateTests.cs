// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Globalization;

namespace KekuleHtml.Tests;

/// <summary>
/// <para>
/// Tests for our own date/year logic in <see cref="GedcomRecordExtensions"/>.
/// </para>
/// <para>
/// Tests <see cref="GedcomRecordExtensions.FormatDate(GedcomDate?)"/>, <see cref="GedcomRecordExtensions.TryGetYear1(GedcomDate, out int?)"/>
/// and <see cref="GedcomRecordExtensions.TryGetYear2(GedcomDate, out int?)"/>.
/// </para>
/// <para>
/// Dates are produced in-memory via <see cref="GedcomDate.ParseDateString(string)"/> – the same GeneGenie behaviour
/// pinned by <see cref="GeneGenieDateParsingTests"/>.
/// </para>
/// <para>
/// Expected years are derived by hand. Only well-formed full dates are localized, everything else is displayed as
/// written. FormatDate's "d" branch is culture dependent, so the culture is pinned to de-DE.
/// </para>
/// </summary>
[TestClass]
public sealed class GedcomRecordExtensionsDateTests
{
    [TestInitialize]
    public void PinCulture()
    {
        // FormatDate calls DateTime.ToString("d"); pin the culture so full-precision dates are deterministic.
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    }

    private static GedcomDate Parse(string input)
    {
        var date = new GedcomDate();
        date.ParseDateString(input);
        return date;
    }

    // ---- FormatDate: exact expectations ----
    // Rows use the same four groups and order as GeneGenieDateParsingTests (Group 4 / blind text lives in the
    // separate robustness test below, since those inputs have no stable display string worth pinning).
    [TestMethod]
    // ===== Group 1: standard GEDCOM forms (parsed as specified) =====
    // Full precision -> localized "d".
    [DataRow("15 JUN 1900", "15.06.1900")]
    // Month+year and year only: no day is invented.
    [DataRow("JUN 1900", "JUN 1900")]
    [DataRow("1900", "1900")]
    [DataRow("ABT 1900", "ABT 1900")]
    [DataRow("BEF 1900", "BEF 1900")]
    [DataRow("AFT JUN 1900", "AFT JUN 1900")]
    [DataRow("EST 1900", "EST 1900")]
    [DataRow("CAL 1900", "CAL 1900")]
    // Two-ended ranges are returned verbatim instead of collapsing to a single invented date.
    [DataRow("BET 1900 AND 1920", "BET 1900 AND 1920")]
    [DataRow("FROM 1900 TO 1920", "FROM 1900 TO 1920")]
    [DataRow("FROM JUN 1900 TO 1920", "FROM JUN 1900 TO 1920")]
    // Open-ended ranges keep their qualifier.
    [DataRow("FROM 1900", "FROM 1900")]
    [DataRow("TO JUN 1920", "TO JUN 1920")]
    [DataRow("TO 1920", "TO 1920")]
    [DataRow("999", "999")]
    [DataRow("1000", "1000")]
    [DataRow("2199", "2199")]
    [DataRow("2200", "2200")]
    [DataRow("", "")]
    // Qualified full-precision dates keep their qualifier instead of being localized.
    [DataRow("ABT 15 JUN 1900", "ABT 15 JUN 1900")]
    [DataRow("BEF 15 JUN 1900", "BEF 15 JUN 1900")]
    [DataRow("AFT 15 JUN 1900", "AFT 15 JUN 1900")]
    [DataRow("FROM 15 JUN 1900", "FROM 15 JUN 1900")]
    [DataRow("TO 15 JUN 1920", "TO 15 JUN 1920")]
    // Ranges are never collapsed to their start date.
    [DataRow("BET 15 JUN 1900 AND 1920", "BET 15 JUN 1900 AND 1920")]
    [DataRow("BET JUN 1900 AND AUG 1920", "BET JUN 1900 AND AUG 1920")]
    [DataRow("FROM JAN 1900 TO MAR 1900", "FROM JAN 1900 TO MAR 1900")]
    // Interpreted dates are shown including their phrase.
    [DataRow("INT 15 JUN 1900 (lorem)", "INT 15 JUN 1900 (lorem)")]
    [DataRow("INT 1900 (lorem 1920)", "INT 1900 (lorem 1920)")]
    // Full dates in other calendars: converted to Gregorian and localized.
    [DataRow("JULIAN 15 JUN 1900", "28.06.1900")]
    [DataRow("@#DHEBREW@ 15 TSH 5660", "19.09.1899")]
    // Dual dating is shown as written, so the second year is not lost.
    [DataRow("15 JUN 1900/01", "15 JUN 1900/01")]
    // ===== Group 2: non-standard forms, parsed correctly (display honest) =====
    [DataRow("15.06.1900", "15.06.1900")]
    [DataRow("06.1900", "06.1900")]
    [DataRow("Juni 1900", "Juni 1900")]
    [DataRow("(06/1900)", "(06/1900)")]
    [DataRow("Vor 1900", "Vor 1900")]
    [DataRow("Ca 1900", "Ca 1900")]
    [DataRow("ABT 06.1900", "ABT 06.1900")]
    [DataRow("BEF 06.1900", "BEF 06.1900")]
    [DataRow("BET 10.1900 AND 1920", "BET 10.1900 AND 1920")]
    [DataRow("ABOUT 1900", "ABOUT 1900")]
    [DataRow("15 JUNE 1900", "15.06.1900")]
    [DataRow("15 SEPT 1900", "15.09.1900")]
    // ===== Group 3: known GeneGenie bugs (mis-parsed internally, but the display stays as written) =====
    [DataRow("BEFORE 1900", "BEFORE 1900")]
    [DataRow("15 Juni 1900", "15 Juni 1900")]
    [DataRow("15. JUN 1900", "15. JUN 1900")]
    [DataRow("15/6/1900", "15/6/1900")]
    [DataRow("1900-1920", "1900-1920")]
    [DataRow("1900 - 1920", "1900 - 1920")]
    [DataRow("BF JUN 1900", "BF JUN 1900")]
    [DataRow("1900  OR 1920", "1900  OR 1920")]
    [DataRow("NN NNN 1900", "NN NNN 1900")]
    public void FormatDate_returns_expected_string(string input, string expected)
    {
        Assert.AreEqual(expected, GedcomRecordExtensions.FormatDate(Parse(input)));
    }

    [TestMethod]
    public void FormatDate_null_returns_null()
    {
        Assert.IsNull(GedcomRecordExtensions.FormatDate(null));
    }

    // Group 4 (data-less garbage / blind text): don't pin the exact string, only guarantee robustness
    // (no throw, non-null result).
    [TestMethod]
    [DataRow("NN NNN NNNN")]
    [DataRow("(lorem)")]
    [DataRow("(lorem ipsum)")]
    public void FormatDate_garbage_does_not_throw(string input)
    {
        Assert.IsNotNull(GedcomRecordExtensions.FormatDate(Parse(input)));
    }

    // ---- TryGetYear1 / TryGetYear2 ----
    // Columns: input, year1 found?, year1, year2 found?, year2 (the year value is ignored when "found" is false).
    // Same four groups and order as the classes above. Note: the unified helper is regex-first, so it repairs most of
    // the Group 3 mis-parses; and a single date reports year2 == year1 (a degenerate end year that
    // MigrationCollector.AddPoint collapses back to null via its yearTo == yearFrom guard).
    [TestMethod]
    // ===== Group 1: standard GEDCOM forms (parsed as specified) =====
    [DataRow("15 JUN 1900", true, 1900, true, 1900)]
    [DataRow("JUN 1900", true, 1900, true, 1900)]
    [DataRow("1900", true, 1900, true, 1900)]
    [DataRow("ABT 1900", true, 1900, true, 1900)]
    [DataRow("BEF 1900", true, 1900, true, 1900)]
    [DataRow("AFT JUN 1900", true, 1900, true, 1900)]
    [DataRow("EST 1900", true, 1900, true, 1900)]
    [DataRow("CAL 1900", true, 1900, true, 1900)]
    [DataRow("BET 1900 AND 1920", true, 1900, true, 1920)]
    [DataRow("FROM 1900 TO 1920", true, 1900, true, 1920)]
    // Mixed-precision range: end year recovered from the Date1 text (GeneGenie leaves DateTime2 null).
    [DataRow("FROM JUN 1900 TO 1920", true, 1900, true, 1920)]
    [DataRow("FROM 1900", true, 1900, true, 1900)]
    [DataRow("TO JUN 1920", true, 1920, true, 1920)]
    [DataRow("TO 1920", true, 1920, true, 1920)]
    // Boundary years fall outside the regex's 1000-2199 window -> the DateTime fallback supplies the year.
    [DataRow("999", true, 999, true, 999)]
    [DataRow("1000", true, 1000, true, 1000)]
    [DataRow("2199", true, 2199, true, 2199)]
    [DataRow("2200", true, 2200, true, 2200)]
    [DataRow("", false, 0, false, 0)]
    [DataRow("ABT 15 JUN 1900", true, 1900, true, 1900)]
    [DataRow("BEF 15 JUN 1900", true, 1900, true, 1900)]
    [DataRow("AFT 15 JUN 1900", true, 1900, true, 1900)]
    [DataRow("FROM 15 JUN 1900", true, 1900, true, 1900)]
    [DataRow("TO 15 JUN 1920", true, 1920, true, 1920)]
    [DataRow("BET 15 JUN 1900 AND 1920", true, 1900, true, 1920)]
    [DataRow("BET JUN 1900 AND AUG 1920", true, 1900, true, 1920)]
    [DataRow("FROM JAN 1900 TO MAR 1900", true, 1900, true, 1900)]
    // The year in the phrase must not be taken for the end year.
    [DataRow("INT 15 JUN 1900 (lorem)", true, 1900, true, 1900)]
    [DataRow("INT 1900 (lorem 1920)", true, 1900, true, 1900)]
    [DataRow("JULIAN 15 JUN 1900", true, 1900, true, 1900)]
    // Hebrew year 5660 is outside the regex window: start year from DateTime1, no end year for a single day (no DateTime2).
    [DataRow("@#DHEBREW@ 15 TSH 5660", true, 1899, false, 0)]
    [DataRow("15 JUN 1900/01", true, 1900, true, 1900)]
    // ===== Group 2: non-standard forms, parsed correctly =====
    [DataRow("15.06.1900", true, 1900, true, 1900)]
    [DataRow("06.1900", true, 1900, true, 1900)]
    [DataRow("Juni 1900", true, 1900, true, 1900)]
    [DataRow("(06/1900)", true, 1900, true, 1900)]
    [DataRow("Vor 1900", true, 1900, true, 1900)]
    [DataRow("Ca 1900", true, 1900, true, 1900)]
    [DataRow("ABT 06.1900", true, 1900, true, 1900)]
    [DataRow("BEF 06.1900", true, 1900, true, 1900)]
    [DataRow("BET 10.1900 AND 1920", true, 1900, true, 1920)]
    [DataRow("ABOUT 1900", true, 1900, true, 1900)]
    [DataRow("15 JUNE 1900", true, 1900, true, 1900)]
    [DataRow("15 SEPT 1900", true, 1900, true, 1900)]
    // ===== Group 3: known GeneGenie bugs -- but the regex-first helper recovers the correct year(s) =====
    [DataRow("BEFORE 1900", true, 1900, true, 1900)]
    [DataRow("15 Juni 1900", true, 1900, true, 1900)]
    [DataRow("15. JUN 1900", true, 1900, true, 1900)]
    // Regex-first rescues the year GeneGenie mis-parsed as 15.
    [DataRow("15/6/1900", true, 1900, true, 1900)]
    // Regex-first rescues the start year (GeneGenie's DateTime says 1920).
    [DataRow("1900-1920", true, 1900, true, 1920)]
    [DataRow("1900 - 1920", true, 1900, true, 1920)]
    [DataRow("BF JUN 1900", true, 1900, true, 1900)]
    [DataRow("1900  OR 1920", true, 1900, true, 1920)]
    [DataRow("NN NNN 1900", true, 1900, true, 1900)]
    // ===== Group 4: data-less garbage / blind text (no year to recover) =====
    [DataRow("NN NNN NNNN", false, 0, false, 0)]
    [DataRow("(lorem)", false, 0, false, 0)]
    [DataRow("(lorem ipsum)", false, 0, false, 0)]
    public void TryGetYear_returns_expected(string input, bool year1Found, int year1, bool year2Found, int year2)
    {
        var date = Parse(input);

        Assert.AreEqual(year1Found, date.TryGetYear1(out var y1), "TryGetYear1 return value");
        if (year1Found)
            Assert.AreEqual(year1, y1, "year1");

        Assert.AreEqual(year2Found, date.TryGetYear2(out var y2), "TryGetYear2 return value");
        if (year2Found)
            Assert.AreEqual(year2, y2, "year2");
    }

    [TestMethod]
    public void TryGetYear_on_null_date_returns_false()
    {
        GedcomDate? date = null;
        Assert.IsFalse(date!.TryGetYear1(out var y1));
        Assert.IsNull(y1);
        Assert.IsFalse(date!.TryGetYear2(out var y2));
        Assert.IsNull(y2);
    }
}
