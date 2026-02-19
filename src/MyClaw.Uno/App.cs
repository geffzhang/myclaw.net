using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace MyClaw.Uno;

/// <summary>
/// MyClaw Uno Platform Application
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        // Initialize application resources
        this.Resources = new ResourceDictionary
        {
            // Default colors
            ["PrimaryBrush"] = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
            ["BackgroundBrush"] = new SolidColorBrush(Microsoft.UI.Colors.WhiteSmoke),
            ["UserMessageBrush"] = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 227, 242, 253)),
            ["AssistantMessageBrush"] = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    /// <summary>
    /// Gets the main window
    /// </summary>
    public Window? MainWindow => _window;
}
