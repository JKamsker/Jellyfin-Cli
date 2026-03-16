using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Cli.Common;

public sealed class CredentialStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jf");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");
    private static readonly string LegacyCredentialFile = Path.Combine(ConfigDir, "credentials.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    private CliConfig? _cache;

    /// <summary>Load credentials from the active profile (backwards-compatible).</summary>
    public StoredCredentials? Load()
    {
        var config = LoadConfig();
        if (config.ActiveProfile is not null && config.Profiles.TryGetValue(config.ActiveProfile, out var profile))
            return profile;

        return config.Profiles.Values.FirstOrDefault();
    }

    /// <summary>Load credentials from a specific named profile.</summary>
    public StoredCredentials? LoadProfile(string profileName)
    {
        var config = LoadConfig();
        return config.Profiles.TryGetValue(profileName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Resolve a profile by explicit name, server URL match, or active profile fallback.
    /// Returns the resolved profile name and credentials.
    /// </summary>
    public (string? ProfileName, StoredCredentials? Credentials) Resolve(string? profileName, string? server)
    {
        var config = LoadConfig();

        // 1. Explicit profile name
        if (!string.IsNullOrEmpty(profileName))
        {
            if (config.Profiles.TryGetValue(profileName, out var profile))
                return (profileName, profile);
            return (profileName, null);
        }

        // 2. Match by server URL
        if (!string.IsNullOrEmpty(server))
        {
            var normalizedServer = NormalizeServer(server);
            var matches = config.Profiles
                .Where(p => !string.IsNullOrEmpty(p.Value.Server) && NormalizeServer(p.Value.Server) == normalizedServer)
                .ToList();

            if (matches.Count == 1)
                return (matches[0].Key, matches[0].Value);

            if (matches.Count > 1)
            {
                // Prefer the active profile if it matches
                var activeMatch = matches.FirstOrDefault(m => m.Key == config.ActiveProfile);
                if (activeMatch.Key is not null)
                    return (activeMatch.Key, activeMatch.Value);

                return (matches[0].Key, matches[0].Value);
            }
        }

        // 3. Active profile
        if (config.ActiveProfile is not null && config.Profiles.TryGetValue(config.ActiveProfile, out var active))
            return (config.ActiveProfile, active);

        // 4. Only one profile exists — use it
        if (config.Profiles.Count == 1)
        {
            var single = config.Profiles.First();
            return (single.Key, single.Value);
        }

        return (null, null);
    }

    /// <summary>Save credentials to the active profile (backwards-compatible).</summary>
    public void Save(StoredCredentials credentials)
    {
        var config = LoadConfig();
        var profileName = config.ActiveProfile ?? "default";
        config.Profiles[profileName] = credentials;
        config.ActiveProfile ??= profileName;
        SaveConfig(config);
    }

    /// <summary>Save credentials to a named profile. First profile becomes active automatically.</summary>
    public void SaveProfile(string profileName, StoredCredentials credentials)
    {
        var config = LoadConfig();
        var isFirst = config.Profiles.Count == 0;
        config.Profiles[profileName] = credentials;
        if (isFirst || config.ActiveProfile is null)
            config.ActiveProfile = profileName;
        SaveConfig(config);
    }

    /// <summary>Delete the active profile (backwards-compatible).</summary>
    public void Delete()
    {
        var config = LoadConfig();
        if (config.ActiveProfile is not null)
        {
            config.Profiles.Remove(config.ActiveProfile);
            config.ActiveProfile = config.Profiles.Keys.FirstOrDefault();
        }
        SaveConfig(config);
    }

    /// <summary>Delete a named profile. If it was active, the next profile becomes active.</summary>
    public void DeleteProfile(string profileName)
    {
        var config = LoadConfig();
        config.Profiles.Remove(profileName);
        if (config.ActiveProfile == profileName)
            config.ActiveProfile = config.Profiles.Keys.FirstOrDefault();
        SaveConfig(config);
    }

    public IReadOnlyDictionary<string, StoredCredentials> GetProfiles()
    {
        return LoadConfig().Profiles;
    }

    public string? GetActiveProfileName()
    {
        return LoadConfig().ActiveProfile;
    }

    public void SetActiveProfile(string profileName)
    {
        var config = LoadConfig();
        if (!config.Profiles.ContainsKey(profileName))
            throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
        config.ActiveProfile = profileName;
        SaveConfig(config);
    }

    private CliConfig LoadConfig()
    {
        if (_cache is not null)
            return _cache;

        if (File.Exists(ConfigFile))
        {
            var json = File.ReadAllText(ConfigFile);
            _cache = JsonSerializer.Deserialize<CliConfig>(json, JsonOptions) ?? new CliConfig();
            return _cache;
        }

        // Migrate from legacy credentials.json
        if (File.Exists(LegacyCredentialFile))
        {
            var json = File.ReadAllText(LegacyCredentialFile);
            var legacy = JsonSerializer.Deserialize<StoredCredentials>(json, JsonOptions);
            if (legacy is not null && !string.IsNullOrEmpty(legacy.Server))
            {
                var config = new CliConfig
                {
                    ActiveProfile = "default",
                    Profiles = { ["default"] = legacy },
                };
                SaveConfig(config);

                var backup = LegacyCredentialFile + ".bak";
                try
                {
                    if (!File.Exists(backup))
                        File.Move(LegacyCredentialFile, backup);
                    else
                        File.Delete(LegacyCredentialFile);
                }
                catch
                {
                    // Best-effort rename; not critical
                }

                return config;
            }
        }

        _cache = new CliConfig();
        return _cache;
    }

    private void SaveConfig(CliConfig config)
    {
        _cache = config;
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigFile, json);
    }

    private static string NormalizeServer(string server)
    {
        return server.TrimEnd('/').ToLowerInvariant();
    }
}

public sealed class StoredCredentials
{
    public string Server { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

internal sealed class CliConfig
{
    public string? ActiveProfile { get; set; }
    public Dictionary<string, StoredCredentials> Profiles { get; set; } = new();
}
