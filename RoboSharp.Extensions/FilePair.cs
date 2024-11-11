using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Class that implements the <see cref="IFilePair"/> and <see cref="IProcessedFilePair"/> interfaces, and allows for copying from the source to the destination.
    /// <br/>Represents a pair of files to be evaluated.
    /// </summary>
    public class FilePair : IFilePair, IProcessedFilePair
    {
        /// <summary>
        /// Create a new FilePair object
        /// </summary>
        /// <param name="source">The source FileInfo object</param>
        /// <param name="destination">The Destination FileInfo object</param>
        /// <param name="parent">
        /// The Parent Directory Pair. 
        /// <br/> - If the supplied object is an <see cref="IProcessedDirectoryPair"/>, the object will be used.
        /// <br/> - If the object is null or does not implement <see cref="IProcessedDirectoryPair"/>, creates a new <see cref="DirectoryPair"/> object.</param>
        /// <exception cref="ArgumentNullException"/>
        public FilePair(FileInfo source, FileInfo destination, IDirectoryPair parent = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            Parent = GetParent(parent, this);
        }

        /// <summary>
        /// Create a new FilePair object from an existing <see cref="IFilePair"/>
        /// <br/> If the input <paramref name="filePair"/> is a <see cref="IProcessedFilePair"/>, adopt the properties.
        /// </summary>
        /// <param name="filePair">Provides Source/Destination FileInfo objects</param>
        /// <inheritdoc cref="FilePair.FilePair(FileInfo, FileInfo, IDirectoryPair)"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <param name="parent"/>
        public FilePair(IFilePair filePair, IDirectoryPair parent = null)
        {
            if (filePair is null) throw new ArgumentNullException(nameof(filePair));
            Source = filePair.Source ?? throw new ArgumentException("filePair.Source is null");
            Destination = filePair.Destination ?? throw new ArgumentException("filePair.Destination is null");

            if (filePair is IProcessedFilePair pf)
            {
                this.ProcessedFileInfo = pf.ProcessedFileInfo;
                if (parent is null) parent = pf.Parent;
                this.ShouldCopy = pf.ShouldCopy;
                this.ShouldPurge = pf.ShouldPurge;
            }
            Parent = GetParent(parent, this);
        }

        /// <param name="source">Source File Path</param>
        /// <param name="destination">Destination File Path</param>
        /// <inheritdoc cref="FilePair.FilePair(FileInfo, FileInfo, IDirectoryPair)"/>
        /// <inheritdoc cref="FileInfo.FileInfo(string)"/>
        /// <param name="parent"/>
        public FilePair(string source, string destination, IDirectoryPair parent = null)
        {
            if (string.IsNullOrWhiteSpace(nameof(source))) throw new ArgumentException("source cannot be empty", nameof(source));
            if (string.IsNullOrWhiteSpace(nameof(destination))) throw new ArgumentException("source cannot be empty", nameof(destination));
            Source = new FileInfo(source);
            Destination = new FileInfo(destination);
            Parent = GetParent(parent, this);
        }

        private static IProcessedDirectoryPair GetParent(IDirectoryPair parent, FilePair pair)
        {
            if (parent is IProcessedDirectoryPair pd)
                return pd;
            else if (parent is null)
                return new DirectoryPair(pair.Source.Directory, pair.Destination.Directory);
            else
                return new DirectoryPair(parent.Source, parent.Destination);
        }

        /// <inheritdoc cref="FilePair(FileInfo, FileInfo, IDirectoryPair)"/>
        public static FilePair CreatePair(FileInfo source, FileInfo destination, IProcessedDirectoryPair parent = null) => new FilePair(source, destination, parent);

        /// <inheritdoc cref="FilePair(IFilePair, IDirectoryPair)"/>
        public static FilePair CreatePair(IFilePair filePair, IProcessedDirectoryPair parent = null) => new FilePair(filePair, parent);

        /// <inheritdoc/>
        public bool ShouldCopy { get; set; }

        /// <inheritdoc/>
        public bool ShouldPurge { get; set; }

        /// <inheritdoc/>
        public FileInfo Source { get; }

        /// <inheritdoc/>
        public FileInfo Destination { get; }

        /// <inheritdoc/>
        public ProcessedFileInfo ProcessedFileInfo { get; set; }

        /// <inheritdoc/>
        public IProcessedDirectoryPair Parent { get; }

        /// <summary>
        /// Refresh the source and destinationd FileInfo objects.
        /// </summary>
        public virtual void Refresh()
        {
            Source.Refresh();
            Destination.Refresh();
        }
    }
}
