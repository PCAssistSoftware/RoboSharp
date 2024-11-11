using System.IO;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Interface for a factory class that can create <see cref="IFileCopier"/> objects.
    /// </summary>
    public interface IFileCopierFactory
    {
        /// <inheritdoc cref="Create(IFileSource, DirectoryInfo)"/>
        IFileCopier Create(IFileSource fileSource, string destination);

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> that copies from the <paramref name="fileSource"/> file to the <paramref name="destination"/>
        /// </summary>
        /// <param name="fileSource">An object that provides the source file path</param>
        /// <param name="destination">The destination directory</param>
        /// <returns>A new <see cref="IFileCopier"/></returns>
        IFileCopier Create(IFileSource fileSource, DirectoryInfo destination);

        /// <inheritdoc cref="Create(FileInfo, FileInfo, IDirectoryPair)"/>
        IFileCopier Create(FileInfo source, FileInfo destination);

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> that copies from the <paramref name="source"/> file to the <paramref name="destination"/> file
        /// </summary>
        /// <param name="source">The source file</param>
        /// <param name="destination">The destination file</param>
        /// <param name="parent">The DirectoryPair parent - optional argument used for robosharp reporting</param>
        /// <returns>A new <see cref="IFileCopier"/></returns>
        IFileCopier Create(FileInfo source, FileInfo destination, IDirectoryPair parent);

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> gets the source/destination information from the <paramref name="filePair"/> file
        /// </summary>
        /// <param name="filePair">THe object that provides Source/Destination information</param>
        /// <returns>A new <see cref="IFileCopier"/></returns>
        IFileCopier Create(IFilePair filePair);

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> that copies from the <paramref name="source"/> file path into the <paramref name="destination"/> directory
        /// </summary>
        /// <param name="destination">The Destination Directory</param>
        /// <inheritdoc cref="Create(FileInfo, FileInfo, IDirectoryPair)"/>
        /// <param name="parent"/><param name="source"/>
        IFileCopier Create(FileInfo source, DirectoryInfo destination, IDirectoryPair parent = null);

        /// <inheritdoc cref="Create(string, string, IDirectoryPair)"/>
        IFileCopier Create(string source, string destination);

        /// <param name="source">The fully qualified source file path</param>
        /// <param name="destination">The fully qualified destination file path</param>
        /// <inheritdoc cref="Create(FileInfo, FileInfo, IDirectoryPair)"/>
        /// <param name="parent"/>
        IFileCopier Create(string source, string destination, IDirectoryPair parent);

        /// <inheritdoc cref="Create(FileInfo, DirectoryInfo, IDirectoryPair)"/>
        IFileCopier Create(string source, DirectoryInfo destination, IDirectoryPair parent = null);
    }
}