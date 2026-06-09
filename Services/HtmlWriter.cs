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
.timeline {
    margin-bottom: 2rem;
    border: 1px solid #ddd;
    background: white;
}

.timeline text {
    font-size: 11px;
}
</style>
</head>
<body>
""";

    #endregion

    #region Write

    public static void Write(string fileName, GedcomIndividualRecord rootPerson, FamilyTree familyTree)
    {
        var html = new StringBuilder();

        html.AppendLine(CSS);

        html.AppendLine($"<h1>Kekulé-Liste für {Escape(rootPerson.GetFormattedName())}</h1>");

        WriteTableOfContents(html, familyTree);

        WriteTimelineSvg(html, familyTree);

        foreach (var generation in familyTree.Generations)
        {
            html.AppendLine($"<section id=\"gen{generation.GenerationNumber}\">");

            html.AppendLine($"<h2>{generation.ExternalName}: {generation.Description}</h2>");

            WriteStatistics(html, generation);

            foreach (var person in generation.Persons)
                WritePerson(html, person);

            html.AppendLine("</section>");
        }

        html.AppendLine("""
</body>
</html>
""");

        File.WriteAllText(fileName, html.ToString(), Encoding.UTF8);
    }

    private static void WriteTableOfContents(StringBuilder html, FamilyTree familyTree)
    {
        html.AppendLine("<nav>");
        html.AppendLine("<h2>Inhalt</h2>");
        html.AppendLine("<ul>");

        foreach (var generation in familyTree.Generations)
            html.AppendLine($"<li><a href=\"#gen{generation.GenerationNumber}\">{generation.ExternalName}</a></li>");

        html.AppendLine("</ul>");
        html.AppendLine("</nav>");
    }

    private static void WriteTimelineSvg(StringBuilder html, FamilyTree familyTree)
    {
        var generations = familyTree.Generations;

        if (generations.Count == 0)
            return;

        if (familyTree.MinYear == 0 || familyTree.MaxYear == 0)
            return;

        var maxGeneration = familyTree.Generations.Last().GenerationNumber;

        const int leftMargin = 120;
        const int rightMargin = 20;
        const int topMargin = 20;
        const int rowHeight = 26;

        const int chartWidth = 1000;

        var chartHeight = topMargin + ((maxGeneration + 1) * rowHeight) + 40;

        double Scale(int year)
        {
            return leftMargin + (year - familyTree.MinYear) * (chartWidth - leftMargin - rightMargin) / (double)(familyTree.MaxYear - familyTree.MinYear);
        }

        html.AppendLine("<h2>Zeitliche Einordnung der Generationen</h2>");

        html.AppendLine($"<svg class=\"timeline\" width=\"{chartWidth}\" height=\"{chartHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // --- background lines (every 50 years)

        for (var year = familyTree.MinYear; year <= familyTree.MaxYear; year += 50)
        {
            var x = Scale(year);

            html.AppendLine($"""
            <line
                x1="{x:F0}"
                y1="0"
                x2="{x:F0}"
                y2="{chartHeight - 25}"
                stroke="#e5e7eb"
                stroke-width="1"/>
            """);

            html.AppendLine($"""
            <text
                x="{x:F0}"
                y="{chartHeight - 5}"
                font-size="11"
                text-anchor="middle">
                {year}
            </text>
            """);
        }

        // --- generations

        foreach (var generation in generations.OrderByDescending(x => x.GenerationNumber))
        {
            var row = maxGeneration - generation.GenerationNumber;

            var y = topMargin + row * rowHeight;

            html.AppendLine($"""
            <text
                x="5"
                y="{y + 10}"
                font-size="12">
                {generation.ShortName}: {generation.Description}
            </text>
            """);

            // outer bar: whole generation

            // when generation has no death year and last birth is within 110 years range, assume they are still living and draw bar up to current year
            int? logicalMaxYear = generation.DeathMaxYear;
            int currentYear = DateTime.Today.Year;
            if (!logicalMaxYear.HasValue &&
                currentYear - generation.BirthMaxYear <= 110)
            {
                logicalMaxYear = currentYear;
            }

            if (generation.BirthMinYear.HasValue && logicalMaxYear.HasValue)
            {
                var x1 = Scale(generation.BirthMinYear.Value);

                var x2 = Scale(logicalMaxYear.Value);

                html.AppendLine($"<a href=\"#gen{generation.GenerationNumber}\">");

                html.AppendLine($"""
                <rect
                    x="{x1:F0}"
                    y="{y}"
                    width="{Math.Max(1, x2 - x1):F0}"
                    height="12"
                    fill="#d1d5db">
                    <title>{generation.Description}&#10;Zeitraum: {generation.BirthMinYear}-{generation.DeathMaxYear}</title>
                </rect>
                """);
            }

            // inner bar: median
            if (generation.BirthMedianYear.HasValue && generation.DeathMedianYear.HasValue)
            {
                var medianX1 = Scale(generation.BirthMedianYear.Value);

                var medianX2 = Scale(generation.DeathMedianYear.Value);

                html.AppendLine($"""
                <rect
                    x="{medianX1:F0}"
                    y="{y}"
                    width="{Math.Max(1, medianX2 - medianX1):F0}"
                    height="12"
                    fill="#2563eb">
                    <title>{generation.Description}&#10;Median-Lebensspanne&#10;{generation.BirthMedianYear}-{generation.DeathMedianYear}</title>
                </rect>
                """);
            }

            html.AppendLine("</a>");
        }

        html.AppendLine("</svg>");
    }

    private static void WriteStatistics(StringBuilder html, Generation generation)
    {
        var theoreticalCount = (int)Math.Pow(2, generation.GenerationNumber);

        var missing = Math.Max(0, theoreticalCount - generation.Count);

        html.AppendLine("<div class=\"stats\">");

        html.Append($"<div class=\"label\">Personen:</div>{generation.Count}");

        if (missing > 0)
            html.Append($" ({missing} fehlen)");

        html.AppendLine("<br>");

        if (generation.BirthMinYear.HasValue && generation.BirthMaxYear.HasValue)
            html.AppendLine($"<div class=\"label\">Geburten:</div>{generation.BirthMinYear} - {generation.BirthMaxYear} <br>");

        if (generation.DeathMinYear.HasValue && generation.DeathMaxYear.HasValue)
            html.AppendLine($"<div class=\"label\">Tode:</div>{generation.DeathMinYear} - {generation.DeathMaxYear} <br>");

        if (generation.BirthAverageYear.HasValue && generation.DeathAverageYear.HasValue)
            html.AppendLine($"<div class=\"label\">Durchschnitt:</div>{generation.BirthAverageYear} - {generation.DeathAverageYear} <br>");

        if (generation.BirthMedianYear.HasValue && generation.DeathMedianYear.HasValue)
            html.AppendLine($"<div class=\"label\">Median:</div>{generation.BirthMedianYear} - {generation.DeathMedianYear} <br>");

        html.AppendLine("</div>");
    }
    private static void WritePerson(StringBuilder html, Person entry)
    {
        var cssClass = entry.Color.ToString().ToLowerInvariant();

        html.AppendLine($"<div class=\"person {cssClass}\">");

        html.AppendLine($"<span class=\"number\">{entry.KekuleNumber}</span>");

        html.Append(Escape(entry.GedcomRecord.GetFormattedName()));

        if (entry.IsDuplicate)
        {
            html.Append($" <span class=\"duplicate-note\">(siehe Nr. {entry.FirstOccurrence})</span>");
        }
        else
        {
            var dates = entry.GedcomRecord.GetFormattedDates();

            if (!string.IsNullOrWhiteSpace(dates))
                html.Append($" <span class=\"dates\">({Escape(dates)})</span>");
        }

        html.AppendLine();
        html.AppendLine("</div>");
    }

    #endregion

    #region Helpers

    private static string Escape(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    #endregion
}