using System.Windows;
using MaterialDesignThemes.Wpf;

namespace TournamentManager;

/// <summary>
/// Code-behind hanya berisi logika murni tampilan (kontrol jendela). Seluruh logika aplikasi ada di ViewModel.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += OnWindowStateChanged;
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        var maximized = WindowState == WindowState.Maximized;

        // Jendela tanpa border melebihi layar ~7px saat maximize; kompensasi dengan margin
        RootGrid.Margin = maximized ? new Thickness(7) : new Thickness(0);
        MaximizeRestoreIcon.Kind = maximized ? PackIconKind.WindowRestore : PackIconKind.WindowMaximize;
    }
}
