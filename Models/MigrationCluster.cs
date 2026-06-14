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
        /// Details about events in this cluster to display in tooltip.
        /// </summary>
        public string Description
        {
            get
            {
                var sb = new StringBuilder();

                foreach (var pointsOfPerson in MigrationPoints.GroupBy(p => p.Person))
                {
                    // --- Name

                    sb.Append($"{pointsOfPerson.Key.FormattedName}: ");

                    // --- Birth

                    // don't know whether multiple birth entries would be allowed. but better ignore others than simply crash.
                    var birth = pointsOfPerson.FirstOrDefault(p => p.PointOrigin == PointOrigin.Birth);

                    if (birth != null)
                        sb.Append($"* {birth.YearFrom}, ");

                    // --- Marriage

                    var marriages = pointsOfPerson.Where(p => p.PointOrigin == PointOrigin.Marriage).ToList();

                    if (marriages.Count > 0)
                    {
                        sb.Append("⚭ ");

                        foreach (var marriage in marriages)
                            sb.Append($"{marriage.YearFrom}, ");
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
                                sb.Append($"⌂ {minYear} - {maxYear}, ");
                            else
                                sb.Append($"⌂ {minYear}, ");
                        }
                    }

                    // --- Death

                    // don't know whether multiple death entries would be allowed. but better ignore others than simply crash.
                    var death = pointsOfPerson.FirstOrDefault(p => p.PointOrigin == PointOrigin.Death);

                    if (death != null)
                        sb.Append($"✝ {death.YearFrom}, ");

                    sb.AppendLine();
                }

                return sb.ToString().TrimEnd().TrimEnd(',');
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string DebuggerDisplay => $"{PlaceName}, Count: {Count}, {MaryHillColour}, {MinYear}-{MaxYear}";
    }
}