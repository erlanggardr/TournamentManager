using CommunityToolkit.Mvvm.ComponentModel;

namespace TournamentManager.Models;

/// <summary>
/// Entitas turnamen. Satu aplikasi dapat menyimpan beberapa turnamen/musim.
/// Tim dan pertandingan dikaitkan ke satu turnamen melalui TournamentId.
/// </summary>
public partial class Tournament : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TournamentType _type = TournamentType.SingleElimination;

    [ObservableProperty]
    private TournamentStatus _status = TournamentStatus.Draft;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private bool _isActive;

    /// <summary>Tim yang terdaftar dalam turnamen ini.</summary>
    public List<Team> Teams { get; set; } = new();

    /// <summary>Pertandingan yang dijadwalkan dalam turnamen ini.</summary>
    public List<Match> Matches { get; set; } = new();
}
