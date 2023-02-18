namespace BackupService;

[Nooson]
public partial class BackupFileChangeIndex
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }

    public Dictionary<string, Dictionary<int, BackupFileChange>> Index { get; set; } = new();
    public DateTime LastChangeUTC { get; set; }
}