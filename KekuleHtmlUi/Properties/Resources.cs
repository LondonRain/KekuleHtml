using System.Globalization;
using System.Resources;

namespace KekuleHtmlUi.Properties;

/// <summary>
/// Thin wrapper around the compiled .resx resources.
/// Static properties/methods here can be used directly from XAML via {x:Static local:Resources.SomeProperty}.
/// </summary>
public static class Resources
{
    #region Variables

    private static readonly ResourceManager _ResourceManager = new(typeof(Resources).FullName!, typeof(Resources).Assembly);

    #endregion

    #region Methods

    private static string Get(string key) => _ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    private static string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    #endregion

    #region Strings

    public static string BrowseButton => Get(nameof(BrowseButton));
    public static string DropOverlayText => Get(nameof(DropOverlayText));
    public static string GedcomFileHeader => Get(nameof(GedcomFileHeader));
    public static string GedcomFileHint => Get(nameof(GedcomFileHint));
    public static string OkButton => Get(nameof(OkButton));
    public static string OpenFileCheckbox => Get(nameof(OpenFileCheckbox));
    public static string OpenFileDialogFilter => Get(nameof(OpenFileDialogFilter));
    public static string OpenFileDialogTitle => Get(nameof(OpenFileDialogTitle));
    public static string Output => Get(nameof(Output));
    public static string PersonHeader => Get(nameof(PersonHeader));
    public static string PersonHint => Get(nameof(PersonHint));
    public static string StatusFileNotFound(string filePath) => Format(nameof(StatusFileNotFound), filePath);
    public static string StatusGenerateError => Get(nameof(StatusGenerateError));
    public static string StatusGeneratingHtml => Get(nameof(StatusGeneratingHtml));
    public static string StatusHtmlGenerated(string outputPath) => Format(nameof(StatusHtmlGenerated), outputPath);
    public static string StatusInvalidExtension => Get(nameof(StatusInvalidExtension));
    public static string StatusPersonsFound(int count) => Format(nameof(StatusPersonsFound), count);
    public static string StatusReadError => Get(nameof(StatusReadError));
    public static string StatusReadingFile => Get(nameof(StatusReadingFile));

    #endregion
}
