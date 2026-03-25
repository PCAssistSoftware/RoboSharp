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
    /// A mock <see cref="IFileCopierFactory"/> that is designed for unit testing scenarios.
    /// <para/> - Creates <see cref="MockIFileCopier"/> objects that will not perform copy or move operations.
    /// </summary>
    public class MockFileCopierFactory : IFileCopierFactory
    {
        public static MockFileCopierFactory Instance => instance ??= new();
        private static MockFileCopierFactory? instance;


        public IFileCopier Create(FileInfo source, FileInfo destination, IDirectoryPair? parent)
        {
            return new MockIFileCopier()
            {
                Source = source,
                Destination = destination,
                Parent = new MockProcessedDirectoryPair() { Source = parent?.Source, Destination = parent?.Destination }
            };
        }

        public IFileCopier Create(IFileSource fileSource, string destination) => Create(fileSource.FilePath, destination);
        public IFileCopier Create(string source, string destination, IDirectoryPair? parent) => Create(new FileInfo(source), new FileInfo(destination), parent);
        public IFileCopier Create(string source, string destination) => Create(new FileInfo(source), new FileInfo(destination), null);
        public IFileCopier Create(FileInfo source, FileInfo destination) => Create(source, destination, null);
        public IFileCopier Create(IFilePair filePair) => Create(filePair.Source, filePair.Destination);



        public IFileCopier Create(string source, DirectoryInfo destination, IDirectoryPair? parent = null)
        {
            return new MockIFileCopier()
            {
                Source = new FileInfo(source),
                Destination = new FileInfo(Path.Combine( destination.FullName, source)),
                Parent = new MockProcessedDirectoryPair() { Source = parent?.Source, Destination = parent?.Destination }
            };
        }
        public IFileCopier Create(IFileSource fileSource, DirectoryInfo destination) => Create(fileSource.FilePath, destination, null);
        public IFileCopier Create(FileInfo source, DirectoryInfo destination, IDirectoryPair? parent = null) => Create(source.Name, destination, parent);

    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member