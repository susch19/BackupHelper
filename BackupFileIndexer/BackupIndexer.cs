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
                globalNodes.AddRange(returnValues);
            }
        }

        foreach ((string backupName, string backupPath, string password, bool onlyLatest) in backupConfig.BackupPaths)
        {
            byte[] pw = CredentialHelper.GetCredentialsFor(backupName, password, "Backup Password", ref credManagerSave);
            var clearPassword = Encoding.UTF8.GetString(pw);
            var files =
                Directory
                    .GetFiles(backupPath, "*.7z", SearchOption.AllDirectories)
                    .Select(x => new FileInfo(x))
                    .Select(x => (zip: x, meta: new FileInfo(GetMetaDataFileName(x))))
                    .OrderBy(x => x.zip.Name)
                    .ToArray();

            List<FileNode> returnValues = new();
            BackupFileNameIndex? fileNameIndex = null;

            int o = 0;
            if (onlyLatest)
            {
                o = Array.FindLastIndex(files, x => x.meta.Exists) + 1;
            }

            for (; o < files.Length; o++)
            {
                (FileInfo file, FileInfo metaData) = files[o];
                using SevenZipExtractor extractor = new(file.FullName, clearPassword);

                if (metaData.Exists)
                {
                    (MetaDataHeader header, byte[] iv) header;
                    using (var fs = metaData.OpenRead())
                        header = BackupEncryptionHelper.ReadHeader(fs);
                    if (header.header.Version != CurrentSupportedHeaderVersion)
                        metaData.Delete();
                    else
                        continue;
                }


                (string, DateTime)[] dataLines = extractor.ArchiveFileData.Select(x => (x.FileName, x.LastWriteTime)).ToArray();

                if (o > 0)
                {
                    if (fileNameIndex is null)
                    {
                        var metaBefore = files[o - 1].meta;
                        using var fs = metaBefore.OpenRead();
                        var header = BackupEncryptionHelper.ReadHeader(fs);

                        (fileNameIndex, returnValues) = BackupEncryptionHelper.DecryptMetaData<FileNode>(pw, fs, header.iv);
                    }

                    var index = globalIndex.GetNextIndex;
                    fileNameIndex.Index[index]
                        = globalIndex.Index[index]
                        = new BackupFileInfo { FullPath = file.FullName, Name = file.Name, CreateDate = GetCreateDateOfBackup(file.Name) };

                    BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, returnValues);
                    BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, globalNodes);
                }
                else
                {
                    fileNameIndex ??= new();

                    var index = globalIndex.GetNextIndex;
                    fileNameIndex.Index[index]
                        = globalIndex.Index[index]
                        = new BackupFileInfo { FullPath = file.FullName, Name = file.Name, CreateDate = GetCreateDateOfBackup(file.Name) };

                    BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, returnValues);
                    BackupEncryptionHelper.ConvertToFileNodes(dataLines, index, globalNodes);
                }

                if (!onlyLatest || o + 1 == files.Length)
                {
                    if (onlyLatest)
                    {
                        foreach (var (_, meta) in files.Where(x => x.meta.Exists))
                            meta.Delete();
                    }

                    BackupEncryptionHelper.SortFileNodes(returnValues);
                    using var fs = metaData.OpenWrite();
                    var header = new MetaDataHeader() { Version = CurrentSupportedHeaderVersion };
                    BackupEncryptionHelper.SaveMetaDataFile(pw, fs, header, fileNameIndex, returnValues);
                    returnValues.Clear();
                    fileNameIndex = null;
                }

                Console.WriteLine($"Finished {o + 1} of {files.Length}, name {file.Name}");
            }
        }

        if (!backupConfig.GlobalIndex.Disable)
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