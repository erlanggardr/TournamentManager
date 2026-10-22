using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentManager.Data;
using TournamentManager.Services;

namespace TournamentManager.ViewModels;

/// <summary>Item menu sidebar. ViewModelType di-resolve lewat DI saat menu dipilih.</summary>
public record NavigationItem(string Title, PackIconKind Icon, Type ViewModelType);

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackupService _backupService;
    private readonly IDialogService _dialogService;

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        ISnackbarMessageQueue snackbarQueue,
        BackupService backupService,
        IDialogService dialogService)
    {
        _serviceProvider = serviceProvider;
        SnackbarQueue = snackbarQueue;
        _backupService = backupService;
        _dialogService = dialogService;

        MenuItems = new ObservableCollection<NavigationItem>
        {
            new("Dashboard",              PackIconKind.ViewDashboard, typeof(DashboardViewModel)),
            new("Registrasi Tim",         PackIconKind.AccountGroup,  typeof(TeamRegistrationViewModel)),
            new("Roster Pemain",          PackIconKind.AccountMultipleOutline, typeof(PlayerRosterViewModel)),
            new("Manajemen Pertandingan", PackIconKind.GamepadVariantOutline,  typeof(MatchManagementViewModel)),
            new("Klasemen",               PackIconKind.FormatListNumbered, typeof(StandingsViewModel)),
            new("Bracket & Otomatisasi",  PackIconKind.Tournament,    typeof(BracketViewModel)),
        };

        SelectedMenuItem = MenuItems[0]; // memicu navigasi awal ke Dashboard
    }

    public string AppTitle => "Tournament Manager";

    public ISnackbarMessageQueue SnackbarQueue { get; }

    public ObservableCollection<NavigationItem> MenuItems { get; }

    [ObservableProperty]
    private NavigationItem? _selectedMenuItem;

    /// <summary>ViewModel halaman aktif; View dipilih otomatis lewat DataTemplate di MainWindow.xaml.</summary>
    [ObservableProperty]
    private object? _currentViewModel;

    partial void OnSelectedMenuItemChanged(NavigationItem? value)
    {
        if (value is null) return;

        // Bersihkan halaman lama (mis. menghentikan timer Dashboard) sebelum berpindah
        (CurrentViewModel as IDisposable)?.Dispose();
        CurrentViewModel = _serviceProvider.GetRequiredService(value.ViewModelType);
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            var path = await _backupService.BackupAsync();
            if (!string.IsNullOrEmpty(path))
            {
                SnackbarQueue.Enqueue($"Backup database berhasil: {System.IO.Path.GetFileName(path)}");
            }
        }
        catch (Exception ex)
        {
            SnackbarQueue.Enqueue($"Gagal melakukan backup database: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var confirmed = await _dialogService.ConfirmAsync(
            "Restore Database",
            "PERINGATAN: Memulihkan database akan menimpa seluruh data saat ini dengan file backup yang dipilih. Lanjutkan?");
        if (!confirmed) return;

        try
        {
            await _backupService.RestoreAsync();
            SnackbarQueue.Enqueue("Database berhasil dipulihkan! Silakan restart aplikasi atau muat ulang halaman.");
            // Refresh current viewmodel
            if (SelectedMenuItem != null)
            {
                (CurrentViewModel as IDisposable)?.Dispose();
                CurrentViewModel = _serviceProvider.GetRequiredService(SelectedMenuItem.ViewModelType);
            }
        }
        catch (Exception ex)
        {
            SnackbarQueue.Enqueue($"Gagal melakukan restore: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetSampleDataAsync()
    {
        var confirmed = await _dialogService.ConfirmAsync(
            "Reset ke Data Contoh",
            "PERINGATAN: Seluruh data saat ini akan diganti dengan data contoh turnamen VCT resmi (8 tim disetujui, tim pending/rejected, roster 7 pemain per tim, pertandingan live & selesai, dan klasemen). Lanjutkan?");
        if (!confirmed) return;

        try
        {
            await using var db = await _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
            DbInitializer.ReSeed(db);

            SnackbarQueue.Enqueue("Database berhasil di-reset ke data contoh VCT!");

            // Refresh halaman aktif saat ini
            if (SelectedMenuItem != null)
            {
                (CurrentViewModel as IDisposable)?.Dispose();
                CurrentViewModel = _serviceProvider.GetRequiredService(SelectedMenuItem.ViewModelType);
            }
        }
        catch (Exception ex)
        {
            SnackbarQueue.Enqueue($"Gagal melakukan reset data contoh: {ex.Message}");
        }
    }
}
