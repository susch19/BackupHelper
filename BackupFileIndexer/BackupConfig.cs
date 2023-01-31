public record BackupConfig
{
    public const string ConfigName = "Backup";
    public BackupPath[]? BackupPaths { get; set; }
    public string? SevenZipDllPath { get; set; }
}
