using System;
using System.ComponentModel;
using Win32 = Windows.Win32.Storage.FileSystem.MOVE_FILE_FLAGS;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// The Options that can be used with MoveFileWithProgressA -- <see cref="FileFunctions.MoveFileWithProgress(string, string, CopyProgressCallback, MoveFileOptions)"/>
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-movefilewithprogressa"/></remarks>
    [DefaultValue(Default)]
    [Flags]
    public enum MoveFileOptions : uint
    {
        /// <summary>
        /// No options specified - Note that this is not the default used by the library.
        /// </summary>
        None = 0U,

        /// <summary>
        /// Default Recommended options to ensure it works across drives and doesn't return until file move completed.
        /// </summary>
        Default = COPY_ALLOWED | WRITE_THROUGH,

        /// <summary>
        /// If the file is to be moved to a different volume, the function simulates the move by using the CopyFile and DeleteFile functions.
        /// <br/> - If the file is successfully copied to a different volume and the original file is unable to be deleted, the function succeeds leaving the source file intact.
        /// <br/> - This value cannot be used with MOVEFILE_DELAY_UNTIL_REBOOT.
        /// </summary>
        COPY_ALLOWED = Win32.MOVEFILE_COPY_ALLOWED,

        /// <summary>
        /// Reserved for future use. 
        /// </summary>
        CREATE_HARDLINK = Win32.MOVEFILE_CREATE_HARDLINK, 

        /// <summary>
        /// The system does not move the file until the operating system is restarted. The system moves the file immediately after AUTOCHK is executed, but before creating any paging files. 
        /// Consequently, this parameter enables the function to delete paging files from previous startups.
        /// <br/> - This value can only be used if the process is in the context of a user who belongs to the administrators group or the LocalSystem account.
        /// <br/> - This value cannot be used with MOVEFILE_COPY_ALLOWED.
        /// </summary>
        DELAY_UNTIL_REBOOT = Win32.MOVEFILE_DELAY_UNTIL_REBOOT,

        /// <summary>
        /// The function fails if the source file is a link source, but the file cannot be tracked after the move. 
        /// This situation can occur if the destination is a volume formatted with the FAT file system.
        /// </summary>
        FAIL_IF_NOT_TRACKABLE = Win32.MOVEFILE_FAIL_IF_NOT_TRACKABLE, 

        /// <summary>
        /// If a file exists at the destination, overwrite it.
        /// <br/> - This option is only valid if the Source and Destination are both files (the destination being a directory path won't work)
        /// </summary>
        REPLACE_EXISTSING = Win32.MOVEFILE_REPLACE_EXISTING,

        /// <summary>
        /// The function does not return until the file has actually been moved on the disk. 
        /// <br/> - Setting this value guarantees that a move performed as a copy and delete operation is flushed to disk before the function returns. The flush occurs at the end of the copy operation. 
        /// <br/> - This value has no effect if MOVEFILE_DELAY_UNTIL_REBOOT is set.
        /// </summary>
        WRITE_THROUGH = Win32.MOVEFILE_WRITE_THROUGH,
        
    }
}