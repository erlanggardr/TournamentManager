using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentManager.Models;
using TournamentManager.ViewModels;

namespace TournamentManager.Views;

public partial class MatchManagementView : UserControl
{
    private Point _dragStartPoint;

    public MatchManagementView()
    {
        InitializeComponent();
    }

    private void OnCardPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !IsInsideButton(e.OriginalSource as DependencyObject))
        {
            _dragStartPoint = e.GetPosition(null);
        }
    }

    private void OnCardPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || IsInsideButton(e.OriginalSource as DependencyObject)) return;

        Point currentPosition = e.GetPosition(null);
        Vector diff = _dragStartPoint - currentPosition;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (sender is FrameworkElement element && element.DataContext is Match match)
            {
                DragDrop.DoDragDrop(element, match, DragDropEffects.Move);
            }
        }
    }

    private static bool IsInsideButton(DependencyObject? obj)
    {
        while (obj != null)
        {
            if (obj is System.Windows.Controls.Primitives.ButtonBase) return true;
            if (obj is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D)
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            else
                obj = LogicalTreeHelper.GetParent(obj);
        }
        return false;
    }

    private void OnColumnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Match)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnColumnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(Match)) is Match match &&
            sender is FrameworkElement col &&
            DataContext is MatchManagementViewModel vm)
        {
            var tag = col.Tag?.ToString();
            if (tag == "InProgress")
            {
                _ = vm.HandleMatchDroppedAsync(match, MatchStatus.InProgress);
            }
            else if (tag == "Completed")
            {
                _ = vm.HandleMatchDroppedAsync(match, MatchStatus.Completed);
            }
        }
        e.Handled = true;
    }
}
