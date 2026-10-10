using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaHuntAnalyzer.Application.Backups;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class BackupRestoreViewModel(IDatabaseBackupService backupService) : ViewModelBase
{
    [ObservableProperty]
    public partial bool ConfirmRestore { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public event EventHandler? BackupRequested;

    public event EventHandler? RestoreRequested;

    public event EventHandler? ExitRequested;

    [RelayCommand]
    private void CreateBackup() => BackupRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void RestoreBackup()
    {
        if (!ConfirmRestore)
        {
            StatusMessage = "Confirm that you understand restoring replaces the current database.";
            return;
        }

        RestoreRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    public async Task CreateBackupAsync(Stream destination)
    {
        try
        {
            await backupService.CreateBackupAsync(destination);
            StatusMessage = "Database backup created successfully.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not create database backup: {exception.Message}";
        }
    }

    public async Task RestoreBackupAsync(Stream source)
    {
        try
        {
            if (!ConfirmRestore)
            {
                StatusMessage = "Confirm that you understand restoring replaces the current database.";
                return;
            }

            var safetyBackupPath = await backupService.RestoreBackupAsync(source);
            StatusMessage = $"Database restored. Your previous data was backed up to: {safetyBackupPath}";
            ConfirmRestore = false;
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not restore database: {exception.Message}";
        }
    }

    public void ReportFileError(string message) =>
        StatusMessage = $"Database file operation failed: {message}";
}
