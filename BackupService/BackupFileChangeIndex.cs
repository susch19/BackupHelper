using NonSucking.Framework.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupService;
public enum BackupType
{
    Full,
    Differential,
    Incremental,
}

public class BackupConfig
{
    public bool Enabled { get; set; }
    public bool Recursive { get; set; }
    public string Schedule { get; set; }
    public string Password { get; set; }
    public bool IgnoreEmptyDirectories { get; set; }
    public List<string> Filter  { get; set; }
    public List<string> BackupSources { get; set; }
    public List<string> TargetSources { get; set; }
}

[Nooson]
public partial class Header
{
    public int Version { get; set; }
}

[Nooson]
public partial class BackupFileChange
{
    public byte[] MD5 { get; set; } = Array.Empty<byte>();
    public long Length { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public BackupType BackupType { get; set; }
}

[Nooson]
public partial class BackupFileChangeIndex
{
    public Dictionary<string, Dictionary<int, BackupFileChange>> Index { get; set; } = new();
    public DateTime LastChangeUTC { get; set; }
}