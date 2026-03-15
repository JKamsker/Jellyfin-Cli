using System.Globalization;

using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

namespace Jellyfin.Cli.Commands.Tasks;

internal static class TasksTriggersCommandHelper
{
    internal static string DescribeTrigger(TaskTriggerInfo trigger)
    {
        return trigger.Type switch
        {
            TaskTriggerInfo_Type.IntervalTrigger => trigger.IntervalTicks is not null
                ? $"Every {OutputHelper.FormatTicks(trigger.IntervalTicks)}"
                : "Interval",
            TaskTriggerInfo_Type.DailyTrigger => trigger.TimeOfDayTicks is not null
                ? $"Daily at {FormatTimeOfDay(trigger.TimeOfDayTicks)}"
                : "Daily",
            TaskTriggerInfo_Type.WeeklyTrigger => trigger.TimeOfDayTicks is not null
                ? $"Weekly on {trigger.DayOfWeek} at {FormatTimeOfDay(trigger.TimeOfDayTicks)}"
                : $"Weekly on {trigger.DayOfWeek}",
            TaskTriggerInfo_Type.StartupTrigger => "On startup",
            _ => trigger.Type?.ToString() ?? "(unknown)",
        };
    }

    internal static TimeSpan ParseDuration(string value, string argumentName)
    {
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var duration) && duration >= TimeSpan.Zero)
            return duration;

        throw new InvalidOperationException($"{argumentName} must be a valid non-negative TimeSpan.");
    }

    internal static TimeSpan ParseTimeOfDay(string value, string argumentName)
    {
        var formats = new[] { @"hh\:mm", @"hh\:mm\:ss", @"h\:mm", @"h\:mm\:ss" };
        if (TimeSpan.TryParseExact(value, formats, CultureInfo.InvariantCulture, out var timeOfDay) &&
            timeOfDay >= TimeSpan.Zero &&
            timeOfDay < TimeSpan.FromDays(1))
        {
            return timeOfDay;
        }

        throw new InvalidOperationException($"{argumentName} must be in HH:mm or HH:mm:ss format.");
    }

    internal static TaskTriggerInfo_DayOfWeek ParseDayOfWeek(string value)
    {
        return Enum.TryParse<TaskTriggerInfo_DayOfWeek>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unknown day of week '{value}'.");
    }

    internal static string FormatTimeOfDay(long? ticks)
    {
        if (ticks is null)
            return string.Empty;

        return TimeSpan.FromTicks(ticks.Value).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }
}
