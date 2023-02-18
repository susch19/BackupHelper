namespace BackupService;

public class ConfigPath
{
    public string Path { get; set; } = "";
    public string CredentialName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class BackupsConfig
{
    public const string ConfigName = "Backup";

    public ConfigPath[] ConfigPaths { get; set; } = Array.Empty<ConfigPath>();
    public string SevenZipPath { get; set; } = "";
    public string DefaultPassword { get; set; } = "";
    public string DefaultCredentialName { get; set; } = "";
}
