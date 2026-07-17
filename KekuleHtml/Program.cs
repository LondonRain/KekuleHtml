using GeneGenie.Gedcom;
using KekuleHtml.Models;
using KekuleHtml.Properties;
using KekuleHtml.Services;

namespace KekuleHtml;

public static class Program
{
    private static void Main(string[] args)
    {
        // check params
        string? path = args.FirstOrDefault();
        if (!Path.Exists(path) || Path.GetExtension(path) != ".ged")
        {
            Console.WriteLine(Resources.ConsoleUsageGedcomPathRequired);
            return;
        }

        var adapter = new GedcomAdapter(path);
        var people = adapter.IndividualsSorted;

        for (var i = 0; i < people.Count; i++)
        {
            Console.WriteLine(
                $"{i + 1}: {people[i].GetFormattedNameWithDates()}");
        }

        // getting start person
        Console.WriteLine();
        Console.WriteLine(Resources.ConsolePromptSelectStartPerson);
        int userChoice;
        while (!int.TryParse(Console.ReadLine(), out userChoice) ||
               userChoice < 0 || userChoice > people.Count)
        {
            Console.Write(Resources.ConsolePromptEnterValidNumber);
        }

        // die
        if (userChoice == 0)
            return;

        var rootPerson = people[userChoice - 1];

        string outputPath = CreateKekuleHtmlAsync(rootPerson, path, adapter).Result;

        Console.WriteLine();
        Console.WriteLine(string.Format(Resources.ConsoleReportCreated, rootPerson.GetFormattedNameWithDates(), outputPath));
    }

    public static Task<string> CreateKekuleHtmlAsync(GedcomIndividualRecord rootPerson, string gedcomPath, GedcomAdapter adapter)
    {
        return Task.Run(() =>
        {
            // creating gedcom reader
            var kekuleListBuilder = new KekuleListBuilder(adapter);

            // getting persons
            var persons = kekuleListBuilder.GetPersons(rootPerson);

            // getting family tree
            var familyTree = FamilyTree.CreateFamilyTree(persons);

            // creating migration clusters
            var migrationCollector = new MigrationCollector(adapter);
            var migrationPoints = migrationCollector.GetMigrationPoints(familyTree);
            var migrationClusters = migrationCollector.BuildMigrationClusters(migrationPoints);

            // create HTML report
            string fileName = "kekule.html";
            var outputPath = Path.Combine(Path.GetDirectoryName(gedcomPath)!, fileName);
            HtmlWriter.Write(outputPath, rootPerson, familyTree, migrationClusters);

            return outputPath;
        });
    }
}