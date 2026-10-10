using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        _viewModel = null!;
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        AttachToInsertViewModel();
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
        {
            AttachToInsertViewModel();
        }
    }

    private void AttachToInsertViewModel()
    {
        if (_viewModel.CurrentViewModel is InsertHuntSessionViewModel insertViewModel)
        {
            insertViewModel.FileSelectionRequested += async (_, _) =>
                await SelectJsonFileAsync(insertViewModel);
        }

        if (_viewModel.CurrentViewModel is SessionQueryViewModel queryViewModel)
        {
            queryViewModel.CsvImportRequested += async (_, _) =>
                await SelectCsvFileAsync(queryViewModel);
            queryViewModel.CsvExportRequested += async (_, _) =>
                await SaveCsvFileAsync(queryViewModel);
        }
    }

    private async Task SelectJsonFileAsync(InsertHuntSessionViewModel viewModel)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a Tibia Hunt Analyzer JSON file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON files")
                    {
                        Patterns = ["*.json"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            await using var stream = await files[0].OpenReadAsync();
            await viewModel.LoadJsonFileAsync(files[0].Name, stream);
        }
        catch (Exception exception)
        {
            viewModel.ReportFileError(exception.Message);
        }
    }

    private async Task SelectCsvFileAsync(SessionQueryViewModel viewModel)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a Tibia Hunt Analyzer CSV file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("CSV files")
                    {
                        Patterns = ["*.csv"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            await viewModel.ImportCsvAsync(reader);
        }
        catch (Exception exception)
        {
            viewModel.ReportCsvError(exception.Message);
        }
    }

    private async Task SaveCsvFileAsync(SessionQueryViewModel viewModel)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Tibia Hunt Analyzer sessions",
                SuggestedFileName = "tibia-hunt-sessions.csv",
                DefaultExtension = "csv",
                FileTypeChoices =
                [
                    new FilePickerFileType("CSV files")
                    {
                        Patterns = ["*.csv"]
                    }
                ]
            });

            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            await viewModel.ExportCsvAsync(writer);
        }
        catch (Exception exception)
        {
            viewModel.ReportCsvError(exception.Message);
        }
    }
}