
## What is this repository?

This will be a tool collection to use in conjuction with backup software, that stores files in 7z files, like Cobian Reflector.

The goal is to be a bit more independent of closed sourced software, without the shortcomings that comes with developing your own backup software from scratch or having to maintain someone elses backup software. Every tool in this collection will most likely only fullfill one purpose, this way it is easier to maintain and everyone can get only the software they need. 

___

## BackupService 

So much for "shortcomings that comes with developing your own backup software from scratch", because that is what this software mostly is. 

It uses 7 zip to archive the files with an encryption and for restore purposes the backup file indexer in conjunction with the BackupRestore can be used.

### Why make your own backup service?

I didn't like, that no backup solution on the market, could provide the features I wanted and there wasn't an open source backup tool that could be forked and continued, so it left me only with the choice of starting from scratch.

The current version is sadly not unit tested, as I didn't have the time to do so yet, but it is working in the background for me and doing my backups, so it seems to work. Of cource take this with a huge grain of salt, as it often works for the developer.

### Features

- "Unlimited" Backup Configs
- Seperat cross plattform config gui (Part of BackupRestore Application of this repo)
- Gitignore like backup path filterings (Called backupignore)
- Custom Filtering for regex, file size and change dates inside the backupignore
- Up to two backupignore per backup config (Path of one can be configured)
- Usage of the CredentialManager (Under Windows) for the storage of the password
- Multiple targets for the same backup (Copy of the created archive will be created)
- Scheduling via Interval, Cron (Syntax) or period with accuracy of 15 seconds 
- Optional Fast change detection with file change date and size

### Installation

For hosting it as a service I used the NSSM (NonSuckingServiceManager). Other than that you only need 7-Zip dll (Part of the installation of 7-Zip). 

Currently you still have to compile this project, for this you would need .NET 7. 

After compilation you would only need to configure the appsettings.json by putting the 7-Zip dll path into it and the rest can be done via the GUI.

___

## BackupFileIndexer (For Cobian Reflector and BackupService)
This Tool created a metadata per archive. In this metadata it is stored, when every file of the full backup was last backuped and in which archive it can be found.
Also a global index file will be created, so all backups can be searched through at once.

Restoring backups for bigger incremental backups can be a pain otherwise.

Currently it is required, that the option "Include backup type in the name" is enabled, so only based on the file it is known, if it is a full or incremental backup.

### Sample Config
____
```json
{
  "Backup": {
    "BackupPaths": [
      {
        "Name": "Main",
        "Path": "G:\\Backups\\",
        "Password": ""
      }
    ],
    "GlobalIndex": {
      "Name": "Global",
      "Path": "G:\\Backups\\GlobalMetaData.metadata.zip.aes",
      "Password": ""
    },
    "SevenZipDllPath": "C:\\Program Files\\7-Zip\\7z.dll"
  }
}
```

1. BackupPath: This includes multiple path metadata for the backups
   1. Name is just the human readable name, so when you get asked for the passwort in credential manager, this will be used and also for the storing in credential manager
   2. Path the backup location. Nested directories will be searched through aswell.
   3. Password is currently only required under linux, for windows we use the credential manager. This means for the first run, you will be asked for the password (for each backup path). After that it can be retrieved from the credential manager for automatic index creation.
2. GlobalIndex is just the info, where to store the global index file to, it can have a different password than the backup itself
3. SevenZipDllPath: The Path where the 7z.dll is stored

## BackupRestore
This is the second tool and focusses on backup restoration of backups, which where indexed with the *BackupFileIndexer*. It's written with blazor and hosted like a desktop app with Photino.Blazor. 


### Currently supported Features include:
- Searching through the backup with regex, including Full Text Search (Must match completely)
- Filtering for Min and Max Date
- Restore Folders with the files or just the files themselves with the original folder structure
- Restore files at specific dates, for example one file can be restored from the backup of 01.01.2023 while another one in the same folder from 10.01.2023
- Preview of single files via double clicking the name. The selected Date will be respected for the open operation
- Opening somewhat big backups (Currently only tested with 40k Files, takes 200ms and uses 240MB of Ram)
- Dark Design only
- Native (OS) File open Dialog (Sadly no browser dialog, because one cannot get the full path of a file from that)

### Screenshots:

#### Configure Overview
<img src="https://github.com/susch19/BackupHelper/blob/master/AdditionalFiles/Configure.png">

#### Configure Single Backup
<img src="https://github.com/susch19/BackupHelper/blob/master/AdditionalFiles/ConfigureBackup.png">

#### Restore Start
<img src="https://github.com/susch19/BackupHelper/blob/master/AdditionalFiles/RestoreStart.png">

#### Restore Select and Filter
<img src="https://github.com/susch19/BackupHelper/blob/master/AdditionalFiles/RestoreSelect.png">

#### Restore Save
<img src="https://github.com/susch19/BackupHelper/blob/master/AdditionalFiles/RestoreSave.png">

