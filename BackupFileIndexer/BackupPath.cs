public record struct GlobalBackupPath(string Name, string Path, string Password, bool Disable = false);

public record struct SingleBackupPath(string Name, string Path, string Password, bool OnlyLatest = false);

