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

[Nooson]
public partial class Header
{
    public int Version { get; set; }
}

[Nooson]
public partial class BackupFileChange
{
    public string MD5 { get; set; }
    public long Length { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public BackupType BackupType { get; set; }
}

[Nooson]
public partial class BackupFileChangeIndex
{
    public Dictionary<string, Dictionary<int, BackupFileChange>> Index { get; set; } = new();
}