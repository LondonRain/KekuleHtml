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
/// Expected years are derived by hand. The display strings of non-standard forms that GeneGenie mis-parses are
/// frozen as golden values (flagged // BUG). FormatDate's "d" branch is culture dependent, so the culture is pinned to de-DE.
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
    [TestMethod]
    // standard forms (derived expectations)
    [DataRow("15 JUN 1900", "15.06.1900")]          // full precision -> localized "d"
    [DataRow("JUN 1900", "JUN 1900")]               // month+year: synthetic "FROM" suppressed
    [DataRow("1900", "1900")]                       // year only
    [DataRow("ABT 1900", "EST 1900")]               // GeneGenie normalizes ABT -> EST
    [DataRow("BEF 1900", "BEF 1900")]
    [DataRow("AFT JUN 1900", "AFT JUN 1900")]
    [DataRow("EST 1900", "EST 1900")]
    [DataRow("CAL 1900", "CAL 1900")]
    [DataRow("BET 1900 AND 1920", "BET 1900 AND 1920")]   // fixed: was collapsing to "01.01.1900"
    [DataRow("FROM 1900 TO 1920", "FROM 1900 TO 1920")]   // fixed: was collapsing to "01.01.1900"
    [DataRow("FROM JUN 1900 TO 1920", "JUN 1900 TO 1920")]
    [DataRow("FROM 1900", "1900")]                        // accepted limitation: loses "FROM"
    [DataRow("TO JUN 1920", "JUN 1920")]
    [DataRow("999", "999")]
    [DataRow("1000", "1000")]
    [DataRow("2199", "2199")]
    [DataRow("2200", "2200")]
    [DataRow("", "")]
    // non-standard forms with a real year (display frozen as golden master)
    [DataRow("15 Juni 1900", "15.01.1900")]         // BUG: German month -> wrong month (January)
    [DataRow("Juni 1900", "Juni 1900")]
    [DataRow("15. JUN 1900", "01.06.1900")]         // BUG: trailing-dot day -> day lost
    [DataRow("15.06.1900", "15.06.1900")]
    [DataRow("06.1900", "06.1900")]
    [DataRow("15/6/1900", "15/6/1900")]
    [DataRow("(06/1900)", "(06/1900)")]
    [DataRow("1900-1920", "1900-1920")]
    [DataRow("1900 - 1920", "31.01.1920")]          // BUG: dash range -> range lost
    [DataRow("Vor 1900", "Vor 1900")]
    [DataRow("Ca 1900", "Ca 1900")]
    [DataRow("BF JUN 1900", "01.06.1900")]          // BUG: "BF" qualifier dropped
    [DataRow("1900  OR 1920", "31.01.1920")]        // BUG: "OR" range lost
    [DataRow("NN NNN 1900", "01.01.1900")]          // BUG: placeholder day/month -> 01.01
    [DataRow("ABT 06.1900", "EST 06.1900")]
    [DataRow("BEF 06.1900", "BEF 06.1900")]
    [DataRow("BET 10.1900 AND 1920", "BET 10.1900 AND 1920")]
    public void FormatDate_returns_expected_string(string input, string expected)
    {
        Assert.AreEqual(expected, GedcomRecordExtensions.FormatDate(Parse(input)));
    }

    [TestMethod]
    public void FormatDate_null_returns_null()
    {
        Assert.IsNull(GedcomRecordExtensions.FormatDate(null));
    }

    // Data-less garbage: don't pin the exact string, only guarantee robustness (no throw, non-null result).
    [TestMethod]
    [DataRow("NN NNN NNNN")]
    [DataRow("(lorem)")]
    [DataRow("(lorem ipsum)")]
    public void FormatDate_garbage_does_not_throw(string input)
    {
        Assert.IsNotNull(GedcomRecordExtensions.FormatDate(Parse(input)));
    }

    // ---- TryGetYear1 / TryGetYear2 ----
    // input, year1 found?, year1, year2 found?, year2  (year value is ignored when "found" is false)
    // Note: with the unified helper, a single date reports year2 == year1 (a degenerate end year, which
    // MigrationCollector.AddPoint collapses back to null via its yearTo == yearFrom guard).
    [TestMethod]
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
    [DataRow("FROM JUN 1900 TO 1920", true, 1900, true, 1920)]   // dee481f: year2 recovered from Date1 text
    [DataRow("FROM 1900", true, 1900, true, 1900)]
    [DataRow("TO JUN 1920", true, 1920, true, 1920)]
    [DataRow("999", true, 999, true, 999)]                // regex misses -> DateTime fallback
    [DataRow("1000", true, 1000, true, 1000)]
    [DataRow("2199", true, 2199, true, 2199)]
    [DataRow("2200", true, 2200, true, 2200)]             // regex misses -> DateTime fallback
    [DataRow("", false, 0, false, 0)]
    [DataRow("15 Juni 1900", true, 1900, true, 1900)]
    [DataRow("Juni 1900", true, 1900, true, 1900)]
    [DataRow("15. JUN 1900", true, 1900, true, 1900)]
    [DataRow("15.06.1900", true, 1900, true, 1900)]
    [DataRow("06.1900", true, 1900, true, 1900)]
    [DataRow("15/6/1900", true, 1900, true, 1900)]        // regex-first rescues the year GeneGenie mis-parsed as 15
    [DataRow("(06/1900)", true, 1900, true, 1900)]
    [DataRow("1900-1920", true, 1900, true, 1920)]        // regex-first rescues start year (DateTime says 1920)
    [DataRow("1900 - 1920", true, 1900, true, 1920)]
    [DataRow("Vor 1900", true, 1900, true, 1900)]
    [DataRow("Ca 1900", true, 1900, true, 1900)]
    [DataRow("BF JUN 1900", true, 1900, true, 1900)]
    [DataRow("1900  OR 1920", true, 1900, true, 1920)]
    [DataRow("NN NNN 1900", true, 1900, true, 1900)]
    [DataRow("ABT 06.1900", true, 1900, true, 1900)]
    [DataRow("BEF 06.1900", true, 1900, true, 1900)]
    [DataRow("BET 10.1900 AND 1920", true, 1900, true, 1920)]
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
