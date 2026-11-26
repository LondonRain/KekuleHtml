// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Services;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KekuleHtml.Tests;

/// <summary>
/// <para>
/// Guards the libraries which the generated HTML file loads from unpkg.com. Their versions are listed in
/// KekuleHtml/Externals/package.json, which Dependabot keeps up to date. Dependabot cannot change the URLs and
/// integrity hashes in <see cref="HtmlWriter"/>, so these tests fail until both have been adjusted by hand.
/// </para>
/// <para>
/// A wrong integrity hash makes the browser reject the file, which breaks the map. Only the test in the category
/// "Online" can detect that, because it downloads the files. Its failure message contains the correct hashes. Skip it
/// when working offline: <c>dotnet test --filter "TestCategory!=Online"</c>
/// </para>
/// </summary>
[TestClass]
public sealed partial class ExternalDependencyTests
{
    private sealed record ExternalReference(string Url, string? Package, string? Version, string? Integrity, bool HasCrossOrigin);

    private static readonly List<ExternalReference> References = ReadReferences();

    public TestContext TestContext { get; set; } = null!;

    #region Offline

    [TestMethod]
    public void Package_json_lists_exactly_the_referenced_libraries()
    {
        // a URL without a version has no package name here and shows up with its full URL.
        var referenced = References.Select(r => r.Package ?? r.Url).Distinct().ToList();

        CollectionAssert.AreEquivalent(ReadManifest().Keys.ToList(), referenced,
            $"Referenced in HtmlWriter: {string.Join(", ", referenced)}");
    }

    [TestMethod]
    public void Every_url_uses_the_version_from_package_json()
    {
        var manifest = ReadManifest();

        var mismatches = References
            .Where(r => r.Package is null || manifest.GetValueOrDefault(r.Package) != r.Version)
            .Select(r => r.Url)
            .ToList();

        Assert.IsEmpty(mismatches,
            $"Adjust URL and integrity hash in HtmlWriter to match package.json:\n{string.Join("\n", mismatches)}");
    }

    [TestMethod]
    public void Every_url_is_protected_by_an_integrity_hash()
    {
        // without the crossorigin attribute the browser cannot check the hash of a file from another host.
        var unprotected = References
            .Where(r => r.Integrity?.StartsWith("sha256-", StringComparison.Ordinal) != true || !r.HasCrossOrigin)
            .Select(r => r.Url)
            .ToList();

        Assert.IsEmpty(unprotected,
            $"Missing integrity=\"sha256-...\" or crossorigin attribute:\n{string.Join("\n", unprotected)}");
    }

    #endregion

    #region Online

    [TestMethod]
    [TestCategory("Online")]
    public async Task Every_integrity_hash_matches_the_published_file()
    {
        using var client = new HttpClient();
        var mismatches = new List<string>();

        foreach (var reference in References)
        {
            var content = await client.GetByteArrayAsync(reference.Url, TestContext.CancellationToken);
            var integrity = $"sha256-{Convert.ToBase64String(SHA256.HashData(content))}";

            if (reference.Integrity != integrity)
                mismatches.Add($"{reference.Url}\n    integrity=\"{integrity}\"");
        }

        Assert.IsEmpty(mismatches, $"Correct hashes:\n{string.Join("\n", mismatches)}");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// All scripts and stylesheets which the HTML file loads from elsewhere.
    /// </summary>
    private static List<ExternalReference> ReadReferences()
    {
        var references = new List<ExternalReference>();

        foreach (Match tag in TagRegex().Matches(HtmlWriter.CSS))
        {
            var url = UrlRegex().Match(tag.Value);
            if (!url.Success)
                continue;

            var unpkg = UnpkgRegex().Match(url.Groups["url"].Value);
            var integrity = IntegrityRegex().Match(tag.Value);

            references.Add(new ExternalReference(
                url.Groups["url"].Value,
                unpkg.Success ? unpkg.Groups["package"].Value : null,
                unpkg.Success ? unpkg.Groups["version"].Value : null,
                integrity.Success ? integrity.Groups["integrity"].Value : null,
                tag.Value.Contains("crossorigin=", StringComparison.Ordinal)));
        }

        return references;
    }

    /// <summary>
    /// Package names and versions from package.json, which the build copies next to the tests.
    /// </summary>
    private static Dictionary<string, string?> ReadManifest()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "package.json"));
        using var document = JsonDocument.Parse(json);

        return document.RootElement.GetProperty("dependencies")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString());
    }

    [GeneratedRegex(@"<(?:link|script)\b[^>]*>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\b(?:href|src)=""(?<url>[^""]+)""")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\bintegrity=""(?<integrity>[^""]+)""")]
    private static partial Regex IntegrityRegex();

    [GeneratedRegex(@"^https://unpkg\.com/(?<package>[^@/]+)@(?<version>[^/]+)/")]
    private static partial Regex UnpkgRegex();

    #endregion
}
