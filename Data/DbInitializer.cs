using Microsoft.EntityFrameworkCore;
using TournamentManager.Models;

namespace TournamentManager.Data;

public static class DbInitializer
{
    /// <summary>
    /// Menjalankan migrasi/skema database lalu mengisi data contoh turnamen VCT
    /// jika database masih kosong. Jika data sudah ada, memastikan tanggal pertandingan
    /// live & terjadwal tetap segar untuk screenshot.
    /// </summary>
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Teams.Any())
        {
            EnsureFreshTimestamps(db);
            return;
        }

        SeedData(db);
    }

    /// <summary>
    /// Memastikan data pertandingan turnamen contoh VCT memiliki stempel waktu
    /// yang selalu realistis dan segar (mis. pertandingan live selalu berjalan
    /// ~52 dan ~28 menit, jadwal semifinal tetap di masa mendatang) meskipun aplikasi
    /// dijalankan pada hari atau minggu yang berbeda untuk keperluan screenshot.
    /// </summary>
    public static void EnsureFreshTimestamps(AppDbContext db)
    {
        try
        {
            var tournament = db.Tournaments.FirstOrDefault(t => t.Name.Contains("VALORANT Champions Tour"));
            if (tournament == null) return;

            var now = DateTime.Now;
            var today = DateTime.Today;
            bool modified = false;

            // 1. Cek pertandingan InProgress:
            // Jika StartedAt sudah lebih dari 2 jam yang lalu atau dari hari sebelumnya, segarkan kembali.
            var inProgressMatches = db.Matches
                .Where(m => m.TournamentId == tournament.Id && m.Status == MatchStatus.InProgress)
                .OrderBy(m => m.Id)
                .ToList();

            if (inProgressMatches.Count >= 2)
            {
                var match1 = inProgressMatches[0];
                var match2 = inProgressMatches[1];

                if (!match1.StartedAt.HasValue || (now - match1.StartedAt.Value).TotalHours > 2 || match1.StartedAt.Value.Date != today)
                {
                    match1.ScheduledAt = now.AddMinutes(-52);
                    match1.StartedAt = now.AddMinutes(-52);
                    modified = true;
                }

                if (!match2.StartedAt.HasValue || (now - match2.StartedAt.Value).TotalHours > 2 || match2.StartedAt.Value.Date != today)
                {
                    match2.ScheduledAt = now.AddMinutes(-28);
                    match2.StartedAt = now.AddMinutes(-28);
                    modified = true;
                }
            }

            // 2. Cek pertandingan NotStarted:
            // Jika ScheduledAt sudah lewat (di masa lalu), jadwalkan ulang ke besok dan lusa.
            var notStartedMatches = db.Matches
                .Where(m => m.TournamentId == tournament.Id && m.Status == MatchStatus.NotStarted)
                .OrderBy(m => m.Id)
                .ToList();

            if (notStartedMatches.Any(m => m.ScheduledAt < now))
            {
                for (int i = 0; i < notStartedMatches.Count; i++)
                {
                    var m = notStartedMatches[i];
                    if (i == 0) m.ScheduledAt = today.AddDays(1).AddHours(14);
                    else if (i == 1) m.ScheduledAt = today.AddDays(1).AddHours(17);
                    else if (i == 2) m.ScheduledAt = today.AddDays(2).AddHours(14);
                    else if (i == 3) m.ScheduledAt = today.AddDays(2).AddHours(17);
                    modified = true;
                }
            }

            // 3. Cek pertandingan Selesai:
            // Pastikan tidak melompat melebihi batas waktu masuk akal (> 7 hari).
            var completedMatches = db.Matches
                .Where(m => m.TournamentId == tournament.Id && m.Status == MatchStatus.Completed)
                .OrderBy(m => m.Id)
                .ToList();

            if (completedMatches.Any(m => (today - m.ScheduledAt.Date).TotalDays > 7))
            {
                for (int i = 0; i < completedMatches.Count; i++)
                {
                    var m = completedMatches[i];
                    if (i < 2)
                    {
                        var baseDate = today.AddDays(-3);
                        int hour = (i == 0) ? 14 : 17;
                        m.ScheduledAt = baseDate.AddHours(hour);
                        m.StartedAt = baseDate.AddHours(hour);
                        m.FinishedAt = (i == 0) ? baseDate.AddHours(15).AddMinutes(35) : baseDate.AddHours(19);
                    }
                    else if (i < 4)
                    {
                        var baseDate = today.AddDays(-2);
                        int hour = (i == 2) ? 14 : 17;
                        m.ScheduledAt = baseDate.AddHours(hour);
                        m.StartedAt = baseDate.AddHours(hour);
                        m.FinishedAt = (i == 2) ? baseDate.AddHours(15).AddMinutes(20) : baseDate.AddHours(18).AddMinutes(55);
                    }
                    else
                    {
                        var baseDate = today.AddDays(-1);
                        int hour = (i % 2 == 0) ? 14 : 17;
                        m.ScheduledAt = baseDate.AddHours(hour);
                        m.StartedAt = baseDate.AddHours(hour);
                        m.FinishedAt = baseDate.AddHours(hour + 1).AddMinutes(40);
                    }
                    modified = true;
                }
            }

            // 4. Pastikan tanggal turnamen aktif dan konsisten
            if (tournament.StartDate.Date < today.AddDays(-30) || (tournament.EndDate.HasValue && tournament.EndDate.Value < today))
            {
                tournament.StartDate = today.AddDays(-14);
                tournament.EndDate = today.AddDays(21);
                modified = true;
            }

            if (modified)
            {
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnsureFreshTimestamps gagal: {ex.Message}");
        }
    }

    /// <summary>
    /// Menghapus seluruh data turnamen yang ada dan mengisi ulang dengan data contoh
    /// turnamen VCT yang lengkap dan realistis untuk presentasi/screenshot.
    /// </summary>
    public static void ReSeed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        // Hapus data berurutan sesuai relasi Foreign Key agar tidak melanggar constraint
        db.MatchEvents.RemoveRange(db.MatchEvents);
        db.Matches.RemoveRange(db.Matches);
        db.Players.RemoveRange(db.Players);
        db.Teams.RemoveRange(db.Teams);
        db.Tournaments.RemoveRange(db.Tournaments);
        db.SaveChanges();

        SeedData(db);
    }

    private static void SeedData(AppDbContext db)
    {
        var today = DateTime.Today;
        var now = DateTime.Now;

        // ── Buat turnamen contoh ─────────────────────────────────────
        var tournament = new Tournament
        {
            Name = "VALORANT Champions Tour 2026: Pacific Stage 1",
            Description = "Turnamen Resmi VCT Pacific 2026 · Standar Format VLR.gg & Riot Games",
            Type = TournamentType.GroupStage,
            Status = TournamentStatus.Active,
            StartDate = today.AddDays(-14),
            EndDate = today.AddDays(21),
            IsActive = true
        };
        db.Tournaments.Add(tournament);
        db.SaveChanges();

        // ── Buat tim esports Valorant ─────────────────────────────────
        // 8 Approved (Disetujui), 2 Pending (Menunggu), 1 Rejected (Ditolak)
        var teams = new[]
        {
            new Team { Name = "Paper Rex",       CaptainName = "Aaron Leonhart",         Contact = "0812-3456-7801", RegisteredAt = today.AddDays(-14), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Sentinels",       CaptainName = "Tyson Ngo",              Contact = "0812-3456-7802", RegisteredAt = today.AddDays(-13), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Fnatic",          CaptainName = "Jake Howlett",           Contact = "0812-3456-7803", RegisteredAt = today.AddDays(-12), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "DRX",             CaptainName = "Kim Gu-taek",            Contact = "0812-3456-7804", RegisteredAt = today.AddDays(-11), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Gen.G Esports",   CaptainName = "Kim Tae-o",              Contact = "0812-3456-7805", RegisteredAt = today.AddDays(-10), Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Rex Regum Qeon",  CaptainName = "David Monangin",         Contact = "0812-3456-7806", RegisteredAt = today.AddDays(-9),  Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "Team Heretics",   CaptainName = "Ricardas Lukasevicius",  Contact = "0812-3456-7807", RegisteredAt = today.AddDays(-8),  Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "EDward Gaming",   CaptainName = "Guo Haodong",            Contact = "0812-3456-7808", RegisteredAt = today.AddDays(-7),  Status = RegistrationStatus.Approved, TournamentId = tournament.Id },
            new Team { Name = "BOOM Esports",    CaptainName = "Fikri Zaki",             Contact = "0812-3456-7809", RegisteredAt = today.AddDays(-2),  Status = RegistrationStatus.Pending,  TournamentId = tournament.Id },
            new Team { Name = "Talon Esports",   CaptainName = "Thanamethk Paphiakhoun", Contact = "0812-3456-7810", RegisteredAt = today.AddDays(-1),  Status = RegistrationStatus.Pending,  TournamentId = tournament.Id },
            new Team { Name = "Team Secret",     CaptainName = "Jessie Cristy",          Contact = "0812-3456-7811", RegisteredAt = today.AddDays(-3),  Status = RegistrationStatus.Rejected, TournamentId = tournament.Id },
        };
        db.Teams.AddRange(teams);
        db.SaveChanges();

        // ── Buat roster lengkap 7 anggota per tim (5 Pemain Inti, 1 Sub, 1 Coach) ──
        db.Players.AddRange(
            // 1. Paper Rex (PRX)
            new Player { TeamId = teams[0].Id, Name = "Alexandre Salle",       InGameName = "alecks",    Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Jason Susanto",         InGameName = "f0rsakeN",  Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Khalish Rusyaidee",     InGameName = "d4v41",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Aaron Leonhart",        InGameName = "mindfreak", Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Wang Jing Jie",         InGameName = "Jinggg",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Ilya Petrov",           InGameName = "something", Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[0].Id, Name = "Cahya Nugraha",         InGameName = "Monyet",    Role = RosterRole.Sub,   IsActive = true },

            // 2. Sentinels (SEN)
            new Player { TeamId = teams[1].Id, Name = "Adam Kaplan",           InGameName = "kaplan",    Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Tyson Ngo",             InGameName = "TenZ",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Zachary Patrone",       InGameName = "zekken",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Gustavo Rossi",         InGameName = "Sacy",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Mohamed Amine",         InGameName = "johnqt",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Jordan Montemurro",     InGameName = "Zellsis",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[1].Id, Name = "Rahul Nemani",          InGameName = "curry",     Role = RosterRole.Sub,   IsActive = true },

            // 3. Fnatic (FNC)
            new Player { TeamId = teams[2].Id, Name = "Chris Sexton",          InGameName = "Elmapuddy", Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[2].Id, Name = "Jake Howlett",          InGameName = "Boaster",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[2].Id, Name = "Nikita Sirmitev",       InGameName = "Derke",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[2].Id, Name = "Timofey Khromov",       InGameName = "Chronicle", Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[2].Id, Name = "Leo Jannesson",         InGameName = "Leo",       Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[2].Id, Name = "Emir Ali Mumin",        InGameName = "Alfajer",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[2].Id, Name = "Maksym Bilyk",          InGameName = "kamyk",     Role = RosterRole.Sub,   IsActive = true },

            // 4. DRX (DRX)
            new Player { TeamId = teams[3].Id, Name = "Pyeon Seon-ho",         InGameName = "termi",     Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[3].Id, Name = "Kim Gu-taek",           InGameName = "stax",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[3].Id, Name = "Yu Byeong-chul",        InGameName = "BuZz",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[3].Id, Name = "Kim Myeong-kwan",       InGameName = "MaKo",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[3].Id, Name = "Jung Min-hyuk",         InGameName = "Flashback", Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[3].Id, Name = "Cho Min-hyuk",          InGameName = "Foxy9",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[3].Id, Name = "Kang Min-seo",          InGameName = "BeYN",      Role = RosterRole.Sub,   IsActive = true },

            // 5. Gen.G Esports (GEN)
            new Player { TeamId = teams[4].Id, Name = "Kang Geun-chul",        InGameName = "solo",      Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[4].Id, Name = "Kim Tae-o",             InGameName = "Meteor",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[4].Id, Name = "Kim Won-tae",           InGameName = "Karon",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[4].Id, Name = "Byeon Sang-beom",       InGameName = "Munchkin",  Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[4].Id, Name = "Kim Hae-sung",          InGameName = "Lakia",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[4].Id, Name = "Ha Jung-woo",           InGameName = "t3xture",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[4].Id, Name = "Lee Jae-hyeok",         InGameName = "Ash",       Role = RosterRole.Sub,   IsActive = true },

            // 6. Rex Regum Qeon (RRQ)
            new Player { TeamId = teams[5].Id, Name = "Marthinus Jacobus",     InGameName = "Ew0k",      Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[5].Id, Name = "David Monangin",        InGameName = "tehbotoL",  Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[5].Id, Name = "Saibani Rahmad",        InGameName = "fl1pzjder", Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[5].Id, Name = "Hagai Tewuh",           InGameName = "Lmemore",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[5].Id, Name = "Maksim Shepelev",       InGameName = "Jemkin",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[5].Id, Name = "Bryan Setiawan",        InGameName = "Kush",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[5].Id, Name = "Emmanuel Morales",      InGameName = "Emman",     Role = RosterRole.Sub,   IsActive = true },

            // 7. Team Heretics (TH)
            new Player { TeamId = teams[6].Id, Name = "Neil Finlay",           InGameName = "neilzinho", Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[6].Id, Name = "Ricardas Lukasevicius", InGameName = "boo",       Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[6].Id, Name = "Dominykas Lukasevicius",InGameName = "MiniBoo",  Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[6].Id, Name = "Enes Ecik",             InGameName = "RieNs",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[6].Id, Name = "Mert Cebi",             InGameName = "Wo0t",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[6].Id, Name = "Benjy David Fish",      InGameName = "benjyfishy",Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[6].Id, Name = "Patryk Fabrowski",      InGameName = "paTiTek",   Role = RosterRole.Sub,   IsActive = true },

            // 8. EDward Gaming (EDG)
            new Player { TeamId = teams[7].Id, Name = "Tang Zheqian",          InGameName = "Muggle",    Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[7].Id, Name = "Guo Haodong",           InGameName = "Haodong",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[7].Id, Name = "Zheng Yongkang",        InGameName = "ZmjjKK",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[7].Id, Name = "Wang Senxu",            InGameName = "nobody",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[7].Id, Name = "Wan Shunji",            InGameName = "CHICHOO",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[7].Id, Name = "Zhang Zhao",            InGameName = "Smoggy",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[7].Id, Name = "Lin Hongyu",            InGameName = "WoodAy1",   Role = RosterRole.Sub,   IsActive = true },

            // 9. BOOM Esports (BME)
            new Player { TeamId = teams[8].Id, Name = "Ji Dong-jun",           InGameName = "meow",      Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[8].Id, Name = "Fikri Zaki",            InGameName = "famouz",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[8].Id, Name = "Sheldon Andersen",      InGameName = "NcSlasher", Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[8].Id, Name = "Rizkie Adla",           InGameName = "BerserX",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[8].Id, Name = "Hildegard Arnaldo",     InGameName = "Shiro",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[8].Id, Name = "Thomas Budi",           InGameName = "Cud",       Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[8].Id, Name = "Randy Sapoetra",        InGameName = "dray",      Role = RosterRole.Sub,   IsActive = true },

            // 10. Talon Esports (TLN)
            new Player { TeamId = teams[9].Id, Name = "Hector Rosario",        InGameName = "FrosT",     Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[9].Id, Name = "Thanamethk Paphiakhoun",InGameName = "Crws",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[9].Id, Name = "Jitboy Srisuwan",       InGameName = "jitboyS",   Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[9].Id, Name = "Joseph Seung-min Oh",   InGameName = "ban",       Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[9].Id, Name = "Peter Governor",        InGameName = "Governor",  Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[9].Id, Name = "Lionel Lim",            InGameName = "lenne",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[9].Id, Name = "Patiphan Chaiwong",     InGameName = "Patiphan",  Role = RosterRole.Sub,   IsActive = true },

            // 11. Team Secret (TS)
            new Player { TeamId = teams[10].Id, Name = "Chong Hok Hin",        InGameName = "dummy",     Role = RosterRole.Coach, IsActive = true },
            new Player { TeamId = teams[10].Id, Name = "Jessie Cristy",        InGameName = "JessieVash",Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[10].Id, Name = "Adrian Reyes",         InGameName = "invy",      Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[10].Id, Name = "Jim Webster",          InGameName = "BORKUM",    Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[10].Id, Name = "Jeremy Cabrera",       InGameName = "Jremy",     Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[10].Id, Name = "Noel De Guia",         InGameName = "NDG",       Role = RosterRole.Main,  IsActive = true },
            new Player { TeamId = teams[10].Id, Name = "James Go",             InGameName = "Wild0reoo", Role = RosterRole.Sub,   IsActive = true }
        );
        db.SaveChanges();

        // ── Buat Pertandingan (Completed, InProgress, NotStarted) ──
        // Format Skor Best-of-3 (Bo3): 2-0, 2-1, 1-2, 0-2
        var matches = new[]
        {
            // === 8 Pertandingan SELESAI (Completed) ===
            // Grup A - Babak 1
            new Match
            {
                TeamAId = teams[0].Id, TeamBId = teams[3].Id, Round = "Group Stage - Grup A",
                ScoreA = 2, ScoreB = 0, Status = MatchStatus.Completed, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-3).Date.AddHours(14),
                StartedAt = now.AddDays(-3).Date.AddHours(14),
                FinishedAt = now.AddDays(-3).Date.AddHours(15).AddMinutes(35)
            },
            new Match
            {
                TeamAId = teams[1].Id, TeamBId = teams[7].Id, Round = "Group Stage - Grup A",
                ScoreA = 2, ScoreB = 1, Status = MatchStatus.Completed, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-3).Date.AddHours(17),
                StartedAt = now.AddDays(-3).Date.AddHours(17),
                FinishedAt = now.AddDays(-3).Date.AddHours(19)
            },
            // Grup B - Babak 1
            new Match
            {
                TeamAId = teams[4].Id, TeamBId = teams[5].Id, Round = "Group Stage - Grup B",
                ScoreA = 2, ScoreB = 0, Status = MatchStatus.Completed, GroupName = "Grup B",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-2).Date.AddHours(14),
                StartedAt = now.AddDays(-2).Date.AddHours(14),
                FinishedAt = now.AddDays(-2).Date.AddHours(15).AddMinutes(20)
            },
            new Match
            {
                TeamAId = teams[6].Id, TeamBId = teams[2].Id, Round = "Group Stage - Grup B",
                ScoreA = 2, ScoreB = 1, Status = MatchStatus.Completed, GroupName = "Grup B",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-2).Date.AddHours(17),
                StartedAt = now.AddDays(-2).Date.AddHours(17),
                FinishedAt = now.AddDays(-2).Date.AddHours(18).AddMinutes(55)
            },
            // Grup A - Babak 2
            new Match
            {
                TeamAId = teams[0].Id, TeamBId = teams[7].Id, Round = "Group Stage - Grup A",
                ScoreA = 2, ScoreB = 1, Status = MatchStatus.Completed, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-1).Date.AddHours(14),
                StartedAt = now.AddDays(-1).Date.AddHours(14),
                FinishedAt = now.AddDays(-1).Date.AddHours(15).AddMinutes(50)
            },
            new Match
            {
                TeamAId = teams[3].Id, TeamBId = teams[1].Id, Round = "Group Stage - Grup A",
                ScoreA = 2, ScoreB = 0, Status = MatchStatus.Completed, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-1).Date.AddHours(17),
                StartedAt = now.AddDays(-1).Date.AddHours(17),
                FinishedAt = now.AddDays(-1).Date.AddHours(18).AddMinutes(25)
            },
            // Grup B - Babak 2
            new Match
            {
                TeamAId = teams[4].Id, TeamBId = teams[2].Id, Round = "Group Stage - Grup B",
                ScoreA = 2, ScoreB = 1, Status = MatchStatus.Completed, GroupName = "Grup B",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-1).Date.AddHours(14),
                StartedAt = now.AddDays(-1).Date.AddHours(14),
                FinishedAt = now.AddDays(-1).Date.AddHours(16).AddMinutes(5)
            },
            new Match
            {
                TeamAId = teams[6].Id, TeamBId = teams[5].Id, Round = "Group Stage - Grup B",
                ScoreA = 2, ScoreB = 1, Status = MatchStatus.Completed, GroupName = "Grup B",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddDays(-1).Date.AddHours(17),
                StartedAt = now.AddDays(-1).Date.AddHours(17),
                FinishedAt = now.AddDays(-1).Date.AddHours(18).AddMinutes(50)
            },

            // === 2 Pertandingan SEDANG BERLANGSUNG (InProgress) ===
            new Match
            {
                TeamAId = teams[0].Id, TeamBId = teams[1].Id, Round = "Group Stage - Grup A",
                ScoreA = 1, ScoreB = 1, Status = MatchStatus.InProgress, GroupName = "Grup A",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddMinutes(-52),
                StartedAt = now.AddMinutes(-52)
            },
            new Match
            {
                TeamAId = teams[4].Id, TeamBId = teams[6].Id, Round = "Group Stage - Grup B",
                ScoreA = 1, ScoreB = 0, Status = MatchStatus.InProgress, GroupName = "Grup B",
                TournamentId = tournament.Id,
                ScheduledAt = now.AddMinutes(-28),
                StartedAt = now.AddMinutes(-28)
            },

            // === 4 Pertandingan BELUM MULAI (NotStarted) ===
            new Match
            {
                TeamAId = teams[0].Id, TeamBId = teams[6].Id, Round = "Upper Bracket - Semifinal",
                Status = MatchStatus.NotStarted,
                TournamentId = tournament.Id,
                ScheduledAt = today.AddDays(1).AddHours(14)
            },
            new Match
            {
                TeamAId = teams[4].Id, TeamBId = teams[3].Id, Round = "Upper Bracket - Semifinal",
                Status = MatchStatus.NotStarted,
                TournamentId = tournament.Id,
                ScheduledAt = today.AddDays(1).AddHours(17)
            },
            new Match
            {
                TeamAId = teams[1].Id, TeamBId = teams[5].Id, Round = "Lower Bracket - Round 1",
                Status = MatchStatus.NotStarted,
                TournamentId = tournament.Id,
                ScheduledAt = today.AddDays(2).AddHours(14)
            },
            new Match
            {
                TeamAId = teams[2].Id, TeamBId = teams[7].Id, Round = "Lower Bracket - Round 1",
                Status = MatchStatus.NotStarted,
                TournamentId = tournament.Id,
                ScheduledAt = today.AddDays(2).AddHours(17)
            }
        };
        db.Matches.AddRange(matches);
        db.SaveChanges();

        // ── Buat kejadian contoh pertandingan (Match Events) ──────────
        var prxForsaken = db.Players.FirstOrDefault(p => p.TeamId == teams[0].Id && p.InGameName == "f0rsakeN");
        var prxJinggg = db.Players.FirstOrDefault(p => p.TeamId == teams[0].Id && p.InGameName == "Jinggg");
        var prxSomething = db.Players.FirstOrDefault(p => p.TeamId == teams[0].Id && p.InGameName == "something");
        var drxStax = db.Players.FirstOrDefault(p => p.TeamId == teams[3].Id && p.InGameName == "stax");
        var senTenz = db.Players.FirstOrDefault(p => p.TeamId == teams[1].Id && p.InGameName == "TenZ");
        var senZekken = db.Players.FirstOrDefault(p => p.TeamId == teams[1].Id && p.InGameName == "zekken");
        var edgZmjjkk = db.Players.FirstOrDefault(p => p.TeamId == teams[7].Id && p.InGameName == "ZmjjKK");

        db.MatchEvents.AddRange(
            // Event Match 1 (PRX vs DRX)
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, PlayerId = prxForsaken?.Id, EventType = EventType.RoundWin,    Minute = 1,  Note = "Pistol Round Won" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, PlayerId = prxForsaken?.Id, EventType = EventType.Ace,         Minute = 4,  Note = "f0rsakeN Ace (Vandal 1v5)" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[3].Id, PlayerId = drxStax?.Id,    EventType = EventType.SpikeDefuse, Minute = 7,  Note = "stax Ninja Defuse di Smoke" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, PlayerId = prxSomething?.Id,EventType = EventType.Clutch,      Minute = 11, Note = "something 1v3 Operator Clutch" },
            new MatchEvent { MatchId = matches[0].Id, TeamId = teams[0].Id, PlayerId = prxJinggg?.Id,   EventType = EventType.RoundWin,    Minute = 13, Note = "Match Point Won (Bind)" },

            // Event Match 2 (SEN vs EDG)
            new MatchEvent { MatchId = matches[1].Id, TeamId = teams[7].Id, PlayerId = edgZmjjkk?.Id,  EventType = EventType.Ace,         Minute = 2,  Note = "ZmjjKK Operator Clean Ace" },
            new MatchEvent { MatchId = matches[1].Id, TeamId = teams[1].Id, PlayerId = senTenz?.Id,    EventType = EventType.Clutch,      Minute = 6,  Note = "TenZ 1v3 Omen Clutch" },
            new MatchEvent { MatchId = matches[1].Id, TeamId = teams[1].Id, PlayerId = senZekken?.Id,  EventType = EventType.RoundWin,    Minute = 13, Note = "zekken Match Point Kill" },

            // Event Match 9 (Live: PRX vs SEN)
            new MatchEvent { MatchId = matches[8].Id, TeamId = teams[0].Id, PlayerId = prxForsaken?.Id, EventType = EventType.RoundWin,    Minute = 1,  Note = "Pistol Round Won" },
            new MatchEvent { MatchId = matches[8].Id, TeamId = teams[1].Id, PlayerId = senZekken?.Id,  EventType = EventType.Ace,         Minute = 5,  Note = "zekken Raze 5K Ace" },
            new MatchEvent { MatchId = matches[8].Id, TeamId = teams[1].Id, PlayerId = senTenz?.Id,    EventType = EventType.Clutch,      Minute = 9,  Note = "TenZ 1v2 Sheriff Clutch" }
        );
        db.SaveChanges();
    }
}
