namespace TournamentManager.Models;

public enum RegistrationStatus
{
    Pending,
    Approved,
    Rejected
}

public enum MatchStatus
{
    NotStarted,
    InProgress,
    Completed
}

/// <summary>Format turnamen yang didukung.</summary>
public enum TournamentType
{
    SingleElimination,
    GroupStage,
    RoundRobin
}

/// <summary>Status keseluruhan sebuah turnamen.</summary>
public enum TournamentStatus
{
    Draft,
    Active,
    Completed,
    Cancelled
}

/// <summary>Jenis kejadian dalam pertandingan esports Valorant.</summary>
public enum EventType
{
    RoundWin,
    Ace,
    Clutch,
    SpikePlant,
    SpikeDefuse,
    // Kompatibilitas
    Goal,
    YellowCard,
    RedCard,
    Substitution,
    OwnGoal,
    PenaltyGoal
}

/// <summary>Peran pengguna untuk sistem login.</summary>
public enum UserRole
{
    Admin,
    Operator,
    Viewer
}

/// <summary>Peran anggota dalam tim esports (5 Pemain Inti, 1 Cadangan/Sub, 1 Coach).</summary>
public enum RosterRole
{
    Main,   // Pemain Inti (Maksimal 5)
    Sub,    // Pemain Cadangan / Substitute (Maksimal 1)
    Coach   // Pelatih / Coach (Maksimal 1)
}
