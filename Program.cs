using KekuleHtml.Services;

var adapter = new GedcomAdapter(args[0]);

var people =
    adapter.Individuals
        .Where(i => i.Names.Any())
        .OrderBy(i => i.GetName().Surname)
        .ThenBy(i => i.GetName().Name)
        .ToList();

for (var i = 0; i < people.Count; i++)
{
    Console.WriteLine(
        $"{i + 1}: {HtmlWriter.GetFormattedName(people[i])} ({HtmlWriter.GetFormattedDates(people[i])})");
}

Console.Write("Person auswählen: ");

var selected = people[int.Parse(Console.ReadLine()!) - 1];

var builder = new KekuleListBuilder(adapter);

var entries = builder.Build(selected);

var statistics = StatisticsCalculator.Calculate(entries);

HtmlWriter.Write(
        "kekule.html",
        selected,
        entries,
        statistics);

Console.WriteLine("kekule.html erzeugt.");