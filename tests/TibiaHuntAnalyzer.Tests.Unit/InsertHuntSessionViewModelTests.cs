using System.Text;
using System.IO;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class InsertHuntSessionViewModelTests
{
    [Fact]
    public async Task SubmitCommand_ValidatesRequiredFieldsBeforePersistence()
    {
        var sessionService = new FakeHuntSessionService();
        var viewModel = CreateViewModel(sessionService);

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(0, sessionService.SaveCount);
        Assert.Contains("Select a character.", viewModel.StatusMessage);
        Assert.Contains("Select a hunt location.", viewModel.StatusMessage);
        Assert.Contains("positive whole number", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SubmitCommand_PersistsCompleteSessionBeforeReportingSuccess()
    {
        var sessionService = new FakeHuntSessionService
        {
            SaveAction = _ => throw new InvalidOperationException("Database unavailable.")
        };
        var viewModel = CreateViewModel(sessionService);
        await viewModel.LoadAsync();
        viewModel.SelectedCharacter = viewModel.Characters.Single();
        viewModel.SelectedHuntLocation = viewModel.HuntLocations.Single();
        viewModel.CharacterLevelText = "150";
        const string json = """
            {
              "Session end": "2026-10-10T11:00:00Z",
              "Session length": "00:45:00",
              "Session start": "2026-10-10T10:00:00Z"
            }
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await viewModel.LoadJsonFileAsync("hunt.json", stream);

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.True(sessionService.SaveCount == 1, viewModel.StatusMessage);
        Assert.Contains("Database unavailable.", viewModel.StatusMessage);
        Assert.DoesNotContain("success", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);

        sessionService.SaveAction = _ => Task.FromResult(Guid.NewGuid());
        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(2, sessionService.SaveCount);
        Assert.Equal("Hunt session saved successfully.", viewModel.StatusMessage);
        Assert.False(viewModel.HasUnsavedInput);
    }

    [Fact]
    public async Task SubmitCommand_UsesTextParserForCopiedTextFormat()
    {
        var sessionService = new FakeHuntSessionService();
        var viewModel = CreateViewModel(sessionService);
        await viewModel.LoadAsync();
        viewModel.SelectedCharacter = viewModel.Characters.Single();
        viewModel.SelectedHuntLocation = viewModel.HuntLocations.Single();
        viewModel.CharacterLevelText = "150";
        viewModel.SelectedInputFormatIndex = 1;
        viewModel.InputContent = """
            From 2026-10-10, 18:03:54 to 2026-10-10, 18:04:59
            Session: 00:01h
            """;

        await viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(1, sessionService.SaveCount);
        Assert.Equal(HuntInputFormat.Text, sessionService.LastRequest!.ParsedSession.SourceFormat);
    }

    [Fact]
    public async Task LoadJsonFileAsync_ReadsTheSelectedFileContent()
    {
        var viewModel = CreateViewModel(new FakeHuntSessionService());
        const string json = """{"Session length":"00:01h"}""";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        await viewModel.LoadJsonFileAsync("hunt.json", stream);

        Assert.Equal("hunt.json", viewModel.SelectedFileName);
        Assert.Equal(json, viewModel.InputContent);
        Assert.True(viewModel.HasUnsavedInput);
    }

    [Fact]
    public void ExitAndClear_RequireConfirmationBeforeDiscardingInput()
    {
        var viewModel = CreateViewModel(new FakeHuntSessionService());
        var exitCount = 0;
        viewModel.ExitRequested += (_, _) => exitCount++;
        viewModel.InputContent = "unsaved text";

        viewModel.ClearCommand.Execute(null);
        Assert.Equal("unsaved text", viewModel.InputContent);
        Assert.Contains("Select Limpar again", viewModel.StatusMessage);
        viewModel.ClearCommand.Execute(null);
        Assert.Empty(viewModel.InputContent);
        Assert.False(viewModel.HasUnsavedInput);

        viewModel.InputContent = "unsaved text";
        viewModel.ExitCommand.Execute(null);
        Assert.Equal(0, exitCount);
        Assert.Contains("Select Voltar again", viewModel.StatusMessage);

        viewModel.ExitCommand.Execute(null);
        Assert.Equal(1, exitCount);
    }

    private static InsertHuntSessionViewModel CreateViewModel(FakeHuntSessionService sessionService)
    {
        var settings = new FakeSettingsService();
        return new InsertHuntSessionViewModel(
            settings,
            sessionService,
            new JsonHuntSessionParser(),
            new TextHuntSessionParser());
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        private readonly IReadOnlyList<SettingItem> _characters =
        [
            new(Guid.NewGuid(), "Knight", true),
            new(Guid.NewGuid(), "Inactive", false)
        ];

        private readonly IReadOnlyList<SettingItem> _locations =
        [
            new(Guid.NewGuid(), "Banuta", true),
            new(Guid.NewGuid(), "Inactive cave", false)
        ];

        public Task<IReadOnlyList<SettingItem>> GetCharactersAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_characters);

        public Task<IReadOnlyList<SettingItem>> GetHuntLocationsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_locations);

        public Task SaveAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeHuntSessionService : IHuntSessionService
    {
        public int SaveCount { get; private set; }

        public SaveHuntSessionRequest? LastRequest { get; private set; }

        public Func<SaveHuntSessionRequest, Task<Guid>> SaveAction { get; set; } =
            _ => Task.FromResult(Guid.NewGuid());

        public async Task<Guid> SaveAsync(
            SaveHuntSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastRequest = request;
            return await SaveAction(request);
        }

        public Task<HuntSessionDetails?> GetDetailsAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<HuntSessionDetails?>(null);

        public Task<IReadOnlyList<HuntSessionSummary>> GetSummariesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HuntSessionSummary>>([]);
    }
}
