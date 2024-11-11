using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RoboSharp.Extensions.Options;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// Helper Class used when using regex to evaluate Directory Paths
    /// </summary>
    public class DirectoryRegex
    {
        /// <summary>
        /// Create a new Regex Pair for evaluating Excluded Directories
        /// </summary>
        /// <param name="isPathRegex"><inheritdoc cref="IsPathRegex" path="*"/></param>
        /// <param name="pattern"><inheritdoc cref="Pattern" path="*"/></param>
        /// <exception cref="ArgumentNullException"/>
        public DirectoryRegex(bool isPathRegex, Regex pattern)
        {
            IsPathRegex = isPathRegex;
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        }

        /// <inheritdoc cref="DirectoryRegex.DirectoryRegex(bool, Regex)"/>
        public DirectoryRegex(bool isPathRegex, string pattern, RegexOptions options = RegexOptions.IgnoreCase) : this(isPathRegex, new Regex(pattern, options))
        { }

        /// <inheritdoc cref="SelectionExtensions.CreateWildCardRegex(string)"/>
        /// <inheritdoc cref="DirectoryRegex.DirectoryRegex(bool, Regex)"/>
        /// <remarks>If the pattern contains any DirectorySeperatorCharacters (which are uncommon in WildCard patterns), it will set <see cref="IsPathRegex"/> = <see langword="true"/></remarks>
        public static DirectoryRegex FromWildCard(string pattern)
        {
            bool isPathRegex = pattern.Contains(Path.DirectorySeparatorChar) || pattern.Contains(Path.AltDirectorySeparatorChar);
            return new DirectoryRegex(isPathRegex, SelectionExtensions.CreateWildCardRegex(pattern));
        }

        /// <summary>
        /// This value should be TRUE when using the regex to evaluate the directory's full path ( <see cref="FileSystemInfo.FullName"/> ). 
        /// <br/> This allows to regex out specific paths, such as  "C:\\Program.?Files*\\*"
        /// </summary>
        /// <remarks>When this value is FALSE, the <see cref="ShouldExcludeDirectory(string)"/> method will only evaluate the directory's name.</remarks>
        public bool IsPathRegex { get; }

        /// <summary>
        /// The Regex Pattern to evaluate with
        /// </summary>
        public Regex Pattern { get; }

        /// <summary>
        /// Evaluate the path against the Regex Pattern to see if it should be excluded.
        /// </summary>
        /// <param name="dirPath">the path to evaluate</param>
        /// <returns>
        ///  - IsPathRegex == true --> Pattern.IsMatch(<paramref name="dirPath"/>) <br/>
        ///  - IsPathRegex == false --> Pattern.IsMatch(Path.GetFileName(<paramref name="dirPath"/>))
        /// </returns>
        public virtual bool ShouldExcludeDirectory(string dirPath)
        {
            if (IsPathRegex)
                return Pattern.IsMatch(dirPath); // evaluate against the entire path
            else
                return Pattern.IsMatch(Path.GetFileName(dirPath));// evaluate against the folder name
        }

        /// <summary>
        /// Evaluate the directory path to see if it should be excluded
        /// </summary>
        /// <inheritdoc cref="ShouldExcludeDirectory(string)"/>
        public bool ShouldExcludeDirectory(DirectoryInfo directory)
        {
            return ShouldExcludeDirectory(directory?.FullName ?? throw new ArgumentNullException(nameof(directory)));
        }

        /// <summary>
        /// Evaluate the directoryPair.Source object to see if it should be excluded
        /// </summary>
        /// <inheritdoc cref="ShouldExcludeDirectory(string)"/>
        public bool ShouldExcludeDirectory(IDirectoryPair directoryPair)
        {
            if (directoryPair is null) throw new ArgumentNullException(nameof(directoryPair));
            return ShouldExcludeDirectory(directoryPair.Source);
        }
    }
}
