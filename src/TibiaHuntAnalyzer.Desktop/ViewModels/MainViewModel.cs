using CommunityToolkit.Mvvm.ComponentModel;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Tibia Hunt Analyzer";
}
