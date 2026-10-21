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
/// ViewModel halaman Bracket/Penjadwalan Otomatis.
/// Mendukung format Single Elimination dan Group Stage.
/// </summary>
public record FormatOption(TournamentType Format, string DisplayName);

public partial class BracketViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;
    private readonly ISnackbarMessageQueue _snackbar;

    public BracketViewModel(
        IDbContextFactory<AppDbContext> factory,
        IDialogService dialog,
        ISnackbarMessageQueue snackbar)
    {
        _factory = factory;
        _dialog = dialog;
        _snackbar = snackbar;

        _ = LoadAsync();
    }

    // ── Properti ───────────────────────────────────────────────────────

    public ObservableCollection<Match> GeneratedMatches { get; } = new();
    public ObservableCollection<Team> ApprovedTeams { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGroupStage))]
    private TournamentType _selectedFormat = TournamentType.SingleElimination;

    public bool IsGroupStage => SelectedFormat == TournamentType.GroupStage;

    [ObservableProperty]
    private int _numberOfGroups = 2;

    [ObservableProperty]
    private string _roundPrefix = "Babak 1";

    [ObservableProperty]
    private DateTime _firstMatchDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private int _minutesBetweenMatches = 90;

    [ObservableProperty]
    private bool _isLoading;

    public IReadOnlyList<FormatOption> Formats { get; } = new List<FormatOption>
    {
        new(TournamentType.SingleElimination, "Sistem Gugur (Single Elimination)"),
        new(TournamentType.GroupStage,        "Babak Grup (Group Stage)"),
        new(TournamentType.RoundRobin,       "Round Robin Penuh")
    };

    // ── Commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task GenerateBracketAsync()
    {
        if (ApprovedTeams.Count < 2)
        {
            _snackbar.Enqueue("Minimal 2 tim yang sudah disetujui untuk membuat bracket.");
            return;
        }

        if (SelectedFormat == TournamentType.GroupStage)
        {
            if (NumberOfGroups <= 0)
            {
                _snackbar.Enqueue("Jumlah grup harus minimal 1.");
                return;
            }
            if (NumberOfGroups > ApprovedTeams.Count / 2)
            {
                _snackbar.Enqueue($"Jumlah grup ({NumberOfGroups}) terlalu banyak. Setiap grup membutuhkan minimal 2 tim (maksimal {ApprovedTeams.Count / 2} grup untuk {ApprovedTeams.Count} tim).");
                return;
            }
        }

        if (MinutesBetweenMatches <= 0)
        {
            MinutesBetweenMatches = 90;
        }

        var formatLabel = SelectedFormat switch
        {
            TournamentType.SingleElimination => "Single Elimination",
            TournamentType.GroupStage        => $"Group Stage ({NumberOfGroups} grup)",
            _                                => "Round Robin"
        };

        var confirmed = await _dialog.ConfirmAsync(
            "Buat Bracket Otomatis",
            $"Ini akan membuat jadwal pertandingan secara otomatis menggunakan format {formatLabel} " +
            $"untuk {ApprovedTeams.Count} tim. Pertandingan yang sudah ada tidak akan dihapus. Lanjutkan?");

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            var teams = ApprovedTeams.OrderBy(_ => Guid.NewGuid()).ToList(); // acak urutan
            var scheduledTime = FirstMatchDate.Date.AddHours(8); // mulai pukul 08:00
            var newMatches = new List<Match>();

            if (SelectedFormat == TournamentType.SingleElimination)
            {
                newMatches = GenerateSingleElimination(teams, ref scheduledTime);
            }
            else if (SelectedFormat == TournamentType.GroupStage)
            {
                newMatches = GenerateGroupStage(teams, NumberOfGroups, ref scheduledTime);
            }
            else
            {
                newMatches = GenerateRoundRobin(teams, ref scheduledTime);
            }

            await using var db = await _factory.CreateDbContextAsync();
            db.Matches.AddRange(newMatches);
            await db.SaveChangesAsync();

            await LoadAsync();
            _snackbar.Enqueue($"{newMatches.Count} pertandingan berhasil dibuat secara otomatis.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal membuat bracket: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearGeneratedMatchesAsync()
    {
        var confirmed = await _dialog.ConfirmAsync(
            "Hapus Pertandingan Belum Mulai",
            "Semua pertandingan berstatus 'Belum Mulai' akan dihapus. Lanjutkan?");
        if (!confirmed) return;

        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var pending = db.Matches.Where(m => m.Status == MatchStatus.NotStarted);
            db.Matches.RemoveRange(pending);
            await db.SaveChangesAsync();

            await LoadAsync();
            _snackbar.Enqueue("Jadwal yang belum mulai berhasil dihapus.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal menghapus jadwal: {ex.Message}");
        }
    }

    // ── Generator Algoritma ────────────────────────────────────────────

    /// <summary>Buat jadwal sistem gugur (single elimination).</summary>
    private List<Match> GenerateSingleElimination(List<Team> teams, ref DateTime scheduledTime)
    {
        var result = new List<Match>();
        var paired = teams.ToList();

        // Jika jumlah ganjil, tambahkan "bye" (tim pertama langsung lolos) — tidak diimplementasikan
        // Cukup pasangkan tim secara berpasangan
        int matchNumber = 1;
        for (int i = 0; i + 1 < paired.Count; i += 2)
        {
            result.Add(new Match
            {
                TeamAId = paired[i].Id,
                TeamBId = paired[i + 1].Id,
                Round = $"{RoundPrefix} — Pertandingan {matchNumber++}",
                Status = MatchStatus.NotStarted,
                ScheduledAt = scheduledTime
            });
            scheduledTime = scheduledTime.AddMinutes(MinutesBetweenMatches);
        }
        return result;
    }

    /// <summary>Buat jadwal sistem grup (round-robin dalam grup).</summary>
    private List<Match> GenerateGroupStage(List<Team> teams, int numGroups, ref DateTime scheduledTime)
    {
        var result = new List<Match>();
        var groups = new List<List<Team>>();

        // Bagi tim ke dalam grup
        for (int g = 0; g < numGroups; g++)
            groups.Add(new List<Team>());

        for (int i = 0; i < teams.Count; i++)
            groups[i % numGroups].Add(teams[i]);

        // Round-robin dalam setiap grup
        for (int g = 0; g < groups.Count; g++)
        {
            var groupTeams = groups[g];
            var groupName = $"Grup {(char)('A' + g)}";

            for (int i = 0; i < groupTeams.Count; i++)
            {
                for (int j = i + 1; j < groupTeams.Count; j++)
                {
                    result.Add(new Match
                    {
                        TeamAId = groupTeams[i].Id,
                        TeamBId = groupTeams[j].Id,
                        Round = $"Penyisihan {groupName}",
                        GroupName = groupName,
                        Status = MatchStatus.NotStarted,
                        ScheduledAt = scheduledTime
                    });
                    scheduledTime = scheduledTime.AddMinutes(MinutesBetweenMatches);
                }
            }
        }
        return result;
    }

    /// <summary>Buat jadwal round-robin penuh (semua lawan semua).</summary>
    private List<Match> GenerateRoundRobin(List<Team> teams, ref DateTime scheduledTime)
    {
        var result = new List<Match>();
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                result.Add(new Match
                {
                    TeamAId = teams[i].Id,
                    TeamBId = teams[j].Id,
                    Round = "Round Robin",
                    Status = MatchStatus.NotStarted,
                    ScheduledAt = scheduledTime
                });
                scheduledTime = scheduledTime.AddMinutes(MinutesBetweenMatches);
            }
        }
        return result;
    }

    // ── Load Data ──────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();

            var teams = await db.Teams
                .AsNoTracking()
                .Where(t => t.Status == RegistrationStatus.Approved)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var matches = await db.Matches
                .AsNoTracking()
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();

            ApprovedTeams.Clear();
            foreach (var t in teams) ApprovedTeams.Add(t);

            GeneratedMatches.Clear();
            foreach (var m in matches) GeneratedMatches.Add(m);
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal memuat data bracket: {ex.Message}");
        }
    }
}
