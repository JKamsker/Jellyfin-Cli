using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Tasks;

public sealed class TasksGetSettings : GlobalSettings
{
    [CommandArgument(0, "<TASK_ID>")]
    [Description("The ID of the scheduled task")]
    public string TaskId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(TaskId))
            return ValidationResult.Error("TASK_ID is required.");

        return ValidationResult.Success();
    }
}

public sealed class TasksGetCommand : ApiCommand<TasksGetSettings>
{
    public TasksGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, TasksGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var task = await TasksCommandHelper.ResolveTaskAsync(client, settings.TaskId, cancellationToken);

        if (task is null)
        {
            AnsiConsole.MarkupLine("[red]Task not found.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(task);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Id", task.Id ?? "(unknown)");
        table.AddRow("Key", task.Key ?? "(unknown)");
        table.AddRow("Name", task.Name ?? "(unknown)");
        table.AddRow("Description", task.Description ?? "(none)");
        table.AddRow("Category", task.Category ?? "(unknown)");
        table.AddRow("State", task.State?.ToString() ?? "(unknown)");
        table.AddRow("Progress", task.CurrentProgressPercentage?.ToString("F1") ?? "(none)");

        if (task.LastExecutionResult is { } result)
        {
            table.AddRow("Last Status", result.Status?.ToString() ?? "(unknown)");
            table.AddRow("Last Start", result.StartTimeUtc?.ToString("u") ?? "(unknown)");
            table.AddRow("Last End", result.EndTimeUtc?.ToString("u") ?? "(unknown)");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                table.AddRow("Error", result.ErrorMessage);
        }
        else
        {
            table.AddRow("Last Status", "(never run)");
        }

        if (task.Triggers is { Count: > 0 })
        {
            var triggerDescriptions = task.Triggers
                .Select(t => t.Type?.ToString() ?? "(unknown)")
                .ToArray();
            table.AddRow("Triggers", string.Join(", ", triggerDescriptions));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}
