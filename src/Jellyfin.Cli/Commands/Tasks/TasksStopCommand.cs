using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Tasks;

public sealed class TasksStopSettings : GlobalSettings
{
    [CommandArgument(0, "<TASK_ID>")]
    [Description("The ID of the running task to stop")]
    public string TaskId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(TaskId))
            return ValidationResult.Error("TASK_ID is required.");

        return ValidationResult.Success();
    }
}

public sealed class TasksStopCommand : ApiCommand<TasksStopSettings>
{
    public TasksStopCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, TasksStopSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.ScheduledTasks.Running[settings.TaskId].DeleteAsync();

        AnsiConsole.MarkupLine($"[green]Task '[white]{settings.TaskId}[/]' stopped.[/]");
        return 0;
    }
}
