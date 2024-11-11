using System;
using System.IO;
using RoboSharp.Interfaces;

// Do Not change NameSpace here! -> Must be RoboSharp due to prior releases
namespace RoboSharp
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// <inheritdoc cref="ProcessedFileInfo"/>
    /// </summary>
    public class FileProcessedEventArgs : EventArgs
    {
        
        public static readonly FileProcessedEventArgs WhiteSpaceLogLine = new FileProcessedEventArgs(string.Empty);

        /// <inheritdoc cref="ProcessedFileInfo"/>
        public ProcessedFileInfo ProcessedFile { get; }

        public FileProcessedEventArgs(ProcessedFileInfo file)
        {
            ProcessedFile = file;
        }

        public FileProcessedEventArgs(string systemMessage)
        {
            ProcessedFile = new ProcessedFileInfo(systemMessage);
        }

        public FileProcessedEventArgs(FileInfo file, IRoboCommand command, ProcessedFileFlag status = ProcessedFileFlag.None)
        {
            ProcessedFile = new ProcessedFileInfo(file, command, status);
        }

        public FileProcessedEventArgs(DirectoryInfo dir, IRoboCommand command, long size = 0, ProcessedDirectoryFlag status = ProcessedDirectoryFlag.None)
        {
            ProcessedFile = new ProcessedFileInfo(dir, command, status, size);
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
