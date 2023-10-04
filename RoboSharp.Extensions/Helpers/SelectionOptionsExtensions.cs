﻿using RoboSharp.Interfaces;
using RoboSharp.Extensions.SymbolicLinkSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// Extension Methods for selections options to assist with custom implementations
    /// </summary>
    public static partial class SelectionOptionsExtensions
    {
        internal static string Format(this string input, params object[] args) => String.Format(input, args);
        internal static string PadCenter(this string paddedString, string otherString) => PadCenter(paddedString, otherString.Length);
        internal static string PadCenter(this string paddedString, int length) 
        {
            if (paddedString.Length >= length) return paddedString;
            int i = length - paddedString.Length;
            return paddedString.PadLeft(paddedString.Length + i / 2);
        }

        /// <summary>
        /// Translate the wildcard pattern to a regex pattern for a file name/path
        /// </summary>
        /// <param name="pattern">
        /// A Windows wildcard pattern to be converted into a new regex pattern. <br/>
        /// Wildcard characters: <br/>
        /// * = any character, unlimted matches<br/>
        /// ? = any single character
        /// </param>
        /// <returns>Translated the wildcard pattern to a regex pattern</returns>
        public static Regex CreateWildCardRegex(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("pattern is null or empty!");
            string sanitized = pattern.Replace(@"\", @"\\").Replace("/", @"\\");
            sanitized = sanitized.Replace(".", @"\.");
            sanitized = sanitized.Replace("*", ".*");
            sanitized = sanitized.Replace("?", ".");
            return new Regex($"^{sanitized}$", options: RegexOptions.IgnoreCase);
        }

        #region < Should Exclude Newer >

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeOlder(this SelectionOptions options, string source, string destination) => options.ExcludeOlder && IFilePairExtensions.IsDestinationNewer(source, destination);
        
        /// <inheritdoc cref="ShouldExcludeOlder(SelectionOptions, string, string)"/>
        public static bool ShouldExcludeOlder(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeOlder && IFilePairExtensions.IsDestinationNewer(source, destination);

        /// <inheritdoc cref="ShouldExcludeOlder(SelectionOptions, FileInfo, FileInfo)"/>
        public static bool ShouldExcludeOlder(this SelectionOptions options, IFilePair pair) => options.ExcludeOlder && pair.IsDestinationNewer();

        #endregion

        #region < Should Exclude Newer >

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeNewer(this SelectionOptions options, string source, string destination) => options.ExcludeNewer && IFilePairExtensions.IsSourceNewer(source, destination);
        
        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, string, string)"/>
        public static bool ShouldExcludeNewer(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeNewer && IFilePairExtensions.IsSourceNewer(source, destination);
        
        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, FileInfo, FileInfo)"/>
        public static bool ShouldExcludeNewer(this SelectionOptions options, IFilePair pair) => options.ExcludeNewer && pair.IsSourceNewer();

        #endregion

        #region < Extra >

        /// <summary>
        /// EXTRAs are files/directories that exist in the destination but not the source.
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Destination"></param>
        /// <returns>TRUE if exists in the destination but not in the source, otherwise false</returns>
        public static bool IsExtra(string Source, string Destination)
        {
            if (File.Exists(Destination))
                return !File.Exists(Source);

            else if (Directory.Exists(Destination))
                return !Directory.Exists(Source);

            else
                return false;
        }

        /// <inheritdoc cref="IsExtra(string, string)"/>
        public static bool IsExtra(FileInfo Source, FileInfo Destination) => Destination.Exists && !Source.Exists;

        /// <summary>
        /// EXTRA directories are folders that exist in the destination but not in the source
        /// </summary>
        /// <returns>TRUE if exists in the destination but not in the source, otherwise false</returns>
        public static bool IsExtra(DirectoryInfo Source, DirectoryInfo Destination) => Destination.Exists && !Source.Exists;

        /// <inheritdoc cref="IsExtra(DirectoryInfo, DirectoryInfo)"/>
        public static bool IsExtra(this IDirectoryPair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : IsExtra(pair.Source, pair.Destination);

        /// <inheritdoc cref="IsExtra(FileInfo, FileInfo)"/>
        public static bool IsExtra(this IFilePair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : IsExtra(pair.Source, pair.Destination);

        ///// <summary> </summary>
        ///// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        //public static bool ShouldExcludeExtra(this SelectionOptions options, string source, string destination) => options.ExcludeExtra&& IsExtra(source, destination);

        ///// <inheritdoc cref="ShouldExcludeExtra(SelectionOptions, string, string)"/>
        //public static bool ShouldExcludeExtra(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeExtra && IsExtra(source, destination);

        ///// <inheritdoc cref="ShouldExcludeExtra(SelectionOptions, FileInfo, FileInfo)"/>
        //public static bool ShouldExcludeExtra(this SelectionOptions options, IFileSourceDestinationPair copier) => options.ExcludeExtra && IsExtra(copier.Source, copier.Destination);

        #endregion

        #region < Lonely >

        /// <summary>
        /// LONELY files/directories that exist in the source but not the destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns>TRUE if exists in source but not in destination, otherwise false</returns>
        public static bool IsLonely(string source, string destination)
        {
            if (File.Exists(source))
                return !File.Exists(destination);

            else if (Directory.Exists(source))
                return !Directory.Exists(destination);

            else
                return false;
        }
        
        /// <inheritdoc cref="IsLonely(string, string)"/>
        public static bool IsLonely(FileInfo Source, FileInfo Destination) => Source.Exists && !Destination.Exists;

        /// <inheritdoc cref="IsLonely(string, string)"/>
        public static bool IsLonely(DirectoryInfo Source, DirectoryInfo Destination) => Source.Exists && !Destination.Exists;

        /// <inheritdoc cref="IsLonely(DirectoryInfo, DirectoryInfo)"/>
        public static bool IsLonely(this IDirectoryPair pair) 
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : IsLonely(pair.Source, pair.Destination);

        /// <inheritdoc cref="IsLonely(FileInfo, FileInfo)"/>
        public static bool IsLonely(this IFilePair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : IsLonely(pair.Source, pair.Destination);

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeLonely(this SelectionOptions options, string source, string destination) => options.ExcludeLonely && IsLonely(source, destination);

        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, string, string)"/>
        public static bool ShouldExcludeLonely(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeLonely && IsLonely(source, destination);

        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, FileInfo, FileInfo)"/>
        public static bool ShouldExcludeLonely(this SelectionOptions options, IFilePair copier) => options.ExcludeLonely && IsLonely(copier.Source, copier.Destination);

        #endregion

        #region < MaxLastAccessDate >

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MaxLastAccessDate)) return false;
            if (DateTime.TryParseExact(options.MaxLastAccessDate, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date > result;
            }
            else if (long.TryParse(options.MaxFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays > days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMaxLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, string source) 
            => ShouldExcludeMaxLastAccessDate(options, File.GetLastAccessTime(source).Date);

        /// <inheritdoc cref="ShouldExcludeMaxLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, FileInfo Source) 
            => ShouldExcludeMaxLastAccessDate(options, Source.LastAccessTime.Date);

        /// <inheritdoc cref="ShouldExcludeMaxLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, IFilePair pair) 
            => ShouldExcludeMaxLastAccessDate(options, pair.Source.LastAccessTime.Date);

        #endregion

        #region < MinLastAccessDate >

        /// <summary> Compare the file date against the <see cref="SelectionOptions.MinLastAccessDate"/> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MinLastAccessDate)) return false;
            if (DateTime.TryParseExact(options.MinLastAccessDate, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date < result;
            }
            else if (long.TryParse(options.MaxFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays < days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMinLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, string source) => ShouldExcludeMinLastAccessDate(options, File.GetLastAccessTime(source).Date);

        /// <inheritdoc cref="ShouldExcludeMinLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, FileInfo Source) => ShouldExcludeMinLastAccessDate(options, Source.LastAccessTime.Date);

        /// <inheritdoc cref="ShouldExcludeMinLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, IFilePair pair) => ShouldExcludeMinLastAccessDate(options, pair.Source.LastAccessTime.Date);

        #endregion

        #region < MaxFileAge >

        /// <summary>
        /// Compare the <see cref="FileSystemInfo.CreationTime"/> to determine the file's age against the <see cref="SelectionOptions.MaxFileAge"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="date"></param>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MaxFileAge)) return false;
            if (DateTime.TryParseExact(options.MaxFileAge, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date > result;
            }
            else if (long.TryParse(options.MaxFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays > days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, string source)
        {
            if (string.IsNullOrWhiteSpace(options.MaxFileAge)) return false;
            return ShouldExcludeMaxFileAge(options, File.GetCreationTime(source).Date);
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, FileInfo Source) => ShouldExcludeMaxFileAge(options, Source.CreationTime.Date);

        /// <inheritdoc cref="ShouldExcludeMaxFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, IFilePair pair) => ShouldExcludeMaxFileAge(options, pair.Source.CreationTime.Date);

        #endregion

        #region < MinFileAge >

        /// <summary>
        /// Compare the <see cref="FileSystemInfo.CreationTime"/> to determine the file's age against the <see cref="SelectionOptions.MinFileAge"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="date"></param>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MinFileAge)) return false;
            if (DateTime.TryParseExact(options.MinFileAge, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date < result;
            }
            else if (long.TryParse(options.MinFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays < days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMinFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, string source)
        {
            if (string.IsNullOrWhiteSpace(options.MinFileAge)) return false;
            return ShouldExcludeMaxFileAge(options, File.GetCreationTime(source).Date);
        }

        /// <inheritdoc cref="ShouldExcludeMinFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, FileInfo Source) 
            => ShouldExcludeMinFileAge(options, Source.CreationTime.Date);

        /// <inheritdoc cref="ShouldExcludeMinFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, IFilePair pair) 
            => ShouldExcludeMinFileAge(options, pair.Source.CreationTime.Date);

        #endregion

        #region < MaxFileSize >

        /// <summary>
        /// Compare the File Size against the <see cref="SelectionOptions.MaxFileSize"/>
        /// </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, long size)
        {
            if (options.MaxFileSize <= 0) return false;
            return size > options.MaxFileSize;
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, string source)
        {
            if (options.MaxFileSize <= 0) return false;
            return ShouldExcludeMaxFileSize(options, new FileInfo(source));
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, FileInfo Source) => ShouldExcludeMaxFileSize(options, Source.Length);

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, IFilePair pair) => ShouldExcludeMaxFileSize(options, pair.Source.Length);

        #endregion

        #region < MinFileSize >

        /// <summary>
        /// Compare the File Size against the <see cref="SelectionOptions.MinFileSize"/>
        /// </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, long size)
        {
            if (options.MaxFileSize <= 0) return false;
            return size < options.MaxFileSize;
        }

        /// <inheritdoc cref="ShouldExcludeMinFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, string source)
        {
            if (options.MaxFileSize <= 0) return false;
            return ShouldExcludeMinFileSize(options, new FileInfo(source));
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, FileInfo Source) => ShouldExcludeMinFileSize(options, Source.Length);

        /// <inheritdoc cref="ShouldExcludeMinFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, IFilePair pair) => ShouldExcludeMinFileSize(options, pair.Source.Length);

        #endregion

        #region < Included Attributes >

        /// <summary>
        /// Compare the File Attributes the <see cref="SelectionOptions.IncludeAttributes"/>
        /// </summary>
        /// <returns> TRUE if the file should be INCLUDED, FALSE if it should be EXCLUDED </returns>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, FileAttributes fileAttributes)
        {
            FileAttributes? attr = options.GetIncludedAttributes();
            if (attr is null) return true; // nothing specified - include all files
            return fileAttributes.HasFlag(attr.Value);
        }

        /// <inheritdoc cref="ShouldIncludeAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, string source) => ShouldIncludeAttributes(options, File.GetAttributes(source));

        /// <inheritdoc cref="ShouldIncludeAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, FileInfo Source) => ShouldIncludeAttributes(options, Source.Attributes);

        /// <inheritdoc cref="ShouldIncludeAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, IFilePair pair) => ShouldIncludeAttributes(options, pair.Source.Attributes);

        #endregion

        #region < Excluded Attributes >

        /// <summary>
        /// Compare the File Attributes the <see cref="SelectionOptions.ExcludeAttributes"/>
        /// </summary>
        /// <returns> TRUE if the file should be EXCLUDED, false if the file should be INCLUDED </returns>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, FileAttributes attributes)
        {
            FileAttributes? attr = options.GetExcludedAttributes();
            if (attr is null) return false; // nothing specified - include all files
            return attributes.HasFlag(attr.Value);
        }

        /// <inheritdoc cref="ShouldExcludeFileAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, string source) => ShouldExcludeFileAttributes(options, File.GetAttributes(source));

        /// <inheritdoc cref="ShouldExcludeFileAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, FileInfo Source) => ShouldExcludeFileAttributes(options, Source.Attributes);

        /// <inheritdoc cref="ShouldExcludeFileAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, IFilePair pair) => ShouldExcludeFileAttributes(options, pair.Source.Attributes);

        #endregion

        #region < ExcludedFiles Names >

        /// <summary>
        /// Generate an array of regex based off the <see cref="SelectionOptions.ExcludedFiles"/>
        /// </summary>
        /// <param name="options">The SelectionOptions object that houses the ExcludedFiles</param>
        /// <returns>
        /// A collection of regex objects derived from <see cref="SelectionOptions.ExcludedFiles"/>. <br/>
        /// If the collection is empty, the returned array will also be empty.
        /// </returns>
        public static Regex[] GetExcludedFileRegex(this SelectionOptions options)
        {
            if (options.ExcludedFiles?.Any() ?? false)
            {
                return options.ExcludedFiles.Select(CreateWildCardRegex).ToArray();
            }
            else
            {
                return Array.Empty<Regex>();
            }
        }

        /// <summary>
        /// Determine if the file should be rejected based on its filename, based off of <see cref="SelectionOptions.ExcludedFiles"/>
        /// </summary>
        /// <param name="options">if the <paramref name="exclusionCollection"/> is null, the options are used to generate the regex</param>
        /// <param name="fileName">filename to evaluate</param>
        /// <returns><see langword="true"/> if any of the regex items in the <paramref name="exclusionCollection"/> match the filename, otherwise false</returns>
        public static bool ShouldExcludeFileName(this SelectionOptions options, string fileName, IEnumerable<Regex> exclusionCollection = null)
        {
            if (exclusionCollection is null) exclusionCollection = options.GetExcludedFileRegex();
            if (!exclusionCollection.Any()) return false;
            string fname = Path.GetFileName(fileName);
            return exclusionCollection.Any(r => r.IsMatch(fname));
        }

        /// <inheritdoc cref="ShouldExcludeFileName(SelectionOptions, string, ref Regex[])"/>
        public static bool ShouldExcludeFileName(this SelectionOptions options, FileInfo Source, IEnumerable<Regex> exclusionCollection) => ShouldExcludeFileName(options, Source.Name, exclusionCollection);

        /// <inheritdoc cref="ShouldExcludeFileName(SelectionOptions, string, ref Regex[])"/>
        public static bool ShouldExcludeFileName(this SelectionOptions options, IFilePair pair, IEnumerable<Regex> exclusionCollection) => ShouldExcludeFileName(options, pair.Source.Name, exclusionCollection);

        #endregion

        #region < Excluded Dir Names >

        /// <summary>
        /// Generate an array of regex based off the <see cref="SelectionOptions.ExcludedDirectories"/>
        /// </summary>
        /// <param name="options">The SelectionOptions object that houses the ExcludedDirectories</param>
        /// <returns>
        /// A collection of regex objects derived from <see cref="SelectionOptions.ExcludedDirectories"/>. <br/>
        /// If the collection is empty, the returned array will also be empty.
        /// </returns>
        public static Helpers.DirectoryRegex[] GetExcludedDirectoryRegex(this SelectionOptions options)
        {
            if (options.ExcludedDirectories?.Any() ?? false)
            {
                return options.ExcludedDirectories.Select(Helpers.DirectoryRegex.FromWildCard).ToArray();
            }
            else
            {
                return Array.Empty<Helpers.DirectoryRegex>();
            }
        }

        /// <summary>
        /// Determine if the file should be rejected based on its filename
        /// </summary>
        /// <param name="options"></param>
        /// <param name="directoryPath">directory Name to compare</param>
        /// <param name="exclusionCollection">
        /// The collection of regex objects to compare against - If this is null, a new array will be generated from <see cref="SelectionOptions.ExcludedFiles"/>. <br/>
        /// ref is used for optimization during the course of the run, to avoid creating regex for every file check.
        /// </param>
        /// <returns></returns>
        public static bool ShouldExcludeDirectoryName(this SelectionOptions options, string directoryPath, IEnumerable<Helpers.DirectoryRegex> exclusionCollection = null)
        {
            if (exclusionCollection is null) exclusionCollection = options.GetExcludedDirectoryRegex();
            if (exclusionCollection.None()) return false;
            return exclusionCollection.Any(ob => ob.ShouldExcludeDirectory(directoryPath));
        }

        /// <inheritdoc cref="ShouldExcludeDirectoryName(SelectionOptions, string, ref Tuple{bool, Regex}[])"/>
        public static bool ShouldExcludeDirectoryName(this SelectionOptions options, DirectoryInfo Source, IEnumerable<Helpers.DirectoryRegex> exclusionCollection = null) => ShouldExcludeDirectoryName(options, Source.FullName, exclusionCollection);

        /// <inheritdoc cref="ShouldExcludeDirectoryName(SelectionOptions, string, ref Tuple{bool, Regex}[])"/>
        public static bool ShouldExcludeDirectoryName(this SelectionOptions options, IDirectoryPair pair, IEnumerable<Helpers.DirectoryRegex> exclusionCollection = null) => ShouldExcludeDirectoryName(options, pair.Source.FullName, exclusionCollection);

        #endregion

        #region < Symbolic Links (Files) >

        /// <summary>
        /// Evaluate if the file should be excluded under the JunctionPoint exclusion settings.
        /// </summary>
        /// <param name="options">Evaluates <see cref="SelectionOptions.ExcludeJunctionPoints"/> and <see cref="SelectionOptions.ExcludeJunctionPointsForFiles"/></param>
        /// <param name="file"></param>
        /// <returns>TRUE if the file should be excluded or doesn't exist, FALSE if the file should be copied.</returns>
        public static bool ExcludeSymbolicFile(this SelectionOptions options, FileInfo file)
        {
            if (!file.Exists) return true;
            if (options.ExcludeJunctionPoints | options.ExcludeJunctionPointsForFiles)
                return file.IsSymbolicLink();
            else
                return false;
        }

        /// <inheritdoc cref="ExcludeSymbolicFile(SelectionOptions, FileInfo)"/>
        public static bool ExcludeSymbolicFile(this SelectionOptions options, IFilePair pair) => ExcludeSymbolicFile(options, pair.Source);

        /// <inheritdoc cref="ExcludeSymbolicFile(SelectionOptions, FileInfo)"/>
        public static bool ExcludeSymbolicFile(this SelectionOptions options, string file) => ExcludeSymbolicFile(options, new FileInfo(file));

        #endregion

        #region < Symbolic Links (Directories) >

        /// <summary>
        /// Evaluate if the Directory should be excluded under the JunctionPoint exclusion settings.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="directory"></param>
        /// <returns>TRUE if the directory is a valid symbolic and the <see cref="SelectionOptions.ExcludeJunctionPointsForDirectories"/> | <see cref="SelectionOptions.ExcludeJunctionPoints"/> options are set. </returns>
        public static bool ShouldExcludeJunctionDirectory(this SelectionOptions options, string directory)
        {
            if (!Directory.Exists(directory)) return true;
            if (options.ExcludeJunctionPoints | options.ExcludeJunctionPointsForDirectories)
                return SymbolicLink.IsJunctionOrSymbolic(directory);
            else
                return false;
        }

        /// <inheritdoc cref="ShouldExcludeJunctionDirectory(SelectionOptions, string)"/>
        public static bool ShouldExcludeJunctionDirectory(this SelectionOptions options, DirectoryInfo directory)
        {
            if (!directory.Exists) return true;
            if (options.ExcludeJunctionPoints | options.ExcludeJunctionPointsForDirectories)
                return SymbolicLink.IsJunctionOrSymbolic(directory.FullName);
            else
                return false;
        }

        /// <inheritdoc cref="ShouldExcludeJunctionDirectory(SelectionOptions, string)"/>
        public static bool ShouldExcludeJunctionDirectory(this SelectionOptions options, IDirectoryPair pair) => ShouldExcludeJunctionDirectory(options, pair.Source);

        #endregion

    }
}
