using Backup.Shared;

using BackupService;
using BackupService.Scheduling;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackupRestore.Data;


public class BackupAppConfig
{
    public BackupsConfig Backup { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }

    [JsonIgnore]
    public List<BackupTaskConfig> BackupTaskConfigs { get; } = new();
}

public class ConfigService
{
    private bool saveCredentials = true;
    private JsonSerializerOptions jsonOptions;
    public ConfigService()
    {
        jsonOptions = new JsonSerializerOptions() { WriteIndented = true, };
    }

    public BackupAppConfig LoadAppConfig(string appConfigPath)
    {
        var content = File.ReadAllText(appConfigPath);

        var appConfig = JsonSerializer.Deserialize<BackupAppConfig>(content);

        ScheduleManager.CheckBackupConfigs(appConfig.Backup, appConfig.BackupTaskConfigs, ref saveCredentials);

        foreach (var item in appConfig.BackupTaskConfigs.Where(x => x.Version == 0))
        {
            item.BackupIgnorePath = Path.Combine(Path.GetDirectoryName(item.Path), ".backupignore");
            item.Version = 1;
        }
        foreach (var item in appConfig.BackupTaskConfigs.Where(x => x.Version ==1))
        {
            item.Version = 2;
        }

        return appConfig;
    }

    public void Save(BackupTaskConfig config, ConfigPath metadata, BackupAppConfig appConfig, string appConfigPath)
    {
        var password = CredentialHelper.GetCredentialsFor(metadata.CredentialName, metadata.Password, "Config Password", ref saveCredentials);

        using (var fs = File.OpenWrite(config.Path))
        {
            using var writer = BackupEncryptionHelper.OpenEncryptedWriter(fs, password);
            config.Serialize(writer);
        }

        File.WriteAllText(appConfigPath, JsonSerializer.Serialize(appConfig, jsonOptions));
    }
    public void Delete(BackupTaskConfig config, BackupAppConfig appConfig, string appConfigPath)
    {
        File.Delete(config.Path);

        File.WriteAllText(appConfigPath, JsonSerializer.Serialize(appConfig, jsonOptions));
    }
}
