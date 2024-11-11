using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Helpers
{

    /// <summary>
    /// Extension Methods for the <see cref="IDirectoryPair"/> interface
    /// </summary>
    public static class IDirectoryPairExtensions
    {
        /// <summary> Evaluate the roots of the Source and Destination </summary>
        /// <returns>True if the Source and Destination have the same root string, otherwise false.</returns>
        public static bool IsLocatedOnSameDrive(this IDirectoryPair pair)
            => Path.GetPathRoot(pair.Source.FullName).Equals(Path.GetPathRoot(pair.Destination.FullName), StringComparison.InvariantCultureIgnoreCase);


        /// <inheritdoc cref="Options.SelectionExtensions.IsExtra{T}(T, T)"/>
        public static bool IsExtra(this IDirectoryPair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : Options.SelectionExtensions.IsExtra(pair.Source, pair.Destination);


        /// <inheritdoc cref="Options.SelectionExtensions.IsLonely{T}(T, T)"/>
        public static bool IsLonely(this IDirectoryPair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : Options.SelectionExtensions.IsLonely(pair.Source, pair.Destination);

        /// <summary>
        /// Check if the  <see cref="IDirectoryPair.Source"/> directory is the root of its drive
        /// </summary>
        /// <returns><see langword="true"/> if the FullName of the source == Root.FullName, otherwise <see langword="false"/></returns>
        public static bool IsRootSource(this IDirectoryPair pair) => pair.Source.IsRootDir();

        /// <summary>
        /// Check if the  <see cref="IDirectoryPair.Destination"/> directory is the root of its drive
        /// </summary>
        /// <inheritdoc cref="IsRootDestination(IDirectoryPair)"/>
        public static bool IsRootDestination(this IDirectoryPair pair) => pair.Destination.IsRootDir();

        /// <summary>
        /// Check if the <paramref name="directory"/> is the root of its drive
        /// </summary>
        /// <inheritdoc cref="IsRootDestination(IDirectoryPair)"/>
        public static bool IsRootDir(this DirectoryInfo directory)
            => directory.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(directory.Root.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        /// <summary>
        /// Check for the existence of the directories and, if able, update the <see cref="IProcessedDirectoryPair.ProcessedFileInfo"/>.Size with the number of files.
        /// </summary>
        /// <param name="pair">The pair to evaluate.</param>
        /// <param name="prioritizeDestination">If both directories exists, setting this to TRUE will use the file count from the destination.</param>
        /// <returns><see langword="true"/> if the object was updated, otherwise false.</returns>
        public static bool TrySetSizeAndPath(this IProcessedDirectoryPair pair, bool prioritizeDestination)
        {
            if (pair.ProcessedFileInfo is null) return false;
            if (prioritizeDestination && pair.Destination.Exists || pair.IsExtra())
            {
                pair.ProcessedFileInfo.Size = pair.Destination.GetFiles().Length;
                pair.ProcessedFileInfo.Name = pair.Destination.FullName;
                return true;
            }
            else if (pair.Source.Exists)
            {
                pair.ProcessedFileInfo.Size = pair.Source.GetFiles().Length;
                pair.ProcessedFileInfo.Name = pair.Source.FullName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Refreshes both the <see cref="IDirectoryPair.Source"/> and <see cref="IDirectoryPair.Destination"/> objects
        /// </summary>
        public static void RefreshDirectoryInfo(this IDirectoryPair pair)
        {
            pair?.Source?.Refresh();
            pair?.Destination?.Refresh();
        }

        #region < Create Pair Functions >

        /// <summary> A constructor delegate for an IFilePair </summary>
        public delegate T CreateProcessedFilePair<T>(FileInfo source, FileInfo destination, IProcessedDirectoryPair parent) where T : IProcessedFilePair;

        /// <summary> A constructor delegate for an IDirectoryPair </summary>
        public delegate T CreateProcessedDirectoryPair<T>(DirectoryInfo source, DirectoryInfo destination) where T : IProcessedDirectoryPair;

        /// <summary>
        /// Create a new DirPair object using a child of the Source directory
        /// </summary>
        /// <typeparam name="T">type of object to create</typeparam>
        /// <param name="dir">the file that is a child of either the Source</param>
        /// <param name="parent">the parent pair</param>
        /// <param name="ctor">the method used to generate the new object</param>
        /// <returns>new <see cref="IProcessedDirectoryPair"/> object</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateSourceChild<T>(this IDirectoryPair parent, DirectoryInfo dir, CreateProcessedDirectoryPair<T> ctor) where T : IProcessedDirectoryPair
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (dir is null) throw new ArgumentNullException(nameof(dir));
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));

            if (!dir.FullName.StartsWith(parent.Source.FullName))
                throw new ArgumentException("Unable to create DirectoryPair - Directory provided is not a child of the parent Source");
            return ctor(
                dir,
                new DirectoryInfo(Path.Combine(parent.Destination.FullName, dir.Name))
                );
        }

        /// <summary>
        /// Create a new DirPair object using a child of the Destination directory
        /// </summary>
        /// <param name="dir">the file that is a child of the Destination</param>
        /// <inheritdoc cref="CreateSourceChild{T}(IDirectoryPair, DirectoryInfo, CreateProcessedDirectoryPair{T})"/>
        /// <param name="ctor"/><param name="parent"/><typeparam name="T"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateDestinationChild<T>(this IDirectoryPair parent, DirectoryInfo dir, CreateProcessedDirectoryPair<T> ctor) where T : IProcessedDirectoryPair
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (dir is null) throw new ArgumentNullException(nameof(dir));
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));

            if (!dir.FullName.StartsWith(parent.Destination.FullName))
                throw new ArgumentException("Unable to create DirectoryPair - Directory provided is not a child of the parent Destination");
            return ctor(
                new DirectoryInfo(Path.Combine(parent.Source.FullName, dir.Name)),
                dir);
        }

        /// <summary>
        /// Create a new DirPair object using a child of the Source directory
        /// </summary>
        /// <typeparam name="T">type of IFilePair to create</typeparam>
        /// <param name="file">the file that is a child of either the Source</param>
        /// <param name="parent">the parent pair</param>
        /// <param name="ctor">the method used to generate the new object</param>
        /// <returns>new <see cref="IProcessedFilePair"/> object</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateSourceChild<T>(this IProcessedDirectoryPair parent, FileInfo file, CreateProcessedFilePair<T> ctor) where T : IProcessedFilePair
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));

            if (!file.FullName.StartsWith(parent.Source.FullName))
                throw new ArgumentException("Unable to create DirectoryPair - Directory provided is not a child of the parent Source");
            return ctor(
                file,
                new FileInfo(Path.Combine(parent.Destination.FullName, file.Name)),
                parent);
        }

        /// <summary>
        /// Create a new FilePair object using a child of the Destination directory
        /// </summary>
        /// <param name="file">the file that is a child of the Destination</param>
        /// <inheritdoc cref="CreateSourceChild{T}(IProcessedDirectoryPair, FileInfo, CreateProcessedFilePair{T})"/>
        /// <param name="ctor"/><param name="parent"/><typeparam name="T"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateDestinationChild<T>(this IProcessedDirectoryPair parent, FileInfo file, CreateProcessedFilePair<T> ctor) where T : IProcessedFilePair
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));

            if (!file.FullName.StartsWith(parent.Destination.FullName))
                throw new ArgumentException("Unable to create DirectoryPair - Directory provided is not a child of the parent Destination");
            return ctor(
                new FileInfo(Path.Combine(parent.Source.FullName, file.Name)),
                file,
                parent);
        }

        #endregion

        #region < Get File Pairs >

        /// <returns>Array of the FilePairs that were found in both the Source and Destination via <see cref="DirectoryInfo.GetFiles()"/></returns>
        /// <inheritdoc cref="EnumerateFilePairs{T}(IProcessedDirectoryPair, CreateProcessedFilePair{T})"/>
        public static T[] GetFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor) where T : IProcessedFilePair
        {
            return parent.EnumerateFilePairs(ctor).ToArray();
        }

        /// <summary>
        /// Enumerate the pairs from the source
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">the parent directory pair</param>
        /// <param name="whereTrue">Function to decide to include the file in the enumeration or not</param>
        /// <param name="ctor">the constructor to create the filepair</param>
        /// <returns>A CachedEnumerable if the directory exists, otherwise null</returns>
        public static IEnumerable<T> EnumerateSourceFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor, Func<FileInfo, bool> whereTrue) where T : IProcessedFilePair
        {
            T createPair(FileInfo f) => parent.CreateSourceChild(f, ctor);
            if (!parent.Source.Exists) return Array.Empty<T>();
            if (whereTrue is null)
                return parent.Source.EnumerateFiles().Select(createPair);
            else
                return parent.Source.EnumerateFiles().Where(whereTrue).Select(createPair);
        }

        /// <inheritdoc cref="EnumerateSourceFilePairs{T}(IProcessedDirectoryPair, CreateProcessedFilePair{T}, Func{FileInfo, bool})"/>
        public static IEnumerable<T> EnumerateSourceFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor) where T : IProcessedFilePair
        {
            T createPair(FileInfo f) => parent.CreateSourceChild(f, ctor);
            if (!parent.Source.Exists) return Array.Empty<T>();
            return parent.Source.EnumerateFiles().Select(createPair);
        }

        /// <inheritdoc cref="EnumerateSourceFilePairs{T}(IProcessedDirectoryPair, CreateProcessedFilePair{T}, Func{FileInfo, bool})"/>
        public static IEnumerable<T> EnumerateSourceFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor, string searchPattern, SearchOption searchOption) where T : IProcessedFilePair
        {
            T createPair(FileInfo f) => parent.CreateSourceChild(f, ctor);
            if (!parent.Source.Exists) return Array.Empty<T>();
            return parent.Source.EnumerateFiles(searchPattern ?? "*", searchOption).Select(createPair);
        }

        /// <summary>
        /// Enumerate the pairs from the destination
        /// </summary>
        /// <inheritdoc cref="EnumerateSourceFilePairs{T}(IProcessedDirectoryPair, CreateProcessedFilePair{T}, Func{FileInfo, bool})"/>
        public static IEnumerable<T> EnumerateDestinationFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor) where T : IProcessedFilePair
        {
            T createPair(FileInfo f) => parent.CreateDestinationChild(f, ctor);
            if (!parent.Destination.Exists) return Array.Empty<T>();
            return parent.Destination.EnumerateFiles().Select(createPair);
        }

        /// <summary>
        /// Enumerate the pairs from the destination
        /// </summary>
        /// <inheritdoc cref="EnumerateSourceFilePairs{T}(IProcessedDirectoryPair, CreateProcessedFilePair{T}, Func{FileInfo, bool})"/>
        public static IEnumerable<T> EnumerateDestinationFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor, Func<FileInfo, bool> whereTrue) where T : IProcessedFilePair
        {
            T createPair(FileInfo f) => parent.CreateDestinationChild(f, ctor);
            if (!parent.Destination.Exists) return Array.Empty<T>();
            if (whereTrue is null)
                return parent.Destination.EnumerateFiles().Select(createPair);
            else
                return parent.Destination.EnumerateFiles().Where(whereTrue).Select(createPair);
        }

        /// <summary>
        /// Gets all the File Pairs from the <see cref="IDirectoryPair"/>
        /// </summary>
        /// <returns>cached Ienumerable of the FilePairs that were found in both the Source and Destination via <see cref="DirectoryInfo.GetFiles()"/></returns>
        /// <inheritdoc cref="CreateSourceChild{T}(IProcessedDirectoryPair, FileInfo, CreateProcessedFilePair{T})"/>
        public static IEnumerable<T> EnumerateFilePairs<T>(this IProcessedDirectoryPair parent, CreateProcessedFilePair<T> ctor) where T : IProcessedFilePair
        {
            T createPair(string f) => parent.CreateDestinationChild(new FileInfo(f), ctor);
            IEnumerable<T> sourceFiles = null;
            IEnumerable<T> destFiles = null;

            if (parent.Source.Exists)
                sourceFiles = parent.EnumerateSourceFilePairs(ctor);
            if (parent.Destination.Exists)
            {
                if (sourceFiles is null)
                    destFiles = parent.EnumerateDestinationFilePairs(ctor);
                else
                {
                    destFiles =
                        Directory.EnumerateFiles(parent.Destination.FullName)
                        .Where(destPath => sourceFiles.None(sourceChild => sourceChild.Destination.FullName == destPath))
                        .Select(createPair);
                }
            }
            if (sourceFiles is null && destFiles is null)
                return Array.Empty<T>();
            else if (sourceFiles is null)
                return destFiles;
            else if (destFiles is null)
                return sourceFiles;
            else
                return destFiles.Concat(sourceFiles);
        }

        #endregion

        #region < Get Directory Pairs >

        /// <returns>Array of the DirectoryPairs that were foudn in both the Source and Destination via <see cref="DirectoryInfo.GetFiles()"/></returns>
        /// <inheritdoc cref="EnumerateDirectoryPairs{T}(IProcessedDirectoryPair, CreateProcessedDirectoryPair{T})"/>
        public static T[] GetDirectoryPairs<T>(this IProcessedDirectoryPair parent, CreateProcessedDirectoryPair<T> ctor) where T : IProcessedDirectoryPair
        {
            return parent.EnumerateDirectoryPairs(ctor).ToArray();
        }

        /// <inheritdoc cref="EnumerateDestinationDirectoryPairs{T}(IProcessedDirectoryPair, CreateProcessedDirectoryPair{T})"/>
        public static IEnumerable<DirectoryPair> EnumerateSourceDirectoryPairs(this IProcessedDirectoryPair parent) => parent.EnumerateSourceDirectoryPairs(DirectoryPair.CreatePair);

        /// <summary>
        /// Enumerate the pairs from the source
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">the parent directory pair</param>
        /// <param name="whereTrue">Function to decide to include the directory in the enumeration or not</param>
        /// <param name="ctor">the constructor to create the Directory Pair</param>
        /// <returns>A CachedEnumerable if the directory exists, otherwise null</returns>
        public static IEnumerable<T> EnumerateSourceDirectoryPairs<T>(this IProcessedDirectoryPair parent, CreateProcessedDirectoryPair<T> ctor, Func<DirectoryInfo, bool> whereTrue) where T : IProcessedDirectoryPair
        {
            T createPair(DirectoryInfo d) => parent.CreateSourceChild(d, ctor);
            if (!parent.Source.Exists) return Array.Empty<T>();
            if (whereTrue is null)
                return parent.Source.EnumerateDirectories().Select(createPair);
            else
                return parent.Source.EnumerateDirectories().Where(whereTrue).Select(createPair);
        }

        /// <inheritdoc cref="EnumerateSourceDirectoryPairs{T}(IProcessedDirectoryPair, CreateProcessedDirectoryPair{T}, Func{DirectoryInfo, bool})"/>
        public static IEnumerable<T> EnumerateSourceDirectoryPairs<T>(this IProcessedDirectoryPair parent, CreateProcessedDirectoryPair<T> ctor) where T : IProcessedDirectoryPair
        {
            T createPair(DirectoryInfo d) => parent.CreateSourceChild(d, ctor);
            if (!parent.Source.Exists) return Array.Empty<T>();
            return parent.Source.EnumerateDirectories().Select(createPair);
        }

        /// <inheritdoc cref="EnumerateDestinationDirectoryPairs{T}(IProcessedDirectoryPair, CreateProcessedDirectoryPair{T})"/>
        public static IEnumerable<DirectoryPair> EnumerateDestinationDirectoryPairs(this IProcessedDirectoryPair parent) => parent.EnumerateDestinationDirectoryPairs(DirectoryPair.CreatePair);

        /// <summary>
        /// Enumerate the pairs from the destination
        /// </summary>
        /// <inheritdoc cref="EnumerateSourceDirectoryPairs{T}(IProcessedDirectoryPair, CreateProcessedDirectoryPair{T}, Func{DirectoryInfo, bool})"/>
        public static IEnumerable<T> EnumerateDestinationDirectoryPairs<T>(this IProcessedDirectoryPair parent, CreateProcessedDirectoryPair<T> ctor) where T : IProcessedDirectoryPair
        {
            T createPair(DirectoryInfo d) => parent.CreateDestinationChild(d, ctor);
            if (!parent.Destination.Exists) return Array.Empty<T>();
            return parent.Destination.EnumerateDirectories().Select(createPair);
        }

        /// <summary>
        /// Enumerate the pairs from the destination
        /// </summary>
        /// <inheritdoc cref="EnumerateSourceDirectoryPairs{T}(IProcessedDirectoryPair, CreateProcessedDirectoryPair{T}, Func{DirectoryInfo, bool})"/>
        public static IEnumerable<T> EnumerateDestinationDirectoryPairs<T>(this IProcessedDirectoryPair parent, CreateProcessedDirectoryPair<T> ctor, Func<DirectoryInfo, bool> whereTrue) where T : IProcessedDirectoryPair
        {
            T createPair(DirectoryInfo d) => parent.CreateDestinationChild(d, ctor);
            if (!parent.Destination.Exists) return Array.Empty<T>();
            if (whereTrue is null)
                return parent.Destination.EnumerateDirectories().Select(createPair);
            else
                return parent.Destination.EnumerateDirectories().Where(whereTrue).Select(createPair);
        }

        /// <summary>
        /// Gets all the Directory Pairs from the <see cref="IProcessedDirectoryPair"/>
        /// </summary>
        /// <returns> IEnumerable{T} of of the Directory Pairs</returns>
        /// <inheritdoc cref="CreateSourceChild{T}(IDirectoryPair, DirectoryInfo, CreateProcessedDirectoryPair{T})"/>
        public static IEnumerable<T> EnumerateDirectoryPairs<T>(this IProcessedDirectoryPair parent, CreateProcessedDirectoryPair<T> ctor) where T : IProcessedDirectoryPair
        {
            T createPair(string d) => parent.CreateDestinationChild(new DirectoryInfo(d), ctor);
            IEnumerable<T> sourceChildren = null;
            IEnumerable<T> destChildren = null;
            if (parent.Source.Exists)
                sourceChildren = parent.Source.EnumerateDirectories().Select((f) => parent.CreateSourceChild(f, ctor));
            if (parent.Destination.Exists)
            {
                if (sourceChildren is null)
                    destChildren = parent.Destination.EnumerateDirectories().Select((f) => parent.CreateDestinationChild(f, ctor));
                else
                {
                    // Enumerate the directory names that don't exist in the source children into new DirectoryInfo Objects
                    destChildren =
                        Directory.EnumerateDirectories(parent.Destination.FullName)
                        .Where(destName => sourceChildren.None(sourceChild => sourceChild.Destination.FullName == destName))
                        .Select(createPair);
                }
            }

            if (sourceChildren is null && destChildren is null)
                return Array.Empty<T>();
            else if (sourceChildren is null)
                return destChildren;
            else if (destChildren is null)
                return sourceChildren;
            else
                return destChildren.Concat(sourceChildren);
        }

        #endregion

    }
}
