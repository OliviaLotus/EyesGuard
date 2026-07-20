using Avalonia.Controls;
using Avalonia.Input;
using EyesGuard.App.ViewModels;

namespace EyesGuard.App.Views;

public partial class DesktopWidgetWindow : Window
{
    private bool _allowClose;

    public DesktopWidgetWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    public void CloseForApplicationExit()
    {
        _allowClose = true;
        Close();
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(args);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        Hide();
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Settings.DesktopWidgetEnabled = false;
        }
    }
}
