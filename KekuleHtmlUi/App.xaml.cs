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
}
