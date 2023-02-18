namespace BackupService;

[Nooson]
public partial class Header
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }
}
