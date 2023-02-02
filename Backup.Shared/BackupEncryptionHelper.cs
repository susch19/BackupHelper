using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Backup.Shared;
public class BackupEncryptionHelper
{

    [Pure]
    public static (BackupFileNameIndex, List<T>) DecryptMetaData<T>(byte[] pw, Stream metaDataContent) where T : IFileNode<T>, IFileNode
    {
        var fileNameIndex = new BackupFileNameIndex();
        List<T> returnValues = new();
        Span<byte> iv = stackalloc byte[16];

        using var aes = Aes.Create();

        aes.Key = SHA256.HashData(pw);

        metaDataContent.Read(iv);
        aes.IV = iv.ToArray();
        using var cs = new CryptoStream(metaDataContent, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var zip = new ZLibStream(cs, System.IO.Compression.CompressionMode.Decompress);
        using var br = new BinaryReader(zip);

        fileNameIndex.Deserialize(br);

        var rootNodeCount = br.ReadInt32();
        for (int c = 0; c < rootNodeCount; c++)
        {
            returnValues.Add(T.Deserialize(br, default));
        }
        return (fileNameIndex, returnValues);
    }

    [Pure]
    public static void ConvertToFileNodes((string, DateTime)[] dataLines, ushort index, List<FileNode> returnValues)
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

    [Pure]
    public static void SaveMetaDataFile<T>(byte[] pw, Stream stream, List<T> returnValues, BackupFileNameIndex fileNameIndex) where T : IFileNode<T>, IFileNode
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(pw);
        aes.GenerateIV();
        stream.Write(aes.IV);
        using var cs = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var zip = new ZLibStream(cs, System.IO.Compression.CompressionLevel.Optimal);
        using var bw = new BinaryWriter(zip);

        fileNameIndex.Serialize(bw);

        bw.Write(returnValues.Count);
        foreach (var item in returnValues)
        {
            item.Serialize(bw);
        }
    }
}
