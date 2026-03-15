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
}
