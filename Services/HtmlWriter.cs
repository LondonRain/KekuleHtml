// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using GeneGenie.Gedcom;
using KekuleHtml.Models;
using System.Globalization;
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
<html>
<head>
<title>Kekulé-Liste</title>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"
     integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY="
     crossorigin=""/>
 <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"
     integrity="sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo="
     crossorigin=""></script>
<style>
body {
    font-family: Segoe UI, sans-serif;
    margin: 2rem auto;
    max-width: 1200px;
    line-height: 1.4;
}
/* general styling */
h1 {
    margin-bottom: 1rem;
}
h2 {
    margin-top: 2rem;
}

/* table of contents */
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

/* generational information */
.stats {
    margin-bottom: 1rem;
    color: #555;
}
.label {
    display: inline-block;
    width: 100px;
}

/* single person entries */
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

/* make sure that links won't be ugly when printed and visited */
a {
    text-decoration: none;
    color: blue;
}
a:hover {
    text-decoration: underline;
}

/* styling of the timeline svg region */
.timeline {
    margin-bottom: 2rem;
    border: 1px solid #ddd;
    background: white;
}
.timeline text {
    font-size: 11px;
}

/* styling of the map */
#migrationMap
{
    width: 100%;
    height: 700px;
    margin-bottom: 2rem;
    border: 1px solid #ddd;
}
.migrationLegend
{
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
    margin-bottom: 1rem;
    padding: 0.75rem;

    border: 1px solid #ddd;
    border-radius: 6px;

    background: #fafafa;
}
.legendItem
{
    display: flex;
    align-items: center;
    gap: 0.5rem;

    font-size: 0.95rem;
}
.legendColor
{
    width: 16px;
    height: 16px;

    border-radius: 50%;

    border: 1px solid #666;

    display: inline-block;
}
.legendColor.blue
{
    background: #005D8F;
}
.legendColor.green
{
    background: #0A7050;
}
.legendColor.red
{
    background: #BE2323;
}
.legendColor.yellow
{
    background: #F5AF00;
}

/* the collapsible details with text about its persons. make sure that list with persons is condensed. */
details
{
font-size: smaller;
line-height: 0.6;
}
details ul
{
margin-block-start: 0rem;
padding-left: 1rem;
}
</style>
</head>
<body>
""";

    #endregion

    #region Write

    public static void Write(string fileName, GedcomIndividualRecord rootPerson, FamilyTree familyTree, IEnumerable<MigrationCluster> migrationClusters)
    {
        var html = new StringBuilder();

        html.AppendLine(CSS);

        html.AppendLine($"<h1>Kekulé-Liste für {EscapeHtml(rootPerson.GetFormattedName())}</h1>");

        WriteTableOfContents(html, familyTree);

        WriteTimelineSvg(html, familyTree);

        WriteMigrationMap(html, familyTree, migrationClusters);

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

        const int chartWidth = 1200;

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

            int? logicalMaxYear = generation.DeathMaxYear;
            int currentYear = DateTime.Today.Year;
            if (!logicalMaxYear.HasValue)
            {
                if (currentYear - generation.BirthMaxYear <= 110)
                {
                    // when generation has no death year and last birth is within 110 years range, assume they are still living and draw bar up to current year
                    logicalMaxYear = currentYear;
                }
                else
                {
                    // for older generations that miss a death year, use last birth year for it
                    logicalMaxYear = generation.BirthMaxYear;
                }
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

    private static void WriteMigrationMap(StringBuilder html, FamilyTree familyTree, IEnumerable<MigrationCluster> migrationClusters)
    {
        if (!migrationClusters.Any())
            return;

        // --- Legend

        html.AppendLine("<h2>Geographische Verteilung der Ahnenlinien</h2>");

        html.AppendLine($"""
<div class="migrationLegend">

    <div class="legendItem">
        <span class="legendColor blue"></span>
        {EscapeHtml(familyTree.GetPerson(4)?.SurName)}
    </div>

    <div class="legendItem">
        <span class="legendColor green"></span>
        {EscapeHtml(familyTree.GetPerson(5)?.SurName)}
    </div>

    <div class="legendItem">
        <span class="legendColor red"></span>
        {EscapeHtml(familyTree.GetPerson(6)?.SurName)}
    </div>

    <div class="legendItem">
        <span class="legendColor yellow"></span>
        {EscapeHtml(familyTree.GetPerson(7)?.SurName)}
    </div>

</div>
""");

        // --- Map

        html.AppendLine("""
<div id="migrationMap"></div>
""");

        html.AppendLine("<script>");

        html.AppendLine("""
const migrationMap = L.map('migrationMap');

L.tileLayer(
    'https://tile.openstreetmap.de/{z}/{x}/{y}.png',
    {
        maxZoom: 18,
        attribution: '©️ OpenStreetMap',
    })
    .addTo(migrationMap);

migrationMap.on('popupopen', function(e) {
    // having details opened and then closing it makes sure that popup already has the size it needs when details are opened by user.
    const details = e.popup._contentNode.querySelector('.auto-close-details');
    details.removeAttribute('open');
});

const bounds = [];
""");

        // draw the small circles on top
        foreach (var cluster in migrationClusters.OrderByDescending(c => c.Count))
        {
            var colour =
                cluster.MaryHillColour switch
                {
                    MaryHillColour.Blue => "#005D8F",
                    MaryHillColour.Green => "#0A7050",
                    MaryHillColour.Red => "#BE2323",
                    MaryHillColour.Yellow => "#F5AF00",
                    _ => throw new InvalidOperationException($"Unexpected colour {cluster.MaryHillColour}!")
                };

            var opacity = GetOpacity(cluster);
            var opacityText = opacity.ToString("F2", CultureInfo.InvariantCulture);

            // this are meters (as per leaflet API documentation) - but it seem to be pixels in reality.
            // the formula is somehow arbitrary to get a balance between small and big sized circles while retaining visibility of the
            // tiniest ones and not drawing the largest ones too big, still preserving distinguishability between different sizes.
            var radius = 6.0 + 3.0 * Math.Sqrt(cluster.Count);
            var radiusText = radius.ToString("F1", CultureInfo.InvariantCulture);

            var latitude = cluster.Latitude;
            var longitude = cluster.Longitude;

            // slightly offset overlaying points (only when needed)
            const double offset = 0.002;
            if (migrationClusters.Any(c => c != cluster &&
                                           Math.Abs(c.Latitude - cluster.Latitude) <= offset &&
                                           Math.Abs(c.Longitude - cluster.Longitude) <= offset))
            {
                switch (cluster.MaryHillColour)
                {
                    case MaryHillColour.Blue:
                        latitude += offset;
                        break;
                    case MaryHillColour.Green:
                        latitude -= offset;
                        break;
                    case MaryHillColour.Red:
                        longitude += offset;
                        break;
                    case MaryHillColour.Yellow:
                        longitude -= offset;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected colour {cluster.MaryHillColour}!");
                }
            }

            var latitudeText = latitude.ToString(CultureInfo.InvariantCulture);
            var longitudeText = longitude.ToString(CultureInfo.InvariantCulture);

            var popup = string.Concat(
                $"{EscapeJs(cluster.PlaceName)}<br/>",
                $"Ereignisse: {cluster.Count}<br/>",
                $"Zeitraum: {cluster.MinYear} - {cluster.MaxYear}<br/><br/>",
                "<details open=\'true\' class=\'auto-close-details\'>",
                "<summary>Details</summary>",
                // make sure details can also be closed by clicking any of its content
                "<div onclick=\\\"this.closest(\'details\').removeAttribute(\'open\');\\\">",
                $"<p>{ReplaceLineBreaks(cluster.DescriptionHtml)}</p>",
                "</div></details>");

            html.AppendLine($$"""
bounds.push([{{latitudeText}}, {{longitudeText}}]);

L.circleMarker(
    [{{latitudeText}}, {{longitudeText}}],
    {
        radius: {{radiusText}},
        color: "{{colour}}",
        fillColor: "{{colour}}",
        fillOpacity: {{opacityText}},
        weight: 1
    })
    .bindPopup("{{popup}}", {maxWidth: 800})
    .addTo(migrationMap);
""");
        }

        html.AppendLine("""
if (bounds.length > 0)
{
    migrationMap.fitBounds(
        bounds,
        {
            padding: [40, 40]
        });
}
""");

        html.AppendLine("</script>");

        double GetOpacity(MigrationCluster cluster)
        {
            // newest years are the darkest
            const double maxOpacity = 0.8;
            const double minOpacity = 0.1;
            const double scalableOpacity = maxOpacity - minOpacity;

            // calculate colour fade per MarryHill colour
            var minYear = migrationClusters.Where(c => c.MaryHillColour == cluster.MaryHillColour).Min(c => c.MinYear);
            var maxYear = migrationClusters.Where(c => c.MaryHillColour == cluster.MaryHillColour).Max(c => c.MinYear);
            int yearRange = maxYear - minYear;

            if (minYear == maxYear)
            {
                return maxOpacity;
            }
            else
            {
                double yearsSinceMinYear = cluster.MinYear - minYear;
                double opacity = Math.Round(yearsSinceMinYear / yearRange * scalableOpacity + minOpacity, 2);
                return opacity;
            }
        }
    }

    private static void WriteStatistics(StringBuilder html, Generation generation)
    {
        var theoreticalCount = (int)Math.Pow(2, generation.GenerationNumber);

        var missing = Math.Max(0, theoreticalCount - generation.Count);

        html.AppendLine("<div class=\"stats\">");

        html.Append($"<div class=\"label\">Personen:</div>{generation.Count}");

        if (missing > 0)
            html.Append($" ({missing} fehlen)");

        html.AppendLine("<br/>");

        if (generation.BirthMinYear.HasValue && generation.BirthMaxYear.HasValue)
            html.AppendLine($"<div class=\"label\">Geburten:</div>{generation.BirthMinYear} - {generation.BirthMaxYear} <br/>");

        if (generation.DeathMinYear.HasValue && generation.DeathMaxYear.HasValue)
            html.AppendLine($"<div class=\"label\">Tode:</div>{generation.DeathMinYear} - {generation.DeathMaxYear} <br/>");

        if (generation.BirthAverageYear.HasValue && generation.DeathAverageYear.HasValue)
            html.AppendLine($"<div class=\"label\">Durchschnitt:</div>{generation.BirthAverageYear} - {generation.DeathAverageYear} <br/>");

        if (generation.BirthMedianYear.HasValue && generation.DeathMedianYear.HasValue)
            html.AppendLine($"<div class=\"label\">Median:</div>{generation.BirthMedianYear} - {generation.DeathMedianYear} <br/>");

        html.AppendLine("</div>");
    }
    private static void WritePerson(StringBuilder html, Person entry)
    {
        var cssClass = entry.Colour.ToString().ToLowerInvariant();

        html.AppendLine($"<div class=\"person {cssClass}\">");

        html.AppendLine($"<span class=\"number\">{entry.KekuleNumber}</span>");

        html.Append(EscapeHtml(entry.GedcomRecord.GetFormattedName()));

        if (entry.IsDuplicate)
        {
            html.Append($" <span class=\"duplicate-note\">(siehe Nr. {entry.FirstOccurrence})</span>");
        }
        else
        {
            var dates = entry.GedcomRecord.GetFormattedDates();

            if (!string.IsNullOrWhiteSpace(dates))
                html.Append($" <span class=\"dates\">({EscapeHtml(dates)})</span>");
        }

        html.AppendLine();
        html.AppendLine("</div>");
    }

    #endregion

    #region Helpers

    private static string ReplaceLineBreaks(string value) => value.Replace(Environment.NewLine, "<br/>");

    private static string EscapeHtml(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return WebUtility.HtmlEncode(value);
    }

    private static string EscapeJs(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'")
            .Replace("\r", "")
            .Replace("\n", "");
    }

    #endregion
}