using NonSucking.Framework.Serialization;

namespace Backup.Shared;

public static class FileCreateSerializer
{
    public static void Serialize(BinaryWriter bw, DateTime dt) => bw.Write(dt.ToFileTimeUtc());
    public static DateTime Deserialize(BinaryReader br) => DateTime.FromFileTimeUtc(br.ReadInt64());
}

[Nooson]
public partial class BackupFileInfo
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }
    public string FullPath { get; set; }
    public string Name { get; set; }
    [NoosonCustom(DeserializeMethodName = nameof(Deserialize), SerializeMethodName = nameof(Serialize), DeserializeImplementationType = typeof(FileCreateSerializer), SerializeImplementationType = typeof(FileCreateSerializer))]
    public DateTime CreateDate { get; set; }
}
[Nooson]
public partial class BackupFileNameIndex
{
    [NoosonIgnore]
    public uint GetNextIndex => nextIndex++;

    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }

    public Dictionary<uint, BackupFileInfo> Index { get; set; } = new();

    [NoosonInclude]
    private uint nextIndex = 0;

    public BackupFileInfo this[uint key]
    {
        get => Index[key];
        set => Index[key] = value;
    }

}
