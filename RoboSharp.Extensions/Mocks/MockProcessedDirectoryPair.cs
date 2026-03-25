using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace RoboSharp.Extensions.Mocks
{
    /// <summary>
    /// A mock <see cref="IProcessedDirectoryPair"/> where all properties are settable
    /// </summary>
    public class MockProcessedDirectoryPair : IProcessedDirectoryPair
    {
        private ProcessedFileInfo? info;
        public ProcessedFileInfo? ProcessedFileInfo 
        {
            get => info ??= new ProcessedFileInfo() { Name = Source?.FullName ?? "", FileClassType = FileClassType.NewDir, Size = 0 };
            set => info = value; 
        }
        public DirectoryInfo? Source { get; set; }
        public DirectoryInfo? Destination { get; set; }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
