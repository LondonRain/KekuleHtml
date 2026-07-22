// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Helpers;
using KekuleHtml.Services;
using KekuleHtmlUi.Controls;
using System.Windows;

namespace KekuleHtmlUi;

public partial class App : Application
{
    /// <summary>
    /// Creates and shows the main window. Command-line arguments are parsed with the same
    /// <see cref="CommandLineParser"/> as the console application: an optional GEDCOM file path
    /// (loaded directly on startup), an optional <c>-maxGenerations</c> value and an optional
    /// <c>-lang</c> language.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = CommandLineParser.Parse(e.Args);

        // apply UI language before creating the window; the FORCE_ENGLISH test build always wins
        // (see FORCE_ENGLISH / Directory.Build.props)
#if FORCE_ENGLISH
        CultureHelper.ForceEnglish();
#else
        CultureHelper.Apply(options.Language);
#endif

        string? gedcomFilePath = options.GedcomPath;
        if (!GedcomAdapter.IsValidPath(gedcomFilePath))
            gedcomFilePath = null;

        var window = new MainWindow(gedcomFilePath, options.MaxGenerations);
        window.Show();
    }
}
