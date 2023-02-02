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

        return BackupEncryptionHelper.DecryptMetaData<FileDisplayInfo>(Encoding.UTF8.GetBytes(password), uploadMS);

    }
}
