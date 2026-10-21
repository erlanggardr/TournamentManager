using CommunityToolkit.Mvvm.ComponentModel;

namespace TournamentManager.Models;

/// <summary>
/// Anggota tim turnamen esports (5 Pemain Inti, 1 Sub, 1 Coach).
/// </summary>
public partial class Player : ObservableObject
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _inGameName = string.Empty;

    [ObservableProperty]
    private RosterRole _role = RosterRole.Main;

    [ObservableProperty]
    private int _jerseyNumber;

    [ObservableProperty]
    private string _position = string.Empty;

    [ObservableProperty]
    private bool _isActive = true;

    /// <summary>Label tampilan peran di roster.</summary>
    public string RoleDisplayName => Role switch
    {
        RosterRole.Main  => "Pemain Inti",
        RosterRole.Sub   => "Cadangan (Sub)",
        RosterRole.Coach => "Pelatih (Coach)",
        _                => Role.ToString()
    };

    /// <summary>Nama singkat untuk ditampilkan di kartu pertandingan.</summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(InGameName)
        ? $"{InGameName} ({Name})"
        : Name;
}
