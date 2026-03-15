using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Tasks;

public sealed class TasksListCommand : ApiCommand<GlobalSettings>
{
    public TasksListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var tasks = await client.ScheduledTasks.GetAsync();

        if (tasks is null || tasks.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No scheduled tasks found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(tasks.Select(t => new
            {
                id = t.Id,
                key = t.Key,
                name = t.Name,
                state = t.State?.ToString(),
                category = t.Category,
                lastExecutionResult = t.LastExecutionResult?.Status?.ToString(),
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Key", "Name", "State", "Category", "LastExecutionResult");
        foreach (var task in tasks)
        {
            table.AddRow(
                task.Id ?? "(unknown)",
                task.Key ?? "(unknown)",
                task.Name ?? "(unknown)",
                task.State?.ToString() ?? "(unknown)",
                task.Category ?? "(unknown)",
                task.LastExecutionResult?.Status?.ToString() ?? "(none)");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}
