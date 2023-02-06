
## What is this repository?

This will be a tool collection to use in conjuction with backup software, that stores files in 7z files, like Cobian Reflector.

The goal is to be a bit more independent of closed sourced software, without the shortcomings that comes with developing your own backup software from scratch or having to maintain someone elses backup software. Every tool in this collection will most likely only fullfill one purpose, this way it is easier to maintain and everyone can get only the software they need. 

___

## BackupFileIndexer (For Cobian Reflector)
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
