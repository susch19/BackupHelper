namespace Backup.Shared;

public class BackupFileInfo
{
    public string FullPath { get; set; }
    public string Name { get; set; }
    public DateTime CreateDate { get; set; }
}
public class BackupFileNameIndex
{
    public uint GetNextIndex => nextIndex++;

    public Dictionary<uint, BackupFileInfo> Index { get; set; } = new();

    private uint nextIndex = 0;

    public void Serialize(BinaryWriter bw)
    {
        bw.Write((uint)Index.Count);

        foreach (var item in Index)
        {
            bw.Write(item.Key);
            bw.Write(item.Value.FullPath);
            bw.Write(item.Value.Name);
            bw.Write(item.Value.CreateDate.ToFileTimeUtc());
        }
    }

    public void Deserialize(BinaryReader br)
    {
        var count = br.ReadUInt32();
        for (int i = 0; i < count; i++)
        {
            var index = br.ReadUInt32();
            var fullPath = br.ReadString();
            var name = br.ReadString();
            var createDate = br.ReadInt64();
            Index[index] = new BackupFileInfo { FullPath = fullPath, Name = name, CreateDate = DateTime.FromFileTimeUtc(createDate) };
            if (index > nextIndex)
                nextIndex = index;
        }
    }
}
