using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using TournamentManager.Data;
using TournamentManager.Models;
using TournamentManager.Services;

namespace TournamentManager.ViewModels;

/// <summary>
/// ViewModel halaman Klasemen. Menghitung poin, selisih gol, dan ranking
/// secara otomatis dari pertandingan yang sudah selesai.
/// </summary>
public partial class StandingsViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ExportService _export;
    private readonly ISnackbarMessageQueue _snackbar;

    public StandingsViewModel(
        IDbContextFactory<AppDbContext> factory,
        ExportService export,
        ISnackbarMessageQueue snackbar)
    {
        _factory = factory;
        _export = export;
        _snackbar = snackbar;

        _ = LoadAsync();
    }

    public ObservableCollection<StandingRow> Standings { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _roundFilter = string.Empty;

    public ObservableCollection<string> AvailableRounds { get; } = new();

    [ObservableProperty]
    private string _selectedRound = "Semua Babak";

    partial void OnSelectedRoundChanged(string value) => _ = LoadAsync();

    // ── Commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!Standings.Any())
        {
            _snackbar.Enqueue("Belum ada data klasemen untuk diekspor.");
            return;
        }
        try
        {
            await _export.ExportStandingsToExcelAsync(Standings);
            _snackbar.Enqueue("Klasemen berhasil diekspor ke Excel.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal ekspor Excel: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (!Standings.Any())
        {
            _snackbar.Enqueue("Belum ada data klasemen untuk diekspor.");
            return;
        }
        try
        {
            await _export.ExportStandingsToPdfAsync(Standings);
            _snackbar.Enqueue("Klasemen berhasil diekspor ke PDF.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal ekspor PDF: {ex.Message}");
        }
    }

    // ── Logika Klasemen ────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = await _factory.CreateDbContextAsync();

            // Ambil semua pertandingan selesai beserta informasi tim
            var completedMatches = await db.Matches
                .AsNoTracking()
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Where(m => m.Status == MatchStatus.Completed)
                .ToListAsync();

            // Ambil semua tim yang disetujui agar tim yang belum tanding tetap muncul di klasemen
            var approvedTeams = await db.Teams
                .AsNoTracking()
                .Where(t => t.Status == RegistrationStatus.Approved)
                .ToListAsync();

            // Isi pilihan babak
            var rounds = completedMatches.Select(m => m.Round).Distinct().OrderBy(r => r).ToList();
            AvailableRounds.Clear();
            AvailableRounds.Add("Semua Babak");
            foreach (var r in rounds) AvailableRounds.Add(r);

            // Filter berdasarkan babak yang dipilih
            var filtered = SelectedRound == "Semua Babak"
                ? completedMatches
                : completedMatches.Where(m => m.Round == SelectedRound).ToList();

            // Inisialisasi statistik untuk seluruh tim yang disetujui (Format Valorant / VLR.gg)
            // tuple: (Name, MatchesPlayed, MatchesWon, MatchesLost, MapsWon, MapsLost)
            var stats = new Dictionary<int, (string Name, int MP, int MW, int ML, int MapsW, int MapsL)>();
            foreach (var team in approvedTeams)
            {
                stats[team.Id] = (team.Name, 0, 0, 0, 0, 0);
            }

            foreach (var m in filtered)
            {
                EnsureTeam(stats, m.TeamAId, m.TeamAName);
                EnsureTeam(stats, m.TeamBId, m.TeamBName);

                var (na, mpa, mwa, mla, mwa_maps, mla_maps) = stats[m.TeamAId];
                var (nb, mpb, mwb, mlb, mwb_maps, mlb_maps) = stats[m.TeamBId];

                if (m.ScoreA > m.ScoreB)
                {
                    // Tim A menang match
                    stats[m.TeamAId] = (na, mpa + 1, mwa + 1, mla, mwa_maps + m.ScoreA, mla_maps + m.ScoreB);
                    stats[m.TeamBId] = (nb, mpb + 1, mwb, mlb + 1, mwb_maps + m.ScoreB, mlb_maps + m.ScoreA);
                }
                else if (m.ScoreB > m.ScoreA)
                {
                    // Tim B menang match
                    stats[m.TeamAId] = (na, mpa + 1, mwa, mla + 1, mwa_maps + m.ScoreA, mla_maps + m.ScoreB);
                    stats[m.TeamBId] = (nb, mpb + 1, mwb + 1, mlb, mwb_maps + m.ScoreB, mlb_maps + m.ScoreA);
                }
                else
                {
                    // Seri (jarang terjadi di Valorant)
                    stats[m.TeamAId] = (na, mpa + 1, mwa, mla, mwa_maps + m.ScoreA, mla_maps + m.ScoreB);
                    stats[m.TeamBId] = (nb, mpb + 1, mwb, mlb, mwb_maps + m.ScoreB, mlb_maps + m.ScoreA);
                }
            }

            // Susun ranking sesuai standar VLR.gg: Points (Match W) DESC → Map Diff DESC → Maps Won DESC → Nama ASC
            var ranked = stats
                .Select(kv =>
                {
                    var p = kv.Value;
                    var mapDiff = p.MapsW - p.MapsL;
                    var winRate = p.MP > 0 ? $"{(int)Math.Round((double)p.MW / p.MP * 100)}%" : "0%";
                    return new
                    {
                        Id = kv.Key,
                        Name = p.Name,
                        MatchesPlayed = p.MP,
                        MatchesWon = p.MW,
                        MatchesLost = p.ML,
                        MatchRecord = $"{p.MW} - {p.ML}",
                        WinRate = winRate,
                        MapsWon = p.MapsW,
                        MapsLost = p.MapsL,
                        MapRecord = $"{p.MapsW} - {p.MapsL}",
                        MapDiff = mapDiff,
                        Points = p.MW // 1 Match Win = 1 Poin (standar VCT)
                    };
                })
                .OrderByDescending(x => x.Points)
                .ThenByDescending(x => x.MapDiff)
                .ThenByDescending(x => x.MapsWon)
                .ThenBy(x => x.Name)
                .ToList();

            Standings.Clear();
            for (int i = 0; i < ranked.Count; i++)
            {
                var r = ranked[i];
                Standings.Add(new StandingRow(
                    i + 1,
                    r.Id,
                    r.Name,
                    r.MatchesPlayed,
                    r.MatchesWon,
                    r.MatchesLost,
                    r.MatchRecord,
                    r.WinRate,
                    r.MapsWon,
                    r.MapsLost,
                    r.MapRecord,
                    r.MapDiff,
                    r.Points));
            }
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal memuat klasemen: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static void EnsureTeam(Dictionary<int, (string Name, int MP, int MW, int ML, int MapsW, int MapsL)> stats, int id, string name)
    {
        if (!stats.ContainsKey(id))
            stats[id] = (name, 0, 0, 0, 0, 0);
    }
}
