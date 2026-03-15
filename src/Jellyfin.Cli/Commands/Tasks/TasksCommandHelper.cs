using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;

namespace Jellyfin.Cli.Commands.Tasks;

internal static class TasksCommandHelper
{
    internal static async Task<TaskInfo?> ResolveTaskAsync(
        JellyfinApiClient client,
        string identifier,
        CancellationToken cancellationToken)
    {
        var tasks = await client.ScheduledTasks.GetAsync(cancellationToken: cancellationToken) ?? [];

        return tasks.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, identifier, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Key, identifier, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, identifier, StringComparison.OrdinalIgnoreCase));
    }

    internal static string GetRouteIdentifier(TaskInfo task, string fallbackIdentifier)
    {
        if (Guid.TryParse(task.Id, out var parsedId))
            return parsedId.ToString("N");

        if (!string.IsNullOrWhiteSpace(task.Id))
            return task.Id;

        if (!string.IsNullOrWhiteSpace(task.Key))
            return task.Key;

        return fallbackIdentifier;
    }
}
