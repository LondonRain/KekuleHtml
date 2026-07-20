// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Services;
using KekuleHtmlUi.Presenters;
using Microsoft.Win32;
using System.Windows;

namespace KekuleHtmlUi.Controls;

public partial class MainWindow : Window
{
    #region Variables

    private readonly MainPresenter _Presenter;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates the main window. An optional <paramref name="gedcomFilePath"/> (e.g. from the command line) is loaded directly.
    /// </summary>
    public MainWindow(string? gedcomFilePath = null)
    {
        InitializeComponent();

        // Setting up DataContext.
        _Presenter = new MainPresenter();
        DataContext = _Presenter;

        // Optionally load a GEDCOM file passed on the command line.
        if (!string.IsNullOrWhiteSpace(gedcomFilePath))
            _Presenter.GedcomFilePath = gedcomFilePath;
    }

    #endregion

    #region Buttons

    /// <summary>
    /// GEDCOM File selection via dialog.
    /// </summary>
    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Properties.Resources.OpenFileDialogTitle,
            Filter = Properties.Resources.OpenFileDialogFilter
        };

        if (dialog.ShowDialog(this) == true)
        {
            // Setting the file path will cause the presenter to parse it.
            _Presenter.GedcomFilePath = dialog.FileName;
        }
    }

    /// <summary>
    /// Generating HTML file.
    /// </summary>
    private async void OkButton_Click(object sender, RoutedEventArgs e)
    {
        await _Presenter.GenerateHtmlAsync();
    }

    #endregion

    #region Drag & drop

    /// <summary>
    /// Enable overlay.
    /// </summary>
    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (!_Presenter.IsBusy && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    /// Disable overlay.
    /// </summary>
    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Handle dropped GEDCOM file.
    /// </summary>
    private async void Window_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var gedcomFile = files.FirstOrDefault(GedcomAdapter.HasGedcomExtension);

            var fileToLoad = gedcomFile ?? files.FirstOrDefault();
            if (fileToLoad is not null)
            {
                _Presenter.GedcomFilePath = fileToLoad;
            }
        }
    }

    #endregion
}
