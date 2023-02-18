using NonSucking.Framework.Serialization;

namespace Backup.Shared;


[Nooson]
public partial class FileNode : IFileNode<FileNode>, IFileNode
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }

    public string Name { get; set; }
    [NoosonIgnore]
    public string FullPath { get; set; }
    [NoosonIgnore]
    public DateTime ChangeDate { get; set; }
    public HashSet<uint> BackupFileIndeces { get; set; } = new();
    [NoosonIgnore]
    public FileNode? Parent { get; set; }
    public List<FileNode> Children { get; set; } = new();

    private FileNode(string name) : this(name, null)
    {
    }

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
    

    public static FileNode Deserialize(BinaryReader br, FileNode? parent)
    {
        var version = br.ReadInt32();
        var name = br.ReadString();

        var fn = new FileNode(name, parent) {Version = version };
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
