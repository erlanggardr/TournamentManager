using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using TournamentManager.Data;
using TournamentManager.Models;
using TournamentManager.Services;

namespace TournamentManager.ViewModels;

/// <summary>
/// ViewModel halaman Roster Pemain Format Esports (Valorant / Mobile Legends).
/// Standar 7 anggota per tim: 5 Pemain Inti, 1 Sub/Cadangan, 1 Coach.
/// </summary>
public partial class PlayerRosterViewModel : ObservableValidator
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;
    private readonly ISnackbarMessageQueue _snackbar;

    private Player? _editingPlayer;

    public PlayerRosterViewModel(
        IDbContextFactory<AppDbContext> factory,
        IDialogService dialog,
        ISnackbarMessageQueue snackbar)
    {
        _factory = factory;
        _dialog = dialog;
        _snackbar = snackbar;

        _ = LoadTeamsAsync();
    }

    // ── Data ───────────────────────────────────────────────────────────

    public ObservableCollection<Team> Teams { get; } = new();
    public ObservableCollection<Player> Players { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayerCountText))]
    [NotifyPropertyChangedFor(nameof(RosterSummaryText))]
    private Team? _selectedTeam;

    public int MaxPlayers => 7;
    public int MaxMain => 5;
    public int MaxSub => 1;
    public int MaxCoach => 1;

    public int MainCount => Players.Count(p => p.Role == RosterRole.Main);
    public int SubCount => Players.Count(p => p.Role == RosterRole.Sub);
    public int CoachCount => Players.Count(p => p.Role == RosterRole.Coach);

    public string PlayerCountText =>
        SelectedTeam is null ? "-" : $"{Players.Count} / {MaxPlayers} Anggota";

    public string RosterSummaryText =>
        SelectedTeam is null
            ? "-"
            : $"Inti: {MainCount}/5  ·  Sub: {SubCount}/1  ·  Coach: {CoachCount}/1";

    partial void OnSelectedTeamChanged(Team? value) => _ = LoadPlayersAsync();

    // ── Form Fields ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Nama lengkap / asli wajib diisi")]
    [MinLength(2, ErrorMessage = "Minimal 2 karakter")]
    [MaxLength(100, ErrorMessage = "Maksimal 100 karakter")]
    private string _playerName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "In-Game Name (IGN) / Nickname wajib diisi")]
    [MaxLength(50, ErrorMessage = "Maksimal 50 karakter")]
    private string _inGameName = string.Empty;

    [ObservableProperty]
    private RosterRole _selectedRole = RosterRole.Main;

    public IEnumerable<RosterRole> AvailableRoles => Enum.GetValues<RosterRole>();

    // ── Mode Form ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool _isEditing;

    public string FormTitle => IsEditing ? "Edit Roster" : "Tambah Anggota Tim (7 Max)";
    public string SaveButtonText => IsEditing ? "Simpan Perubahan" : "Daftarkan ke Roster";

    // ── Commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedTeam is null)
        {
            _snackbar.Enqueue("Pilih tim esports terlebih dahulu.");
            return;
        }

        ValidateAllProperties();
        if (HasErrors) return;

        await using var db = await _factory.CreateDbContextAsync();

        // Cek kuota peran berdasarkan aturan turnamen esports (5 Main, 1 Sub, 1 Coach)
        var currentPlayers = await db.Players
            .Where(p => p.TeamId == SelectedTeam.Id)
            .ToListAsync();

        if (_editingPlayer != null)
        {
            currentPlayers.RemoveAll(p => p.Id == _editingPlayer.Id);
        }

        var mainExisting = currentPlayers.Count(p => p.Role == RosterRole.Main);
        var subExisting = currentPlayers.Count(p => p.Role == RosterRole.Sub);
        var coachExisting = currentPlayers.Count(p => p.Role == RosterRole.Coach);

        if (SelectedRole == RosterRole.Main && mainExisting >= MaxMain)
        {
            _snackbar.Enqueue($"Kuota Pemain Inti sudah penuh (Maksimal {MaxMain} pemain inti).");
            return;
        }
        if (SelectedRole == RosterRole.Sub && subExisting >= MaxSub)
        {
            _snackbar.Enqueue($"Kuota Pemain Cadangan sudah penuh (Maksimal {MaxSub} cadangan/sub).");
            return;
        }
        if (SelectedRole == RosterRole.Coach && coachExisting >= MaxCoach)
        {
            _snackbar.Enqueue($"Kuota Pelatih sudah penuh (Maksimal {MaxCoach} coach).");
            return;
        }
        if (currentPlayers.Count >= MaxPlayers)
        {
            _snackbar.Enqueue($"Total roster sudah mencapai batas maksimal {MaxPlayers} peserta (5 Inti + 1 Sub + 1 Coach).");
            return;
        }

        // Cek IGN duplikat dalam tim
        var duplicateIgn = currentPlayers.Any(p =>
            string.Equals(p.InGameName, InGameName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicateIgn)
        {
            _snackbar.Enqueue($"In-Game Name (IGN) '{InGameName}' sudah terdaftar di tim ini.");
            return;
        }

        if (_editingPlayer is null)
        {
            var player = new Player
            {
                TeamId = SelectedTeam.Id,
                Name = PlayerName.Trim(),
                InGameName = InGameName.Trim(),
                Role = SelectedRole,
                Position = string.Empty,
                JerseyNumber = 0,
                IsActive = true
            };
            db.Players.Add(player);
            await db.SaveChangesAsync();

            Players.Add(player);
            UpdateRosterCounters();
            _snackbar.Enqueue($"[{player.RoleDisplayName}] {player.InGameName} ({player.Name}) berhasil ditambahkan.");
        }
        else
        {
            var entity = await db.Players.FindAsync(_editingPlayer.Id);
            if (entity is null) { ResetForm(); return; }

            entity.Name = PlayerName.Trim();
            entity.InGameName = InGameName.Trim();
            entity.Role = SelectedRole;
            await db.SaveChangesAsync();

            _editingPlayer.Name = entity.Name;
            _editingPlayer.InGameName = entity.InGameName;
            _editingPlayer.Role = entity.Role;

            UpdateRosterCounters();
            _snackbar.Enqueue($"Data {entity.InGameName} berhasil diperbarui.");
        }

        ResetForm();
    }

    [RelayCommand]
    private void CancelEdit() => ResetForm();

    [RelayCommand]
    private void Edit(Player? player)
    {
        if (player is null) return;
        _editingPlayer = player;
        IsEditing = true;
        PlayerName = player.Name;
        InGameName = player.InGameName;
        SelectedRole = player.Role;
        ClearErrors();
    }

    [RelayCommand]
    private async Task DeleteAsync(Player? player)
    {
        if (player is null) return;

        var confirmed = await _dialog.ConfirmAsync(
            "Hapus dari Roster",
            $"{player.RoleDisplayName} '{player.InGameName}' ({player.Name}) akan dihapus dari roster. Lanjutkan?");
        if (!confirmed) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Players.FindAsync(player.Id);
        if (entity is not null)
        {
            db.Players.Remove(entity);
            await db.SaveChangesAsync();
        }

        Players.Remove(player);
        if (ReferenceEquals(_editingPlayer, player)) ResetForm();
        UpdateRosterCounters();
        _snackbar.Enqueue($"{player.InGameName} dihapus dari roster.");
    }

    // ── Load ───────────────────────────────────────────────────────────

    private async Task LoadTeamsAsync()
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var teams = await db.Teams
                .AsNoTracking()
                .Where(t => t.Status == RegistrationStatus.Approved)
                .OrderBy(t => t.Name)
                .ToListAsync();

            Teams.Clear();
            foreach (var t in teams) Teams.Add(t);

            if (Teams.Any()) SelectedTeam = Teams[0];
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal memuat tim: {ex.Message}");
        }
    }

    private async Task LoadPlayersAsync()
    {
        if (SelectedTeam is null) { Players.Clear(); UpdateRosterCounters(); return; }
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var list = await db.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeam.Id)
                .OrderBy(p => p.Role)
                .ThenBy(p => p.Name)
                .ToListAsync();

            Players.Clear();
            foreach (var p in list) Players.Add(p);
            UpdateRosterCounters();
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal memuat roster: {ex.Message}");
        }
    }

    private void UpdateRosterCounters()
    {
        OnPropertyChanged(nameof(MainCount));
        OnPropertyChanged(nameof(SubCount));
        OnPropertyChanged(nameof(CoachCount));
        OnPropertyChanged(nameof(PlayerCountText));
        OnPropertyChanged(nameof(RosterSummaryText));
    }

    private void ResetForm()
    {
        _editingPlayer = null;
        IsEditing = false;
        PlayerName = string.Empty;
        InGameName = string.Empty;
        SelectedRole = RosterRole.Main;
        ClearErrors();
    }
}
