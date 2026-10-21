using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace TournamentManager.Services;

/// <summary>
/// Layanan backup dan restore database SQL Server.
/// Backup menggunakan perintah BACKUP DATABASE TO DISK (SQL Server native).
/// </summary>
public class BackupService
{
    private readonly string _connectionString;

    public BackupService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Menjalankan BACKUP DATABASE ke file .bak yang dipilih pengguna.
    /// Mengembalikan path file backup jika berhasil, null jika dibatalkan.
    /// </summary>
    public async Task<string?> BackupAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Pilih Lokasi Backup Database",
            FileName = $"TournamentManagerBackup_{DateTime.Now:yyyyMMdd_HHmm}",
            Filter = "SQL Server Backup|*.bak|All Files|*.*",
            AddExtension = true
        };

        if (dlg.ShowDialog() != true) return null;

        var backupPath = dlg.FileName;

        await Task.Run(async () =>
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Dapatkan nama database dari connection string
            var dbName = conn.Database;

            var sql = $"""
                BACKUP DATABASE [{dbName}]
                TO DISK = N'{backupPath}'
                WITH FORMAT, INIT,
                     NAME = N'Tournament Manager Backup',
                     STATS = 10;
                """;

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 300 };
            await cmd.ExecuteNonQueryAsync();
        });

        return backupPath;
    }

    /// <summary>
    /// Restore database dari file .bak yang dipilih pengguna.
    /// PERHATIAN: Ini akan MENIMPA database yang ada. Gunakan dengan hati-hati.
    /// </summary>
    public async Task RestoreAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Pilih File Backup untuk Direstore",
            Filter = "SQL Server Backup|*.bak|All Files|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        var backupPath = dlg.FileName;
        if (!File.Exists(backupPath)) return;

        await Task.Run(async () =>
        {
            // Buka koneksi ke master untuk operasi restore
            var builder = new SqlConnectionStringBuilder(_connectionString)
            {
                InitialCatalog = "master"
            };

            await using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();

            var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;

            // Putus koneksi aktif ke database sebelum restore
            var killSql = $"""
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{dbName}')
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                END
                """;

            await using (var killCmd = new SqlCommand(killSql, conn) { CommandTimeout = 60 })
                await killCmd.ExecuteNonQueryAsync();

            // Restore database
            var restoreSql = $"""
                RESTORE DATABASE [{dbName}]
                FROM DISK = N'{backupPath}'
                WITH REPLACE, STATS = 10;
                """;

            await using (var restoreCmd = new SqlCommand(restoreSql, conn) { CommandTimeout = 600 })
                await restoreCmd.ExecuteNonQueryAsync();

            // Kembalikan ke multi-user
            var multiSql = $"ALTER DATABASE [{dbName}] SET MULTI_USER;";
            await using (var multiCmd = new SqlCommand(multiSql, conn) { CommandTimeout = 60 })
                await multiCmd.ExecuteNonQueryAsync();
        });
    }
}
