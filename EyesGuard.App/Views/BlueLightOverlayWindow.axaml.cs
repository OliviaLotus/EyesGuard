using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EyesGuard.App.Views;

public partial class BlueLightOverlayWindow : Window
{
    public BlueLightOverlayWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
