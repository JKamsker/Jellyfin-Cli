namespace Jellyfin.Cli.Common;

/// <summary>Root config file structure (<c>config.json</c>).</summary>
public sealed class CliConfig
{
    public string? DefaultHost { get; set; }
    public Dictionary<string, HostConfig> Hosts { get; set; } = new();
}

/// <summary>A single Jellyfin server identified by hostname.</summary>
public sealed class HostConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string? DefaultProfile { get; set; }
    public List<string>? Aliases { get; set; }
    public Dictionary<string, ProfileConfig> Profiles { get; set; } = new();
}

/// <summary>Credentials and optional base URL override for one account on a host.</summary>
public sealed class ProfileConfig
{
    public string? BaseUrl { get; set; }
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? UserId { get; set; }
    public string? ApiKey { get; set; }
}

/// <summary>Fully resolved connection context returned by <see cref="CredentialStore.Resolve"/>.</summary>
public sealed class ResolvedContext
{
    public string Hostname { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string? Token { get; init; }
    public string? ApiKey { get; init; }
    public string? Username { get; init; }
    public string? UserId { get; init; }
}

/// <summary>
/// Backward-compatible credential view used by existing commands.
/// Not serialized to disk — only returned by <see cref="CredentialStore.Load"/>.
/// </summary>
public sealed class StoredCredentials
{
    public string Server { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
