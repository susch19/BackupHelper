using SevenZip;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupFileIndexer;
public class BackupIndexer
{
    private readonly SevenZipCompressor compressor;
    public BackupIndexer()
    {
        compressor = new()
        {
            ArchiveFormat = OutArchiveFormat.SevenZip,
            DirectoryStructure = false,
            PreserveDirectoryRoot = false,
            CompressionLevel = CompressionLevel.High,
            ZipEncryptionMethod = ZipEncryptionMethod.Aes256,
            EncryptHeaders = false
        };
    }

    public void CreateMetaDataFiles(BackupConfig backupConfig)
    {
        if (backupConfig is null)
            throw new ArgumentNullException(nameof(backupConfig));

        foreach ((string Path, string Password) in backupConfig.BackupPaths)
        {
            IGrouping<string?, FileInfo>[] groupFiles =
                backupConfig
                .BackupPaths
                .SelectMany(x =>
                    Directory
                        .GetFiles(Path, "*.7z", SearchOption.AllDirectories)
                        .Where(x => !x.EndsWith(".metadata.7z"))
                        .Select(x => new FileInfo(x)))
                .GroupBy(x => x.DirectoryName)
                .ToArray();

            for (int i = 0; i < groupFiles.Length; i++)
            {
                FileInfo[] files = groupFiles[i].OrderBy(x => x.Name).ToArray();

                for (int o = 0; o < files.Length; o++)
                {
                    FileInfo? file = files[o];
                    using SevenZipExtractor extractor = new(file.FullName, Password);

                    string metaDataFileName = GetMetaDataFileName(file);
                    if (File.Exists(metaDataFileName))
                        continue;

                    string[] data = extractor.ArchiveFileData.Select(x => x.FileName).ToArray();

                    if (file.FullName.Contains("(Incremental)") && o > 0)
                    {
                        string before = GetMetaDataFileName(files[o - 1]);
                        using SevenZipExtractor beforeExtractor = new(before, Password);
                        using MemoryStream ms = new();
                        beforeExtractor.ExtractFile("metadata", ms);
                        ms.Position = 0;
                        using StreamReader sr = new(ms);
                        string[] newLines = sr.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                        ReplaceWithNewData(data, newLines);
                        data = newLines;
                    }

                    File.WriteAllLines("metadata", data.Select(x => string.Join((char)0, x, file.Name)));
                    compressor.CompressFilesEncrypted(metaDataFileName, Password, "metadata");
                    File.Delete("metadata");
                }
            }
        }
    }

    private static void ReplaceWithNewData(Span<string> newLines, Span<string> oldLines)
    {
        if (newLines.Length <= 0)
            return;
        
        for (int i = 0; i < oldLines.Length; i++)
        {
            string? oldLine = oldLines[i];

            foreach (var newLine in newLines)
            {
                if (!MemoryExtensions.StartsWith(oldLine, newLine, StringComparison.OrdinalIgnoreCase))
                    continue;

                oldLines[i] = newLine;
                break;
            }
        }
    }

    public static string GetMetaDataFileName(FileInfo file) => file.FullName.Replace(".7z", ".metadata.7z");
}
