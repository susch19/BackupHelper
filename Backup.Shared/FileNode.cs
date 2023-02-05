namespace Backup.Shared;


public class FileNode : IFileNode<FileNode>, IFileNode
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public DateTime ChangeDate { get; set; }
    public HashSet<uint> BackupFileIndeces { get; set; } = new();
    public FileNode? Parent { get; set; }
    public List<FileNode> Children { get; set; } = new();

    public FileNode(string name, FileNode? parent)
    {
        Name = name;
        if (parent is null)
        {
            FullPath = Name;
        }
        else
        {
            FullPath = Path.Combine(parent.FullPath, Name);
        }
        Parent = parent;
    }

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Name);

        bw.Write(BackupFileIndeces.Count);
        foreach (var item in BackupFileIndeces)
        {
            bw.Write(item);
        }
        bw.Write(Children.Count);
        foreach (var item in Children)
        {
            item.Serialize(bw);
        }
    }

    public static FileNode Deserialize(BinaryReader br, FileNode? parent)
    {
        var name = br.ReadString();

        var fn = new FileNode(name, parent);
        var indices = br.ReadInt32();
        for (int i = 0; i < indices; i++)
        {
            fn.BackupFileIndeces.Add(br.ReadUInt32());
        }
        var children = br.ReadInt32();
        for (int i = 0; i < children; i++)
        {
            fn.Children.Add(Deserialize(br, fn));
        }
        return fn;
    }
}
