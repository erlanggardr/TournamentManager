using MaterialDesignThemes.Wpf;
using TournamentManager.Views;

namespace TournamentManager.Services;

public record ConfirmDialogModel(string Title, string Message);

public interface IDialogService
{
    /// <summary>Menampilkan dialog konfirmasi Material Design dan mengembalikan true jika pengguna memilih "Ya".</summary>
    Task<bool> ConfirmAsync(string title, string message);
}

public class DialogService : IDialogService
{
    // Identifier harus sama dengan DialogHost di MainWindow.xaml
    private const string DialogIdentifier = "RootDialog";

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var view = new ConfirmDialog { DataContext = new ConfirmDialogModel(title, message) };
        var result = await DialogHost.Show(view, DialogIdentifier);
        return result is true;
    }
}
