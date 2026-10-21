using CommunityToolkit.Mvvm.ComponentModel;

namespace TournamentManager.Models;

/// <summary>
/// Entitas tim peserta. Mewarisi ObservableObject agar perubahan (mis. status approve)
/// langsung tampil di DataGrid tanpa reload.
/// </summary>
public partial class Team : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _captainName = string.Empty;

    [ObservableProperty]
    private string _contact = string.Empty;

    [ObservableProperty]
    private DateTime _registeredAt = DateTime.Today;

    [ObservableProperty]
    private RegistrationStatus _status = RegistrationStatus.Pending;

    /// <summary>Path ke file logo tim (opsional). Null berarti tampilkan ikon default.</summary>
    [ObservableProperty]
    private string? _logoPath;

    /// <summary>Turnamen yang diikuti tim ini.</summary>
    public int? TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    /// <summary>Daftar pemain anggota tim (roster).</summary>
    public List<Player> Players { get; set; } = new();
}

