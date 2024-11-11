using RoboSharp.Interfaces;
using System;
using System.IO;

namespace RoboSharp
{
    /// <summary>
    /// Message Type reported by RoboCopy
    /// </summary>
    public enum FileClassType
    {
        /// <summary>Details about a Directory</summary>
        NewDir,
        /// <summary>Details about a FILE</summary>
        File,
        /// <summary>Status Message reported by RoboCopy</summary>
        SystemMessage
    }

    /// <summary>Contains information about the current item being processed by RoboCopy</summary>
    public class ProcessedFileInfo
    {
        /// <summary>
        /// String used to identify the 'FileClass' property of a System Message
        /// </summary>
        public const string SystemMessageFileClass = "System Message";

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ProcessedFileInfo() { }

        /// <summary>
        /// Generate a new object by explicitly defining the values
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="*" /></param>
        /// <param name="fileClassType"><inheritdoc cref="FileClassType" path="*" /></param>
        /// <param name="fileClass"><inheritdoc cref="FileClass" path="*" /></param>
        /// <param name="size"><inheritdoc cref="Size" path="*" /></param>
        public ProcessedFileInfo(string name, FileClassType fileClassType, string fileClass, long size = 0)
        {
            Name = name;
            FileClassType = fileClassType;
            FileClass = fileClass;
            Size = size;
        }

        /// <summary>
        /// Create a new ProcessedFileInfo object that represents some file
        /// <br/> The <see cref="Name"/> will depend on <see cref="LoggingOptions.IncludeFullPathNames"/>
        /// </summary>
        /// <param name="file">the FileInfo object that was evaluated</param>
        /// <param name="status">The status of the file to look up from the config</param>
        /// <param name="command">The command that provides the <see cref="RoboSharpConfiguration"/> and <see cref="LoggingOptions"/> objects that will be evaluated</param>
        public ProcessedFileInfo(FileInfo file, IRoboCommand command, ProcessedFileFlag status = ProcessedFileFlag.None)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (command is null) throw new ArgumentNullException(nameof(command));
            FileClassType = FileClassType.File;
            FileClass = command.Configuration.GetFileClass(status);
            Name = command.LoggingOptions.IncludeFullPathNames ? file.FullName : file.Name;
            Size = file.Length;
        }

        /// <summary>
        /// Report a message from the process
        /// </summary>
        /// <param name="systemMessage"></param>
        public ProcessedFileInfo(string systemMessage)
        {
            FileClassType = FileClassType.SystemMessage;
            FileClass = SystemMessageFileClass;
            Name = systemMessage;
            Size = 0;
        }

        /// <summary>
        /// Create a new ProcessedFileInfo object that represents some directory.
        /// <br/> The <see cref="Name"/> will depend on <see cref="LoggingOptions.IncludeFullPathNames"/>
        /// </summary>
        /// <param name="directory">the Directory</param>
        /// <param name="size">number of files in the directory. Use -1 if purging.</param>
        /// <param name="status">The status of the file to look up from the config</param>
        /// <param name="command">The command that provides the <see cref="RoboSharpConfiguration"/> and <see cref="LoggingOptions"/> objects that will be evaluated</param>
        /// <exception cref="ArgumentNullException"/>
        public ProcessedFileInfo(DirectoryInfo directory, IRoboCommand command, ProcessedDirectoryFlag status = ProcessedDirectoryFlag.None, long size = 0)
        {
            if (directory is null) throw new ArgumentNullException(nameof(directory));
            if (command is null) throw new ArgumentNullException(nameof(command));
            FileClassType = FileClassType.NewDir;
            FileClass = command.Configuration.GetDirectoryClass(status);
            Name = directory.FullName; //command.LoggingOptions.IncludeFullPathNames ? directory.FullName : directory.Name;
            Size = size;
        }

        /// <summary>Description of the item as reported by RoboCopy</summary>
        public string FileClass { get; set; }

        /// <inheritdoc cref="RoboSharp.FileClassType"/>
        public FileClassType FileClassType { get; set; }

        /// <summary>
        /// File -> File Size <br/>
        /// Directory -> Number of selected files in folder -> Can be negative if PURGE is used <br/>
        /// SystemMessage -> Should be 0
        /// </summary>
        public long Size { get; set; }

        /// <summary>Folder or File Name / Message Text</summary>
        public string Name { get; set; }

        /// <summary>
        /// Translates the object back to the log line.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (FileClassType)
            {
                case FileClassType.SystemMessage: return Name;
                case FileClassType.NewDir: return DirInfoToString(true);
                case FileClassType.File: return FileInfoToString(FileClass, true, true);
                default: throw new NotImplementedException("Unknown FileClassType");
            }
        }

        /// <summary>
        /// Get the log line, taking the <paramref name="options"/> into consideration.
        /// </summary>
        /// <param name="options">
        /// Evaluates <see cref="LoggingOptions.NoFileClasses"/> and <see cref="LoggingOptions.NoFileSizes"/> to determine if those values should be included in the output string.
        /// </param>
        /// <returns>The string equivalent log line for this item</returns>
        /// <exception cref="NotImplementedException"></exception>
        public string ToString(LoggingOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            switch (FileClassType)
            {
                case FileClassType.SystemMessage: return Name;
                case FileClassType.NewDir: return DirInfoToString(!options.NoFileSizes);
                case FileClassType.File: return FileInfoToString(FileClass, !options.NoFileClasses, !options.NoFileSizes);
                default: throw new NotImplementedException("Unknown FileClassType");
            }
        }

        /// <summary>
        /// Generates the error log line for a failed file operation, using the <see cref="IRoboCommand.Configuration"/> 
        /// </summary>
        /// <param name="command">The associated IRobocommand</param>
        /// <param name="datetime">If not specified, uses DateTime.Now</param>
        /// <param name="ex">The exception whose error code and message shall be used</param>
        /// <param name="stepDescription">The description of the step that failed</param>
        /// <returns>
        /// With exception provided:
        /// <br/><c>[DateTime] [Configuration.ErrorToken] [ErrorCode] [stepDescription] [FilePath][Environment.Newline][ex.Message]</c>
        /// <para/>Without exception:
        /// <br/><c>[DateTime] [Configuration.ErrorToken] [stepDescription] [FilePath]</c>
        /// </returns>
        public string ToStringFailed(IRoboCommand command, Exception ex = null, DateTime? datetime = null, string stepDescription = null)
        {
            if (command is null) throw new ArgumentNullException(nameof(command));
            if (command.Configuration is null) throw new ArgumentNullException(nameof(command.Configuration));
            switch (FileClassType)
            {
                case FileClassType.SystemMessage:
                case FileClassType.NewDir:
                case FileClassType.File:
                    if (ex is null && string.IsNullOrWhiteSpace(stepDescription))
                        return $"{datetime ?? DateTime.Now} {command.Configuration.ErrorToken} {Name}";
                    
                    else if (ex is null)
                        return $"{datetime ?? DateTime.Now} {command.Configuration.ErrorToken} {stepDescription} {Name}";
                    
                    else if (string.IsNullOrWhiteSpace(stepDescription))
                        return $"{datetime ?? DateTime.Now} {command.Configuration.ErrorToken} {ex.HResult} {Name}{Environment.NewLine}{ex.Message}";

                    else
                        return $"{datetime ?? DateTime.Now} {command.Configuration.ErrorToken} {ex.HResult} {stepDescription} {Name}{Environment.NewLine}{ex.Message}";

                default: throw new NotImplementedException("Unknown FileClassType");
            }
        }

        /// <summary>
        /// "\t[FileClass]  \t[FileCount]\t[DirectoryPath]"
        /// </summary>
        private string DirInfoToString(bool includeSize)
        {
            if (includeSize)
                return $"\t{FileClass,-10}            \t{Name}";
            else
                return $"\t{FileClass,-10}{Size,12}\t{Name}";
        }

        /// <summary>
        /// "\t    [FileClass]  \t\t    [FileSize]\t[FileName]"
        /// </summary>
        private string FileInfoToString(string fileClass, bool includeClass, bool includeSize)
        {
            if (includeClass)
            {
                if (includeSize)
                    return $"\t{fileClass,10}  \t\t{Size,8}\t{Name}";
                else
                    return $"\t{fileClass,10}  \t\t        \t{Name}";
            }
            else if (includeSize)
                return $"\t            \t\t{Size,8}\t{Name}";
            else // name only
                return $"\t            \t\t        \t{Name}";
        }

        /// <summary>
        /// Set the <see cref="FileClass"/> property <br/>
        /// Only meant for consumers to use upon custom implementations of IRoboCommand
        /// </summary>
        /// <param name="status">Status to set</param>
        /// <param name="config">configuration provider</param>
        public void SetDirectoryClass(ProcessedDirectoryFlag status, RoboSharpConfiguration config)
        {
            if (FileClassType != FileClassType.NewDir) throw new System.Exception("Unable to apply ProcessedDirectoryFlag to File or System Message");
            FileClass = config.GetDirectoryClass(status);
        }
        /// <inheritdoc cref="SetDirectoryClass(ProcessedDirectoryFlag, RoboSharpConfiguration)"/>
        public void SetDirectoryClass(ProcessedDirectoryFlag status, IRoboCommand config) => SetDirectoryClass(status, config.Configuration);


        /// <inheritdoc cref="SetDirectoryClass(ProcessedDirectoryFlag, RoboSharpConfiguration)"/>
        public void SetFileClass(ProcessedFileFlag status, RoboSharpConfiguration config)
        {
            if (FileClassType != FileClassType.File) throw new System.Exception("Unable to apply ProcessedFileFlag to Directory or System Message");
            FileClass = config.GetFileClass(status);
        }
        /// <inheritdoc cref="SetDirectoryClass(ProcessedDirectoryFlag, RoboSharpConfiguration)"/>
        public void SetFileClass(ProcessedFileFlag status, IRoboCommand config) => SetFileClass(status, config.Configuration);

        /// <summary>
        /// Try to get the corresponding <see cref="ProcessedFileFlag"/> that was assigned to this object
        /// </summary>
        /// <inheritdoc cref="TryGetDirectoryClass(RoboSharpConfiguration, out ProcessedDirectoryFlag)"/>
        public bool TryGetFileClass(RoboSharpConfiguration conf, out ProcessedFileFlag flag)
        {
            foreach (ProcessedFileFlag f in typeof(ProcessedFileFlag).GetEnumValues())
            {
                if (this.FileClass == conf.GetFileClass(f))
                {
                    flag = f;
                    return true;
                }
            }
            flag = ProcessedFileFlag.None;
            return false;
        }

        /// <summary>
        /// Try to get the corresponding <see cref="ProcessedDirectoryFlag"/> that was 
        /// </summary>
        /// <param name="conf">The RoboSharpConfiguration that has a matching <see cref="FileClass"/> value</param>
        /// <param name="flag">If found, this will return the located enum value</param>
        /// <returns>TRUE if a match was found, otherwise false.</returns>
        public bool TryGetDirectoryClass(RoboSharpConfiguration conf, out ProcessedDirectoryFlag flag)
        {
            foreach (ProcessedDirectoryFlag f in typeof(ProcessedDirectoryFlag).GetEnumValues())
            {
                if (this.FileClass == conf.GetDirectoryClass(f))
                {
                    flag = f;
                    return true;
                }
            }
            flag = ProcessedDirectoryFlag.None;
            return false;
        }
    }
}
