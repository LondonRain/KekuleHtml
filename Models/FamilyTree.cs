namespace KekuleHtml.Models
{
    /// <summary>
    /// Family tree holds all our <see cref="Generation"/>s consisting of <see cref="Person"/>s.
    /// </summary>
    public sealed class FamilyTree
    {
        #region Properties

        /// <summary>
        /// Gemerations starting from G0.
        /// </summary>
        public required IReadOnlyList<Generation> Generations { get; init; }

        public IEnumerable<Person> AllPersons
        {
            get
            {
                foreach (var generation in Generations)
                {
                    foreach (var person in generation.Persons)
                        yield return person;
                }
            }
        }

        public Person? GetPerson(int kekuleNumber)
        {
            return AllPersons.FirstOrDefault(p => p.KekuleNumber == kekuleNumber);
        }

        public int MinYear
        {
            get
            {
                return Generations.Where(s => s.BirthMinYear.HasValue)
                    .Select(s => s.BirthMinYear!.Value)
                    .DefaultIfEmpty()
                    .Min();
            }
        }

        public int MaxYear
        {
            get
            {
                return Generations.Where(s => s.DeathMaxYear.HasValue)
                    .Select(s => s.DeathMaxYear!.Value)
                    .DefaultIfEmpty()
                    .Max();
            }
        }

        #endregion

        #region Construction

        public static FamilyTree CreateFamilyTree(IEnumerable<Person> persons)
        {
            var generations = new List<Generation>();

            // sorting them by generation is critical for a valid FamilyTree structure
            foreach (var group in persons.GroupBy(e => e.Generation).OrderBy(g => g.Key))
                generations.Add(CreateStatistics(group));

            return new FamilyTree { Generations = generations };

        }

        private static Generation CreateStatistics(IGrouping<int, Person> group)
        {
            var birthYears = group.Select(x => x.GedcomRecord.Birth?.Date?.DateTime1?.Year)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .OrderBy(x => x)
                .ToList();
            if (birthYears.Count == (0))
                birthYears = null;

            var deathYears = group.Select(x => x.GedcomRecord.Death?.Date?.DateTime1?.Year)
                   .Where(x => x.HasValue)
                   .Select(x => x!.Value)
                   .OrderBy(x => x)
                   .ToList();
            if (deathYears.Count == (0))
                deathYears = null;

            return new Generation
            {
                GenerationNumber = group.Key,
                Persons = group.ToList(),

                BirthMinYear = birthYears?.Min(),
                BirthMaxYear = birthYears?.Max(),
                BirthAverageYear = birthYears != null ? (int)Math.Round(birthYears.Average()) : null,
                BirthMedianYear = birthYears != null ? CalculateMedian(birthYears) : null,

                DeathMinYear = deathYears?.Min(),
                DeathMaxYear = deathYears?.Max(),
                DeathAverageYear = deathYears != null ? (int)Math.Round(deathYears.Average()) : null,
                DeathMedianYear = deathYears != null ? CalculateMedian(deathYears) : null
            };

            static int CalculateMedian(List<int> years)
            {
                return years.Count % 2 == 1 ?
                    years[years.Count / 2] :
                    (int)Math.Round((years[years.Count / 2 - 1] + years[years.Count / 2]) / 2.0);
            }
        }

        #endregion
    }
}
