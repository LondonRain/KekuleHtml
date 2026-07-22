// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml;
using KekuleHtml.Services;
using KekuleHtmlUi.Properties;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace KekuleHtmlUi.Presenters;

/// <summary>
/// Presenter for the main window.
/// Contains the workflow logic (load file, generate HTML) as async methods that are called directly by the view (code-behind).
/// </summary>
public class MainPresenter : BindableBase
{
    #region Variables

    private GedcomAdapter? _GedcomAdapter;

    #endregion

    #region Properties

    private string _GedcomFilePath = string.Empty;
    /// <summary>
    /// File path of the GEDCOM file to path. Setting it will trigger <see cref="LoadGedcomFileAsync"/>.
    /// </summary>
    public string GedcomFilePath
    {
        get => _GedcomFilePath;
        set
        {
            if (SetProperty(ref _GedcomFilePath, value))
            {
                _ = LoadGedcomFileAsync();
                RaisePropertyChanged(nameof(CanGenerateHtml));
            }
        }
    }

    /// <summary>
    /// Persons from the most recently loaded GEDCOM file, bound to the SuggestionComboBox.
    /// </summary>
    public ObservableCollection<PersonPresenter> Persons { get; } = new();

    private PersonPresenter? _SelectedPerson;
    /// <summary>
    /// The currently selected person in the SuggestionComboBox.
    /// </summary>
    public PersonPresenter? SelectedPerson
    {
        get => _SelectedPerson;
        set => SetProperty(ref _SelectedPerson, value, () => RaisePropertyChanged(nameof(CanGenerateHtml)));
    }

    private string? _Text;
    /// <summary>
    /// Current filter text typed by user.
    /// </summary>
    public string? Text
    {
        get => _Text;
        set => SetProperty(ref _Text, value, () => RaisePropertyChanged(nameof(CanGenerateHtml)));
    }

    /// <summary>
    /// Whether HTML can be generated.
    /// </summary>
    public bool CanGenerateHtml => !IsBusy && SelectedPerson is not null && !string.IsNullOrEmpty(GedcomFilePath);

    private int _MaxGenerations = KekuleDefaults.DefaultMaxGenerations;
    /// <summary>
    /// Number of generations to traverse (excluding the proband). Clamped to the supported range.
    /// </summary>
    public int MaxGenerations
    {
        get => _MaxGenerations;
        set => SetProperty(ref _MaxGenerations, Math.Clamp(value, KekuleDefaults.MinGenerations, KekuleDefaults.MaxGenerations));
    }

    private bool _OpenFileAfterGeneration = true;
    /// <summary>
    /// Whether the generated HTML file should be opened in browser after generation.
    /// </summary>
    public bool OpenFileAfterGeneration
    {
        get => _OpenFileAfterGeneration;
        set => SetProperty(ref _OpenFileAfterGeneration, value);
    }

    private bool _IsBusy;
    /// <summary>
    /// Whether an async operation is currently running and UI needs to be disabled.
    /// </summary>
    public bool IsBusy
    {
        get => _IsBusy;
        private set
        {
            if (SetProperty(ref _IsBusy, value))
                RaisePropertyChanged(nameof(CanGenerateHtml));
        }
    }

    private string _StatusText = string.Empty;
    /// <summary>
    /// Current status of things going on.
    /// </summary>
    public string StatusText
    {
        get => _StatusText;
        private set => SetProperty(ref _StatusText, value);
    }

    private bool _HasError;
    /// <summary>
    /// Drives the <see cref="StatusText"/> text color via a DataTrigger in XAML.
    /// </summary>
    public bool HasError
    {
        get => _HasError;
        private set => SetProperty(ref _HasError, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Loads and parses the given GEDCOM file. Triggered when _GedcomFilePath changes.
    /// </summary>
    private async Task LoadGedcomFileAsync()
    {
        // --- Checks

        if (string.IsNullOrWhiteSpace(_GedcomFilePath))
            return;

        if (!File.Exists(_GedcomFilePath))
        {
            SetStatus(Resources.StatusFileNotFound(_GedcomFilePath), isError: true);
            return;
        }

        if (!GedcomAdapter.HasGedcomExtension(_GedcomFilePath))
        {
            SetStatus(Resources.StatusInvalidExtension, isError: true);
            return;
        }

        try
        {
            IsBusy = true;
            SetStatus(Resources.StatusReadingFile);

            var presenters = await Task.Run(() =>
            {
                _GedcomAdapter = new GedcomAdapter(_GedcomFilePath);

                return _GedcomAdapter.IndividualsSorted
                    .Select(person => new PersonPresenter(person))
                    .ToList();
            });

            Persons.Clear();

            foreach (var presenter in presenters)
            {
                Persons.Add(presenter);
            }

            SelectedPerson = null;
            Text = null;

            SetStatus(Resources.StatusPersonsFound(Persons.Count));
        }
        catch (Exception ex)
        {
            SetErrorStatus(ex, Resources.StatusReadError);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Generates the HTML file for the currently <see cref="SelectedPerson"> and optionally opens it in the default application. Called by the OK button in the code-behind.
    /// </summary>
    public async Task GenerateHtmlAsync()
    {
        var person = SelectedPerson;
        if (person is null || string.IsNullOrWhiteSpace(GedcomFilePath) || _GedcomAdapter is null)
            return;

        try
        {
            IsBusy = true;
            SetStatus(Resources.StatusGeneratingHtml);

            var outputPath = await Program.CreateKekuleHtmlAsync(person.Person, MaxGenerations, GedcomFilePath, _GedcomAdapter);

            SetStatus(Resources.StatusHtmlGenerated(outputPath));

            if (OpenFileAfterGeneration && File.Exists(outputPath))
            {
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            SetErrorStatus(ex, Resources.StatusGenerateError);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText = message;
        HasError = isError;
    }

    private void SetErrorStatus(Exception ex, string message)
    {
        StatusText = string.Format(message, KekuleHtml.Helpers.ExceptionHelper.GetMessageText(ex));
        HasError = true;
    }

    #endregion
}
