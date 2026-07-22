// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Services;
using KekuleHtmlUi.Controls;
using System.Windows;

namespace KekuleHtmlUi;

public partial class App : Application
{
#if FORCE_ENGLISH
    public App()
    {
        // Forces the English resources for testing (see FORCE_ENGLISH / Directory.Build.props).
        var english = new System.Globalization.CultureInfo("en");
        System.Globalization.CultureInfo.CurrentCulture = english;
        System.Globalization.CultureInfo.CurrentUICulture = english;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = english;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = english;
    }
#endif

    /// <summary>
    /// Creates and shows the main window. Command-line arguments are parsed with the same
    /// <see cref="CommandLineParser"/> as the console application: an optional GEDCOM file path
    /// (loaded directly on startup) and an optional <c>-maxGenerations</c> value.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = CommandLineParser.Parse(e.Args);

        string? gedcomFilePath = options.GedcomPath;
        if (!GedcomAdapter.IsValidPath(gedcomFilePath))
            gedcomFilePath = null;

        var window = new MainWindow(gedcomFilePath, options.MaxGenerations);
        window.Show();
    }
}
