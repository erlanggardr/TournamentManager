using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentManager.Data;
using TournamentManager.Services;
using TournamentManager.ViewModels;

namespace TournamentManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Format tanggal & angka mengikuti budaya Indonesia (juga untuk StringFormat di XAML)
        var culture = new CultureInfo("id-ID");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Pastikan database ada + isi data contoh saat pertama kali dijalankan
        using (var db = Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
        {
            DbInitializer.Seed(db);
        }

        var window = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainWindowViewModel>()
        };
        window.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=TournamentManagerDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString));

        services.AddSingleton<ISnackbarMessageQueue>(new SnackbarMessageQueue(TimeSpan.FromSeconds(3)));
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton(new BackupService(connectionString));

        services.AddSingleton<MainWindowViewModel>();
        // Transient: setiap navigasi membuat ViewModel baru sehingga data selalu segar
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TeamRegistrationViewModel>();
        services.AddTransient<MatchManagementViewModel>();
        services.AddTransient<StandingsViewModel>();
        services.AddTransient<BracketViewModel>();
        services.AddTransient<PlayerRosterViewModel>();
    }
}
