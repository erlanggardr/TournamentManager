using TournamentManager.Models;

namespace TournamentManager.Data;

public static class DbInitializer
{
    /// <summary>
    /// Menjalankan EF Core Migrations (membuat/memperbarui skema) lalu mengisi data contoh
    /// agar UI langsung terlihat hidup saat pertama kali dijalankan.
    /// </summary>
    public static void Seed(AppDbContext db)
    {
        // Gunakan Migrate() agar perubahan skema dapat diterapkan tanpa menghapus database
        db.Database.EnsureCreated();

        if (db.Teams.Any()) return; // data sudah ada, lewati seeding

        var today = DateTime.Today;
        var now = DateTime.Now;

        // ── Buat turnamen contoh ─────────────────────────────────────
        var tournament = new Tournament
        {
            Name = "VALORANT Champions Tour 2026 (VCT)",
            Description = "Turnamen Resmi VALORANT 2026 · Standar Format VLR.gg",
            Type = TournamentType.GroupStage,
            Status = TournamentStatus.Active,
            StartDate = today.AddDays(-14),
            EndDate = today.AddDays(30),
            IsActive = true
        };
        db.Tournaments.Add(tournament);
        db.SaveChanges();

        // ── Buat tim esports Valorant ─────────────────────────────────
        var teams = new[]
        {
            new Team { Name = "Paper Rex",       CaptainName = "Aaron Leonhart",  Contact = "081234567801", RegisteredAt = today.AddDays(-12), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Sentinels",       CaptainName = "Tyson Ngo",       Contact = "081234567802", RegisteredAt = today.AddDays(-11), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Fnatic",          CaptainName = "Jake Howlett",    Contact = "081234567803", RegisteredAt = today.AddDays(-10), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "DRX",             CaptainName = "Kim Gu-taek",     Contact = "081234567804", RegisteredAt = today.AddDays(-9),  Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Gen.G Esports",   CaptainName = "Kim Tae-o",       Contact = "081234567805", RegisteredAt = today.AddDays(-8),  Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "RRQ",             CaptainName = "David Monangin",  Contact = "081234567806", RegisteredAt = today.AddDays(-7),  Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "BOOM Esports",    CaptainName = "Fikri Zaki",      Contact = "081234567807", RegisteredAt = today.AddDays(-2),  Status = RegistrationStatus.Pending,  TournamentId = tournament.Id },
            new Team { Name = "Team Secret",     CaptainName = "Jessie Cristy",   Contact = "081234567808", RegisteredAt = today.AddDays(-1),  Status = RegistrationStatus.Rejected, TournamentId = tournament.Id },
        };
        db.Teams.AddRange(teams);
        db.SaveChanges(); // Id tim dibutuhkan untuk FK pertandingan

        // ── Buat roster Valorant (7 peserta: 5 Pemain Inti, 1 Sub, 1 Coach) ──
        db.Players.AddRange(
            // Paper Rex (PRX)
            new Player { TeamId = teams[0].Id, Name = "Jason Susanto",       InGameName = "f0rsakeN",  Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Khalish Rusyaidee",   InGameName = "d4v41",     Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Aaron Leonhart",      InGameName = "mindfreak", Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Wang Jing Jie",       InGameName = "Jinggg",    Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Ilya Petrov",         InGameName = "something", Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Cahya Nugraha",       InGameName = "Monyet",    Role = RosterRole.Sub,   Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Alexandre Sallé",     InGameName = "alecks",    Role = RosterRole.Coach, Position = "", JerseyNumber = 0, IsActive = true },

            // Sentinels (SEN)
            new Player { TeamId = teams[1].Id, Name = "Tyson Ngo",           InGameName = "TenZ",      Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Zachary Patrone",     InGameName = "zekken",    Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Gustavo Rossi",       InGameName = "Sacy",      Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Mohamed Amine",       InGameName = "johnqt",    Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Jordan Montemurro",   InGameName = "Zellsis",   Role = RosterRole.Main,  Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Rahul Nemani",        InGameName = "curry",     Role = RosterRole.Sub,   Position = "", JerseyNumber = 0, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Adam Kaplan",         InGameName = "kaplan",    Role = RosterRole.Coach, Position = "", JerseyNumber = 0, IsActive = true }
        );

        // ── Buat pertandingan Valorant (Skor Format Map Bo3: 2-1, 2-0) ──
        var matches = new[]
        {
            new Match
            {
                TeamAId = teams[0].Id, TeamBId = teams[1].Id, Round = "Group Stage - Opening",
                ScoreA = 2, ScoreB = 1, Status = MatchStatus.Completed, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-1), StartedAt = now.AddDays(-1), FinishedAt = now.AddDays(-1).AddMinutes(90)
            },
            new Match
            {
                TeamAId = teams[2].Id, TeamBId = teams[3].Id, Round = "Group Stage - Opening",
                ScoreA = 2, ScoreB = 0, Status = MatchStatus.Completed, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-1), StartedAt = now.AddDays(-1).AddHours(2), FinishedAt = now.AddDays(-1).AddHours(3).AddMinutes(30)
            },
            new Match
            {
                TeamAId = teams[4].Id, TeamBId = teams[5].Id, Round = "Group Stage - Opening",
                ScoreA = 1, ScoreB = 0, Status = MatchStatus.InProgress, GroupName = "Grup B",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddMinutes(-40), StartedAt = now.AddMinutes(-40)
            },
            new Match
            {
                TeamAId = teams[1].Id, TeamBId = teams[3].Id, Round = "Group Stage - Elimination",
                ScoreA = 0, ScoreB = 1, Status = MatchStatus.InProgress, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddMinutes(-15), StartedAt = now.AddMinutes(-15)
            },
            new Match
            {
                TeamAId = teams[0].Id, TeamBId = teams[2].Id, Round = "Winners Match",
                Status = MatchStatus.NotStarted, TournamentId = tournament.Id,
                ScheduledAt = today.AddDays(1).AddHours(15)
            },
            new Match
            {
                TeamAId = teams[4].Id, TeamBId = teams[1].Id, Round = "Decider Match",
                Status = MatchStatus.NotStarted, TournamentId = tournament.Id,
                ScheduledAt = today.AddDays(1).AddHours(17)
            }
        };
        db.Matches.AddRange(matches);
        db.SaveChanges();

        // ── Buat kejadian contoh untuk pertandingan selesai ──────────
        db.MatchEvents.AddRange(
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, EventType = EventType.Goal,       Minute = 12, Note = "Tendangan bebas" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, EventType = EventType.Goal,       Minute = 34, Note = "" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[1].Id, EventType = EventType.Goal,       Minute = 45, Note = "Umpan balik" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, EventType = EventType.Goal,       Minute = 78, Note = "Penalti" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[1].Id, EventType = EventType.YellowCard, Minute = 55, Note = "" }
        );
        db.SaveChanges();
    }
}

