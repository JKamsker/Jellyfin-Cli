using System.Text.Json;
using System.Text.Json.Serialization;

using Spectre.Console;

namespace Jellyfin.Cli.Common;

public sealed class CredentialStore
{
    private static readonly string DefaultConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jf");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private CliConfig? _cache;

    /// <summary>Override the config file path (set from --config or JF_CONFIG).</summary>
    public string? ConfigPathOverride { get; set; }

    // ── Resolution ──────────────────────────────────────────────────────

    /// <summary>
    /// Two-level resolution: host (from server flag/env/default) → profile within host.
    /// Returns null if resolution fails.
    /// </summary>
    public ResolvedContext? Resolve(string? serverOrHostname, string? profileName)
    {
        var config = LoadConfig();

        // Step 1: Determine hostname and optional URL override
        string? hostname = null;
        string? urlOverride = null;

        if (!string.IsNullOrEmpty(serverOrHostname))
        {
            if (IsFullUrl(serverOrHostname))
            {
                hostname = ExtractHostname(serverOrHostname);
                urlOverride = serverOrHostname.TrimEnd('/');
            }
            else
            {
                hostname = NormalizeHostname(serverOrHostname);
            }
        }

        // Step 2: Find host entry
        HostConfig? host;
        string resolvedHostname;

        if (hostname is not null)
        {
            var lookup = LookupHost(config, hostname);
            if (lookup is not null)
            {
                resolvedHostname = lookup.Value.Hostname;
                host = lookup.Value.Host;
            }
            else
            {
                // Host not in config — return URL-only context if we have a full URL
                if (urlOverride is not null)
                    return new ResolvedContext { Hostname = hostname, BaseUrl = urlOverride };
                return null;
            }
        }
        else if (!string.IsNullOrEmpty(config.DefaultHost) && config.Hosts.TryGetValue(config.DefaultHost, out host))
        {
            resolvedHostname = config.DefaultHost;
        }
        else if (config.Hosts.Count == 1)
        {
            var entry = config.Hosts.First();
            resolvedHostname = entry.Key;
            host = entry.Value;
        }
        else
        {
            return null;
        }

        // Step 3: Find profile within host
        ProfileConfig? profile;
        string resolvedProfileName;

        if (!string.IsNullOrEmpty(profileName))
        {
            if (!host.Profiles.TryGetValue(profileName, out profile))
                return null;
            resolvedProfileName = profileName;
        }
        else if (!string.IsNullOrEmpty(host.DefaultProfile) && host.Profiles.TryGetValue(host.DefaultProfile, out profile))
        {
            resolvedProfileName = host.DefaultProfile;
        }
        else if (host.Profiles.Count == 1)
        {
            var entry = host.Profiles.First();
            resolvedProfileName = entry.Key;
            profile = entry.Value;
        }
        else
        {
            return null;
        }

        // Step 4: Resolve base URL (--server URL > profile.baseUrl > host.baseUrl)
        var baseUrl = (urlOverride ?? profile.BaseUrl ?? host.BaseUrl).TrimEnd('/');

        return new ResolvedContext
        {
            Hostname = resolvedHostname,
            ProfileName = resolvedProfileName,
            BaseUrl = baseUrl,
            Token = profile.Token,
            ApiKey = profile.ApiKey,
            Username = profile.Username,
            UserId = profile.UserId,
        };
    }

    /// <summary>Resolve just the host for management commands. Does not resolve a profile.</summary>
    public (string Hostname, HostConfig Host)? ResolveHost(string? serverOrHostname)
    {
        var config = LoadConfig();

        if (!string.IsNullOrEmpty(serverOrHostname))
        {
            var hostname = IsFullUrl(serverOrHostname)
                ? ExtractHostname(serverOrHostname)
                : NormalizeHostname(serverOrHostname);
            return LookupHost(config, hostname);
        }

        if (!string.IsNullOrEmpty(config.DefaultHost) && config.Hosts.TryGetValue(config.DefaultHost, out var dh))
            return (config.DefaultHost, dh);

        if (config.Hosts.Count == 1)
        {
            var entry = config.Hosts.First();
            return (entry.Key, entry.Value);
        }

        return null;
    }

    // ── Backward-compatible API ─────────────────────────────────────────

    /// <summary>Load credentials from the default host / default profile.</summary>
    public StoredCredentials? Load()
    {
        var resolved = Resolve(null, null);
        return resolved is null ? null : ToStoredCredentials(resolved);
    }

    /// <summary>Save credentials to the default host. Creates host+profile if needed.</summary>
    public void Save(StoredCredentials credentials)
    {
        var hostname = ExtractHostname(credentials.Server);
        var profile = new ProfileConfig
        {
            Token = NullIfEmpty(credentials.Token),
            ApiKey = NullIfEmpty(credentials.ApiKey),
            Username = NullIfEmpty(credentials.UserName),
            UserId = NullIfEmpty(credentials.UserId),
        };
        SaveProfile(hostname, "default", profile, credentials.Server.TrimEnd('/'));
    }

    /// <summary>Delete the default host's default profile.</summary>
    public void Delete()
    {
        var resolved = Resolve(null, null);
        if (resolved is not null)
            DeleteProfile(resolved.Hostname, resolved.ProfileName);
    }

    // ── Host Management ─────────────────────────────────────────────────

    public IReadOnlyDictionary<string, HostConfig> GetHosts() => LoadConfig().Hosts;

    public string? GetDefaultHostname() => LoadConfig().DefaultHost;

    public void SetDefaultHost(string hostname)
    {
        var config = LoadConfig();
        if (!config.Hosts.ContainsKey(hostname))
            throw new InvalidOperationException($"Host '{hostname}' does not exist.");
        config.DefaultHost = hostname;
        SaveConfig(config);
    }

    public void RenameHost(string oldName, string newName)
    {
        var config = LoadConfig();
        newName = NormalizeHostname(newName);
        if (!config.Hosts.Remove(oldName, out var host))
            throw new InvalidOperationException($"Host '{oldName}' does not exist.");
        if (config.Hosts.ContainsKey(newName))
            throw new InvalidOperationException($"Host '{newName}' already exists.");
        config.Hosts[newName] = host;
        if (config.DefaultHost == oldName)
            config.DefaultHost = newName;
        SaveConfig(config);
    }

    public void DeleteHost(string hostname)
    {
        var config = LoadConfig();
        config.Hosts.Remove(hostname);
        if (config.DefaultHost == hostname)
            config.DefaultHost = config.Hosts.Keys.FirstOrDefault();
        SaveConfig(config);
    }

    // ── Host Aliases ────────────────────────────────────────────────────

    /// <summary>
    /// Add an alias to a host. Warns (via return) if another host already uses it.
    /// Returns the hostname of the conflicting host, or null if no conflict.
    /// </summary>
    public string? AddAlias(string hostname, string alias)
    {
        var config = LoadConfig();
        alias = NormalizeHostname(alias);

        if (!config.Hosts.TryGetValue(hostname, out var host))
            throw new InvalidOperationException($"Host '{hostname}' does not exist.");

        // Check for conflicts on other hosts
        string? conflictHost = null;
        foreach (var (key, other) in config.Hosts)
        {
            if (key == hostname) continue;
            if (other.Aliases?.Contains(alias, StringComparer.OrdinalIgnoreCase) == true)
            {
                conflictHost = key;
                break;
            }
        }

        host.Aliases ??= new();
        if (!host.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
            host.Aliases.Add(alias);

        SaveConfig(config);
        return conflictHost;
    }

    public void RemoveAlias(string hostname, string alias)
    {
        var config = LoadConfig();
        alias = NormalizeHostname(alias);

        if (!config.Hosts.TryGetValue(hostname, out var host))
            throw new InvalidOperationException($"Host '{hostname}' does not exist.");

        if (host.Aliases is null)
            throw new InvalidOperationException($"Host '{hostname}' has no aliases.");

        var idx = host.Aliases.FindIndex(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            throw new InvalidOperationException($"Alias '{alias}' not found on host '{hostname}'.");

        host.Aliases.RemoveAt(idx);
        if (host.Aliases.Count == 0)
            host.Aliases = null;

        SaveConfig(config);
    }

    /// <summary>
    /// Look up a host by key or alias. Direct key match wins.
    /// If an alias matches multiple hosts, warns to stderr and returns the first.
    /// </summary>
    private static (string Hostname, HostConfig Host)? LookupHost(CliConfig config, string key)
    {
        // 1. Direct key match
        if (config.Hosts.TryGetValue(key, out var directHost))
            return (key, directHost);

        // 2. Alias match
        var matches = config.Hosts
            .Where(h => h.Value.Aliases?.Contains(key, StringComparer.OrdinalIgnoreCase) == true)
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count > 1)
        {
            var hostnames = string.Join(", ", matches.Select(m => m.Key));
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Alias '{Markup.Escape(key)}' matches multiple hosts: {Markup.Escape(hostnames)}. Using '{Markup.Escape(matches[0].Key)}'.");
        }

        return (matches[0].Key, matches[0].Value);
    }

    // ── Profile Management ──────────────────────────────────────────────

    /// <summary>
    /// Create or update a profile under a host. Creates the host entry if it doesn't exist.
    /// Sets defaults when this is the first host or first profile on a host.
    /// </summary>
    public void SaveProfile(string hostname, string profileName, ProfileConfig profile, string? hostBaseUrl = null)
    {
        var config = LoadConfig();
        hostname = NormalizeHostname(hostname);

        if (!config.Hosts.TryGetValue(hostname, out var host))
        {
            host = new HostConfig
            {
                BaseUrl = (hostBaseUrl ?? "").TrimEnd('/'),
                Profiles = new(),
            };
            config.Hosts[hostname] = host;
        }
        else if (!string.IsNullOrEmpty(hostBaseUrl))
        {
            // If the login URL differs from the host's baseUrl, store as profile override
            var normalizedHostBase = host.BaseUrl.TrimEnd('/');
            var normalizedLoginUrl = hostBaseUrl.TrimEnd('/');
            if (!string.Equals(normalizedHostBase, normalizedLoginUrl, StringComparison.OrdinalIgnoreCase))
                profile.BaseUrl = normalizedLoginUrl;
        }

        host.Profiles[profileName] = profile;

        // First profile on host → set as default
        if (host.DefaultProfile is null || host.Profiles.Count == 1)
            host.DefaultProfile = profileName;

        // First host → set as default
        if (config.DefaultHost is null || config.Hosts.Count == 1)
            config.DefaultHost = hostname;

        SaveConfig(config);
    }

    public void SetDefaultProfile(string hostname, string profileName)
    {
        var config = LoadConfig();
        if (!config.Hosts.TryGetValue(hostname, out var host))
            throw new InvalidOperationException($"Host '{hostname}' does not exist.");
        if (!host.Profiles.ContainsKey(profileName))
            throw new InvalidOperationException($"Profile '{profileName}' does not exist on host '{hostname}'.");
        host.DefaultProfile = profileName;
        SaveConfig(config);
    }

    public void RenameProfile(string hostname, string oldName, string newName)
    {
        var config = LoadConfig();
        if (!config.Hosts.TryGetValue(hostname, out var host))
            throw new InvalidOperationException($"Host '{hostname}' does not exist.");
        if (!host.Profiles.Remove(oldName, out var profile))
            throw new InvalidOperationException($"Profile '{oldName}' does not exist on host '{hostname}'.");
        if (host.Profiles.ContainsKey(newName))
            throw new InvalidOperationException($"Profile '{newName}' already exists on host '{hostname}'.");
        host.Profiles[newName] = profile;
        if (host.DefaultProfile == oldName)
            host.DefaultProfile = newName;
        SaveConfig(config);
    }

    public void DeleteProfile(string hostname, string profileName)
    {
        var config = LoadConfig();
        if (!config.Hosts.TryGetValue(hostname, out var host))
            return;
        host.Profiles.Remove(profileName);

        if (host.DefaultProfile == profileName)
            host.DefaultProfile = host.Profiles.Keys.FirstOrDefault();

        // Last profile → remove the host
        if (host.Profiles.Count == 0)
        {
            config.Hosts.Remove(hostname);
            if (config.DefaultHost == hostname)
                config.DefaultHost = config.Hosts.Keys.FirstOrDefault();
        }

        SaveConfig(config);
    }

    // ── Config Loading & Migration ──────────────────────────────────────

    private CliConfig LoadConfig()
    {
        if (_cache is not null)
            return _cache;

        var configPath = GetConfigFilePath();

        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // New two-level format (has "hosts" or "defaultHost")
            if (root.TryGetProperty("hosts", out _) || root.TryGetProperty("defaultHost", out _))
            {
                _cache = JsonSerializer.Deserialize<CliConfig>(json, JsonOptions) ?? new CliConfig();
                return _cache;
            }

            // Previous flat format (has "activeProfile" and "profiles")
            if (root.TryGetProperty("activeProfile", out _) || root.TryGetProperty("profiles", out _))
            {
                _cache = MigrateFromFlatConfig(json);
                return _cache;
            }
        }

        // Legacy credentials.json
        var legacyPath = Path.Combine(GetConfigDir(), "credentials.json");
        if (File.Exists(legacyPath))
        {
            _cache = MigrateFromLegacyCredentials(legacyPath);
            return _cache;
        }

        _cache = new CliConfig();
        return _cache;
    }

    private CliConfig MigrateFromLegacyCredentials(string legacyPath)
    {
        var json = File.ReadAllText(legacyPath);
        // Legacy file uses PascalCase; PropertyNameCaseInsensitive handles it
        var legacy = JsonSerializer.Deserialize<StoredCredentials>(json, JsonOptions);

        if (legacy is null || string.IsNullOrEmpty(legacy.Server))
            return new CliConfig();

        var hostname = ExtractHostname(legacy.Server);
        var config = new CliConfig
        {
            DefaultHost = hostname,
            Hosts =
            {
                [hostname] = new HostConfig
                {
                    BaseUrl = legacy.Server.TrimEnd('/'),
                    DefaultProfile = "default",
                    Profiles =
                    {
                        ["default"] = new ProfileConfig
                        {
                            Token = NullIfEmpty(legacy.Token),
                            ApiKey = NullIfEmpty(legacy.ApiKey),
                            Username = NullIfEmpty(legacy.UserName),
                            UserId = NullIfEmpty(legacy.UserId),
                        }
                    }
                }
            }
        };

        SaveConfig(config);

        var backup = legacyPath + ".bak";
        try
        {
            if (!File.Exists(backup))
                File.Move(legacyPath, backup);
            else
                File.Delete(legacyPath);
        }
        catch { /* best-effort */ }

        AnsiConsole.MarkupLine($"[dim]Migrated credentials to new profile format. Backup: {Markup.Escape(backup)}[/]");
        return config;
    }

    private CliConfig MigrateFromFlatConfig(string json)
    {
        var flat = JsonSerializer.Deserialize<FlatConfig>(json, JsonOptions);
        if (flat is null)
            return new CliConfig();

        var config = new CliConfig();

        foreach (var (name, fp) in flat.Profiles)
        {
            if (string.IsNullOrEmpty(fp.Server))
                continue;

            var hostname = ExtractHostname(fp.Server);

            if (!config.Hosts.TryGetValue(hostname, out var host))
            {
                host = new HostConfig
                {
                    BaseUrl = fp.Server.TrimEnd('/'),
                    Profiles = new(),
                };
                config.Hosts[hostname] = host;
            }

            var profileName = name == hostname ? "default" : name;
            var profile = new ProfileConfig
            {
                Token = NullIfEmpty(fp.Token),
                ApiKey = NullIfEmpty(fp.ApiKey),
                Username = NullIfEmpty(fp.UserName),
                UserId = NullIfEmpty(fp.UserId),
            };

            // If this profile's server differs from host baseUrl, set override
            if (!string.Equals(fp.Server.TrimEnd('/'), host.BaseUrl, StringComparison.OrdinalIgnoreCase))
                profile.BaseUrl = fp.Server.TrimEnd('/');

            host.Profiles[profileName] = profile;
            host.DefaultProfile ??= profileName;
        }

        // Set default host from the active profile's server
        if (flat.ActiveProfile is not null
            && flat.Profiles.TryGetValue(flat.ActiveProfile, out var active)
            && !string.IsNullOrEmpty(active.Server))
        {
            config.DefaultHost = ExtractHostname(active.Server);
        }
        else if (config.Hosts.Count > 0)
        {
            config.DefaultHost = config.Hosts.Keys.First();
        }

        SaveConfig(config);
        return config;
    }

    private void SaveConfig(CliConfig config)
    {
        _cache = config;
        var dir = GetConfigDir();
        Directory.CreateDirectory(dir);
        var path = GetConfigFilePath();
        var tempPath = path + ".tmp";
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private string GetConfigDir()
    {
        if (!string.IsNullOrEmpty(ConfigPathOverride))
            return Path.GetDirectoryName(ConfigPathOverride) ?? DefaultConfigDir;
        var envPath = Environment.GetEnvironmentVariable("JF_CONFIG");
        if (!string.IsNullOrEmpty(envPath))
            return Path.GetDirectoryName(envPath) ?? DefaultConfigDir;
        return DefaultConfigDir;
    }

    private string GetConfigFilePath()
    {
        if (!string.IsNullOrEmpty(ConfigPathOverride))
            return ConfigPathOverride;
        var envPath = Environment.GetEnvironmentVariable("JF_CONFIG");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;
        return Path.Combine(DefaultConfigDir, "config.json");
    }

    public static string ExtractHostname(string serverUrl)
    {
        if (string.IsNullOrEmpty(serverUrl))
            return string.Empty;

        if (IsFullUrl(serverUrl))
        {
            try
            {
                return new Uri(serverUrl).Host.ToLowerInvariant();
            }
            catch
            {
                return NormalizeHostname(serverUrl);
            }
        }

        return NormalizeHostname(serverUrl);
    }

    private static bool IsFullUrl(string value)
        => value.Contains("://", StringComparison.Ordinal);

    private static string NormalizeHostname(string hostname)
        => hostname.Trim().ToLowerInvariant();

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrEmpty(value) ? null : value;

    private static StoredCredentials ToStoredCredentials(ResolvedContext ctx) => new()
    {
        Server = ctx.BaseUrl,
        Token = ctx.Token ?? string.Empty,
        ApiKey = ctx.ApiKey ?? string.Empty,
        UserId = ctx.UserId ?? string.Empty,
        UserName = ctx.Username ?? string.Empty,
    };

    // ── Legacy flat-config types (for migration only) ───────────────────

    private sealed class FlatConfig
    {
        public string? ActiveProfile { get; set; }
        public Dictionary<string, FlatProfile> Profiles { get; set; } = new();
    }

    private sealed class FlatProfile
    {
        public string Server { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
