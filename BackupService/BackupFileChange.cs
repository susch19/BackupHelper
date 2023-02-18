namespace BackupService;

[Nooson]
public partial class BackupFileChange
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }
    public byte[] MD5 { get; set; } = Array.Empty<byte>();
    public long Length { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public BackupType BackupType { get; set; }

}
