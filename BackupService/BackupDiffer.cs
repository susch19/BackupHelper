using Ignore;

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace BackupService;
public class BackupDiffer
{
    public void Backup(string sourcePath, string backupPath)
    {
        var ignore = new BackupIgnore();
        var backupIgnorePath = new FileInfo(Path.Combine(sourcePath, ".backupignore"));
        if (backupIgnorePath.Exists)
        {
            ignore.Add(File.ReadAllLines(backupIgnorePath.FullName)
                .Where(x=>!string.IsNullOrWhiteSpace(x) && x[0]!='#')
                );
        }

        var dirName = backupIgnorePath.DirectoryName!;
        string backupIndex = "backupIndex.aes.zip";
        BackupFileChangeIndex index;
        using var fs = File.Open(backupIndex, FileMode.OpenOrCreate);
        var allFiles = Directory
                .GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Where(x => !ignore.IsIgnored(x[(dirName.Length + 1)..]))
                .Select(x => new FileInfo(x))
                .ToArray();
        
        if (fs.Length > 0)
        {

            using var ds = new ZLibStream(fs, CompressionMode.Decompress);
            using var br = new BinaryReader(ds);

            index = BackupFileChangeIndex.Deserialize(br);
        }
        else
        {
            index = new();


            BackupFileChange backupInfo;
            foreach (var file in allFiles)
            {
                var changeDate = file.LastWriteTimeUtc;
                var length = file.Length;
                string md5;
                if (index.Index.TryGetValue(file.FullName, out var backupInfoDict))
                {
                    backupInfo = backupInfoDict.Last().Value;

                    if (backupInfo.Length != length || backupInfo.LastWriteTimeUtc != changeDate)
                    {
                        continue;
                    }
                    md5 = Convert.ToBase64String(MD5.HashData(File.OpenRead(file.FullName)));
                    if (backupInfo.MD5 != md5)
                        continue;
                }
                else
                {
                    backupInfoDict = new();
                    index.Index[file.FullName] = backupInfoDict;
                    md5 = Convert.ToBase64String(MD5.HashData(File.OpenRead(file.FullName)));
                }

                backupInfo = new() { MD5 = md5, LastWriteTimeUtc = changeDate, Length = length };
                backupInfoDict[backupInfoDict.Count] = backupInfo;

            }
        }

        using var zl = new ZLibStream(fs, CompressionLevel.Fastest);
        using var bw = new BinaryWriter(zl);
        index.Serialize(bw);
        ;


    }
}
