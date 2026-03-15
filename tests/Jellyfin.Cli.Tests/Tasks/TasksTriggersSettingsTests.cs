using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Commands.Tasks;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Tests.Tasks;

public sealed class TasksTriggersSettingsTests
{
    [Fact]
    public void Validate_AllowsGetterShortcutWithoutExtraFlags()
    {
        var settings = new TasksTriggersSettings
        {
            TaskIdOrAction = "AudioNormalization",
        };

        var result = settings.Validate();

        Assert.True(result.Successful);
        Assert.Equal("AudioNormalization", settings.ResolveTaskId());
    }

    [Fact]
    public void Validate_RejectsSetterFlagsForGetterMode()
    {
        var settings = new TasksTriggersSettings
        {
            TaskIdOrAction = "AudioNormalization",
            DailyTimes = ["03:00"],
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
        Assert.Equal("Setter options are only valid with 'tasks triggers set <TASK_ID>'.", result.Message);
    }

    [Fact]
    public void BuildTriggers_CreatesTriggersFromFlagsAndMaxRuntime()
    {
        var settings = new TasksTriggersSettings
        {
            TaskIdOrAction = "set",
            TaskId = "AudioNormalization",
            Intervals = ["12:00:00"],
            WeeklyTimes = ["Friday@21:30"],
            Startup = true,
            MaxRuntime = "01:15:00",
        };

        var result = settings.Validate();
        var triggers = settings.BuildTriggers();

        Assert.True(result.Successful);
        Assert.Collection(
            triggers,
            interval =>
            {
                Assert.Equal(TaskTriggerInfo_Type.IntervalTrigger, interval.Type);
                Assert.Equal(TimeSpan.FromHours(12).Ticks, interval.IntervalTicks);
                Assert.Equal(new TimeSpan(1, 15, 0).Ticks, interval.MaxRuntimeTicks);
            },
            weekly =>
            {
                Assert.Equal(TaskTriggerInfo_Type.WeeklyTrigger, weekly.Type);
                Assert.Equal(TaskTriggerInfo_DayOfWeek.Friday, weekly.DayOfWeek);
                Assert.Equal(new TimeSpan(21, 30, 0).Ticks, weekly.TimeOfDayTicks);
                Assert.Equal(new TimeSpan(1, 15, 0).Ticks, weekly.MaxRuntimeTicks);
            },
            startup =>
            {
                Assert.Equal(TaskTriggerInfo_Type.StartupTrigger, startup.Type);
                Assert.Equal(new TimeSpan(1, 15, 0).Ticks, startup.MaxRuntimeTicks);
            });
    }

    [Fact]
    public void BuildTriggers_ReturnsEmptyListForClear()
    {
        var settings = new TasksTriggersSettings
        {
            TaskIdOrAction = "set",
            TaskId = "AudioNormalization",
            Clear = true,
        };

        var result = settings.Validate();
        var triggers = settings.BuildTriggers();

        Assert.True(result.Successful);
        Assert.Empty(triggers);
    }

    [Fact]
    public void ResolveTaskId_SupportsExplicitGetVerb()
    {
        var settings = new TasksTriggersSettings
        {
            TaskIdOrAction = "get",
            TaskId = "AudioNormalization",
        };

        Assert.Equal(TasksTriggersAction.Get, settings.ResolveAction());
        Assert.Equal("AudioNormalization", settings.ResolveTaskId());
    }
}
