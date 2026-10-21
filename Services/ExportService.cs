using System.Diagnostics;
using System.IO;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TournamentManager.Models;

namespace TournamentManager.Services;

/// <summary>
/// Layanan ekspor data turnamen ke format Excel (.xlsx) dan PDF.
/// Menggunakan ClosedXML untuk Excel dan QuestPDF untuk PDF.
/// </summary>
public class ExportService
{
    static ExportService()
    {
        // QuestPDF Community License (gratis untuk proyek non-komersial)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Excel ─────────────────────────────────────────────────────────────

    /// <summary>Ekspor daftar tim ke file Excel dan buka hasilnya.</summary>
    public async Task ExportTeamsToExcelAsync(IEnumerable<Team> teams)
    {
        var path = PickSavePath("Daftar Tim", "Excel Files|*.xlsx");
        if (path is null) return;

        await Task.Run(() =>
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Daftar Tim");

            // Header
            string[] headers = ["No", "Nama Tim", "Nama Kapten", "Kontak", "Tgl Daftar", "Status"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data
            int row = 2;
            foreach (var (team, idx) in teams.Select((t, i) => (t, i)))
            {
                ws.Cell(row, 1).Value = idx + 1;
                ws.Cell(row, 2).Value = team.Name;
                ws.Cell(row, 3).Value = team.CaptainName;
                ws.Cell(row, 4).Value = team.Contact;
                ws.Cell(row, 5).Value = team.RegisteredAt.ToString("dd MMM yyyy");
                ws.Cell(row, 6).Value = team.Status switch
                {
                    RegistrationStatus.Approved => "Disetujui",
                    RegistrationStatus.Rejected => "Ditolak",
                    _                           => "Menunggu"
                };

                // Warna status
                ws.Cell(row, 6).Style.Fill.BackgroundColor = team.Status switch
                {
                    RegistrationStatus.Approved => XLColor.FromHtml("#D1FAE5"),
                    RegistrationStatus.Rejected => XLColor.FromHtml("#FEE2E2"),
                    _                           => XLColor.FromHtml("#FEF3C7")
                };

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wb.SaveAs(path);
        });

        OpenFile(path);
    }

    /// <summary>Ekspor jadwal pertandingan ke Excel.</summary>
    public async Task ExportMatchesToExcelAsync(IEnumerable<Match> matches)
    {
        var path = PickSavePath("Jadwal Pertandingan", "Excel Files|*.xlsx");
        if (path is null) return;

        await Task.Run(() =>
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Jadwal Pertandingan");

            string[] headers = ["No", "Babak", "Grup", "Tim A", "Skor A", "Skor B", "Tim B", "Jadwal", "Status"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var (m, idx) in matches.Select((m, i) => (m, i)))
            {
                ws.Cell(row, 1).Value = idx + 1;
                ws.Cell(row, 2).Value = m.Round;
                ws.Cell(row, 3).Value = m.GroupName ?? "-";
                ws.Cell(row, 4).Value = m.TeamAName;
                ws.Cell(row, 5).Value = m.Status == MatchStatus.Completed ? (XLCellValue)m.ScoreA : (XLCellValue)"-";
                ws.Cell(row, 6).Value = m.Status == MatchStatus.Completed ? (XLCellValue)m.ScoreB : (XLCellValue)"-";
                ws.Cell(row, 7).Value = m.TeamBName;
                ws.Cell(row, 8).Value = m.ScheduledAt.ToString("dd MMM yyyy HH:mm");
                ws.Cell(row, 9).Value = m.Status switch
                {
                    MatchStatus.Completed  => "Selesai",
                    MatchStatus.InProgress => "Berjalan",
                    _                      => "Belum Mulai"
                };

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wb.SaveAs(path);
        });

        OpenFile(path);
    }

    /// <summary>Ekspor tabel klasemen ke Excel.</summary>
    public async Task ExportStandingsToExcelAsync(IEnumerable<StandingRow> standings)
    {
        var path = PickSavePath("Klasemen", "Excel Files|*.xlsx");
        if (path is null) return;

        await Task.Run(() =>
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Klasemen");

            string[] headers = ["Pos", "Tim", "M (Match)", "Match W-L", "Win Rate", "Map W-L", "Map Diff (Δ)", "Poin (Pts)"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var s in standings)
            {
                ws.Cell(row, 1).Value = s.Rank;
                ws.Cell(row, 2).Value = s.TeamName;
                ws.Cell(row, 3).Value = s.MatchesPlayed;
                ws.Cell(row, 4).Value = s.MatchRecord;
                ws.Cell(row, 5).Value = s.WinRate;
                ws.Cell(row, 6).Value = s.MapRecord;
                ws.Cell(row, 7).Value = s.MapDiff >= 0 ? $"+{s.MapDiff}" : s.MapDiff.ToString();
                ws.Cell(row, 8).Value = s.Points;
                ws.Cell(row, 8).Style.Font.Bold = true;

                // Warna posisi teratas
                if (s.Rank <= 2)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
                else if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wb.SaveAs(path);
        });

        OpenFile(path);
    }

    // ── PDF ───────────────────────────────────────────────────────────────

    /// <summary>Ekspor daftar tim ke PDF.</summary>
    public async Task ExportTeamsToPdfAsync(IEnumerable<Team> teams)
    {
        var path = PickSavePath("Daftar Tim", "PDF Files|*.pdf");
        if (path is null) return;

        var teamList = teams.ToList();

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(11));

                    page.Header().Element(ComposeHeader("DAFTAR TIM PESERTA TURNAMEN"));

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(30);   // No
                            cols.RelativeColumn(2);    // Nama Tim
                            cols.RelativeColumn(2);    // Kapten
                            cols.RelativeColumn(1.5f); // Kontak
                            cols.ConstantColumn(90);   // Tgl Daftar
                            cols.ConstantColumn(80);   // Status
                        });

                        // Header
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "No", "Nama Tim", "Kapten", "Kontak", "Tgl Daftar", "Status" })
                            {
                                header.Cell().Background("#4F46E5").Padding(5)
                                    .Text(h).FontColor("#FFFFFF").Bold();
                            }
                        });

                        // Rows
                        foreach (var (team, idx) in teamList.Select((t, i) => (t, i)))
                        {
                            var bg = idx % 2 == 0 ? "#FFFFFF" : "#F8FAFC";
                            table.Cell().Background(bg).Padding(4).Text((idx + 1).ToString());
                            table.Cell().Background(bg).Padding(4).Text(team.Name).Bold();
                            table.Cell().Background(bg).Padding(4).Text(team.CaptainName);
                            table.Cell().Background(bg).Padding(4).Text(team.Contact);
                            table.Cell().Background(bg).Padding(4).Text(team.RegisteredAt.ToString("dd MMM yyyy"));
                            var statusBg = team.Status == RegistrationStatus.Approved ? "#D1FAE5"
                                         : team.Status == RegistrationStatus.Rejected ? "#FEE2E2" : "#FEF3C7";
                            var statusLabel = team.Status == RegistrationStatus.Approved ? "Disetujui"
                                            : team.Status == RegistrationStatus.Rejected ? "Ditolak" : "Menunggu";
                            table.Cell().Background(statusBg).Padding(4).Text(statusLabel);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Dicetak pada ");
                        x.Span(DateTime.Now.ToString("dd MMMM yyyy HH:mm")).Bold();
                        x.Span(" · Tournament Manager v2.0");
                    });
                });
            }).GeneratePdf(path);
        });

        OpenFile(path);
    }

    /// <summary>Ekspor jadwal pertandingan ke PDF.</summary>
    public async Task ExportMatchesToPdfAsync(IEnumerable<Match> matches)
    {
        var path = PickSavePath("Jadwal Pertandingan", "PDF Files|*.pdf");
        if (path is null) return;

        var matchList = matches.ToList();

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10));

                    page.Header().Element(ComposeHeader("JADWAL PERTANDINGAN"));

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(30);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.ConstantColumn(60);
                            cols.RelativeColumn(2);
                            cols.ConstantColumn(100);
                            cols.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "No", "Babak", "Tim A", "Skor", "Tim B", "Jadwal", "Status" })
                            {
                                header.Cell().Background("#4F46E5").Padding(5)
                                    .Text(h).FontColor("#FFFFFF").Bold();
                            }
                        });

                        foreach (var (m, idx) in matchList.Select((m, i) => (m, i)))
                        {
                            var bg = idx % 2 == 0 ? "#FFFFFF" : "#F8FAFC";
                            var score = m.Status == MatchStatus.Completed ? $"{m.ScoreA} : {m.ScoreB}" : "-";
                            var status = m.Status switch
                            {
                                MatchStatus.Completed  => "Selesai",
                                MatchStatus.InProgress => "Berjalan",
                                _                      => "Belum Mulai"
                            };

                            table.Cell().Background(bg).Padding(4).Text((idx + 1).ToString());
                            table.Cell().Background(bg).Padding(4).Text(m.Round);
                            table.Cell().Background(bg).Padding(4).Text(m.TeamAName).Bold();
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(score).Bold();
                            table.Cell().Background(bg).Padding(4).Text(m.TeamBName).Bold();
                            table.Cell().Background(bg).Padding(4).Text(m.ScheduledAt.ToString("dd MMM yyyy HH:mm"));
                            table.Cell().Background(bg).Padding(4).Text(status);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Dicetak pada ");
                        x.Span(DateTime.Now.ToString("dd MMMM yyyy HH:mm")).Bold();
                        x.Span(" · Tournament Manager v2.0");
                    });
                });
            }).GeneratePdf(path);
        });

        OpenFile(path);
    }

    /// <summary>Ekspor tabel klasemen ke PDF.</summary>
    public async Task ExportStandingsToPdfAsync(IEnumerable<StandingRow> standings)
    {
        var path = PickSavePath("Klasemen", "PDF Files|*.pdf");
        if (path is null) return;

        var list = standings.ToList();

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10));

                    page.Header().Element(ComposeHeader("TABEL KLASEMEN VALORANT (VLR.GG FORMAT)"));

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(30);   // Pos
                            cols.RelativeColumn(3);    // Tim
                            cols.ConstantColumn(40);   // M
                            cols.RelativeColumn(1.5f); // Match W-L
                            cols.ConstantColumn(60);   // Win Rate
                            cols.RelativeColumn(1.5f); // Map W-L
                            cols.ConstantColumn(60);   // Map Diff
                            cols.ConstantColumn(50);   // Pts
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Pos", "Tim", "M", "Match W-L", "Win %", "Map W-L", "Map Diff", "Pts" })
                            {
                                header.Cell().Background("#4F46E5").Padding(5)
                                    .Text(h).FontColor("#FFFFFF").Bold();
                            }
                        });

                        foreach (var s in list)
                        {
                            var bg = s.Rank <= 2 ? "#D1FAE5" : (s.Rank % 2 == 0 ? "#FFFFFF" : "#F8FAFC");
                            table.Cell().Background(bg).Padding(4).Text(s.Rank.ToString()).Bold();
                            table.Cell().Background(bg).Padding(4).Text(s.TeamName).Bold();
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(s.MatchesPlayed.ToString());
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(s.MatchRecord).Bold();
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(s.WinRate);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(s.MapRecord);
                            var diffColor = s.MapDiff > 0 ? "#047857" : (s.MapDiff < 0 ? "#DC2626" : "#374151");
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text($"{(s.MapDiff > 0 ? "+" : "")}{s.MapDiff}").FontColor(diffColor);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(s.Points.ToString()).Bold();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Dicetak pada ");
                        x.Span(DateTime.Now.ToString("dd MMMM yyyy HH:mm")).Bold();
                        x.Span(" · Valorant Tournament Manager (VLR.gg format)");
                    });
                });
            }).GeneratePdf(path);
        });

        OpenFile(path);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static Action<IContainer> ComposeHeader(string title) => c =>
        c.PaddingBottom(10).Column(col =>
        {
            col.Item().Text(title).FontSize(18).Bold().FontColor("#4F46E5");
            col.Item().Text($"Dibuat oleh Tournament Manager v2.0  ·  {DateTime.Now:dd MMMM yyyy}").FontSize(9).FontColor("#6B7280");
            col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#E5E7EB");
        });

    private static string? PickSavePath(string defaultName, string filter)
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"{defaultName}_{DateTime.Now:yyyyMMdd_HHmm}",
            Filter = $"{filter}|All Files|*.*",
            AddExtension = true
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* file mungkin terkunci, abaikan */ }
    }
}

/// <summary>Satu baris dalam tabel klasemen Valorant (format standar VLR.gg).</summary>
public record StandingRow(
    int    Rank,
    int    TeamId,
    string TeamName,
    int    MatchesPlayed, // Total Series/Match (M)
    int    MatchesWon,    // Series Won (W)
    int    MatchesLost,   // Series Lost (L)
    string MatchRecord,   // "W - L"
    string WinRate,       // "100%", "75%", dsb.
    int    MapsWon,       // Total Map Menang
    int    MapsLost,      // Total Map Kalah
    string MapRecord,     // "Map W - L"
    int    MapDiff,       // Selisih Map (Δ)
    int    Points         // Poin (1 Match Menang = 1 Poin)
);
