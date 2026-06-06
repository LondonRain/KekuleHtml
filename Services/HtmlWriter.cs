using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Net;
using System.Text;

namespace KekuleHtml.Services;

/// <summary>
/// Writes the final HTML file.
/// </summary>
public static class HtmlWriter
{
    #region CSS

    private const string CSS = """
<!DOCTYPE html>
<html lang="de">
<head>
<meta charset="utf-8">
<title>Kekulé-Liste</title>
<style>
body {
    font-family: Segoe UI, sans-serif;
    margin: 2rem auto;
    max-width: 1200px;
    line-height: 1.4;
}

h1 {
    margin-bottom: 1rem;
}

h2 {
    margin-top: 2rem;
}

nav {
    margin-bottom: 2rem;
}

nav ul {
    columns: 3;
    padding-left: 1rem;
}

nav li {
    margin-bottom: 0.25rem;
}

.person {
    padding: 4px 8px;
    margin: 2px 0;
    border-left: 8px solid;
}

.blue {
    border-color: #005D8F;
}

.green {
    border-color: #0A7050;
}

.red {
    border-color: #BE2323;
}

.yellow {
    border-color: #F5AF00;
}

.number {
    display: inline-block;
    width: 6em;
    font-weight: bold;
}

.dates {
    color: #555;
}

.duplicate-note {
    color: #888;
    font-style: italic;
}

.stats {
    margin-bottom: 1rem;
    color: #555;
}

a {
    text-decoration: none;
    color: blue;
}

a:hover {
    text-decoration: underline;
}
.label {
    display: inline-block;
    width: 100px;
}
</style>
</head>
<body>
""";

    #endregion

    #region Write

    public static void Write(string fileName, GedcomIndividualRecord rootPerson, IReadOnlyList<Person> entries, IReadOnlyDictionary<int, Generation> statistics)
    {
        var html = new StringBuilder();

        html.AppendLine(CSS);

        html.AppendLine($"<h1>Kekulé-Liste für {Escape(GetFormattedName(rootPerson))}</h1>");

        WriteTableOfContents(html, entries);

        foreach (var generationGroup in entries.GroupBy(e => e.Generation).OrderBy(g => g.Key))
        {
            var generation = generationGroup.Key;

            html.AppendLine($"<section id=\"gen{generation}\">");

            html.AppendLine($"<h2>Generation {generation}</h2>");

            if (statistics.TryGetValue(generation, out var stat))
                WriteStatistics(html, stat);

            foreach (var entry in generationGroup.OrderBy(e => e.KekuleNumber))
                WritePerson(html, entry);

            html.AppendLine("</section>");
        }

        html.AppendLine("""
</body>
</html>
""");

        File.WriteAllText(fileName, html.ToString(), Encoding.UTF8);
    }

    private static void WriteTableOfContents(
        StringBuilder html,
        IReadOnlyList<Person> entries)
    {
        html.AppendLine("<nav>");
        html.AppendLine("<h2>Inhalt</h2>");
        html.AppendLine("<ul>");

        foreach (var generation in entries.Select(e => e.Generation).Distinct().OrderBy(g => g))
            html.AppendLine($"<li><a href=\"#gen{generation}\">Generation {generation}</a></li>");

        html.AppendLine("</ul>");
        html.AppendLine("</nav>");
    }

    private static void WriteStatistics(StringBuilder html, Generation statistics)
    {
        var theoreticalCount = (int)Math.Pow(2, statistics.GenerationNumber);

        var missing = Math.Max(0, theoreticalCount - statistics.Count);

        html.AppendLine("<div class=\"stats\">");

        html.Append($"<div class=\"label\">Personen:</div>{statistics.Count}");

        if (missing > 0)
            html.Append($" ({missing} fehlen)");

        html.AppendLine("<br>");

        if (statistics.BirthMinYear.HasValue && statistics.BirthMaxYear.HasValue)
            html.AppendLine($"<div class=\"label\">Geburten:</div>{statistics.BirthMinYear} - {statistics.BirthMaxYear} <br>");

        if (statistics.DeathMinYear.HasValue && statistics.DeathMaxYear.HasValue)
            html.AppendLine($"<div class=\"label\">Tode:</div>{statistics.DeathMinYear} - {statistics.DeathMaxYear} <br>");

        if (statistics.BirthAverageYear.HasValue && statistics.DeathAverageYear.HasValue)
            html.AppendLine($"<div class=\"label\">Durchschnitt:</div>{statistics.BirthAverageYear} - {statistics.DeathAverageYear} <br>");

        if (statistics.BirthMedianYear.HasValue && statistics.DeathMedianYear.HasValue)
            html.AppendLine($"<div class=\"label\">Median:</div>{statistics.BirthMedianYear} - {statistics.DeathMedianYear} <br>");

        html.AppendLine("</div>");
    }
    private static void WritePerson(StringBuilder html, Person entry)
    {
        var cssClass = entry.Color.ToString().ToLowerInvariant();

        html.AppendLine($"<div class=\"person {cssClass}\">");

        html.AppendLine($"<span class=\"number\">{entry.KekuleNumber}</span>");

        html.Append(Escape(GetFormattedName(entry.GedcomRecord)));

        if (entry.IsDuplicate)
        {
            html.Append($" <span class=\"duplicate-note\">(siehe Nr. {entry.FirstOccurrence})</span>");
        }
        else
        {
            var dates = GetFormattedDates(entry.GedcomRecord);

            if (!string.IsNullOrWhiteSpace(dates))
                html.Append($" <span class=\"dates\">({Escape(dates)})</span>");
        }

        html.AppendLine();
        html.AppendLine("</div>");
    }

    #endregion

    #region Helpers

    internal static string GetFormattedDates(GedcomIndividualRecord person)
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

    internal static string GetFormattedName(GedcomIndividualRecord person)
    {
        var name = person.GetName();

        if (name == null)
            return "(unbekannt)";

        var surname = name.Surname?.Trim();
        var given = name.Given?.Trim();

        if (!string.IsNullOrWhiteSpace(surname) &&
            !string.IsNullOrWhiteSpace(given))
        {
            return $"{surname}, {given}";
        }

        var raw = name.Name ?? string.Empty;

        return raw.Replace("/", string.Empty).Trim();
    }

    private static string Escape(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    #endregion
}