using System.Collections;
using System.Reflection;

using Spectre.Console.Cli;

namespace Jellyfin.Cli.Common;

internal static class ConfiguratorDocumentationExtensions
{
    private sealed record ExampleArgument(PropertyInfo Property, CommandArgumentAttribute Attribute);

    private sealed record ExampleOption(PropertyInfo Property, CommandOptionAttribute Attribute, bool DeclaredOnCommand);

    private static readonly Dictionary<string, string[]> ManualExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auth login"] = ["auth", "login", "--username", "demo", "--password", "secret"],
        ["auth quick approve"] = ["auth", "quick", "approve", "--code", "ABCD1234"],
        ["raw get"] = ["raw", "get", "/System/Info"],
        ["raw post"] = ["raw", "post", "/Sessions/Capabilities", "--body", "{}"],
        ["raw put"] = ["raw", "put", "/Sessions/Capabilities", "--body", "{}"],
        ["raw delete"] = ["raw", "delete", "/Items/12345"],
        ["syncplay create"] = ["syncplay", "create", "--name", "movie-night"],
        ["syncplay join"] = ["syncplay", "join", "--group-id", "00000000-0000-0000-0000-000000000001"],
    };

    private static readonly HashSet<string> MutationCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "add",
        "approve",
        "command",
        "create",
        "delete",
        "disable",
        "download",
        "enable",
        "install",
        "join",
        "login",
        "message",
        "move",
        "password",
        "play",
        "policy",
        "post",
        "put",
        "refresh",
        "remove",
        "rename",
        "restore",
        "search",
        "set",
        "state",
        "uninstall",
        "update",
        "upload",
        "use",
    };

    public static void AddDocumentationExamples(this IConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        foreach (var command in GetChildren(configurator, "Commands"))
        {
            PopulateExamples(command, []);
        }
    }

    private static void PopulateExamples(object configuredCommand, IReadOnlyList<string> parentPath)
    {
        if (IsHidden(configuredCommand))
        {
            return;
        }

        var commandName = GetName(configuredCommand);
        if (IsDefaultCommandName(commandName))
        {
            return;
        }

        var path = parentPath.Concat([commandName]).ToArray();
        var children = GetChildren(configuredCommand, "Children")
            .Where(child => !IsHidden(child) && !IsDefaultCommandName(GetName(child)))
            .ToList();

        foreach (var child in children)
        {
            PopulateExamples(child, path);
        }

        var examples = GetExamples(configuredCommand);
        if (examples.Count > 0)
        {
            return;
        }

        var generatedExample = children.Count > 0
            ? BuildBranchExample(children)
            : BuildLeafExample(configuredCommand, path);

        if (generatedExample.Length > 0)
        {
            examples.Add(generatedExample);
        }
    }

    private static string[] BuildBranchExample(IEnumerable<object> children)
    {
        foreach (var child in children)
        {
            var examples = GetExamples(child);
            if (examples.Count == 0 || examples[0] is not string[] example)
            {
                continue;
            }

            return example.ToArray();
        }

        return [];
    }

    private static string[] BuildLeafExample(object configuredCommand, IReadOnlyList<string> path)
    {
        var pathKey = string.Join(' ', path);
        if (ManualExamples.TryGetValue(pathKey, out var manualExample))
        {
            return manualExample;
        }

        var settingsType = GetSettingsType(configuredCommand);
        var arguments = GetArguments(settingsType);
        var options = GetOptions(settingsType);

        var tokens = path.ToList();
        foreach (var argument in arguments)
        {
            tokens.Add(CreateSampleValue(argument.Property.PropertyType, argument.Attribute.ValueName, argument.Property.Name));
        }

        AppendOptions(tokens, options.Where(option => option.Attribute.IsRequired));

        if (ShouldAddIllustrativeOption(path[^1], arguments.Length, options))
        {
            var candidate = GetIllustrativeOption(options);

            if (candidate is not null)
            {
                AppendOptions(tokens, [candidate]);
            }
        }

        return tokens.ToArray();
    }

    private static bool ShouldAddIllustrativeOption(
        string commandName,
        int argumentCount,
        IEnumerable<ExampleOption> options)
    {
        var optionalOptions = options.Any(option => !option.Attribute.IsRequired && !IsNoiseOption(option.Attribute));
        if (!optionalOptions)
        {
            return false;
        }

        return argumentCount == 0 || MutationCommands.Contains(commandName);
    }

    private static void AppendOptions(
        ICollection<string> tokens,
        IEnumerable<ExampleOption> options)
    {
        foreach (var option in options)
        {
            tokens.Add(GetPrimaryOptionName(option.Attribute));
            if (IsFlagOption(option.Property.PropertyType, option.Attribute))
            {
                continue;
            }

            tokens.Add(CreateSampleValue(option.Property.PropertyType, option.Attribute.ValueName, option.Property.Name));
        }
    }

    private static ExampleArgument[] GetArguments(Type settingsType)
    {
        return GetCommandProperties(settingsType)
            .Select(property => TryCreateArgument(property))
            .Where(argument => argument is not null)
            .Cast<ExampleArgument>()
            .OrderBy(argument => argument.Attribute.Position)
            .ToArray();
    }

    private static ExampleOption[] GetOptions(Type settingsType)
    {
        return GetCommandProperties(settingsType)
            .Select(property => TryCreateOption(settingsType, property))
            .Where(option => option is not null)
            .Cast<ExampleOption>()
            .ToArray();
    }

    private static IEnumerable<PropertyInfo> GetCommandProperties(Type settingsType)
    {
        var hierarchy = new Stack<Type>();
        for (var type = settingsType; type is not null && type != typeof(object); type = type.BaseType)
        {
            hierarchy.Push(type);
        }

        while (hierarchy.Count > 0)
        {
            foreach (var property in hierarchy.Pop()
                         .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                         .OrderBy(property => property.MetadataToken))
            {
                yield return property;
            }
        }
    }

    private static ExampleArgument? TryCreateArgument(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<CommandArgumentAttribute>();
        return attribute is null ? null : new ExampleArgument(property, attribute);
    }

    private static ExampleOption? TryCreateOption(Type settingsType, PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<CommandOptionAttribute>();
        if (attribute is null || attribute.IsHidden)
        {
            return null;
        }

        return new ExampleOption(property, attribute, property.DeclaringType == settingsType);
    }

    private static ExampleOption? GetIllustrativeOption(IEnumerable<ExampleOption> options)
    {
        var candidates = options
            .Where(option => !option.Attribute.IsRequired && !IsNoiseOption(option.Attribute))
            .ToArray();

        if (candidates.Length == 0)
        {
            return null;
        }

        var commandSpecificCandidates = candidates
            .Where(option => option.DeclaredOnCommand)
            .ToArray();

        return (commandSpecificCandidates.Length > 0 ? commandSpecificCandidates : candidates)
            .OrderBy(option => GetOptionPriority(option))
            .ThenBy(option => option.Property.MetadataToken)
            .FirstOrDefault();
    }

    private static bool IsNoiseOption(CommandOptionAttribute option)
    {
        var primaryName = NormalizeToken(GetPrimaryOptionName(option));
        return primaryName is "JSON" or "VERBOSE" or "YES" or "CONFIG" or "TOKEN" or "API_KEY" or "LIMIT" or "START" or "ALL";
    }

    private static int GetOptionPriority(ExampleOption option)
    {
        var name = NormalizeToken(option.Attribute.ValueName) ?? NormalizeToken(GetPrimaryOptionName(option.Attribute));
        if (string.IsNullOrWhiteSpace(name))
        {
            return 50;
        }

        return name switch
        {
            "SERVER" or "URL" => 0,
            "USER" or "USER_ID" => 1,
            "USERNAME" => 0,
            "PASSWORD" => 2,
            "CODE" => 3,
            "NAME" => 4,
            "ID" => 5,
            "PATH" or "FILE" => 6,
            "QUERY" or "BODY" or "DATA" => 7,
            _ => 10,
        };
    }

    private static bool IsFlagOption(Type propertyType, CommandOptionAttribute attribute)
    {
        return !attribute.ValueIsOptional &&
               (propertyType == typeof(bool) || propertyType == typeof(bool?));
    }

    private static string CreateSampleValue(Type type, string? tokenName, string propertyName)
    {
        var normalizedToken = NormalizeToken(tokenName) ?? NormalizeToken(propertyName) ?? "VALUE";
        var elementType = Nullable.GetUnderlyingType(type) ?? type;
        if (elementType.IsArray)
        {
            elementType = elementType.GetElementType() ?? typeof(string);
        }

        if (elementType == typeof(bool))
        {
            return "true";
        }

        if (elementType == typeof(Guid))
        {
            return "00000000-0000-0000-0000-000000000001";
        }

        if (elementType == typeof(int) || elementType == typeof(long) || elementType == typeof(short))
        {
            return normalizedToken == "YEAR" ? "2024" : "1";
        }

        if (elementType == typeof(decimal) || elementType == typeof(double) || elementType == typeof(float))
        {
            return "1.0";
        }

        if (elementType == typeof(TimeSpan))
        {
            return "00:15:00";
        }

        if (elementType == typeof(DateTime) || elementType == typeof(DateTimeOffset))
        {
            return "2025-01-01T00:00:00Z";
        }

        if (elementType.IsEnum)
        {
            return Enum.GetNames(elementType).First();
        }

        return normalizedToken switch
        {
            "GROUP_ID" => "00000000-0000-0000-0000-000000000001",
            "ID" or "ITEM_ID" or "USER_ID" or "TASK_ID" or "TASK_ID_OR_ACTION" => "12345",
            "URL" => "http://localhost:8096",
            "USERNAME" => "demo",
            "PASSWORD" or "NEW_PASSWORD" => "secret",
            "TOKEN" => "token",
            "API_KEY" or "KEY" => "api-key",
            "NAME" or "PROFILE" or "HOSTNAME" or "HOST" or "ALIAS" => "demo",
            "PATH" => "./sample",
            "FILE" or "ARCHIVE_FILE" => "sample.json",
            "ENDPOINT" => "/System/Info",
            "TERM" or "SEARCH" => "example",
            "TYPE" => "Movie",
            "LANGUAGE" or "CODE" => "en",
            "JSON" or "BODY" or "DATA" => "{}",
            "QUERY" => "name=value",
            "HEADER" => "X-Test: 1",
            "BOOL" => "true",
            "YEAR" => "2024",
            _ when normalizedToken.Contains("URL", StringComparison.Ordinal) => "http://localhost:8096",
            _ when normalizedToken.Contains("PASSWORD", StringComparison.Ordinal) => "secret",
            _ when normalizedToken.Contains("USER", StringComparison.Ordinal) => "demo",
            _ when normalizedToken.Contains("NAME", StringComparison.Ordinal) => "demo",
            _ when normalizedToken.Contains("PATH", StringComparison.Ordinal) => "./sample",
            _ when normalizedToken.Contains("FILE", StringComparison.Ordinal) => "sample.json",
            _ when normalizedToken.Contains("JSON", StringComparison.Ordinal) => "{}",
            _ when normalizedToken.Contains("BODY", StringComparison.Ordinal) => "{}",
            _ when normalizedToken.Contains("QUERY", StringComparison.Ordinal) => "name=value",
            _ when normalizedToken.Contains("HEADER", StringComparison.Ordinal) => "X-Test: 1",
            _ when normalizedToken.Contains("ID", StringComparison.Ordinal) => "12345",
            _ => normalizedToken.ToLowerInvariant(),
        };
    }

    private static string? NormalizeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return token.Trim('<', '>', '[', ']', '-', ' ')
            .Replace("|", string.Empty, StringComparison.Ordinal)
            .Replace(' ', '_')
            .ToUpperInvariant();
    }

    private static IList GetExamples(object configuredCommand)
    {
        return (IList)(GetRequiredProperty(configuredCommand, "Examples").GetValue(configuredCommand)
            ?? throw new InvalidOperationException("Configured command examples are not available."));
    }

    private static IEnumerable<object> GetChildren(object target, string propertyName)
    {
        return ((IEnumerable)(GetRequiredProperty(target, propertyName).GetValue(target)
                ?? throw new InvalidOperationException($"Property `{propertyName}` is not available.")))
            .Cast<object>();
    }

    private static Type GetSettingsType(object configuredCommand)
    {
        return (Type)(GetRequiredProperty(configuredCommand, "SettingsType").GetValue(configuredCommand)
            ?? throw new InvalidOperationException("Configured command settings type is not available."));
    }

    private static string GetName(object configuredCommand)
    {
        return (string)(GetRequiredProperty(configuredCommand, "Name").GetValue(configuredCommand)
            ?? throw new InvalidOperationException("Configured command name is not available."));
    }

    private static bool IsHidden(object configuredCommand)
    {
        return (bool)(GetRequiredProperty(configuredCommand, "IsHidden").GetValue(configuredCommand)
            ?? false);
    }

    private static bool IsDefaultCommandName(string commandName)
    {
        return string.Equals(commandName, "__default_command", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPrimaryOptionName(CommandOptionAttribute option)
    {
        if (option.LongNames.Count > 0)
        {
            return $"--{option.LongNames[0]}";
        }

        return $"-{option.ShortNames[0]}";
    }

    private static PropertyInfo GetRequiredProperty(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Property `{propertyName}` was not found on `{target.GetType().FullName}`.");
    }
}
