using CommunityToolkit.Mvvm.ComponentModel;

namespace TournamentManager.Models;

/// <summary>
/// Riwayat kejadian dalam pertandingan: gol, kartu, pergantian pemain.
/// Setiap kejadian dicatat beserta menit terjadinya.
/// </summary>
public partial class MatchEvent : ObservableObject
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match? Match { get; set; }

    /// <summary>Tim yang melakukan kejadian (nullable untuk kejadian netral).</summary>
    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>Pemain yang terlibat (nullable jika tidak diketahui).</summary>
    public int? PlayerId { get; set; }
    public Player? Player { get; set; }

    [ObservableProperty]
    private EventType _eventType = EventType.Goal;

    [ObservableProperty]
    private int _minute;

    [ObservableProperty]
    private string _note = string.Empty;

    public DateTime RecordedAt { get; set; } = DateTime.Now;

    /// <summary>Label singkat untuk ditampilkan di timeline.</summary>
    public string EventLabel => EventType switch
    {
        EventType.Goal        => "⚽ Gol",
        EventType.OwnGoal     => "⚽ Gol Bunuh Diri",
        EventType.PenaltyGoal => "⚽ Penalti",
        EventType.YellowCard  => "🟨 Kartu Kuning",
        EventType.RedCard     => "🟥 Kartu Merah",
        EventType.Substitution => "🔄 Pergantian",
        _                     => EventType.ToString()
    };

    /// <summary>Teks ringkas untuk timeline: "45' ⚽ Gol — Nama Pemain".</summary>
    public string TimelineText =>
        $"{Minute}' {EventLabel}" +
        (Player is not null ? $" — {Player.Name}" : string.Empty) +
        (!string.IsNullOrWhiteSpace(Note) ? $" ({Note})" : string.Empty);
}
