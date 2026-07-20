using Avalonia.Controls;

namespace EyesGuard.App.Views;

public partial class BreakWindow : Window
{
    private bool _allowClose;

    public BreakWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_allowClose || DataContext is not ViewModels.MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.BreakControl.StrictModeEnabled && (viewModel.BreakControl.IsBreakPrompt || viewModel.BreakControl.IsResting))
        {
            args.Cancel = true;
        }
    }
}
