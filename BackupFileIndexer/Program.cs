


using Microsoft.Extensions.Configuration;

using SevenZip;


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var backupConfig = new BackupConfig();
configuration.Bind(BackupConfig.ConfigName, backupConfig);


foreach ((string Path, string Password) in backupConfig.BackupPaths)
{



    var groupFiles =
        backupConfig
        .BackupPaths
        .SelectMany(x =>
            Directory
                .GetFiles(Path, "*.7z", SearchOption.AllDirectories)
                .Where(x => !x.EndsWith(".metadata.7z"))
                .Select(x => new FileInfo(x)))
        .GroupBy(x => x.DirectoryName)
        .ToArray();

    SevenZipBase.SetLibraryPath(backupConfig.SevenZipDllPath);

    for (int i = 0; i < groupFiles.Length; i++)
    {
        var files = groupFiles[i].OrderBy(x => x.Name).ToArray();

        for (int o = 0; o < files.Length; o++)
        {
            FileInfo? file = files[o];
            using var extractor = new SevenZipExtractor(file.FullName, Password);

            var metaDataFileName = GetMetaDataFileName(file);
            if (File.Exists(metaDataFileName))
                continue;
            var data = extractor.ArchiveFileData.Select(x => x.FileName).ToArray();
            if (file.FullName.Contains("(Incremental)") && o > 0)
            {
                var before = GetMetaDataFileName(files[o - 1]);
                using var beforeExtractor = new SevenZipExtractor(before, Password);
                using var ms = new MemoryStream();
                beforeExtractor.ExtractFile("metadata", ms);
                ms.Position = 0;
                using var sr = new StreamReader(ms);
                var lines = sr.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length > 0)
                {
                    for (int k = 0; k < lines.Length; k++)
                    {
                        string? item = lines[k];
                        var existingLineIndex = Array.FindIndex(data, x => item.StartsWith(x, StringComparison.OrdinalIgnoreCase));
                        if (existingLineIndex < 0)
                            continue;
                        lines[k] = data[existingLineIndex];
                    }
                }
                data = lines;
            }

            File.WriteAllLines("metadata", data.Select(x => string.Join((char)0, x, file.Name)));
            var compressor = new SevenZipCompressor()
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false,
                PreserveDirectoryRoot = false,
                CompressionLevel = CompressionLevel.High,
                ZipEncryptionMethod = ZipEncryptionMethod.Aes256,
                EncryptHeaders = false
            };

            compressor.CompressFilesEncrypted(metaDataFileName, Password, "metadata");
            File.Delete("metadata");
        }
    }
}

static string GetMetaDataFileName(FileInfo file)
{
    return file.FullName.Replace(".7z", ".metadata.7z");
}


public record BackupPath(string Path, string Password);
public record BackupConfig
{
    public const string ConfigName = "Backup";
    public BackupPath[] BackupPaths { get; set; }
    public string SevenZipDllPath { get; set; }
}
