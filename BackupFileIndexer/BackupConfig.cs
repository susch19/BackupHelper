public record BackupConfig
{
    public const string ConfigName = "Backup";
    public SingleBackupPath[]? BackupPaths { get; set; }
    public string? SevenZipDllPath { get; set; }
    public GlobalBackupPath GlobalIndex { get; set; }
}
