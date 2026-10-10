using System;
using CommunityToolkit.Mvvm.Input;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class MessageViewModel(string message, Action close) : ViewModelBase
{
    public string Message { get; } = message;

    [RelayCommand]
    private void Close() => close();
}
