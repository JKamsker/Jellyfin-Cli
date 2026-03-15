using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Tasks;

public sealed class TasksStartSettings : GlobalSettings
{
    [CommandArgument(0, "<TASK_ID>")]
    [Description("The ID, key, or name of the scheduled task to start")]
    public string TaskId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(TaskId))
            return ValidationResult.Error("TASK_ID is required.");

        return ValidationResult.Success();
    }
}

public sealed class TasksStartCommand : ApiCommand<TasksStartSettings>
{
    public TasksStartCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, TasksStartSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var task = await TasksCommandHelper.ResolveTaskAsync(client, settings.TaskId, cancellationToken);
        if (task is null)
        {
            AnsiConsole.MarkupLine("[red]Task not found.[/]");
            return 1;
        }

        await client.ScheduledTasks.Running[TasksCommandHelper.GetRouteIdentifier(task, settings.TaskId)].PostAsync();

        AnsiConsole.MarkupLine($"[green]Task '[white]{Markup.Escape(task.Name ?? settings.TaskId)}[/]' started.[/]");
        return 0;
    }
}
