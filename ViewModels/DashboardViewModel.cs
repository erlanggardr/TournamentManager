using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using TournamentManager.Data;
using TournamentManager.Models;

namespace TournamentManager.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly DispatcherTimer _timer;

    public DashboardViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;

        _ = LoadAsync();

        // "Real-time": polling setiap 5 detik agar skor & durasi pertandingan selalu terbarui
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += async (_, _) => await LoadAsync();
        _timer.Start();
    }

    [ObservableProperty]
    private int _totalTeams;

    [ObservableProperty]
    private int _ongoingMatches;

    [ObservableProperty]
    private int _completedMatches;

    [ObservableProperty]
    private string _lastUpdated = "-";

    public ObservableCollection<Match> LiveMatches { get; } = new();

    private async Task LoadAsync()
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();

            TotalTeams = await db.Teams.CountAsync();
            OngoingMatches = await db.Matches.CountAsync(m => m.Status == MatchStatus.InProgress);
            CompletedMatches = await db.Matches.CountAsync(m => m.Status == MatchStatus.Completed);

            var live = await db.Matches
                .AsNoTracking()
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Where(m => m.Status == MatchStatus.InProgress)
                .OrderBy(m => m.StartedAt)
                .ToListAsync();

            LiveMatches.Clear();
            foreach (var match in live)
                LiveMatches.Add(match);

            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            // Dashboard tidak boleh crash karena kegagalan refresh; tampilkan di Output saat debugging
            System.Diagnostics.Debug.WriteLine($"Dashboard refresh gagal: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        GC.SuppressFinalize(this);
    }
}
