using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backup.Shared;
public class MetaDataHeader
{
    public int Version { get; set; }

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Version);
    }

    public static MetaDataHeader Deserialize(BinaryReader br)
    {
        MetaDataHeader header = new();
        header.Version = br.ReadInt32();
        return header;
    }
}

public class BackupEncryptionHelper
{

    public static (MetaDataHeader header, byte[] iv) ReadHeader(Stream content)
    {
        var fileNameIndex = new BackupFileNameIndex();
        Span<byte> iv = stackalloc byte[16];

        content.Read(iv);

        using var br = new BinaryReader(content, Encoding.UTF8, leaveOpen: true);

        return (MetaDataHeader.Deserialize(br), iv.ToArray());
    }

    public static (BackupFileNameIndex, List<T>) DecryptMetaData<T>(byte[] pw, Stream metaDataContent, byte[] iv) where T : IFileNode<T>, IFileNode
    {
        List<T> returnValues = new();

        using var aes = Aes.Create();

        aes.Key = SHA256.HashData(pw);
        aes.IV = iv;
        using var cs = new CryptoStream(metaDataContent, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var zip = new ZLibStream(cs, System.IO.Compression.CompressionMode.Decompress);
        using var br = new BinaryReader(zip);


        BackupFileNameIndex fileIndex = new();
        fileIndex.Deserialize(br);
        var rootNodeCount = br.ReadInt32();
        for (int c = 0; c < rootNodeCount; c++)
        {
            returnValues.Add(T.Deserialize(br, default));
        }
        return (fileIndex, returnValues);
    }

    public static void SaveMetaDataFile<T>(byte[] pw, Stream stream, MetaDataHeader header, BackupFileNameIndex fileIndex, List<T> returnValues) where T : IFileNode<T>, IFileNode
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(pw);
        aes.GenerateIV();
        stream.Write(aes.IV);
        {
            using var bw = new BinaryWriter(stream, Encoding.UTF8, true);
            header.Serialize(bw);
        }
        {
            using var cs = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using var zip = new ZLibStream(cs, System.IO.Compression.CompressionLevel.Optimal);
            using var bw = new BinaryWriter(zip);
            fileIndex.Serialize(bw);
            bw.Write(returnValues.Count);
            foreach (var item in returnValues)
            {
                item.Serialize(bw);
            }
        }
    }

    public static void ConvertToFileNodes((string, DateTime)[] dataLines, uint index, List<FileNode> returnValues)
    {
        for (int d = 0; d < dataLines.Length; d++)
        {
            string? line = dataLines[d].Item1;
            var data = line.Split((char)0);
            var groups = data[0].Split(Path.DirectorySeparatorChar);
            FileNode? previousGroup = null;
            foreach (var group in groups)
            {
                if (previousGroup is null)
                {
                    previousGroup = returnValues.FirstOrDefault(x => x.Name == group);
                    if (previousGroup is null)
                    {
                        previousGroup = new(group, null);
                        returnValues.Add(previousGroup);
                    }
                }
                else
                {
                    var newPreviousGroup = previousGroup.Children.FirstOrDefault(x => x.Name == group);
                    if (newPreviousGroup is not null)
                        previousGroup = newPreviousGroup;
                    else
                    {
                        newPreviousGroup = new(group, previousGroup);
                        previousGroup.Children.Add(newPreviousGroup);
                        previousGroup = newPreviousGroup;
                    }
                }
            }
            if (previousGroup is not null)
            {
                previousGroup.ChangeDate = dataLines[d].Item2;
                previousGroup.BackupFileIndeces.Add(index);
            }
        }

    }

    public static void SortFileNodes(List<FileNode> nodes)
    {
        foreach (var node in nodes)
        {
            Stack<FileNode> stack = new();
            stack.Push(node);

            while (stack.Count > 0)
            {
                var  current = stack.Pop();

                current.Children = current.Children.OrderByDescending(x => x.Children.Any()).ThenBy(x => x.Name).ToList();
                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.Children[i]);
                }
            }
        }
    }

}
