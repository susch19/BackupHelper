using Backup.Shared;

using Microsoft.AspNetCore.Mvc;

using SevenZip;

using System.Diagnostics;
using System.Text;

namespace BackupRestore.Data;


public class RecoveryService
{
    [HttpPost]
    public (BackupFileNameIndex, List<FileDisplayInfo>) GetBackupInformation(string path, string password)
    {
        using var fs = File.OpenRead(path);

        var header = BackupEncryptionHelper.ReadHeader(fs);

        return BackupEncryptionHelper.DecryptMetaData<FileDisplayInfo>(Encoding.UTF8.GetBytes(password), fs, header.iv);

    }

    public Task<string> FileSave(string filter)
    {
        var file = NativeFileDialogSharp.Dialog.FileSave(filter);
        if (file.IsOk)
        {
            var end = "." + filter;
            if (file.Path.EndsWith(end, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(file.Path);
            return Task.FromResult(file.Path + end);
        }
        return Task.FromResult("");
    }
    public Task<string> GetFilePath(string filter)
    {
        var file = NativeFileDialogSharp.Dialog.FileOpen(filter);
        if (file.IsOk)
            return Task.FromResult(file.Path);
        return Task.FromResult("");
    }
    public Task<string> GetDirectoryPath()
    {
        var dir = NativeFileDialogSharp.Dialog.FolderPicker();
        if (dir.IsOk)
            return Task.FromResult(dir.Path);
        return Task.FromResult("");
    }
    public void RestoreFiles(string restorePath, string password, FileDisplayInfo[] nodes, BackupFileNameIndex index)
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

    public void PreviewFile(string password, FileDisplayInfo node, BackupFileNameIndex index)
    {
        using SevenZipExtractor extractor = new(index.Index[node.HistoryFileSelected].FullPath, password);
        var outputPath = Path.Combine(Path.GetTempPath(), node.Name);
        using (var fs = File.OpenWrite(outputPath))
            extractor.ExtractFile(node.FullPath, fs);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(outputPath) { UseShellExecute = true };
        process.Start();
    }
}
