using KekuleHtml.Models;
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
        $"{i + 1}: {people[i].GetFormattedName()} ({people[i].GetFormattedDates()})");
}

// getting start person
Console.Write("Person auswählen: ");
var rootPerson = people[int.Parse(Console.ReadLine()!) - 1];

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
HtmlWriter.Write("kekule.html", rootPerson, familyTree, migrationClusters);
Console.WriteLine("kekule.html erzeugt.");