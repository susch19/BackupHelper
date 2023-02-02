namespace Backup.Shared;

public class BackupFileNameIndex
{
    public ushort GetNextIndex => nextIndex++;

    public Dictionary<ushort, string> Index { get; set; } = new();

    private ushort nextIndex = 0;

    public void Serialize(BinaryWriter bw)
    {
        bw.Write((ushort)Index.Count);

        foreach (var item in Index)
        {
            bw.Write(item.Key);
            bw.Write(item.Value);
        }
    }

    public void Deserialize(BinaryReader br)
    {
        nextIndex = br.ReadUInt16();
        for (int i = 0; i < nextIndex; i++)
            Index[br.ReadUInt16()] = br.ReadString();
    }
}
