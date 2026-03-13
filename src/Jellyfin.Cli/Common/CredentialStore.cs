using System.Text.Json;

namespace Jellyfin.Cli.Common;

public sealed class CredentialStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jf");
    private static readonly string CredentialFile = Path.Combine(ConfigDir, "credentials.json");

    public StoredCredentials? Load()
    {
        if (!File.Exists(CredentialFile))
            return null;

        var json = File.ReadAllText(CredentialFile);
        return JsonSerializer.Deserialize<StoredCredentials>(json);
    }

    public void Save(StoredCredentials credentials)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CredentialFile, json);
    }

    public void Delete()
    {
        if (File.Exists(CredentialFile))
            File.Delete(CredentialFile);
    }
}

public sealed class StoredCredentials
{
    public string Server { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
