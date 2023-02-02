using Backup.Shared;

using SevenZip;

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BackupFileIndexer;

public class BackupIndexer
{
    public void CreateMetaDataFiles(BackupConfig backupConfig)
    {
        if (backupConfig is null)
            throw new ArgumentNullException(nameof(backupConfig));

        foreach ((string backupPath, string password) in backupConfig.BackupPaths)
        {
            var pw = Encoding.UTF8.GetBytes(password);

            IGrouping<string?, FileInfo>[] groupFiles =
                backupConfig
                .BackupPaths
                .SelectMany(x =>
                    Directory
                        .GetFiles(backupPath, "*.7z", SearchOption.AllDirectories)
                        .Where(x => !x.EndsWith(".metadata.zip.aes"))
                        .Select(x => new FileInfo(x)))
                .GroupBy(x => x.DirectoryName)
                .ToArray();

            for (int i = 0; i < groupFiles.Length; i++)
            {
                FileInfo[] files = groupFiles[i].OrderBy(x => x.Name).ToArray();

                for (int o = 0; o < files.Length; o++)
                {
                    FileInfo? file = files[o];
                    using SevenZipExtractor extractor = new(file.FullName, password);

                    string metaDataFileName = GetMetaDataFileName(file);
                    if (File.Exists(metaDataFileName))
                    {
                        continue;
                    }

                    (string, DateTime)[] dataLines = extractor.ArchiveFileData.Select(x => (x.FileName, x.LastWriteTime)).ToArray();

                    List<FileNode> returnValues = new();
                    BackupFileNameIndex fileNameIndex;
                    if (o > 0)
                    {
                        string before = GetMetaDataFileName(files[o - 1]);
                        using var fs = File.OpenRead(before);
                        (fileNameIndex, returnValues) = BackupEncryptionHelper.DecryptMetaData<FileNode>(pw, fs);

                        var index = fileNameIndex.GetNextIndex;
                        fileNameIndex.Index[index] = file.Name;
                        BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, returnValues);
                    }
                    else
                    {
                        fileNameIndex = new();
                        var index = fileNameIndex.GetNextIndex;
                        fileNameIndex.Index[index] = file.Name;
                        BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, returnValues);
                    }

                    {
                        using var fs = File.OpenWrite(metaDataFileName);
                        BackupEncryptionHelper.SaveMetaDataFile(pw, fs, returnValues, fileNameIndex);
                    }
                }
            }
        }
    }


    public static string GetMetaDataFileName(FileInfo file) => file.FullName.Replace(".7z", ".metadata.zip.aes");
}
