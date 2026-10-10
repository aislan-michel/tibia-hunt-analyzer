using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class SettingEntryViewModel(SettingItem item) : ObservableObject
{
    public Guid? Id { get; } = item.Id;

    [ObservableProperty]
    public partial string Name { get; set; } = item.Name;

    [ObservableProperty]
    public partial bool IsActive { get; set; } = item.IsActive;

    public SettingItem ToSettingItem() => new(Id, Name, IsActive);
}
