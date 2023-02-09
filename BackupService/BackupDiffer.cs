using Backup.Shared;

using Ignore;

using SevenZip;

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CompressionMode = System.IO.Compression.CompressionMode;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace BackupService;
public class BackupDiffer
{
    public Dictionary<string, BackupFileChange>? GetChangedFiles(string sourcePath, bool fastExit, BackupType backupType)
    {
        var ignore = new BackupIgnore();
        var backupIgnorePath = new FileInfo(Path.Combine(sourcePath, ".backupignore"));
        if (backupIgnorePath.Exists)
        {
            ignore.Add(File.ReadAllLines(backupIgnorePath.FullName)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x[0] != '#'));
        }

        var dirName = backupIgnorePath.DirectoryName!;
        string backupIndex = "backupIndex.aes.zip";
        BackupFileChangeIndex index;
        var allFiles = Directory
                .GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Where(x => !ignore.IsIgnored(x[(dirName.Length + 1)..]))
                .Select(x => new FileInfo(x))
                .ToArray();

        if (File.Exists(backupIndex))
        {
            using var fs = File.Open(backupIndex, FileMode.Open);
            using var ds = new ZLibStream(fs, CompressionMode.Decompress);
            using var br = new BinaryReader(ds);

            index = BackupFileChangeIndex.Deserialize(br);
        }
        else
        {
            index = new();
        }
        var indexChange = index.LastChangeUTC;
        BackupFileChange backupInfo;
        Dictionary<string, BackupFileChange> changes = new();

        foreach (var file in allFiles)
        {
            byte[] md5;
            var changeDate = file.LastWriteTimeUtc;
            var length = file.Length;
            if ((backupType == BackupType.Incremental || backupType == BackupType.Differential)
                && index.Index.TryGetValue(file.FullName, out var backupInfoDict)
                && backupInfoDict.Count > 0)
            {
                if (backupType == BackupType.Differential)
                    backupInfo = backupInfoDict.Last(x => x.Value.BackupType == BackupType.Full).Value;
                else
                    backupInfo = backupInfoDict.Last().Value;

                if (fastExit && backupInfo.Length == length && backupInfo.LastWriteTimeUtc == changeDate)
                {
                    continue;
                }
                try
                {
                    md5 = MD5.HashData(File.OpenRead(file.FullName));
                }
                catch (Exception)
                {
                    continue;
                }

                if (md5.SequenceEqual(backupInfo.MD5))
                {
                    continue;
                }
            }

            else
            {
                try
                {
                    md5 = MD5.HashData(File.OpenRead(file.FullName));
                }
                catch (Exception)
                {
                    continue;
                }
            }
            index.LastChangeUTC = DateTime.UtcNow;
            backupInfo = new() { MD5 = md5, LastWriteTimeUtc = changeDate, Length = length, BackupType = backupType };
            changes[file.FullName] = backupInfo;
        }

        if (index.LastChangeUTC == indexChange) //No changes made
            return null;

        return changes;
    }

    public void BackupDetectedChanges(Dictionary<string, BackupFileChange> changes, string sourcePath, string outputPath, string password, BackupType backupType)
    {
        var sourceDir = new DirectoryInfo(sourcePath);
        var fileName = $"{sourceDir.Name} {DateTime.UtcNow:yyyy-MM-dd HH;mm;ss} ({backupType}).7z";
        var compressor = new SevenZipCompressor
        {
            ArchiveFormat = OutArchiveFormat.SevenZip,
            PreserveDirectoryRoot = true,
            DirectoryStructure = true,
            IncludeEmptyDirectories = false,
            EncryptHeaders = true,
            ZipEncryptionMethod = ZipEncryptionMethod.Aes256
        };

        var outFile = Path.Combine(outputPath, fileName);
        compressor.CompressFilesEncrypted(outFile, (sourceDir.Parent?.FullName.Length ?? sourceDir.FullName.Length) + 1, password, changes.Keys.ToArray());

    }

    public void StoreNewChangesInIndex(Dictionary<string, BackupFileChange> changes)
    {

        string backupIndex = "backupIndex.aes.zip";
        BackupFileChangeIndex index;
        if (File.Exists(backupIndex))
        {
            using var fs = File.Open(backupIndex, FileMode.OpenOrCreate);
            using var ds = new ZLibStream(fs, CompressionMode.Decompress);
            using var br = new BinaryReader(ds, Encoding.UTF8);

            index = BackupFileChangeIndex.Deserialize(br);
        }
        else
        {
            index = new();
        }
        foreach (var change in changes)
        {

            if (!index.Index.TryGetValue(change.Key, out var backupInfoDict))
            {
                index.Index[change.Key] = backupInfoDict = new();
            }
            backupInfoDict[backupInfoDict.Count] = change.Value;
        }
        {

            using var fs = File.Open(backupIndex, FileMode.OpenOrCreate);
            using var zlibStream = new ZLibStream(fs, CompressionLevel.Fastest);
            using var bw = new BinaryWriter(zlibStream, Encoding.UTF8);
            index.Serialize(bw);
        }
    }
}
