using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Options
{
    /// <summary>
    /// Extension Methods for <see cref="RoboSharp.LoggingOptions"/> to assist with custom implementations
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Evalute the path against the <see cref="LoggingOptions.UnicodeLogPath"/> and the other paths
        /// </summary>
        /// <param name="options"></param>
        /// <param name="path"></param>
        /// <param name="CompareAgainstAppendUniPath">true to compare against <see cref="LoggingOptions.AppendUnicodeLogPath"/></param>
        /// <param name="CompareAgainstLogPath">true to compare against <see cref="LoggingOptions.LogPath"/></param>
        /// <returns>True if the path is writable and unique</returns>
        private static bool IsUniquePath(LoggingOptions options, string path, bool CompareAgainstAppendUniPath, bool CompareAgainstLogPath)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(path)) return false;
            bool Unique = !path.Equals(options.UnicodeLogPath, StringComparison.InvariantCultureIgnoreCase);
            if (Unique)
                Unique = !CompareAgainstAppendUniPath || !path.Equals(options.AppendUnicodeLogPath, StringComparison.InvariantCultureIgnoreCase);
            if (Unique)
                Unique = !CompareAgainstLogPath || !path.Equals(options.AppendLogPath, StringComparison.InvariantCultureIgnoreCase);
            return Unique;
        }

        /// <summary>
        /// Ensures that the directories for each log path are created (required to prevent errors when using <see cref="AppendToLogs(LoggingOptions, string[])"/>
        /// </summary>
        /// <param name="options"></param>
        public static void EnsureLogFileDirectoriesCreated(this LoggingOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (!string.IsNullOrWhiteSpace(options.UnicodeLogPath))
                Directory.CreateDirectory(Path.GetDirectoryName(options.UnicodeLogPath));

            if (!string.IsNullOrWhiteSpace(options.AppendUnicodeLogPath))
                Directory.CreateDirectory(Path.GetDirectoryName(options.AppendUnicodeLogPath));

            if (!string.IsNullOrWhiteSpace(options.LogPath))
                Directory.CreateDirectory(Path.GetDirectoryName(options.LogPath));

            if (!string.IsNullOrWhiteSpace(options.AppendLogPath))
                Directory.CreateDirectory(Path.GetDirectoryName(options.AppendLogPath));
        }

        /// <summary>
        /// Calls <see cref="File.WriteAllLines(string, IEnumerable{string})"/> for each of the logging paths.
        /// Checks each of the log paths to ensure the lines are not appended multiple times on the same path.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="lines">lines to append to the log paths</param>
        /// <param name="defaultEncoding">If not specified, uses <see cref="Encoding.Default"/></param>
        public static void AppendToLogs(this LoggingOptions options, Encoding defaultEncoding = null, params string[] lines)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (lines.Length == 0) return;

            if (!string.IsNullOrWhiteSpace(options.UnicodeLogPath))
                File.AppendAllLines(options.UnicodeLogPath, lines, Encoding.Unicode);

            if (IsUniquePath(options, options.AppendUnicodeLogPath, false, false))
                File.AppendAllLines(options.AppendUnicodeLogPath, lines, Encoding.Unicode);

            if (IsUniquePath(options, options.LogPath, true, false))
            {
                if (options.OutputAsUnicode)
                    File.AppendAllLines(options.LogPath, lines, Encoding.Unicode);
                else
                    File.AppendAllLines(options.LogPath, lines, defaultEncoding ?? Encoding.Default);
            }

            if (IsUniquePath(options, options.AppendLogPath, true, true))
            {
                if (options.OutputAsUnicode)
                    File.AppendAllLines(options.AppendLogPath, lines, Encoding.Unicode);
                else
                    File.AppendAllLines(options.AppendLogPath, lines, defaultEncoding ?? Encoding.Default);
            }
        }

        /// <inheritdoc cref="AppendToLogs(LoggingOptions, Encoding, string[])"/>
        public static void AppendToLogs(this LoggingOptions options, params string[] lines)
            => options.AppendToLogs(null, lines);

        /// <summary>
        /// <see cref="AppendToLogs(LoggingOptions, string[])"/> wrapped inside of Task.Run
        /// </summary>
        /// <inheritdoc cref="AppendToLogs(LoggingOptions, string[])"/>
        /// <remarks><inheritdoc cref="AppendToLogs(LoggingOptions, string[])" path="*"/></remarks>
        public static Task AppendToLogsAsync(this LoggingOptions LoggingOptions, Encoding defaultEncoding = null, params string[] lines) => Task.Run(() => LoggingOptions.AppendToLogs(defaultEncoding, lines));

        /// <inheritdoc cref="AppendToLogsAsync(LoggingOptions, Encoding, string[])"/>
        public static Task AppendToLogsAsync(this LoggingOptions LoggingOptions, params string[] lines) => Task.Run(() => LoggingOptions.AppendToLogs(lines));


        /// <summary>
        /// Deletes the files at the <see cref="LoggingOptions.LogPath"/> and <see cref="LoggingOptions.UnicodeLogPath"/> to allow the Append extension methods to write to them as if they were overwritten.
        /// </summary>
        /// <param name="options"></param>
        public static void DeleteLogFiles(this LoggingOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (!string.IsNullOrWhiteSpace(options.UnicodeLogPath))
                if (File.Exists(options.UnicodeLogPath))
                    File.Delete(options.UnicodeLogPath);

            if (!string.IsNullOrWhiteSpace(options.LogPath))
                if (File.Exists(options.LogPath))
                    File.Delete(options.LogPath);

        }

    }
}
