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
    /// Creates and shows the main window. An optional GEDCOM file path can be passed as the
    /// first command-line argument to load it directly on startup (like the console application).
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Optional single command-line argument: a GEDCOM file to load directly.
        string? gedcomFilePath = e.Args.FirstOrDefault();
        if (!GedcomAdapter.IsValidPath(gedcomFilePath))
            gedcomFilePath = null;

        var window = new MainWindow(gedcomFilePath);
        window.Show();
    }
}
