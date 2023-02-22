using Backup.Shared;

using SevenZip;

using System.Text;
using System.Text.RegularExpressions;

namespace BackupFileIndexer;

public partial class BackupIndexer
{
    const byte CurrentSupportedHeaderVersion = 3;
    bool credManagerSave = true;

    public void CreateMetaDataFiles(BackupConfig backupConfig)
    {
        if (backupConfig is null)
            throw new ArgumentNullException(nameof(backupConfig));

        byte[] globalIndexPW = CredentialHelper.GetCredentialsFor(backupConfig.GlobalIndex.Name, backupConfig.GlobalIndex.Password, "Backup Password", ref credManagerSave);

        BackupFileNameIndex globalIndex = new();
        var globalNodes = new List<FileNode>();
        if (File.Exists(backupConfig.GlobalIndex.Path))
        {
            using var fs = File.OpenRead(backupConfig.GlobalIndex.Path);

            var header = BackupEncryptionHelper.ReadHeader(fs);
            if (header.header.Version == CurrentSupportedHeaderVersion)
            {
                var (fileNameIndex, returnValues) = BackupEncryptionHelper.DecryptMetaData<FileNode>(globalIndexPW, fs, header.iv);
                globalIndex = fileNameIndex;
                globalNodes = returnValues;
            }
        }

        foreach ((string backupName, string backupPath, string password) in backupConfig.BackupPaths)
        {
            byte[] pw = CredentialHelper.GetCredentialsFor(backupName, password, "Backup Password", ref credManagerSave);
            var clearPassword = Encoding.UTF8.GetString(pw);
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
                    using SevenZipExtractor extractor = new(file.FullName, clearPassword);

                    string metaDataFileName = GetMetaDataFileName(file);

                    if (File.Exists(metaDataFileName))
                    {
                        (MetaDataHeader header, byte[] iv) header;
                        using (var fs = File.OpenRead(metaDataFileName))
                            header = BackupEncryptionHelper.ReadHeader(fs);
                        if (header.header.Version != CurrentSupportedHeaderVersion)
                            File.Delete(metaDataFileName);
                        else
                            continue;
                    }


                    (string, DateTime)[] dataLines = extractor.ArchiveFileData.Select(x => (x.FileName, x.LastWriteTime)).ToArray();

                    List<FileNode> returnValues = new();
                    BackupFileNameIndex fileNameIndex;
                    if (o > 0)
                    {
                        string before = GetMetaDataFileName(files[o - 1]);
                        using var fs = File.OpenRead(before);
                        var header = BackupEncryptionHelper.ReadHeader(fs);

                        (fileNameIndex, returnValues) = BackupEncryptionHelper.DecryptMetaData<FileNode>(pw, fs, header.iv);

                        var index = globalIndex.GetNextIndex;
                        fileNameIndex.Index[index]
                            = globalIndex.Index[index]
                            = new BackupFileInfo { FullPath = file.FullName, Name = file.Name, CreateDate = GetCreateDateOfBackup(file.Name) };

                        BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, returnValues);
                        BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, globalNodes);
                    }
                    else
                    {
                        fileNameIndex = new();
                        var index = globalIndex.GetNextIndex;
                        fileNameIndex.Index[index]
                            = globalIndex.Index[index]
                            = new BackupFileInfo { FullPath = file.FullName, Name = file.Name, CreateDate = GetCreateDateOfBackup(file.Name) };

                        BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, returnValues);
                        BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, globalNodes);
                    }

                    {
                        BackupEncryptionHelper.SortFileNodes(returnValues);
                        using var fs = File.OpenWrite(metaDataFileName);
                        var header = new MetaDataHeader() { Version = CurrentSupportedHeaderVersion };
                        BackupEncryptionHelper.SaveMetaDataFile(pw, fs, header, fileNameIndex, returnValues);
                    }
                }
            }
        }


        {
            File.Delete(backupConfig.GlobalIndex.Path);
            BackupEncryptionHelper.SortFileNodes(globalNodes);
            using var fs = File.OpenWrite(backupConfig.GlobalIndex.Path);
            var header = new MetaDataHeader() { Version = CurrentSupportedHeaderVersion };
            BackupEncryptionHelper.SaveMetaDataFile(globalIndexPW, fs, header, globalIndex, globalNodes);
        }
    }

    [GeneratedRegex("[0-4]{4}-[0-9]{2}-[0-9]{2}")]
    private partial Regex GetDateRegex();
    const string datePattern = "yyyy-MM-dd HH;mm;ss";

    private DateTime GetCreateDateOfBackup(string name)
    {
        var match = GetDateRegex().Match(name);
        if (match.Success)
        {
            if (DateTime.TryParseExact(name[match.Index..(match.Index + datePattern.Length)], datePattern, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var dateTime))
                return dateTime;
        }
        return new DateTime(2000, 01, 01);
    }


    public static string GetMetaDataFileName(FileInfo file) => file.FullName.Replace(".7z", ".metadata.zip.aes");
}