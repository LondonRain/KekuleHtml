using System.Diagnostics;
using System.Text;

namespace KekuleHtml.Models
{
    /// <summary>
    /// A cluster of several <see cref="MigrationPoints"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class MigrationCluster
    {
        public required IReadOnlyList<MigrationPoint> MigrationPoints { get; init; }

        public required double Latitude { get; init; }

        public required double Longitude { get; init; }

        public required MaryHillColour MaryHillColour { get; init; }

        public required int Count { get; init; }

        public required int MinYear { get; init; }

        public required int MaxYear { get; init; }

        public required string PlaceName { get; init; }

        /// <summary>
        /// Detailed HTML about events in this cluster to display in tooltip.
        /// </summary>
        public string DescriptionHtml
        {
            get
            {
                var sbOuter = new StringBuilder();

                if (MigrationPoints.Any())
                {
                    sbOuter.Append("<ul>");

                    foreach (var pointsOfPerson in MigrationPoints.GroupBy(p => p.Person))
                    {
                        var sbInner = new StringBuilder();

                        // --- Name

                        sbInner.Append($"{pointsOfPerson.Key.FormattedName}: ");

                        // --- Birth

                        // don't know whether multiple birth entries would be allowed. but better ignore others than simply crash.
                        var birth = pointsOfPerson.FirstOrDefault(p => p.PointOrigin == PointOrigin.Birth);

                        if (birth != null)
                            sbInner.Append($"* {birth.YearFrom}, ");

                        // --- Marriage

                        var marriages = pointsOfPerson.Where(p => p.PointOrigin == PointOrigin.Marriage).ToList();

                        if (marriages.Count > 0)
                        {
                            sbInner.Append("⚭ ");

                            foreach (var marriage in marriages)
                                sbInner.Append($"{marriage.YearFrom}, ");
                        }

                        // --- Residence

                        var residences = pointsOfPerson.Where(p => p.PointOrigin == PointOrigin.Residence).ToList();

                        if (residences.Count > 0)
                        {
                            var allYears = residences.Select(r => r.YearFrom).Concat(residences.Select(r => r.YearTo).OfType<int>()).ToList();

                            if (allYears.Count > 0)
                            {
                                int minYear = allYears.Min();
                                int? maxYear = allYears.Max();

                                if (maxYear.HasValue && maxYear != minYear)
                                    sbInner.Append($"⌂ {minYear} - {maxYear}, ");
                                else
                                    sbInner.Append($"⌂ {minYear}, ");
                            }
                        }

                        // --- Death

                        // don't know whether multiple death entries would be allowed. but better ignore others than simply crash.
                        var death = pointsOfPerson.FirstOrDefault(p => p.PointOrigin == PointOrigin.Death);

                        if (death != null)
                            sbInner.Append($"✝ {death.YearFrom}, ");

                        sbOuter.AppendLine($"<li>{sbInner.ToString().TrimEnd().TrimEnd(',')}</li>");
                    }

                    sbOuter.Append("</ul>");
                }

                return sbOuter.ToString();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string DebuggerDisplay => $"{PlaceName}, Count: {Count}, {MaryHillColour}, {MinYear}-{MaxYear}";
    }
}