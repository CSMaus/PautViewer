using System.Configuration;
using System.Data;
using System.Windows;
using ControlzEx.Theming;

namespace PAUTViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // base themes: "Light", "Dark"
            // color schemes: "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
            // "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber",
            // "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
            // Set the application theme like this: Dark.Green
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
        }
    }

}
