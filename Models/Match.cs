using CommunityToolkit.Mvvm.ComponentModel;

namespace TournamentManager.Models;

/// <summary>
/// Entitas pertandingan antara dua tim. Properti get-only di bawah (TeamAName, ScoreText, dst.)
/// tidak dipetakan oleh EF Core karena tidak memiliki setter.
/// </summary>
public partial class Match : ObservableObject
{
    public int Id { get; set; }

    public int TeamAId { get; set; }
    public int TeamBId { get; set; }
    public Team? TeamA { get; set; }
    public Team? TeamB { get; set; }

    [ObservableProperty]
    private string _round = "Penyisihan";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreText))]
    [NotifyPropertyChangedFor(nameof(WinnerText))]
    private int _scoreA;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreText))]
    [NotifyPropertyChangedFor(nameof(WinnerText))]
    private int _scoreB;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WinnerText))]
    [NotifyPropertyChangedFor(nameof(ElapsedText))]
    private MatchStatus _status = MatchStatus.NotStarted;

    public DateTime ScheduledAt { get; set; } = DateTime.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ElapsedText))]
    private DateTime? _startedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ElapsedText))]
    private DateTime? _finishedAt;

    /// <summary>Nama grup (mis. "Grup A") untuk format grup. Null untuk sistem gugur.</summary>
    public string? GroupName { get; set; }

    /// <summary>Turnamen yang menaungi pertandingan ini.</summary>
    public int? TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    /// <summary>Daftar kejadian dalam pertandingan (round win, ace, clutch, spike plant/defuse, pergantian pemain).</summary>
    public List<MatchEvent> Events { get; set; } = new();

    public string TeamAName => TeamA?.Name ?? "-";
    public string TeamBName => TeamB?.Name ?? "-";

    public string ScoreText => $"{ScoreA} : {ScoreB}";

    public string WinnerText => Status != MatchStatus.Completed
        ? string.Empty
        : ScoreA == ScoreB
            ? "Hasil seri"
            : $"Pemenang: {(ScoreA > ScoreB ? TeamAName : TeamBName)}";

    public string ElapsedText
    {
        get
        {
            if (StartedAt is null) return "-";
            var end = Status == MatchStatus.Completed ? FinishedAt ?? DateTime.Now : DateTime.Now;
            var span = end - StartedAt.Value;
            return $"{(int)Math.Max(0, span.TotalMinutes)} menit";
        }
    }
}
