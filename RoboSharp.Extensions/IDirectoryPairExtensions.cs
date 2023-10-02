﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions
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


        #region < Create Pair Functions >

        /// <summary>
        /// Create a new DirPair object using a child of the Source directory
        /// </summary>
        /// <typeparam name="T">type of object to create</typeparam>
        /// <param name="dir">the file that is a child of either the Source</param>
        /// <param name="parent">the parent pair</param>
        /// <param name="ctor">the method used to generate the new object</param>
        /// <returns>new <see cref="IDirectoryPair"/> object</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateSourceChild<T>(this IDirectoryPair parent, DirectoryInfo dir, Func<DirectoryInfo, DirectoryInfo, T> ctor) where T : IDirectoryPair
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
        /// <inheritdoc cref="CreateSourceChild{T}(IDirectoryPair, DirectoryInfo, Func{DirectoryInfo, DirectoryInfo, T})"/>
        /// <param name="ctor"/><param name="parent"/><typeparam name="T"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateDestinationChild<T>(this IDirectoryPair parent, DirectoryInfo dir, Func<DirectoryInfo, DirectoryInfo, T> ctor) where T : IDirectoryPair
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
        /// <returns>new <see cref="IDirectoryPair"/> object</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateSourceChild<T>(this IDirectoryPair parent, FileInfo file, Func<FileInfo, FileInfo, T> ctor) where T : IFilePair
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));
            
            if (!file.FullName.StartsWith(parent.Source.FullName))
                throw new ArgumentException("Unable to create DirectoryPair - Directory provided is not a child of the parent Source");
            return ctor(
                file,
                new FileInfo(Path.Combine(parent.Destination.FullName, file.Name))
                );
        }

        /// <summary>
        /// Create a new FilePair object using a child of the Destination directory
        /// </summary>
        /// <param name="file">the file that is a child of the Destination</param>
        /// <inheritdoc cref="CreateSourceChild{T}(IDirectoryPair, FileInfo, Func{FileInfo, FileInfo, T})"/>
        /// <param name="ctor"/><param name="parent"/><typeparam name="T"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static T CreateDestinationChild<T>(this IDirectoryPair parent, FileInfo file, Func<FileInfo, FileInfo, T> ctor) where T : IFilePair
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));
            
            if (!file.FullName.StartsWith(parent.Destination.FullName))
                throw new ArgumentException("Unable to create DirectoryPair - Directory provided is not a child of the parent Destination");
            return ctor(
                new FileInfo(Path.Combine(parent.Source.FullName, file.Name)),
                file);
        }

        #endregion

        #region < Get File Pairs >

        /// <returns>Array of the FilePairs that were found in both the Source and Destination via <see cref="DirectoryInfo.GetFiles()"/></returns>
        /// <inheritdoc cref="EnumerateFilePairs{T}(IDirectoryPair, Func{FileInfo, FileInfo, T})"/>
        public static T[] GetFilePairs<T>(this IDirectoryPair parent, Func<FileInfo, FileInfo, T> ctor) where T : IFilePair
        {
            return EnumerateFilePairs(parent, ctor).ToArray();
        }

        /// <summary>
        /// Evaluate the enumerated file path to determine if it should be included in the enumerated file pairs
        /// </summary>
        /// <returns>TRUE to include, FALSE to exclude</returns>
        public delegate bool IncludeFilePathDelegate(FileInfo enumeratedFile);

        /// <summary>
        /// Enumerate the pairs from the source
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">the parent directory pair</param>
        /// <param name="whereTrue">Function to decide to include the file in the enumeration or not</param>
        /// <param name="ctor">the constructor to create the filepair</param>
        /// <returns>A CachedEnumerable if the directory exists, otherwise null</returns>
        public static CachedEnumerable<T> EnumerateSourcePairs<T>(this IDirectoryPair parent, IncludeFilePathDelegate whereTrue, Func<FileInfo, FileInfo, T> ctor) where T : IFilePair
        {
            if (!parent.Source.Exists) return null;
            return parent.Source.EnumerateFiles().Where(f => whereTrue(f)).Select((f) => CreateSourceChild(parent, f, ctor)).AsCachedEnumerable();
            //return parent.Source.EnumerateFiles().Where( f=> whereTrue(f)).Select((f) => CreateSourceChild(parent, f, ctor)).AsCachedEnumerable(); ;
        }

        /// <summary>
        /// Enumerate the pairs from the destination
        /// </summary>
        /// <inheritdoc cref="EnumerateSourcePairs{T}(IDirectoryPair, IncludeFilePathDelegate, Func{FileInfo, FileInfo, T})"/>
        public static CachedEnumerable<T> EnumerateDestinationPairs<T>(this IDirectoryPair parent, IncludeFilePathDelegate whereTrue, Func<FileInfo, FileInfo, T> ctor) where T : IFilePair
        {
            if (!parent.Destination.Exists) return null;
            return parent.Destination.EnumerateFiles().Where(f => whereTrue(f)).Select((f) => CreateDestinationChild(parent, f, ctor)).AsCachedEnumerable();
            //return parent.Destination.EnumerateFiles().Where(f => whereTrue(f)).Select((f) => CreateDestinationChild(parent, f, ctor)).AsCachedEnumerable();
        }

        /// <summary>
        /// Gets all the File Pairs from the <see cref="IDirectoryPair"/>
        /// </summary>
        /// <returns>cached Ienumerable of the FilePairs that were found in both the Source and Destination via <see cref="DirectoryInfo.GetFiles()"/></returns>
        /// <inheritdoc cref="CreateSourceChild{T}(IDirectoryPair, FileInfo, Func{FileInfo, FileInfo, T})"/>
        public static CachedEnumerable<T> EnumerateFilePairs<T>(this IDirectoryPair parent, Func<FileInfo, FileInfo, T> ctor) where T : IFilePair
        {
            CachedEnumerable<T> sourceFiles = null;
            CachedEnumerable<T> destFiles = null;
            bool WhereTrue(FileInfo f) => true;

            if (parent.Source.Exists)
                sourceFiles = EnumerateSourcePairs(parent, WhereTrue, ctor);
            if (parent.Destination.Exists)
            {
                if (sourceFiles is null)
                    destFiles = EnumerateDestinationPairs(parent, WhereTrue, ctor);
                else
                {
                    destFiles =
                        Directory.EnumerateFiles(parent.Destination.FullName)
                        .Where(destPath => sourceFiles.None(sourceChild => sourceChild.Destination.FullName == destPath))
                        .Select(f => new FileInfo(f))
                        .Select(f => CreateDestinationChild(parent, f, ctor))
                        .AsCachedEnumerable();
                }
            }
            if (sourceFiles is null && destFiles is null)
                return CachedEnumerable<T>.Empty;
            else if (sourceFiles is null)
                return destFiles;
            else if (destFiles is null)
                return sourceFiles;
            else
                return new CachedEnumerable<T>(sourceFiles.Concat(destFiles));
        }

        #endregion

        #region < Get Directory Pairs >

        /// <returns>Array of the DirectoryPairs that were foudn in both the Source and Destination via <see cref="DirectoryInfo.GetFiles()"/></returns>
        /// <inheritdoc cref="EnumerateDirectoryPairs{T}(IDirectoryPair, Func{DirectoryInfo, DirectoryInfo, T})"/>
        public static T[] GetDirectoryPairs<T>(this IDirectoryPair parent, Func<DirectoryInfo, DirectoryInfo, T> ctor) where T : IDirectoryPair
        {
            return EnumerateDirectoryPairs(parent, ctor).ToArray();
        }

        /// <summary>
        /// Evaluate the enumerated file path to determine if it should be included in the enumerated file pairs
        /// </summary>
        /// <returns>TRUE to include, FALSE to exclude</returns>
        public delegate bool IncludeDirectoryPathDelegate(DirectoryInfo enumeratedDirectory);

        /// <summary>
        /// Enumerate the pairs from the source
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">the parent directory pair</param>
        /// <param name="whereTrue">Function to decide to include the directory in the enumeration or not</param>
        /// <param name="ctor">the constructor to create the Directory Pair</param>
        /// <returns>A CachedEnumerable if the directory exists, otherwise null</returns>
        public static CachedEnumerable<T> EnumerateSourcePairs<T>(this IDirectoryPair parent, IncludeDirectoryPathDelegate whereTrue, Func<DirectoryInfo, DirectoryInfo, T> ctor) where T : IDirectoryPair
        {
            if (!parent.Source.Exists) return null;
            return parent.Source.EnumerateDirectories().Where(f => whereTrue(f)).Select((f) => CreateSourceChild(parent, f, ctor)).AsCachedEnumerable();
        }

        /// <summary>
        /// Enumerate the pairs from the destination
        /// </summary>
        /// <inheritdoc cref="EnumerateSourcePairs{T}(IDirectoryPair, IncludeDirectoryPathDelegate, Func{DirectoryInfo, DirectoryInfo, T})"/>
        public static CachedEnumerable<T> EnumerateDestinationPairs<T>(this IDirectoryPair parent, IncludeDirectoryPathDelegate whereTrue, Func<DirectoryInfo, DirectoryInfo, T> ctor) where T : IDirectoryPair
        {
            if (!parent.Destination.Exists) return null;
            return parent.Destination.EnumerateDirectories().Where(f => whereTrue(f)).Select((f) => CreateDestinationChild(parent, f, ctor)).AsCachedEnumerable();
        }

        /// <summary>
        /// Gets all the Directory Pairs from the <see cref="IDirectoryPair"/>
        /// </summary>
        /// <returns> IEnumerable{T} of of the Directory Pairs</returns>
        /// <inheritdoc cref="CreateSourceChild{T}(IDirectoryPair, DirectoryInfo, Func{DirectoryInfo, DirectoryInfo, T})"/>
        public static CachedEnumerable<T> EnumerateDirectoryPairs<T>(this IDirectoryPair parent, Func<DirectoryInfo, DirectoryInfo, T> ctor) where T : IDirectoryPair
        {
            CachedEnumerable<T> sourceChildren = null;
            CachedEnumerable<T> destChildren = null;
            if (parent.Source.Exists)
                sourceChildren = parent.Source.EnumerateDirectories().Select((f) => CreateSourceChild(parent, f, ctor)).AsCachedEnumerable();
            if (parent.Destination.Exists)
            {
                if (sourceChildren is null)
                    destChildren = parent.Destination.EnumerateDirectories().Select((f) => CreateDestinationChild(parent, f, ctor)).AsCachedEnumerable();
                else
                {
                    // Enumerate the directory names that don't exist in the source children into new DirectoryInfo Objects
                    destChildren =
                        Directory.EnumerateDirectories(parent.Destination.FullName)
                        .Where(destName => sourceChildren.None(sourceChild => sourceChild.Destination.FullName == destName))
                        .Select(d => CreateDestinationChild(parent, new DirectoryInfo(d), ctor))
                        .AsCachedEnumerable();
                }
            }

            if (sourceChildren is null && destChildren is null)
                return CachedEnumerable<T>.Empty;
            else if (sourceChildren is null)
                return destChildren;
            else if (destChildren is null)
                return sourceChildren;
            else
                return new CachedEnumerable<T>(sourceChildren.Concat(destChildren));
        }

        #endregion

    }
}