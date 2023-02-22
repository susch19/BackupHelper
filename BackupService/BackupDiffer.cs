using Ignore;

using SevenZip;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

using CompressionLevel = System.IO.Compression.CompressionLevel;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace BackupService;
public class BackupDiffer
{
    public (Dictionary<string, BackupFileChange> changes, BackupType backupType)? GetChangedFiles(string sourcePath, string secondIgnore, bool fastExit, bool recursive, BackupType backupType)
    {
        var ignore = new BackupIgnore();
        var backupIgnorePath = new FileInfo(Path.Combine(sourcePath, ".backupignore"));
        if (backupIgnorePath.Exists)
        {
            ignore.Add(File.ReadAllLines(backupIgnorePath.FullName).Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        var secondBackupIgnorePath = new FileInfo(secondIgnore);

        if (secondBackupIgnorePath.Exists)
        {
            ignore.Add(File.ReadAllLines(secondBackupIgnorePath.FullName).Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        var dirName = backupIgnorePath.DirectoryName!;
        string backupIndex = "backupIndex.aes.zip";
        BackupFileChangeIndex index;
        var allFiles = Directory
                .GetFiles(sourcePath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(x => !ignore.IsIgnored(x[(dirName.Length + 1)..]))
                .Select(x => new FileInfo(x))
                .Where(x => !ignore.IsIgnored(x))
                .ToArray();
        //File.WriteAllLines("allfiles.txt", allFiles.Select(x => x.FullName).ToArray());
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
        bool onlyNew = true;
        foreach (var file in allFiles)
        {
            byte[] md5;
            var changeDate = file.LastWriteTimeUtc;
            var length = file.Length;
            if ((backupType == BackupType.Incremental || backupType == BackupType.Differential)
                && index.Index.TryGetValue(file.FullName, out var backupInfoDict)
                && backupInfoDict.Count > 0)
            {
                onlyNew = false;
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

        return (changes, onlyNew ? BackupType.Full : backupType);
    }

    public string BackupDetectedChanges(Dictionary<string, BackupFileChange> changes, string sourcePath, string outputPath, string password, BackupType backupType)
    {
        var sourceDir = new DirectoryInfo(sourcePath);
        string outFile = GetBackupFileName(sourceDir, outputPath, backupType);

        var compressor = new SevenZipCompressor
        {
            ArchiveFormat = OutArchiveFormat.SevenZip,
            PreserveDirectoryRoot = true,
            DirectoryStructure = true,
            IncludeEmptyDirectories = false,
            EncryptHeaders = true,
            ZipEncryptionMethod = ZipEncryptionMethod.Aes256
        };

        compressor.CompressFilesEncrypted(outFile, (sourceDir.Parent?.FullName.Length ?? sourceDir.FullName.Length) + 1, password, changes.Keys.ToArray());
        return outFile;
    }

    public static string GetBackupFileName(DirectoryInfo sourceDir, string outputPath, BackupType backupType)
    {
        var fileName = $"{sourceDir.Name} {DateTime.UtcNow:yyyy-MM-dd HH;mm;ss} ({backupType}).7z";
        var outFile = Path.Combine(outputPath, fileName);
        return outFile;
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
