using System.IO;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace RoboSharp.Extensions.Mocks
{
    /// <summary>
    /// A mock <see cref="IProcessedFilePair"/> where all properties are settable
    /// </summary>
    public class MockProcessedFilePair : IProcessedFilePair
    {
        private ProcessedFileInfo? info;
        public ProcessedFileInfo? ProcessedFileInfo
        {
            get => info ??= new ProcessedFileInfo() { Name = Source?.FullName ?? "", FileClassType = FileClassType.File, Size = Source?.Length ?? 0 };
            set => info = value;
        }
        public IProcessedDirectoryPair? Parent { get; set; }
        public bool ShouldCopy { get; set; } = true;
        public bool ShouldPurge { get; set; }
        public FileInfo? Source { get; set; }
        public FileInfo? Destination { get; set; }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
