﻿using BackupService.Events;
using BackupService.Scheduling;

namespace BackupService;

[Nooson]
public partial class BackupTaskConfig
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }

    public bool Enabled { get; set; } = true;
    [NoosonVersioning(nameof(IsVersion3), "true", nameof(Version))]
    public bool FastDifferenceCheck { get; set; } = true;

    public string Path { get; set; } = "";

    [NoosonVersioning(nameof(IsVersion1), "", nameof(Version))]
    public string BackupIgnorePath { get; set; } = "";
    public bool Recursive { get; set; } = true;
    [NoosonVersioning(nameof(IsVersion4), "false", nameof(Version))]
    public bool SplitArchives { get; set; } = false;
    [NoosonVersioning(nameof(IsVersion5), "", nameof(Version))]
    public string ArchiveName { get; set; } = "";
    public string Password { get; set; } = "";

    [NoosonVersioning(nameof(IsVersion2), "", nameof(Version))]
    public bool CredentialManager { get; set; } = true;

    [NoosonVersioning(nameof(IsVersion3), "\"backupIndex.aes.zip\"", nameof(Version))]
    public string BackupIndexPath { get; set; } = "backupIndex.aes.zip";

    public List<string> BackupSources { get; set; } = new();
    public List<string> TargetSources { get; set; } = new();

    public List<BackupEvent> ProgrammEvents { get; set; } = new();

    public List<Schedule> Schedules { get; set; } = new();

    private static bool IsVersion1(int version)=> version > 0;
    private static bool IsVersion2(int version)=> version > 1;
    private static bool IsVersion3(int version)=> version > 2;
    private static bool IsVersion4(int version)=> version > 3;
    private static bool IsVersion5(int version)=> version > 4;

}
