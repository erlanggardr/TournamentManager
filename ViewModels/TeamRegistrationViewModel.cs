using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using TournamentManager.Data;
using TournamentManager.Models;
using TournamentManager.Services;

namespace TournamentManager.ViewModels;

/// <summary>
/// Form registrasi + daftar tim. Mewarisi ObservableValidator sehingga validasi DataAnnotations
/// otomatis tampil di TextBox (INotifyDataErrorInfo).
/// </summary>
public partial class TeamRegistrationViewModel : ObservableValidator
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;
    private readonly ISnackbarMessageQueue _snackbar;
    private readonly ExportService _export;

    private Team? _editingTeam;

    public TeamRegistrationViewModel(
        IDbContextFactory<AppDbContext> factory,
        IDialogService dialog,
        ISnackbarMessageQueue snackbar,
        ExportService export)
    {
        _factory = factory;
        _dialog = dialog;
        _snackbar = snackbar;
        _export = export;

        TeamsView = CollectionViewSource.GetDefaultView(Teams);
        TeamsView.Filter = FilterTeam;

        _ = LoadAsync();
    }

    // ---------- Data daftar tim ----------
    public ObservableCollection<Team> Teams { get; } = new();
    public ICollectionView TeamsView { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => TeamsView.Refresh();

    private bool FilterTeam(object item)
    {
        if (item is not Team team) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        return team.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || team.CaptainName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Field form (dengan validasi) ----------
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Nama tim wajib diisi")]
    [MinLength(3, ErrorMessage = "Minimal 3 karakter")]
    [MaxLength(100, ErrorMessage = "Maksimal 100 karakter")]
    private string _teamName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Nama kapten wajib diisi")]
    [MinLength(3, ErrorMessage = "Minimal 3 karakter")]
    [MaxLength(100, ErrorMessage = "Maksimal 100 karakter")]
    private string _captainName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Kontak wajib diisi")]
    [RegularExpression(@"^[0-9+\-\s]{8,16}$", ErrorMessage = "Format kontak tidak valid (8-16 digit)")]
    private string _contact = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Tanggal daftar wajib diisi")]
    private DateTime? _registeredAt = DateTime.Today;

    // ---------- Mode form ----------
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool _isEditing;

    public string FormTitle => IsEditing ? "Edit Tim" : "Registrasi Tim Baru";
    public string SaveButtonText => IsEditing ? "Simpan Perubahan" : "Daftarkan Tim";

    // ---------- Commands ----------
    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        var name = TeamName.Trim();

        await using var db = await _factory.CreateDbContextAsync();

        var duplicate = await db.Teams.AnyAsync(t =>
            t.Name == name && (_editingTeam == null || t.Id != _editingTeam.Id));
        if (duplicate)
        {
            _snackbar.Enqueue("Nama tim sudah terdaftar. Gunakan nama lain.");
            return;
        }

        if (_editingTeam is null)
        {
            var team = new Team
            {
                Name = name,
                CaptainName = CaptainName.Trim(),
                Contact = Contact.Trim(),
                RegisteredAt = (RegisteredAt ?? DateTime.Today).Date,
                Status = RegistrationStatus.Pending
            };
            db.Teams.Add(team);
            await db.SaveChangesAsync();

            Teams.Insert(0, team);
            _snackbar.Enqueue($"Tim \"{team.Name}\" berhasil didaftarkan (menunggu validasi).");
        }
        else
        {
            var entity = await db.Teams.FindAsync(_editingTeam.Id);
            if (entity is null)
            {
                _snackbar.Enqueue("Tim tidak ditemukan. Mungkin sudah dihapus.");
                ResetForm();
                return;
            }

            entity.Name = name;
            entity.CaptainName = CaptainName.Trim();
            entity.Contact = Contact.Trim();
            entity.RegisteredAt = (RegisteredAt ?? DateTime.Today).Date;
            await db.SaveChangesAsync();

            // Perbarui objek yang tampil di DataGrid (Team adalah ObservableObject)
            _editingTeam.Name = entity.Name;
            _editingTeam.CaptainName = entity.CaptainName;
            _editingTeam.Contact = entity.Contact;
            _editingTeam.RegisteredAt = entity.RegisteredAt;

            _snackbar.Enqueue($"Data tim \"{entity.Name}\" berhasil diperbarui.");
        }

        TeamsView.Refresh();
        ResetForm();
    }

    [RelayCommand]
    private void CancelEdit() => ResetForm();

    [RelayCommand]
    private void Edit(Team? team)
    {
        if (team is null) return;

        _editingTeam = team;
        IsEditing = true;

        TeamName = team.Name;
        CaptainName = team.CaptainName;
        Contact = team.Contact;
        RegisteredAt = team.RegisteredAt;
        ClearErrors();
    }

    [RelayCommand]
    private async Task DeleteAsync(Team? team)
    {
        if (team is null) return;

        var confirmed = await _dialog.ConfirmAsync(
            "Hapus Tim",
            $"Tim \"{team.Name}\" akan dihapus permanen. Lanjutkan?");
        if (!confirmed) return;

        await using var db = await _factory.CreateDbContextAsync();

        var hasMatches = await db.Matches.AnyAsync(m => m.TeamAId == team.Id || m.TeamBId == team.Id);
        if (hasMatches)
        {
            _snackbar.Enqueue("Tim tidak bisa dihapus karena sudah terdaftar di pertandingan.");
            return;
        }

        var entity = await db.Teams.FindAsync(team.Id);
        if (entity is not null)
        {
            db.Teams.Remove(entity);
            await db.SaveChangesAsync();
        }

        Teams.Remove(team);
        if (ReferenceEquals(_editingTeam, team)) ResetForm();
        _snackbar.Enqueue($"Tim \"{team.Name}\" dihapus.");
    }

    [RelayCommand]
    private Task ApproveAsync(Team? team) =>
        SetStatusAsync(team, RegistrationStatus.Approved, "disetujui");

    [RelayCommand]
    private Task RejectAsync(Team? team) =>
        SetStatusAsync(team, RegistrationStatus.Rejected, "ditolak");

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        try
        {
            await _export.ExportTeamsToExcelAsync(Teams);
            _snackbar.Enqueue("Daftar tim berhasil diekspor ke Excel.");
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
            await _export.ExportTeamsToPdfAsync(Teams);
            _snackbar.Enqueue("Daftar tim berhasil diekspor ke PDF.");
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal ekspor: {ex.Message}");
        }
    }

    // ---------- Helper ----------
    private async Task SetStatusAsync(Team? team, RegistrationStatus status, string label)
    {
        if (team is null) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Teams.FindAsync(team.Id);
        if (entity is null) return;

        entity.Status = status;
        await db.SaveChangesAsync();

        team.Status = status; // memicu pembaruan UI langsung
        _snackbar.Enqueue($"Pendaftaran \"{team.Name}\" {label}.");
    }

    private async Task LoadAsync()
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var list = await db.Teams
                .AsNoTracking()
                .OrderByDescending(t => t.RegisteredAt)
                .ThenBy(t => t.Name)
                .ToListAsync();

            Teams.Clear();
            foreach (var team in list) Teams.Add(team);
        }
        catch (Exception ex)
        {
            _snackbar.Enqueue($"Gagal memuat data tim: {ex.Message}");
        }
    }

    private void ResetForm()
    {
        _editingTeam = null;
        IsEditing = false;

        TeamName = string.Empty;
        CaptainName = string.Empty;
        Contact = string.Empty;
        RegisteredAt = DateTime.Today;
        ClearErrors(); // form kosong tidak boleh langsung tampil merah
    }
}
