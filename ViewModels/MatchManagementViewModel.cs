using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using TournamentManager.Data;
using TournamentManager.Models;
using TournamentManager.Services;

namespace TournamentManager.ViewModels;

public partial class MatchManagementViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;
    private readonly ISnackbarMessageQueue _snackbar;
    private readonly ExportService _export;

    private List<Match> _allMatches = new();

    public MatchManagementViewModel(
        IDbContextFactory<AppDbContext> factory,
        IDialogService dialog,
        ISnackbarMessageQueue snackbar,
        ExportService export)
    {
        _factory = factory;
        _dialog = dialog;
        _snackbar = snackbar;
        _export = export;

        _ = LoadAsync();
    }

    // ---------- Kolom Kanban ----------
    public ObservableCollection<Match> NotStartedMatches { get; } = new();
    public ObservableCollection<Match> InProgressMatches { get; } = new();
    public ObservableCollection<Match> CompletedMatches { get; } = new();

    // ---------- Filter / Pencarian ----------
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedRoundFilter = "Semua Babak";

    public ObservableCollection<string> AvailableRounds { get; } = new();

    partial void OnSearchTextChanged(string value) => RebuildBoards();
    partial void OnSelectedRoundFilterChanged(string value) => RebuildBoards();

    // ---------- Form pertandingan baru ----------
    public ObservableCollection<Team> ApprovedTeams { get; } = new();

    [ObservableProperty]
    private Team? _selectedTeamA;

    [ObservableProperty]
    private Team? _selectedTeamB;

    [ObservableProperty]
    private string _round = "Penyisihan";

    [ObservableProperty]
    private DateTime? _scheduledDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _scheduledTime = DateTime.Today.AddHours(15);

    // ---------- Commands ----------
    [RelayCommand]
    private async Task CreateMatchAsync()
    {
        if (SelectedTeamA is null || SelectedTeamB is null)
        {
            _snackbar.Enqueue("Pilih Tim A dan Tim B terlebih dahulu.");
            return;
        }

        if (SelectedTeamA.Id == SelectedTeamB.Id)
        {
            _snackbar.Enqueue("Tim A dan Tim B tidak boleh sama.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Round))
        {
            _snackbar.Enqueue("Babak/ronde wajib diisi.");
            return;
        }

        var scheduledAt = (ScheduledDate ?? DateTime.Today).Date + (ScheduledTime?.TimeOfDay ?? TimeSpan.Zero);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Matches.Add(new Match
            {
                TeamAId = SelectedTeamA.Id,
                TeamBId = SelectedTeamB.Id,
                Round = Round.Trim(),
                ScheduledAt = scheduledAt,
                Status = MatchStatus.NotStarted
            });
            await db.SaveChangesAsync();
        }

        _snackbar.Enqueue($"Pertandingan {SelectedTeamA.Name} vs {SelectedTeamB.Name} dijadwalkan.");
        SelectedTeamA = null;
        SelectedTeamB = null;

        await LoadAsync();
    }

    [RelayCommand]
    private async Task StartMatchAsync(Match? match)
    {
        if (match is null || match.Status != MatchStatus.NotStarted) return;

        match.Status = MatchStatus.InProgress;
        match.StartedAt = DateTime.Now;
        await SaveMatchAsync(match);

        RebuildBoards();
        _snackbar.Enqueue($"Pertandingan {match.TeamAName} vs {match.TeamBName} dimulai.");
    }

    [RelayCommand]
    private async Task FinishMatchAsync(Match? match)
    {
        if (match is null || match.Status != MatchStatus.InProgress) return;

        var confirmed = await _dialog.ConfirmAsync(
            "Akhiri Pertandingan",
            $"{match.TeamAName} {match.ScoreText} {match.TeamBName}\nSkor ini akan dijadikan hasil akhir. Lanjutkan?");
        if (!confirmed) return;

        match.Status = MatchStatus.Completed;
        match.FinishedAt = DateTime.Now;
        await SaveMatchAsync(match);

        RebuildBoards();
        _snackbar.Enqueue($"Pertandingan selesai. {match.WinnerText}");
    }

    [RelayCommand]
    private async Task DeleteMatchAsync(Match? match)
    {
        if (match is null || match.Status != MatchStatus.NotStarted) return;

        var confirmed = await _dialog.ConfirmAsync(
            "Batalkan Pertandingan",
            $"Jadwal {match.TeamAName} vs {match.TeamBName} akan dihapus. Lanjutkan?");
        if (!confirmed) return;

        await using (var db = await _factory.CreateDbContextAsync())
        {
            var entity = await db.Matches.FindAsync(match.Id);
            if (entity is not null)
            {
                db.Matches.Remove(entity);
                await db.SaveChangesAsync();
            }
        }

        _allMatches.Remove(match);
        RebuildBoards();
        _snackbar.Enqueue("Jadwal pertandingan dihapus.");
    }

    [RelayCommand]
    private Task AddScoreA(Match? match) => AdjustScoreAsync(match, 1, 0);

    [RelayCommand]
    private Task SubScoreA(Match? match) => AdjustScoreAsync(match, -1, 0);

    [RelayCommand]
    private Task AddScoreB(Match? match) => AdjustScoreAsync(match, 0, 1);

    [RelayCommand]
    private Task SubScoreB(Match? match) => AdjustScoreAsync(match, 0, -1);

    // ---------- Export Commands ----------
    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        try
        {
            await _export.ExportMatchesToExcelAsync(_allMatches);
            _snackbar.Enqueue("Jadwal pertandingan berhasil diekspor ke Excel.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal ekspor: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        try
        {
            await _export.ExportMatchesToPdfAsync(_allMatches);
            _snackbar.Enqueue("Jadwal pertandingan berhasil diekspor ke PDF.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal ekspor: {ex.Message}");
        }
    }

    // ---------- Drag & Drop Handler ----------
    public async Task HandleMatchDroppedAsync(Match match, MatchStatus newStatus)
    {
        if (newStatus == MatchStatus.InProgress && match.Status == MatchStatus.NotStarted)
        {
            match.Status = MatchStatus.InProgress;
            match.StartedAt = DateTime.Now;
            await SaveMatchAsync(match);
            RebuildBoards();
            _snackbar.Enqueue($"Pertandingan {match.TeamAName} vs {match.TeamBName} dimulai (drag & drop).");
        }
        else if (newStatus == MatchStatus.Completed && match.Status == MatchStatus.InProgress)
        {
            var confirmed = await _dialog.ConfirmAsync(
                "Akhiri Pertandingan",
                $"{match.TeamAName} {match.ScoreText} {match.TeamBName}\nAkhiri pertandingan via drag & drop? Lanjutkan?");
            if (!confirmed) return;

            match.Status = MatchStatus.Completed;
            match.FinishedAt = DateTime.Now;
            await SaveMatchAsync(match);
            RebuildBoards();
            _snackbar.Enqueue($"Pertandingan selesai. {match.WinnerText}");
        }
    }

    // ---------- Helper ----------
    private async Task AdjustScoreAsync(Match? match, int deltaA, int deltaB)
    {
        if (match is null || match.Status != MatchStatus.InProgress) return;

        match.ScoreA = Math.Max(0, match.ScoreA + deltaA);
        match.ScoreB = Math.Max(0, match.ScoreB + deltaB);
        await SaveMatchAsync(match);
    }

    private async Task SaveMatchAsync(Match match)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var entity = await db.Matches.FindAsync(match.Id);
            if (entity is null) return;

            entity.ScoreA = match.ScoreA;
            entity.ScoreB = match.ScoreB;
            entity.Status = match.Status;
            entity.StartedAt = match.StartedAt;
            entity.FinishedAt = match.FinishedAt;

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal menyimpan pertandingan: {ex.Message}");
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();

            _allMatches = await db.Matches
                .AsNoTracking()
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();

            var teams = await db.Teams
                .AsNoTracking()
                .Where(t => t.Status == RegistrationStatus.Approved)
                .OrderBy(t => t.Name)
                .ToListAsync();

            ApprovedTeams.Clear();
            foreach (var team in teams) ApprovedTeams.Add(team);

            // Isi pilihan filter babak
            var rounds = _allMatches.Select(m => m.Round).Distinct().OrderBy(r => r).ToList();
            AvailableRounds.Clear();
            AvailableRounds.Add("Semua Babak");
            foreach (var r in rounds) AvailableRounds.Add(r);

            RebuildBoards();
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal memuat pertandingan: {ex.Message}");
        }
    }

    /// <summary>Membagi ulang seluruh pertandingan ke tiga kolom kanban sesuai statusnya, dengan filter aktif.</summary>
    private void RebuildBoards()
    {
        var filtered = _allMatches.AsEnumerable();

        // Filter berdasarkan pencarian teks
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(m =>
                m.TeamAName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.TeamBName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.Round.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Filter berdasarkan babak
        if (!string.IsNullOrEmpty(SelectedRoundFilter) && SelectedRoundFilter != "Semua Babak")
        {
            filtered = filtered.Where(m => m.Round == SelectedRoundFilter);
        }

        var list = filtered.ToList();

        Fill(NotStartedMatches, list.Where(m => m.Status == MatchStatus.NotStarted).OrderBy(m => m.ScheduledAt));
        Fill(InProgressMatches, list.Where(m => m.Status == MatchStatus.InProgress).OrderBy(m => m.StartedAt));
        Fill(CompletedMatches, list.Where(m => m.Status == MatchStatus.Completed).OrderByDescending(m => m.FinishedAt));
    }

    private static void Fill(ObservableCollection<Match> target, IEnumerable<Match> source)
    {
        target.Clear();
        foreach (var match in source) target.Add(match);
    }
}
