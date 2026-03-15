using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Commands.Tasks;

namespace Jellyfin.Cli.Tests.Tasks;

public sealed class TasksTriggersCommandHelperTests
{
    [Fact]
    public void ParseDuration_AcceptsInvariantTimeSpan()
    {
        var duration = TasksTriggersCommandHelper.ParseDuration("12:34:56", "--interval");

        Assert.Equal(new TimeSpan(12, 34, 56), duration);
    }

    [Fact]
    public void ParseDuration_RejectsNegativeValues()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TasksTriggersCommandHelper.ParseDuration("-00:30:00", "--interval"));

        Assert.Equal("--interval must be a valid non-negative TimeSpan.", ex.Message);
    }

    [Theory]
    [InlineData("3:15", 3, 15)]
    [InlineData("03:15:30", 3, 15, 30)]
    public void ParseTimeOfDay_AcceptsSupportedFormats(string value, int hours, int minutes, int seconds = 0)
    {
        var timeOfDay = TasksTriggersCommandHelper.ParseTimeOfDay(value, "--daily");

        Assert.Equal(new TimeSpan(hours, minutes, seconds), timeOfDay);
    }

    [Fact]
    public void ParseTimeOfDay_RejectsOutOfRangeValue()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TasksTriggersCommandHelper.ParseTimeOfDay("24:00", "--daily"));

        Assert.Equal("--daily must be in HH:mm or HH:mm:ss format.", ex.Message);
    }

    [Fact]
    public void ParseDayOfWeek_IsCaseInsensitive()
    {
        var day = TasksTriggersCommandHelper.ParseDayOfWeek("mOnDay");

        Assert.Equal(TaskTriggerInfo_DayOfWeek.Monday, day);
    }

    [Fact]
    public void DescribeTrigger_FormatsWeeklyTrigger()
    {
        var trigger = new TaskTriggerInfo
        {
            Type = TaskTriggerInfo_Type.WeeklyTrigger,
            DayOfWeek = TaskTriggerInfo_DayOfWeek.Friday,
            TimeOfDayTicks = new TimeSpan(21, 30, 0).Ticks,
        };

        var description = TasksTriggersCommandHelper.DescribeTrigger(trigger);

        Assert.Equal("Weekly on Friday at 21:30", description);
    }
}
