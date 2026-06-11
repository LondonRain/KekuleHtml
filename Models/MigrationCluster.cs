namespace KekuleHtml.Models
{
    public sealed class MigrationCluster
    {
        public required double Latitude { get; init; }

        public required double Longitude { get; init; }

        public required MaryHillColour MaryHillColour { get; init; }

        public required int Count { get; init; }

        public required int MinYear { get; init; }

        public required int MaxYear { get; init; }

        public required string PlaceName { get; init; }
    }
}