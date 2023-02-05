using Backup.Shared;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupRestore.Data;


public class FileDisplayInfo : FileNode, IFileNode<FileDisplayInfo>
{
    private bool isExpanded;

    private bool isSelected;
    public bool IsSelected { get => isSelected; set { if (isSelected == value) return; isSelected = value; ToggleOther(); } }
    public bool IsExpanded { get => (isExpanded && (Parent is null || Parent.IsExpanded)); set => isExpanded = value; }
    public bool AnyChildSelected => Children.Any(x => x.IsSelected || x.AnyChildSelected);

    public uint HistoryFileSelected { get; set; } = uint.MaxValue;

    public new List<FileDisplayInfo> Children { get; set; } = new();
    public new FileDisplayInfo? Parent { get; set; }

    public FileDisplayInfo(string name, FileNode? parent) : base(name, parent)
    {
        if (parent is FileDisplayInfo fid)
            Parent = fid;
    }

    private void ToggleOther()
    {
        foreach (var item in Children)
        {
            item.IsSelected = isSelected;
        }

        void ToggleParent(FileDisplayInfo node)
        {
            if (!node.isSelected && node.Parent is not null)
            {
                node.Parent.isSelected = node.isSelected;
                ToggleParent(node.Parent);
            }
        }
        if (!isSelected && Parent is not null)
            ToggleParent(this);

    }

    public static bool Matches(FileDisplayInfo node, Regex filter, bool fulltext)
    {
        if(!fulltext)
            return filter.IsMatch(node.Name) || node.Children.Any(x => Matches(x, filter, fulltext));
        var match =filter.Match(node.Name);
        if (!match.Success || match.Index != 0 || match.Length != node.Name.Length)
        {
            if (node.Children.Any(x => Matches(x, filter, fulltext)))
                return true;
            return false;
        }
        return true;
    }

    public static FileDisplayInfo Deserialize(BinaryReader br, FileDisplayInfo? parent)
    {
        var name = br.ReadString();

        var fn = new FileDisplayInfo(name, parent);
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

