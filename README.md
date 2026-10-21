# Tournament Manager (WPF · .NET 10 · MVVM)

Aplikasi desktop Sistem Informasi Manajemen Turnamen dengan Material Design, CommunityToolkit.Mvvm, dan EF Core SQLite.
Spesifikasi lengkap ada di [`docs/PRD.md`](docs/PRD.md).

## Persyaratan

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 (atau 2022 17.14+) (workload ".NET desktop development") atau `dotnet` CLI

## Menjalankan

```powershell
cd TournamentManager
dotnet restore
dotnet run
```

Saat pertama kali dijalankan, database dibuat otomatis di
`%LocalAppData%\TournamentManager\tournament.db` lengkap dengan data contoh (8 tim, 6 pertandingan).
Hapus file tersebut untuk mengembalikan data ke kondisi awal.

## Struktur

```
App.xaml(.cs)            Tema Material Design, DI container, seed database
MainWindow.xaml(.cs)     Shell: top bar, sidebar, host konten, dialog & snackbar
Models/                  Team, Match, enum status
Data/                    AppDbContext, DbInitializer
Services/                IDialogService (dialog konfirmasi)
ViewModels/              MainWindow, Dashboard, TeamRegistration, MatchManagement
Views/                   Dashboard, TeamRegistration, MatchManagement, ConfirmDialog
Themes/Styles.xaml       Palet warna, gradien, shadow, style kartu/sidebar/tombol jendela
```

## Menambah halaman baru

1. Buat `XxxViewModel` dan daftarkan di `App.ConfigureServices` (`AddTransient`).
2. Buat `XxxView.xaml` (UserControl).
3. Tambahkan `DataTemplate` ViewModel→View di `MainWindow.xaml`.
4. Tambahkan `NavigationItem` baru di `MainWindowViewModel`.
