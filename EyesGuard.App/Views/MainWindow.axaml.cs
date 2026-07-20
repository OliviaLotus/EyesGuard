using System.ComponentModel;
using Avalonia.Controls;
using EyesGuard.App.ViewModels;

namespace EyesGuard.App.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;
    private BreakWindow? _breakWindow;
    private bool _allowClose;
    private bool _hasClosed;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closed += OnClosed;
        Closing += OnClosing;
    }

    private void OnOpened(object? sender, EventArgs args)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _viewModel = viewModel;
        _viewModel.BreakAttentionRequested += OnBreakAttentionRequested;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        _hasClosed = true;
        if (_viewModel is not null)
        {
            _viewModel.BreakAttentionRequested -= OnBreakAttentionRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _breakWindow?.ForceClose();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (!_allowClose)
        {
            args.Cancel = true;
            Hide();
        }
    }

    public void ActivateFromExternalRequest()
    {
        if (_hasClosed)
        {
            return;
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    public void CloseFromTray()
    {
        _allowClose = true;
        Close();
    }

    private void OnBreakAttentionRequested(object? sender, EventArgs args) => ShowBreakWindow();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (_viewModel is null || args.PropertyName is not (nameof(BreakControlViewModel.IsBreakPrompt) or nameof(BreakControlViewModel.IsResting)))
        {
            return;
        }

        if (_viewModel.BreakControl.IsBreakPrompt || _viewModel.BreakControl.IsResting)
        {
            ShowBreakWindow();
        }
        else
        {
            _breakWindow?.Close();
        }
    }

    private void ShowBreakWindow()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (_breakWindow is not null)
        {
            _breakWindow.Activate();
            return;
        }

        _breakWindow = new BreakWindow { DataContext = _viewModel };
        _breakWindow.Closed += (_, _) => _breakWindow = null;
        _breakWindow.Show(this);
        _breakWindow.Activate();
    }
}
