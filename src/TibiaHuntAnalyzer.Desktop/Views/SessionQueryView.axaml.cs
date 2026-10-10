using Avalonia.Controls;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Desktop.Views;

public partial class SessionQueryView : UserControl
{
    public SessionQueryView()
    {
        InitializeComponent();
    }

    private void OnSortCharacterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        SortBy(SessionSortField.Character);

    private void OnSortLevelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        SortBy(SessionSortField.Level);

    private void OnSortLocationClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        SortBy(SessionSortField.HuntLocation);

    private void OnSortStartClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        SortBy(SessionSortField.Start);

    private void OnSortEndClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        SortBy(SessionSortField.End);

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SessionQueryViewModel viewModel &&
            viewModel.SelectedSession is not null &&
            !viewModel.IsDetailsVisible)
        {
            _ = viewModel.OpenDetailsCommand.ExecuteAsync(null);
        }
    }

    private void SortBy(SessionSortField field)
    {
        if (DataContext is SessionQueryViewModel viewModel)
        {
            viewModel.SortByCommand.Execute(field);
        }
    }
}
