using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions.Options
{
    /// <summary>
    /// Extension Methods for <see cref="RoboSharp.CopyOptions"/> to assist with custom implementations
    /// </summary>
    public static class CopyExtensions
    {
        /// <summary>
        /// Evaluates the <paramref name="flag"/> to check if any of the MOV options are enabled
        /// </summary>
        public static bool IsMovingFiles(this CopyActionFlags flag) =>
            flag.HasFlag(CopyActionFlags.MoveFiles) |
            flag.HasFlag(CopyActionFlags.MoveFilesAndDirectories);

        /// <summary>
        /// Returns <see langword="true"/> if either <see cref="CopyOptions.MoveFiles"/> | <see cref="CopyOptions.MoveFilesAndDirectories"/> are enabled.
        /// </summary>
        public static bool IsMovingFiles(this CopyOptions options) =>
            options.MoveFiles ||
            options.MoveFilesAndDirectories;

        /// <summary>
        /// Evaluates the <paramref name="flag"/> to check if any of the options requiring recursion into subdirectories are enabled
        /// </summary>
        public static bool IsRecursive(this CopyActionFlags flag) =>
            flag.HasFlag(CopyActionFlags.CopySubdirectories) ||
            flag.HasFlag(CopyActionFlags.CopySubdirectoriesIncludingEmpty) ||
            flag.HasFlag(CopyActionFlags.Mirror);

        /// <summary>
        /// Evaluates the <paramref name="options"/> to check if any of the options the recurse through subdirectories are enabled
        /// </summary>
        public static bool IsRecursive(this CopyOptions options) =>
            options.CopySubdirectories ||
            options.CopySubdirectoriesIncludingEmpty ||
            options.Mirror;

        /// <summary>
        /// Evaluates the <paramref name="flag"/> to check if any of the PURGE options are enabled
        /// </summary>
        public static bool IsPurging(this CopyActionFlags flag) =>
            flag.HasFlag(CopyActionFlags.Purge) ||
            flag.HasFlag(CopyActionFlags.Mirror);

        /// <summary>
        /// Evaluates the <paramref name="options"/> to check if any of the PURGE options are enabled
        /// </summary>
        public static bool IsPurging(this CopyOptions options) =>
            options.Purge ||
            options.Mirror;

        /// <summary>
        /// Compare the current depth against the maximum allowed depth, and determine if directory recursion can continue.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="depth"> The depth of the recursion. </param>
        /// <returns>
        /// <see langword="true"/> if the tested <paramref name="depth"/> will exceed the maximum depth specified in the <paramref name="options"/>. <br/>
        /// <see langword="false"/> if recursion into the directory tree can continue. <br/>
        /// <see langword="false"/> if <see cref="CopyOptions.Depth"/> &lt;= 0 ( No Limit )
        /// </returns>
        /// <remarks>
        /// This was written to easily break out for For{} loops during recursion in directories. If (CopyOptions.ExceedsAllowedDepth(depth)) break;
        /// <br/><br/>
        /// The first directory in the tree must be considered depth = 1.
        /// <br/>MyDocuments/ -- Depth = 1
        /// <br/>MyDocuments/Taxes/ -- Depth = 2
        /// <br/>MyDocuments/School/ -- Depth = 2
        /// <br/>MyDocuments/School/C#/ -- Depth = 3
        /// </remarks>
        public static bool ExceedsAllowedDepth(this CopyOptions options, int depth)
        {
            if (options.Depth <= 0) return false;
            return depth > options.Depth;
        }

        /// <summary>
        /// Evaluate the pair and determine if it the file at the destination should be purged or not.
        /// <br/> Store the result into <see cref="IProcessedFilePair.ShouldPurge"/>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="pair"></param>
        /// <returns>TRUE if destination file should be deleted, otherwise false.</returns>
        public static bool ShouldPurge(this IRoboCommand command, IProcessedFilePair pair)
        {
            bool result = false;
            if (command.CopyOptions.Mirror | command.CopyOptions.Purge)
            {
                result = !command.SelectionOptions.ExcludeExtra && pair.IsExtra();
            }
            pair.ShouldPurge = result;
            return result;
        }


        /// <summary>
        /// Evaluate the pair and determine if it the directory at the destination should be purged or not
        /// </summary>
        /// <param name="command"></param>
        /// <param name="pair"></param>
        /// <returns>TRUE if destination directory should be deleted, otherwise false.</returns>
        public static bool ShouldPurge(this IRoboCommand command, IDirectoryPair pair)
        {
            if (command.CopyOptions.Mirror | command.CopyOptions.Purge)
            {
                return !command.SelectionOptions.ExcludeExtra && pair.IsExtra();
            }
            return false;
        }

        #region < IncludedFiles >

        /// <summary>
        /// Generate an array of regex based off the <see cref="CopyOptions.FileFilter"/>
        /// </summary>
        /// <param name="options">The CopyOptions object that houses the FileFilter</param>
        /// <returns>
        /// A collection of regex objects derived from <see cref="CopyOptions.FileFilter"/>. <br/>
        /// If the collection is empty, the returned array will also be empty.
        /// </returns>
        public static Regex[] GetFileFilterRegex(this CopyOptions options)
        {
            //Check if any filters exist, or if the single filter is equivalent to the default filter
            if (options.FileFilter.None() || HasDefaultFileFilter(options))
            {
                return Array.Empty<Regex>();
            }
            //Non-Default filters have been specified - convert into regex
            return options.FileFilter.Select(SelectionExtensions.CreateWildCardRegex).ToArray();
        }

        /// <summary>
        /// Check if the <see cref="CopyOptions.FileFilter"/> is using the <see cref="CopyOptions.DefaultFileFilter"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool HasDefaultFileFilter(this CopyOptions options)
        {
            if (options.FileFilter?.Any() ?? false)
                return !options.FileFilter.All(s => s != CopyOptions.DefaultFileFilter && s != "*");
            else
                return true;
        }

        /// <summary>
        /// Determine if the file should be rejected based on its filename by checking against the FileFilter 
        /// </summary>
        /// <param name="options">Evaluates <see cref="CopyOptions.FileFilter"/> and generates a regex collection based on the wildcard pattern.</param>
        /// <param name="fileName">filename to compare</param>
        /// <param name="fileFilterRegex">
        /// The collection of regex objects to compare against - If this is null, a new array will be generated from <see cref="CopyOptions.FileFilter"/>. <br/>
        /// ref is used for optimization during the course of the run, to avoid re-creating the regex for every file check.
        /// </param>
        /// <returns>
        /// If <see cref="CopyOptions.FileFilter"/> has no values, or only contains <see cref="CopyOptions.DefaultFileFilter"/>, this method will always return true.
        /// </returns>
        public static bool ShouldIncludeFileName(this CopyOptions options, string fileName, IEnumerable<Regex> fileFilterRegex = null)
        {

            if (fileFilterRegex is null) fileFilterRegex = options.GetFileFilterRegex();
            if (fileFilterRegex.None()) return true;
            return fileFilterRegex.Any(r => r.IsMatch(fileName));
        }

        /// <inheritdoc cref="ShouldIncludeFileName(CopyOptions, string, IEnumerable{Regex})"/>
        public static bool ShouldIncludeFileName(this CopyOptions options, FileInfo Source, IEnumerable<Regex> inclusionCollection = null) 
            => ShouldIncludeFileName(options, Source.Name, inclusionCollection);

        /// <inheritdoc cref="ShouldIncludeFileName(CopyOptions, string, IEnumerable{Regex})"/>
        public static bool ShouldIncludeFileName(this CopyOptions options, IFilePair comparer, IEnumerable<Regex> inclusionCollection = null) 
            => ShouldIncludeFileName(options, comparer.Source.Name, inclusionCollection);

        #endregion

        #region < SetAttributes >

        /// <summary>
        /// Parses <see cref="CopyOptions.AddAttributes"/> and <see cref="CopyOptions.RemoveAttributes"/> and applies them to the destination. <br/> Any attribute addition comes before attribute removal.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="destination"></param>
        public static void SetFileAttributes(this CopyOptions options, FileInfo destination)
        {
            var Addattr = options.GetAddAttributes();
            var Subattr = options.GetRemoveAttributes();
            if (Addattr.HasValue)
                destination.Attributes &= Addattr.Value;
            if (Subattr.HasValue)
                destination.Attributes &= ~Subattr.Value;
        }

        /// <inheritdoc cref="SetFileAttributes(CopyOptions, FileInfo)"/>
        public static void SetFileAttributes(this CopyOptions options, string destination)
        {
            SetFileAttributes(options, new FileInfo(destination));
        }

        /// <inheritdoc cref="SetFileAttributes(CopyOptions, FileInfo)"/>
        public static void SetFileAttributes(this CopyOptions options, IFilePair comparer)
        {
            SetFileAttributes(options, comparer.Destination);

        }

        #endregion

        #region < AddAttributes >

        /// <summary>
        /// Add the attributes to the destination
        /// </summary>
        /// <param name="options"></param>
        /// <param name="destination"></param>
        public static void AddFileAttributes(this CopyOptions options, FileInfo destination)
        {
            var attr = options.GetAddAttributes();
            if (attr is null) return;
            destination.Attributes &= attr.Value;
        }

        /// <inheritdoc cref="AddFileAttributes(CopyOptions, FileInfo)"/>
        public static void AddFileAttributes(this CopyOptions options, string destination)
        {
            AddFileAttributes(options, new FileInfo(destination));
        }

        /// <inheritdoc cref="AddFileAttributes(CopyOptions, FileInfo)"/>
        public static void AddFileAttributes(this CopyOptions options, IFilePair comparer)
        {
            AddFileAttributes(options, comparer.Destination);
        }

        #endregion

        #region < RemoveAttributes >

        /// <summary>
        /// Removes the attributes from the destination
        /// </summary>
        /// <param name="options"></param>
        /// <param name="destination"></param>
        public static void RemoveFileAttributes(this CopyOptions options, FileInfo destination)
        {
            var attr = options.GetRemoveAttributes();
            if (attr is null) return;
            destination.Attributes &= ~attr.Value;
        }

        /// <inheritdoc cref="RemoveFileAttributes(CopyOptions, FileInfo)"/>
        public static void RemoveFileAttributes(this CopyOptions options, string destination)
        {
            RemoveFileAttributes(options, new FileInfo(destination));
        }

        /// <inheritdoc cref="RemoveFileAttributes(CopyOptions, FileInfo)"/>
        public static void RemoveFileAttributes(this CopyOptions options, IFilePair comparer)
        {
            RemoveFileAttributes(options, comparer.Destination);
        }

        #endregion

    }
}
