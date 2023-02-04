using Backup.Shared;

using Microsoft.AspNetCore.Mvc;

using SevenZip;

using System.Text;

namespace BackupRestore.Data;

public class MetaDataFileUpload
{
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }
}


public class RecoveryService
{
    [HttpPost]
    public (BackupFileNameIndex, List<FileDisplayInfo>) GetBackupInformation(MetaDataFileUpload files, string password)
    {
        using var uploadMS = new MemoryStream(files.FileContent);

        var header = BackupEncryptionHelper.ReadHeader(uploadMS);

        return BackupEncryptionHelper.DecryptMetaData<FileDisplayInfo>(Encoding.UTF8.GetBytes(password), uploadMS, header.iv);

    }

    public async Task RestoreFiles(string restorePath, string password, FileDisplayInfo[] nodes, BackupFileNameIndex index)
    {
        var grouped = nodes
            .GroupBy(x => x.HistoryFileSelected == ushort.MaxValue ? x.BackupFileIndeces.Last() : x.HistoryFileSelected);
        foreach (var item in grouped.OrderBy(x => x.Key))
        {
            var dirInfo = Directory.CreateDirectory(restorePath);
            using SevenZipExtractor extractor = new(index.Index[item.Key].FullPath, password);
            extractor.ExtractFiles(dirInfo.FullName, item.Select(x => x.FullPath).ToArray());
        }
    }
}
