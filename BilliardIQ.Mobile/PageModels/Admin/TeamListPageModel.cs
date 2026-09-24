using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilliardIQ.Mobile.PageModels.Admin;

public partial class TeamListPageModel : BasePageModel
{
    private readonly TeamSession _session;
    private readonly TeamRepository _repository;
    private readonly IRaspberryPiConnectionService _connection;
    private readonly IErrorHandler _errorHandler;

    public TeamListPageModel(TeamSession session, TeamRepository repository, IRaspberryPiConnectionService connection, IErrorHandler errorHandler)
    {
        _session = session;
        _repository = repository;
        _connection = connection;
        _errorHandler = errorHandler;

        ErrorsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(HasNameError));
        };
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (_connection.State != PiConnectionState.Connected)
            await Shell.Current.GoToAsync("//connect");
    }

    public ObservableCollection<ScoreboardTeam> Teams => _session.Teams;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(SaveButtonText))]
    public partial int? EditingTeamId { get; set; }

    public bool IsEditing => EditingTeamId is not null;
    public string SaveButtonText => IsEditing ? L["Action_Update"] : L["Admin_AddTeam"];

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Team name is required")]
    [NotifyCanExecuteChangedFor(nameof(AddTeamCommand))]
    public partial string? Name { get; set; }

    [RelayCommand]
    private void SelectTeam(ScoreboardTeam team)
    {
        EditingTeamId = team.Id;
        Name = team.Name;
        ClearErrors();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingTeamId = null;
        Name = null;
        ClearErrors();
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddTeam()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        var name = Name!.Trim();
        if (await SendTeamToRemoteAsync(name) is { } team)
        {
            var existing = EditingTeamId is not null ? _session.FindById(team.Id) : null;
            if (existing is not null)
                existing.Name = team.Name;
            else
                _session.Teams.Add(team);

            await _repository.UpsertAsync(team);
            EditingTeamId = null;
            Name = null;
        }
    }

    private async Task<ScoreboardTeam?> SendTeamToRemoteAsync(string name)
    {
        if (_connection.State != PiConnectionState.Connected)
        {
            _errorHandler.HandleError(new InvalidOperationException("Not connected to the scoreboard."));
            return null;
        }

        try
        {
            // RemoteId isn't user-editable — it gets populated once the Pi acknowledges the team.
            var existingRemoteId = EditingTeamId is { } editingId ? _session.FindById(editingId)?.RemoteId : null;
            var team = new ScoreboardTeam { Id = EditingTeamId ?? _session.NextId(), RemoteId = existingRemoteId, Name = name };
            var command = new ScoreBoardCommand("AddTeam", new { id = team.Id, remoteId = team.RemoteId, name = team.Name });
            await _connection.SendMessageAsync(JsonSerializer.Serialize(command, _jsonOptions));
            return team;
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            return null;
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private bool CanAdd() => !HasErrors;

    public string? NameError => GetErrors(nameof(Name)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasNameError => GetErrors(nameof(Name)).Cast<object>().Any();
}
