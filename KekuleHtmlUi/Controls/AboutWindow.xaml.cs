// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace KekuleHtmlUi.Controls;

/// <summary>
/// Small info window showing copyright, the application version and links to the project on GitHub (license, documentation, third-party notices, releases).
/// </summary>
public partial class AboutWindow : Window
{
    #region Constants

    /// <summary>Base URL of the project on GitHub; every link below is derived from it.</summary>
    private const string RepoBaseUrl = "https://github.com/LondonRain/KekuleHtml";

    private const string LicenseUrl = RepoBaseUrl + "/blob/main/licence.txt";
    private const string ThirdPartyUrl = RepoBaseUrl + "/blob/main/KekuleHtml/Externals/THIRD-PARTY-NOTICES.md";
    private const string ReleasesUrl = RepoBaseUrl + "/releases";
    private const string DocumentationUrlDe = RepoBaseUrl + "/blob/main/README.md";
    private const string DocumentationUrlEn = RepoBaseUrl + "/blob/main/README.en.md";

    #endregion

    #region Constructor

    public AboutWindow()
    {
        InitializeComponent();

        // Version comes from the single, central value in Directory.Build.props.
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
        VersionText.Text = Properties.Resources.AboutVersionLabel(version);

        LicenseLink.NavigateUri = new Uri(LicenseUrl);
        ThirdPartyLink.NavigateUri = new Uri(ThirdPartyUrl);
        ReleasesLink.NavigateUri = new Uri(ReleasesUrl);

        // German documentation only when the UI actually runs in German, English otherwise.
        var isGerman = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase);
        DocumentationLink.NavigateUri = new Uri(isGerman ? DocumentationUrlDe : DocumentationUrlEn);
    }

    #endregion

    #region Events

    /// <summary>
    /// Opens the clicked link in the user's default browser.
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    #endregion
}
