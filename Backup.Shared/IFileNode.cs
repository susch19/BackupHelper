using System.IO;

namespace Backup.Shared;

public interface IFileNode
{
    HashSet<ushort> BackupFileIndeces { get; set; }
    DateTime ChangeDate { get; set; }
    string FullPath { get; set; }
    string Name { get; set; }

    void Serialize(BinaryWriter bw);
}

public interface IFileNode<T> where T : IFileNode
{

    List<T> Children { get; set; }
    T? Parent { get; set; }

    static abstract T Deserialize(BinaryReader br, T? parent);
}