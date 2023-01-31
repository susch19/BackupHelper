
## What is this repository?

This will be a tool collection to use in conjuction with backup software, that stores files in 7z files, like Cobian Reflector.

The goal is to be a bit more independent of closed sourced software, without the shortcomings that comes with developing your own backup software from scratch or having to maintain someone elses backup software. Every tool in this collection will most likely only fullfill one purpose, this way it is easier to maintain and everyone can get only the software they need. 

___

## BackupFileIndexer (For Cobian Reflector)
This Tool created a metadata per archive. In this metadata it is stored, when every file of the full backup was last backuped and in which archive it can be found.

On its own this information wont do anything, but later on a BackupFileRestorer will be implemented, where any backup file can be selected and restore the latest version of any file based on this backup.

Restoring backups for bigger incremental backups can be a pain otherwise.

Currently it is required, that the option "Include backup type in the name" is enabled, so only based on the file it is known, if it is a full or incremental backup.

Later on the sqlite history.db of Cobian Reflector will be used as a supliment for this.

