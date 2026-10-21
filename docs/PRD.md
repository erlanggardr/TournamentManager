# PRD — Sistem Informasi Manajemen Turnamen

| | |
|---|---|
| **Versi dokumen** | 1.0 (Draft) |
| **Tanggal** | 21 Oktober 2026 |
| **Platform** | Desktop Windows — C# / .NET 10 / WPF |
| **Arsitektur** | MVVM (CommunityToolkit.Mvvm) + EF Core (SQLite) |
| **UI Library** | MaterialDesignInXamlToolkit 5.x |

---

## 1. Ringkasan

**Sistem Informasi Manajemen Turnamen** adalah aplikasi desktop untuk membantu panitia mengelola turnamen skala kecil–menengah (olahraga/e-sport): mendaftarkan tim peserta, memvalidasi pendaftaran, menjadwalkan pertandingan, mencatat skor, dan memantau progres turnamen dari satu dashboard.

Aplikasi berjalan **offline** dengan database lokal SQLite, sehingga mudah dipasang di laptop panitia tanpa server.

## 2. Latar Belakang & Masalah

Panitia turnamen umumnya mencatat pendaftaran dan jadwal di spreadsheet atau chat. Akibatnya:

- Data tim tersebar dan rawan duplikat/salah ketik.
- Status validasi pendaftaran tidak jelas (siapa sudah disetujui, siapa belum).
- Skor dan status pertandingan sulit dipantau saat banyak pertandingan berjalan bersamaan.
- Tidak ada ringkasan cepat untuk panitia/pimpinan acara.

## 3. Tujuan & Metrik Keberhasilan

| Tujuan | Indikator keberhasilan |
|---|---|
| Pendaftaran tim cepat dan bebas duplikat | Mendaftarkan 1 tim ≤ 30 detik; nama tim duplikat selalu ditolak |
| Validasi pendaftaran terpusat | 100% tim memiliki status (Menunggu / Disetujui / Ditolak) |
| Pemantauan pertandingan real-time | Dashboard menampilkan perubahan skor ≤ 5 detik |
| UI modern dan nyaman dipakai | Skor kepuasan uji pengguna (SUS) ≥ 80; transisi halaman halus (≥ 60 fps pada PC standar) |

**Non-tujuan (v1.0):** bracket/klasemen otomatis, multi-user/jaringan, pembayaran, aplikasi web/mobile.

## 4. Pengguna Target

| Persona | Kebutuhan utama |
|---|---|
| **Admin/Panitia Pendaftaran** | Input data tim, edit kesalahan, approve/reject pendaftaran |
| **Operator Pertandingan** | Menjadwalkan pertandingan, memulai/mengakhiri, update skor |
| **Ketua Panitia** | Melihat ringkasan jumlah tim dan progres pertandingan |

Pada v1.0 aplikasi digunakan oleh satu pengguna pada satu perangkat (tanpa login).

## 5. Lingkup Fitur (Functional Requirements)

Prioritas: **P0** wajib rilis v1.0, **P1** penting, **P2** nice-to-have.

### 5.1 Shell & Navigasi

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-SH-01 | Sidebar navigasi dengan menu Dashboard, Registrasi Tim, Manajemen Pertandingan; menu aktif ditandai | P0 |
| FR-SH-02 | Top bar menampilkan judul aplikasi dan tombol Minimize, Maximize/Restore, Close | P0 |
| FR-SH-03 | Jendela tanpa border sistem tetapi tetap dapat di-resize, di-drag, dan snap (WindowChrome) | P0 |
| FR-SH-04 | Animasi transisi halus saat berpindah halaman | P0 |
| FR-SH-05 | Notifikasi singkat (snackbar) untuk hasil aksi; dialog konfirmasi untuk aksi destruktif | P0 |

### 5.2 Dashboard

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-DB-01 | Card **Total Pendaftar**: jumlah seluruh tim terdaftar | P0 |
| FR-DB-02 | Card **Pertandingan On Progress**: jumlah pertandingan berstatus berjalan | P0 |
| FR-DB-03 | Card **Pertandingan Selesai**: jumlah pertandingan berstatus selesai | P0 |
| FR-DB-04 | Tabel **Pertandingan Sedang Berjalan** (babak, Tim A, skor, Tim B, jam mulai, durasi) | P0 |
| FR-DB-05 | Data diperbarui otomatis tiap 5 detik tanpa aksi pengguna; menampilkan jam pembaruan terakhir | P0 |
| FR-DB-06 | Empty state bila tidak ada pertandingan berjalan | P1 |

### 5.3 Registrasi Tim

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-TM-01 | Form: Nama Tim, Nama Kapten, Kontak, Tanggal Daftar | P0 |
| FR-TM-02 | Validasi: semua field wajib; nama/kapten 3–100 karakter; kontak 8–16 karakter angka/`+`/`-`/spasi; pesan error tampil di bawah field | P0 |
| FR-TM-03 | Nama tim harus unik (tanpa memperhatikan spasi di tepi) | P0 |
| FR-TM-04 | DataGrid daftar tim: nama, kapten, kontak, tanggal daftar, status, aksi | P0 |
| FR-TM-05 | **Edit**: data tim dimuat ke form, tombol berubah menjadi "Simpan Perubahan", ada "Batal Edit" | P0 |
| FR-TM-06 | **Delete** dengan dialog konfirmasi; ditolak jika tim sudah dipakai dalam pertandingan | P0 |
| FR-TM-07 | **Approve / Reject** pendaftaran; tim baru berstatus *Menunggu*; chip status berwarna | P0 |
| FR-TM-08 | Pencarian daftar berdasarkan nama tim atau kapten | P1 |

### 5.4 Manajemen Pertandingan

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-MT-01 | Kanban tiga kolom: **Belum Mulai**, **On Progress**, **Selesai**, masing-masing dengan jumlah kartu | P0 |
| FR-MT-02 | Kartu menampilkan Tim A vs Tim B, skor sementara, babak, jadwal, dan status | P0 |
| FR-MT-03 | Form jadwalkan pertandingan: pilih Tim A, Tim B (hanya tim **Disetujui**), babak, tanggal, jam | P0 |
| FR-MT-04 | Validasi: Tim A ≠ Tim B; kedua tim wajib dipilih; babak wajib diisi | P0 |
| FR-MT-05 | Aksi **Mulai** memindahkan kartu ke On Progress dan mencatat waktu mulai | P0 |
| FR-MT-06 | Tombol +/− skor per tim pada pertandingan On Progress; skor tidak boleh negatif; tersimpan otomatis | P0 |
| FR-MT-07 | Aksi **Akhiri** (dengan konfirmasi) memindahkan kartu ke Selesai dan menampilkan pemenang/seri | P0 |
| FR-MT-08 | Pertandingan Belum Mulai dapat dibatalkan (hapus jadwal) | P1 |
| FR-MT-09 | Drag & drop kartu antar kolom | P2 |

### 5.5 Alur Status

```
Tim:         Menunggu ──approve──▶ Disetujui
                 └─────reject───▶ Ditolak        (dapat diubah kembali oleh admin)

Pertandingan: Belum Mulai ──Mulai──▶ On Progress ──Akhiri──▶ Selesai
```

## 6. Kebutuhan Non-Fungsional

| Kategori | Kebutuhan |
|---|---|
| **Performa** | Startup ≤ 3 detik; navigasi antar halaman ≤ 500 ms pada ≤ 1.000 tim dan ≤ 5.000 pertandingan |
| **Responsivitas UI** | Seluruh akses database asinkron (`async/await`); UI tidak freeze |
| **Keandalan** | Kegagalan refresh/penyimpanan menampilkan pesan, bukan menutup aplikasi |
| **Penyimpanan** | SQLite di `%LocalAppData%\TournamentManager\tournament.db`; dibuat otomatis beserta data contoh |
| **Kegunaan** | Ukuran minimum jendela 1080×640; kontras teks memenuhi WCAG AA; tooltip pada tombol ikon |
| **Lokalisasi** | Bahasa antarmuka Indonesia; format tanggal/angka `id-ID` |
| **Kemudahan pemeliharaan** | MVVM murni (code-behind hanya kontrol jendela); dependency injection; ViewModel dapat diuji unit |
| **Kompatibilitas** | Windows 10 (1809+) / Windows 11, .NET 10 Desktop Runtime |

## 7. Desain UI/UX

- **Gaya:** Material Design 3, warna dasar Indigo/Deep Purple, latar abu-biru muda `#F4F6FB`, kartu putih.
- **Rounded corners:** kartu 16–20 px, tombol 10–12 px, kolom kanban 18 px.
- **Drop shadow:** efek bayangan lembut pada kartu dan top bar.
- **Sidebar:** gradien indigo gelap, item aktif ber-highlight transparan.
- **Summary card:** gradien berbeda per kategori (indigo = tim, amber = berjalan, emerald = selesai) dengan ikon dalam lingkaran semi-transparan.
- **Animasi:** transisi *slide-in from bottom + fade* tiap pergantian halaman; indikator LIVE berkedip pada tabel pertandingan berjalan.
- **Umpan balik:** snackbar untuk sukses/gagal, dialog modal untuk konfirmasi hapus dan akhiri pertandingan.
- **Status berwarna:** kuning = menunggu/On Progress, hijau = disetujui/selesai, merah = ditolak, abu = belum mulai.

## 8. Arsitektur Teknis

### 8.1 Struktur Proyek

```
TournamentManager/
├── App.xaml / App.xaml.cs          # Resource tema, konfigurasi DI, seed database
├── MainWindow.xaml / .cs           # Shell: top bar, sidebar, content host, DialogHost, Snackbar
├── Models/                         # Team, Match, enum status
├── Data/                           # AppDbContext, DbInitializer (seed)
├── Services/                       # IDialogService (konfirmasi Material Design)
├── ViewModels/                     # MainWindow, Dashboard, TeamRegistration, MatchManagement
├── Views/                          # DashboardView, TeamRegistrationView, MatchManagementView, ConfirmDialog
├── Themes/Styles.xaml              # Warna, gradien, shadow, style kartu/sidebar/tombol jendela
└── docs/PRD.md
```

### 8.2 Pola Utama

- **Navigasi:** `MainWindowViewModel.SelectedMenuItem` → `CurrentViewModel` (di-resolve via DI) → `DataTemplate` ViewModel→View di `MainWindow.xaml`, dibungkus `TransitioningContent` untuk animasi.
- **Data access:** `IDbContextFactory<AppDbContext>`; satu `DbContext` per operasi (aman untuk aplikasi desktop berumur panjang).
- **Validasi form:** `ObservableValidator` + atribut DataAnnotations + `INotifyDataErrorInfo`.
- **Real-time dashboard:** `DispatcherTimer` 5 detik; ViewModel `IDisposable` agar timer berhenti saat berpindah halaman.
- **Commands:** `[RelayCommand]` (sync/async) dari CommunityToolkit.Mvvm; binding aksi baris DataGrid/kartu memakai `RelativeSource AncestorType=UserControl`.

### 8.3 Model Data

**Team**

| Kolom | Tipe | Aturan |
|---|---|---|
| Id | int | PK |
| Name | string(100) | Wajib, unik |
| CaptainName | string(100) | Wajib |
| Contact | string(20) | Wajib |
| RegisteredAt | DateTime | Wajib |
| Status | enum | Pending / Approved / Rejected |

**Match**

| Kolom | Tipe | Aturan |
|---|---|---|
| Id | int | PK |
| TeamAId, TeamBId | int | FK → Team (Restrict delete) |
| Round | string(60) | Wajib |
| ScoreA, ScoreB | int | ≥ 0 |
| Status | enum | NotStarted / InProgress / Completed |
| ScheduledAt | DateTime | Wajib |
| StartedAt, FinishedAt | DateTime? | Diisi saat Mulai / Akhiri |

### 8.4 Dependensi

| Paket | Versi |
|---|---|
| CommunityToolkit.Mvvm | 8.3.2 |
| MaterialDesignThemes | 5.1.0 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.0 |
| Microsoft.Extensions.DependencyInjection | 10.0.0 |

## 9. Kriteria Penerimaan (Contoh Skenario)

1. **Registrasi:** Saat form kosong disimpan, setiap field menampilkan pesan error dan tidak ada data tersimpan. Saat form valid disimpan, tim muncul di DataGrid berstatus *Menunggu*, form dikosongkan, dan snackbar tampil.
2. **Duplikat:** Mendaftarkan nama tim yang sudah ada menampilkan snackbar penolakan dan tidak menambah data.
3. **Approve:** Menekan Approve mengubah chip menjadi *Disetujui*, tombol Approve hilang, dan tim muncul pada pilihan Tim A/B di halaman pertandingan.
4. **Hapus tim:** Tim yang sudah terdaftar di pertandingan tidak dapat dihapus dan pengguna diberi tahu alasannya.
5. **Pertandingan:** Memilih Tim A = Tim B ditolak. Menjadwalkan pertandingan valid menambah kartu di kolom *Belum Mulai*.
6. **Mulai & skor:** Tombol Mulai memindahkan kartu ke *On Progress*; tombol +/− mengubah skor seketika dan tetap tersimpan setelah aplikasi dibuka ulang; skor tidak turun di bawah 0.
7. **Akhiri:** Setelah konfirmasi, kartu pindah ke *Selesai* dan menampilkan pemenang atau "Hasil seri".
8. **Dashboard:** Angka pada tiga card sesuai jumlah data; pertandingan yang baru dimulai muncul di tabel dalam ≤ 5 detik tanpa refresh manual.
9. **Jendela:** Minimize, Maximize/Restore, Close, drag top bar, resize tepi, dan snap Windows berfungsi.

## 10. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|---|---|---|
| Versi MaterialDesignThemes berubah (nama style/key) | Build/XAML error | Kunci versi paket; perbarui key style saat upgrade |
| Polling 5 detik membebani DB pada data besar | UI lambat | Query hanya status berjalan + `COUNT`; interval dapat disesuaikan |
| Data hilang karena file DB terhapus | Kehilangan data turnamen | Roadmap: tombol backup/export; lokasi DB terdokumentasi |
| Dua operator mengedit bersamaan (jika file dibagi) | Konflik data | v1.0 single-user; v2 beralih ke DB server |

## 11. Roadmap

| Rilis | Isi |
|---|---|
| **v1.0** | Seluruh fitur P0 dan P1 pada dokumen ini |
| **v1.1** | Drag & drop kanban, export daftar tim/pertandingan ke Excel/PDF, backup/restore database |
| **v1.2** | Klasemen otomatis dan pembuatan bracket (gugur / grup) |
| **v2.0** | Login & peran pengguna, mode multi-user (database server), tampilan layar publik (scoreboard) |

## 12. Pertanyaan Terbuka

1. Apakah satu turnamen saja per database, atau perlu banyak turnamen/musim dalam satu aplikasi?
2. Apakah tim perlu daftar pemain (roster) dan batas jumlah anggota?
3. Aturan skor khusus (mis. babak/set, perpanjangan waktu, adu penalti) yang harus didukung?
4. Apakah diperlukan cetak/export kartu peserta atau jadwal?
