using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Tasks;

public sealed class TasksTriggersSettings : GlobalSettings
{
    [CommandArgument(0, "<TASK_ID_OR_ACTION>")]
    [Description("Task ID or key, or 'set' to update triggers")]
    public string TaskIdOrAction { get; set; } = string.Empty;

    [CommandArgument(1, "[TASK_ID]")]
    [Description("Task ID when using the 'set' action")]
    public string? TaskId { get; set; }

    [CommandOption("--file <FILE>")]
    [Description("Load a trigger list from JSON")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline JSON trigger list")]
    public string? InlineJson { get; set; }

    [CommandOption("--interval <TIMESPAN>")]
    [Description("Add an interval trigger, repeatable")]
    public string[]? Intervals { get; set; }

    [CommandOption("--daily <TIME>")]
    [Description("Add a daily trigger, repeatable")]
    public string[]? DailyTimes { get; set; }

    [CommandOption("--weekly <SCHEDULE>")]
    [Description("Add a weekly trigger, repeatable")]
    public string[]? WeeklyTimes { get; set; }

    [CommandOption("--startup")]
    [Description("Add a startup trigger")]
    public bool Startup { get; set; }

    [CommandOption("--max-runtime <TIMESPAN>")]
    [Description("Apply one max runtime to all flag-built triggers")]
    public string? MaxRuntime { get; set; }

    [CommandOption("--clear")]
    [Description("Replace all triggers with an empty list")]
    public bool Clear { get; set; }

    public override ValidationResult Validate()
    {
        try
        {
            var action = ResolveAction();
            _ = ResolveTaskId();

            if (action == TasksTriggersAction.Get)
            {
                return HasSetterOptions()
                    ? ValidationResult.Error("Setter options are only valid with 'tasks triggers set <TASK_ID>'.")
                    : ValidationResult.Success();
            }

            var hasJsonInput = !string.IsNullOrWhiteSpace(FilePath) || !string.IsNullOrWhiteSpace(InlineJson);
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(InlineJson))
                return ValidationResult.Error("Specify exactly one of --file or --data.");

            var hasFlagTriggers =
                Startup ||
                Clear ||
                (Intervals?.Length ?? 0) > 0 ||
                (DailyTimes?.Length ?? 0) > 0 ||
                (WeeklyTimes?.Length ?? 0) > 0;

            if (hasJsonInput && hasFlagTriggers)
                return ValidationResult.Error("Use either --file/--data or trigger flags, not both.");

            if (!hasJsonInput && !hasFlagTriggers)
                return ValidationResult.Error("Specify trigger JSON, --clear, or at least one trigger flag.");

            _ = BuildTriggers();
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }

    internal TasksTriggersAction ResolveAction()
    {
        return string.Equals(TaskIdOrAction, "set", StringComparison.OrdinalIgnoreCase)
            ? TasksTriggersAction.Set
            : TasksTriggersAction.Get;
    }

    internal string ResolveTaskId()
    {
        if (ResolveAction() == TasksTriggersAction.Set)
        {
            return string.IsNullOrWhiteSpace(TaskId)
                ? throw new InvalidOperationException("TASK_ID is required after 'set'.")
                : TaskId;
        }

        if (string.Equals(TaskIdOrAction, "get", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(TaskId)
                ? throw new InvalidOperationException("TASK_ID is required after 'get'.")
                : TaskId;
        }

        if (!string.IsNullOrWhiteSpace(TaskId))
            throw new InvalidOperationException("Unexpected extra argument. Use 'tasks triggers <TASK_ID>' or 'tasks triggers set <TASK_ID>'.");

        return string.IsNullOrWhiteSpace(TaskIdOrAction)
            ? throw new InvalidOperationException("TASK_ID is required.")
            : TaskIdOrAction;
    }

    internal List<TaskTriggerInfo> BuildTriggers()
    {
        if (ResolveAction() != TasksTriggersAction.Set)
            return [];

        if (Clear)
            return [];

        var triggers = new List<TaskTriggerInfo>();
        var maxRuntimeTicks = string.IsNullOrWhiteSpace(MaxRuntime)
            ? (long?)null
            : TasksTriggersCommandHelper.ParseDuration(MaxRuntime, "--max-runtime").Ticks;

        foreach (var interval in Intervals ?? [])
        {
            triggers.Add(new TaskTriggerInfo
            {
                Type = TaskTriggerInfo_Type.IntervalTrigger,
                IntervalTicks = TasksTriggersCommandHelper.ParseDuration(interval, "--interval").Ticks,
                MaxRuntimeTicks = maxRuntimeTicks,
            });
        }

        foreach (var dailyTime in DailyTimes ?? [])
        {
            triggers.Add(new TaskTriggerInfo
            {
                Type = TaskTriggerInfo_Type.DailyTrigger,
                TimeOfDayTicks = TasksTriggersCommandHelper.ParseTimeOfDay(dailyTime, "--daily").Ticks,
                MaxRuntimeTicks = maxRuntimeTicks,
            });
        }

        foreach (var weeklyTime in WeeklyTimes ?? [])
        {
            var parts = weeklyTime.Split('@', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new InvalidOperationException("--weekly values must use DAY@HH:mm format.");

            triggers.Add(new TaskTriggerInfo
            {
                Type = TaskTriggerInfo_Type.WeeklyTrigger,
                DayOfWeek = TasksTriggersCommandHelper.ParseDayOfWeek(parts[0]),
                TimeOfDayTicks = TasksTriggersCommandHelper.ParseTimeOfDay(parts[1], "--weekly").Ticks,
                MaxRuntimeTicks = maxRuntimeTicks,
            });
        }

        if (Startup)
        {
            triggers.Add(new TaskTriggerInfo
            {
                Type = TaskTriggerInfo_Type.StartupTrigger,
                MaxRuntimeTicks = maxRuntimeTicks,
            });
        }

        return triggers;
    }

    private bool HasSetterOptions()
    {
        return !string.IsNullOrWhiteSpace(FilePath) ||
               !string.IsNullOrWhiteSpace(InlineJson) ||
               Startup ||
               Clear ||
               !string.IsNullOrWhiteSpace(MaxRuntime) ||
               (Intervals?.Length ?? 0) > 0 ||
               (DailyTimes?.Length ?? 0) > 0 ||
               (WeeklyTimes?.Length ?? 0) > 0;
    }
}

public sealed class TasksTriggersCommand : ApiCommand<TasksTriggersSettings>
{
    public TasksTriggersCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, TasksTriggersSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var taskIdentifier = settings.ResolveTaskId();
        var task = await TasksCommandHelper.ResolveTaskAsync(client, taskIdentifier, cancellationToken);

        if (task is null)
        {
            AnsiConsole.MarkupLine("[red]Task not found.[/]");
            return 1;
        }

        if (settings.ResolveAction() == TasksTriggersAction.Set)
        {
            var triggers = !string.IsNullOrWhiteSpace(settings.FilePath) || !string.IsNullOrWhiteSpace(settings.InlineJson)
                ? JsonCommandHelper.DeserializeFromFileOrInline<List<TaskTriggerInfo>>(settings.FilePath, settings.InlineJson, "--file/--data")
                : settings.BuildTriggers();

            await client.ScheduledTasks[task.Id ?? taskIdentifier].Triggers.PostAsync(triggers, cancellationToken: cancellationToken);

            if (settings.Json)
            {
                OutputHelper.WriteJson(new
                {
                    id = task.Id,
                    key = task.Key,
                    name = task.Name,
                    triggerCount = triggers.Count,
                    triggers,
                });
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]Updated [white]{triggers.Count}[/] trigger(s) for task [white]{Markup.Escape(task.Name ?? taskIdentifier)}[/].[/]");
            return 0;
        }

        var taskTriggers = task.Triggers ?? [];
        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                id = task.Id,
                key = task.Key,
                name = task.Name,
                triggers = taskTriggers,
            });
            return 0;
        }

        if (taskTriggers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No triggers configured.[/]");
            return 0;
        }

        var table = OutputHelper.CreateTable("Type", "Schedule", "MaxRuntime");
        foreach (var trigger in taskTriggers)
        {
            table.AddRow(
                trigger.Type?.ToString() ?? string.Empty,
                TasksTriggersCommandHelper.DescribeTrigger(trigger),
                OutputHelper.FormatTicks(trigger.MaxRuntimeTicks));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

internal enum TasksTriggersAction
{
    Get,
    Set,
}
