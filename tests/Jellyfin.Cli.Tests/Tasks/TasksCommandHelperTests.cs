using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Commands.Tasks;

namespace Jellyfin.Cli.Tests.Tasks;

public sealed class TasksCommandHelperTests
{
    [Fact]
    public void GetRouteIdentifier_UsesCompactGuid_WhenTaskIdIsGuid()
    {
        var task = new TaskInfo
        {
            Id = "b461ef91-8ab2-8520-9281-83e794350e3c",
            Key = "AudioNormalization",
        };

        var routeIdentifier = TasksCommandHelper.GetRouteIdentifier(task, "fallback");

        Assert.Equal("b461ef918ab28520928183e794350e3c", routeIdentifier);
    }

    [Fact]
    public void GetRouteIdentifier_UsesTaskId_WhenTaskIdIsNotGuid()
    {
        var task = new TaskInfo
        {
            Id = "AudioNormalization",
            Key = "NormalizeAudio",
        };

        var routeIdentifier = TasksCommandHelper.GetRouteIdentifier(task, "fallback");

        Assert.Equal("AudioNormalization", routeIdentifier);
    }

    [Fact]
    public void GetRouteIdentifier_UsesTaskKey_WhenIdIsMissing()
    {
        var task = new TaskInfo
        {
            Key = "AudioNormalization",
        };

        var routeIdentifier = TasksCommandHelper.GetRouteIdentifier(task, "fallback");

        Assert.Equal("AudioNormalization", routeIdentifier);
    }

    [Fact]
    public void GetRouteIdentifier_UsesFallback_WhenTaskHasNoIdOrKey()
    {
        var task = new TaskInfo();

        var routeIdentifier = TasksCommandHelper.GetRouteIdentifier(task, "fallback");

        Assert.Equal("fallback", routeIdentifier);
    }
}
