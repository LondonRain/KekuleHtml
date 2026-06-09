using GeneGenie.Gedcom;

namespace KekuleHtml.Models
{
    internal static class GedcomIndividualRecordExtensions
    {
        internal static string GetFormattedName(this GedcomIndividualRecord person)
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

        internal static string GetFormattedDates(this GedcomIndividualRecord person)
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
    }
}
