using Microsoft.EntityFrameworkCore;
using TournamentManager.Models;

namespace TournamentManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Tournament ──────────────────────────────────────────────────
        modelBuilder.Entity<Tournament>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(150);
            e.Property(t => t.Description).HasMaxLength(500);
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ── Team ────────────────────────────────────────────────────────
        modelBuilder.Entity<Team>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.CaptainName).IsRequired().HasMaxLength(100);
            e.Property(t => t.Contact).IsRequired().HasMaxLength(20);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.LogoPath).HasMaxLength(500);
            e.HasIndex(t => t.Name).IsUnique();

            // Relasi opsional ke Tournament (tim bisa berdiri sendiri)
            e.HasOne(t => t.Tournament)
             .WithMany(to => to.Teams)
             .HasForeignKey(t => t.TournamentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Match ───────────────────────────────────────────────────────
        modelBuilder.Entity<Match>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Round).IsRequired().HasMaxLength(60);
            e.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.GroupName).HasMaxLength(50);

            // Dua FK ke tabel yang sama → Restrict agar menghapus tim tidak diam-diam menghapus pertandingan
            e.HasOne(m => m.TeamA).WithMany().HasForeignKey(m => m.TeamAId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.TeamB).WithMany().HasForeignKey(m => m.TeamBId).OnDelete(DeleteBehavior.Restrict);

            // Relasi opsional ke Tournament
            e.HasOne(m => m.Tournament)
             .WithMany(to => to.Matches)
             .HasForeignKey(m => m.TournamentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Player ──────────────────────────────────────────────────────
        modelBuilder.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.InGameName).HasMaxLength(50);
            e.Property(p => p.Position).HasMaxLength(50);
            e.Property(p => p.Role).HasConversion<string>().HasMaxLength(20);

            e.HasOne(p => p.Team)
             .WithMany(t => t.Players)
             .HasForeignKey(p => p.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MatchEvent ──────────────────────────────────────────────────
        modelBuilder.Entity<MatchEvent>(e =>
        {
            e.HasKey(me => me.Id);
            e.Property(me => me.EventType).HasConversion<string>().HasMaxLength(20);
            e.Property(me => me.Note).HasMaxLength(200);

            e.HasOne(me => me.Match)
             .WithMany(m => m.Events)
             .HasForeignKey(me => me.MatchId)
             .OnDelete(DeleteBehavior.Cascade);

            // Tim opsional
            e.HasOne(me => me.Team)
             .WithMany()
             .HasForeignKey(me => me.TeamId)
             .OnDelete(DeleteBehavior.NoAction);

            // Pemain opsional
            e.HasOne(me => me.Player)
             .WithMany()
             .HasForeignKey(me => me.PlayerId)
             .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
