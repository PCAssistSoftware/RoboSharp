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
    /// Extension Methods for the <see cref="IFilePair"/> interface
    /// </summary>
    public static class IFilePairExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool FileDoesntExist(string destination) => !File.Exists(destination);

        /// <summary> Evaluate the roots of the Source and Destination </summary>
        /// <returns>True if the Source and Destination have the same root, otherwise false.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static bool IsLocatedOnSameDrive(this IFilePair pair)
        {
            if (pair is null) throw new ArgumentNullException(nameof(pair));
            return Path.GetPathRoot(pair.Source.FullName).Equals(Path.GetPathRoot(pair.Destination.FullName), StringComparison.InvariantCultureIgnoreCase);
        }


        /// <inheritdoc cref="Options.SelectionExtensions.IsExtra{T}(T, T)"/>
        public static bool IsExtra(this IFilePair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : Options.SelectionExtensions.IsExtra(pair.Source, pair.Destination);


        /// <inheritdoc cref="Options.SelectionExtensions.IsLonely{T}(T, T)"/>
        public static bool IsLonely(this IFilePair pair)
            => pair is null ? throw new ArgumentNullException(nameof(pair)) : Options.SelectionExtensions.IsLonely(pair.Source, pair.Destination);

        /// <summary>
        /// Gets the file length from the pair, prioritizing the source file over the destination file.
        /// </summary>
        /// <param name="pair">the file pair</param>
        /// <returns>
        /// If the source file exists, return the source file's length in bytes. <br/>
        /// If the destination file exists, return the destination file's length in bytes. <br/>
        /// If neither exist, return 0;
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        public static long GetFileLength(this IFilePair pair)
        {
            if (pair is null) throw new ArgumentNullException(nameof(pair));

            // Check source
            if (pair.Source is null) throw new ArgumentNullException("IFilePair.Source is null");
            if (pair.Source.Exists) return pair.Source.Length;

            // Check Destination
            if (pair.Destination is null) throw new ArgumentNullException("IFilePair.Destination is null");
            if (pair.Destination.Exists) return pair.Destination.Length;

            return 0; // neither exist
        }
        #region < IsSourceNewer >

        /// <summary>
        /// Check if the Source file is newer than the Destination File
        /// </summary>
        /// <param name="source">This path is assumed to exist since it is the patht to copy from</param>
        /// <param name="destination">the destination path - may or may not exist</param>
        /// <returns>TRUE if the source is newer, or the destination does not exist. FALSE if the destination is newer.</returns>
        /// <exception cref="ArgumentException"/>
        public static bool IsSourceNewer(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("source can not be empty!");
            if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("destination can not be empty!");

            bool sourceExists = File.Exists(source);
            if (File.Exists(destination) && sourceExists)
                return File.GetLastWriteTime(source) > File.GetLastWriteTime(destination);
            else
                return sourceExists; // destination does not exist, return true if source exists.
        }

        /// <inheritdoc cref="IsSourceNewer(string, string)"/>
        /// <exception cref="ArgumentNullException"/>
        public static bool IsSourceNewer(FileInfo source, FileInfo destination)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (destination.Exists && source.Exists)
                return source.LastWriteTime > destination.LastWriteTime;
            else
                return source.Exists;
        }

        /// <inheritdoc cref="IsSourceNewer(FileInfo, FileInfo)"/>
        public static bool IsSourceNewer(this IFilePair filepair)
        {
            if (filepair is null) throw new ArgumentNullException(nameof(filepair));
            return IsSourceNewer(filepair.Source, filepair.Destination);
        }

        #endregion

        #region < IsDestinationNewer >

        /// <summary>
        /// Check if the Destination file is newer than the Source file
        /// </summary>
        /// <param name="source">This path is assumed to exist since it is the patht to copy from</param>
        /// <param name="destination">the destination path - may or may not exist</param>
        /// <returns>TRUE if the destination file is newer, otherwise false</returns>
        /// <exception cref="ArgumentException"/>
        public static bool IsDestinationNewer(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("source can not be empty!");
            if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("destination can not be empty!");
            bool destExists = File.Exists(destination);
            if (File.Exists(source) && destExists)
                return File.GetLastWriteTime(source) < File.GetLastWriteTime(destination);
            else
                return destExists; // Source cannot exist at this point in code, so if destination exists it is newer. Otherwise neither exist.
        }

        /// <inheritdoc cref="IsDestinationNewer(string, string)"/>
        /// <exception cref="ArgumentNullException"/>
        public static bool IsDestinationNewer(FileInfo source, FileInfo destination)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (source.Exists && destination.Exists)
                return source.LastWriteTime < destination.LastWriteTime;
            else
                return destination.Exists;
        }

        /// <inheritdoc cref="IsDestinationNewer(FileInfo, FileInfo)"/>
        public static bool IsDestinationNewer(this IFilePair filePair)
        {
            if (filePair is null) throw new ArgumentNullException(nameof(filePair));
            return IsDestinationNewer(filePair.Source, filePair.Destination);
        }

        #endregion

        #region < IsSameDate >

        /// <summary>
        /// Check if the Source and Destination files have the same LastWriteTime
        /// </summary>
        /// <param name="source">This path is assumed to exist since it is the patht to copy from</param>
        /// <param name="destination">the destination path - may or may not exist</param>
        /// <returns>TRUE if both files exist, and their LastWriteTime is identical</returns>
        /// <exception cref="ArgumentException"/>
        public static bool IsSameDate(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("source can not be empty!");
            if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("destination can not be empty!");
            if (File.Exists(destination) && File.Exists(source))
                return File.GetLastWriteTime(source) == File.GetLastWriteTime(destination);
            else
                return false;
        }

        /// <inheritdoc cref="IsDestinationNewer(string, string)"/>
        /// <exception cref="ArgumentNullException"/>
        public static bool IsSameDate(FileInfo source, FileInfo destination)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (destination.Exists && source.Exists)
                return source.LastWriteTime == destination.LastWriteTime;
            else
                return false;
        }

        /// <inheritdoc cref="IsDestinationNewer(FileInfo, FileInfo)"/>
        public static bool IsSameDate(this IFilePair filePair)
        {
            if (filePair is null) throw new ArgumentNullException(nameof(filePair));
            return IsSameDate(filePair.Source, filePair.Destination);
        }

        #endregion
    }
}
