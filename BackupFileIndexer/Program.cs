


using BackupFileIndexer;

using Microsoft.Extensions.Configuration;

using SevenZip;

using System.CodeDom.Compiler;
using System.Globalization;

IConfigurationRoot configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

BackupConfig backupConfig = new();
configuration.Bind(BackupConfig.ConfigName, backupConfig);

SevenZipBase.SetLibraryPath(backupConfig.SevenZipDllPath);


var backupIndexer = new BackupIndexer();
backupIndexer.CreateMetaDataFiles(backupConfig);
