using System;
using System.IO;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// A basic implementation of the <see cref="IFileCopierFactory"/> that accepts a delegate to create a new <see cref="IFileCopier"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IFileCopier"/></typeparam>
    public class FileCopierFactory<T> : AbstractFileCopierFactory<T> where T: class, IFileCopier
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        private readonly Func<FileInfo, FileInfo, IDirectoryPair, T> _createFunc;
        public FileCopierFactory(Func<FileInfo, FileInfo,IDirectoryPair, T> createFunc) => _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        public override T Create(FileInfo source, FileInfo destination, IDirectoryPair parent) => _createFunc(source, destination, parent);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }


    /// <summary>
    /// Abstract base implementation of <see cref="IFileCopierFactory"/>
    /// </summary>
    public abstract class AbstractFileCopierFactory<T> : AbstractFileCopierFactory, IFileCopierFactory where T : IFileCopier
    {
        /// <inheritdoc cref="IFileCopierFactory.Create(IFileSource, string)"/>
        public virtual T Create(IFileSource fileSource, string destination)
        {
            if (fileSource is null) throw new ArgumentNullException(nameof(fileSource));
            return Create(fileSource.FilePath, destination);
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(IFileSource, DirectoryInfo)"/>
        public virtual T Create(IFileSource fileSource, DirectoryInfo destination)
        {
            if (fileSource is null) throw new ArgumentNullException(nameof(fileSource));
            return Create(fileSource.FilePath, destination);
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(FileInfo, FileInfo, IDirectoryPair)"/>
        public abstract T Create(FileInfo source, FileInfo destination, IDirectoryPair parent);


        /// <inheritdoc cref="IFileCopierFactory.Create(string, string)"/>
        public virtual T Create(string source, string destination)
        {
            EvaluateSource(source);
            EvaluateDestination(destination);
            return Create(new FileInfo(source), new FileInfo(destination));
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(string, string, IDirectoryPair)"/>
        public virtual T Create(string source, string destination, IDirectoryPair parent)
        {
            EvaluateSource(source);
            EvaluateDestination(destination);
            var sourceFile = new FileInfo(source);
            var destFile = new FileInfo(destination);
            if (parent is null) parent = new DirectoryPair(sourceFile.Directory, destFile.Directory);
            return Create(sourceFile, destFile, parent);
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(FileInfo, FileInfo)"/>
        public virtual T Create(FileInfo source, FileInfo destination)
        {
            return Create(source, destination, null);
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(IFilePair)"/>
        public virtual T Create(IFilePair filePair)
        {
            if (filePair is null) throw new ArgumentNullException(nameof(filePair));
            if (filePair is IProcessedFilePair fp)
            {
                var copier = Create(filePair.Source, filePair.Destination, fp.Parent);
                copier.ProcessedFileInfo = fp.ProcessedFileInfo;
                copier.ShouldCopy = fp.ShouldCopy;
                copier.ShouldPurge = fp.ShouldPurge;
                return copier;
            }
            else
            {
                return Create(filePair.Source, filePair.Destination);
            }
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(string, DirectoryInfo, IDirectoryPair)"/>
        public virtual T Create(string source, DirectoryInfo destination, IDirectoryPair parent = null)
        {
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            EvaluateSource(source);
            var sourceFile = new FileInfo(source);
            var destFile = new FileInfo(Path.Combine(destination.FullName, sourceFile.Name));
            if (parent is null) parent = new DirectoryPair(sourceFile.Directory, destination);
            return Create(sourceFile, destFile, parent);
        }

        /// <inheritdoc cref="IFileCopierFactory.Create(FileInfo, DirectoryInfo, IDirectoryPair)"/>
        public virtual T Create(FileInfo source, DirectoryInfo destination, IDirectoryPair parent = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            var destFile = new FileInfo(Path.Combine(destination.FullName, source.Name));
            if (parent is null) parent = new DirectoryPair(source.Directory, destination);
            return Create(source, destFile, parent);
        }

        IFileCopier IFileCopierFactory.Create(IFileSource fileSource, string destination)
            => Create(fileSource, destination);

        IFileCopier IFileCopierFactory.Create(IFileSource fileSource, DirectoryInfo destination)
            => Create(fileSource, destination);

        IFileCopier IFileCopierFactory.Create(string source, string destination)
            => Create(source, destination);

        IFileCopier IFileCopierFactory.Create(FileInfo source, FileInfo destination)
            => Create(source, destination);

        IFileCopier IFileCopierFactory.Create(IFilePair filePair)
            => Create(filePair);

        IFileCopier IFileCopierFactory.Create(FileInfo source, FileInfo destination, IDirectoryPair parent)
            => Create(source, destination, parent);

        IFileCopier IFileCopierFactory.Create(FileInfo source, DirectoryInfo destination, IDirectoryPair parent)
            => Create(source, destination, parent);

        IFileCopier IFileCopierFactory.Create(string source, string destination, IDirectoryPair parent)
            => Create(source, destination, parent);

        IFileCopier IFileCopierFactory.Create(string source, DirectoryInfo destination, IDirectoryPair parent)
            => Create(source, destination, parent);

    }

    /// <summary>
    /// Abstract base class that can not be instantiated that provides shared static methods for <see cref="AbstractFileCopierFactory{T}"/>
    /// </summary>
    public abstract class AbstractFileCopierFactory
    {
        internal AbstractFileCopierFactory() { }

        /// <summary>
        /// Evaluate the <paramref name="source"/> Path to ensure its a fully qualified file path
        /// <br/> If the path if not a fully qualified file path, than an <see cref="ArgumentException"/> will be thrown. Otherwise returns successfully.
        /// </summary>
        /// <param name="source">Fully Qualified Source File Path</param>
        /// <exception cref="ArgumentException"/>
        public static void EvaluateSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("No Source Path Specified", nameof(source));
            if (!Path.IsPathRooted(source)) throw new ArgumentException("Source Path is not rooted", nameof(source));
            if (string.IsNullOrEmpty(Path.GetFileName(source))) throw new ArgumentException("No FileName Provided in Source", nameof(source));
        }

        /// <summary>
        /// Evaluate the <paramref name="destination"/> Path to ensure its a fully qualified file path.
        /// <br/> If the path if not a fully qualified file path, than an <see cref="ArgumentException"/> will be thrown. Otherwise returns successfully.
        /// </summary>
        /// <param name="destination">Fully Qualified Destination File Path</param>
        /// <exception cref="ArgumentException"/>
        public static void EvaluateDestination(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("No Destination Path Specified", nameof(destination));
            if (!Path.IsPathRooted(destination)) throw new ArgumentException("Destination Path is not rooted", nameof(destination));
            if (string.IsNullOrEmpty(Path.GetFileName(destination))) throw new ArgumentException("No Destination FileName Provided", nameof(destination));
        }

        /// <returns>TRUE if the path is fully qualified, otherwise false.</returns>
        /// <inheritdoc cref="EvaluateDestination(string)"/>
        public static bool TryEvaluateDestination(string destination, out Exception ex)
        {
            ex = null;
            try { EvaluateDestination(destination); return true; } catch (Exception e) { ex = e; return false; }
        }

        /// <returns>TRUE if the path is fully qualified, otherwise false.</returns>
        /// <inheritdoc cref="EvaluateSource(string)"/>
        public static bool TryEvaluateSource(string source, out Exception ex)
        {
            ex = null;
            try { EvaluateSource(source); return true; } catch (Exception e) { ex = e; return false; }
        }
    }
}