// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
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

        public Person? GetPerson(ulong kekuleNumber)
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

        private static Generation CreateStatistics(IGrouping<int, Person> generationGroup)
        {
            List<int>? birthYears = new();
            List<int>? deathYears = new();

            int currentYear = DateTime.Today.Year;
            bool mightStillBeLiving = false;

            foreach (var person in generationGroup)
            {
                if (person.GedcomRecord.Birth != null &&
                    person.GedcomRecord.Birth.Date.TryGetYear1(out var birthYear))
                {
                    int birthYearValue = birthYear!.Value;

                    birthYears.Add(birthYearValue);

                    if (!mightStillBeLiving && currentYear - birthYearValue <= 110)
                    {
                        // person was born less than 110 years ago, assume it might be still alive
                        mightStillBeLiving = true;
                    }
                }

                if (person.GedcomRecord.Death != null &&
                    person.GedcomRecord.Death.Date.TryGetYear1(out var deathYear))
                {
                    deathYears.Add(deathYear!.Value);
                }
            }

            if (mightStillBeLiving)
            {
                // ...but everybody died alreaqdy
                if (generationGroup.Count() == deathYears.Count)
                    mightStillBeLiving = false;
            }

            // sort and handle empty lists
            birthYears = birthYears.Count > 0 ? birthYears.Order().ToList() : null;
            deathYears = deathYears.Count > 0 ? deathYears.Order().ToList() : null;

            return new Generation
            {
                GenerationNumber = generationGroup.Key,
                Persons = generationGroup.ToList(),

                BirthMinYear = birthYears?.Min(),
                BirthMaxYear = birthYears?.Max(),
                BirthAverageYear = birthYears != null ? (int)Math.Round(birthYears.Average()) : null,
                BirthMedianYear = birthYears != null ? CalculateMedian(birthYears) : null,

                DeathMinYear = deathYears?.Min(),
                DeathMaxYear = deathYears?.Max(),
                DeathAverageYear = deathYears != null ? (int)Math.Round(deathYears.Average()) : null,
                DeathMedianYear = deathYears != null ? CalculateMedian(deathYears) : null,

                MightStillBeLiving = mightStillBeLiving
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
