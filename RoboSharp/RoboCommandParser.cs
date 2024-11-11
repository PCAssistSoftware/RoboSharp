using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboSharp
{
    /// <summary>
    /// Factory class used to parse a string that represents a Command Line call to robocommand, and return a command with those parameters.
    /// </summary>
    public static partial class RoboCommandParser
    {
        #region < Public Methods >

        /// <summary>Attempt the parse the <paramref name="command"/> into a new IRoboCommand object</summary>
        /// <returns>True if successful, otherwise false</returns>
        /// <param name="result">If successful, a new IRobocommand, otherwise null</param>
        /// <param name="factory">The factory used to generate the robocommand. <br/>If not specified, uses <see cref="RoboCommandFactory.Default"/></param>
        /// <inheritdoc cref="Parse(string, IRoboCommandFactory)"/>
        /// <param name="command"/>
        public static bool TryParse(string command, out IRoboCommand result, IRoboCommandFactory factory = default)
        {
            try
            {
                result = Parse(command, factory ?? RoboCommandFactory.Default);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>Attempt the parse the <paramref name="commandOptions"/> into a new IRoboCommand object</summary>
        /// <returns>True if successful, otherwise false</returns>
        /// <param name="result">If successful, a new IRobocommand, otherwise null</param>
        /// <inheritdoc cref="ParseOptions(string, IRoboCommandFactory)"/>
        /// <param name="commandOptions"/><param name="factory"/>
        public static bool TryParseOptions(string commandOptions, out IRoboCommand result, IRoboCommandFactory factory = default)
        {
            try
            {
                result = ParseOptions(commandOptions, factory ?? RoboCommandFactory.Default);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <returns>A new <see cref="RoboCommand"/></returns>
        /// <inheritdoc cref="Parse(string, Interfaces.IRoboCommandFactory)"/>
        public static IRoboCommand Parse(string command) => Parse(command, RoboCommandFactory.Default);

        /// <summary>
        /// Parse the <paramref name="command"/> text into a new IRoboCommand.
        /// </summary>
        /// <param name="command">The Command-Line string of options to parse. <br/>Example:  robocopy "C:\source" "D:\destination" *.pdf /xc /copyall </param>
        /// <param name="factory">The factory used to generate the robocommand</param>
        /// <returns>A new IRoboCommand object generated from the <paramref name="factory"/></returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="RoboCommandParserException"/>
        public static IRoboCommand Parse(string command, IRoboCommandFactory factory)
        {
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException("Input string is null or empty!", nameof(command));
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            Debugger.Instance.DebugMessage($"RoboCommandParser.Parse - Begin parsing input string : {command}");
                        
            // Trim robocopy.exe from the beginning of the string, then extract the source/destination.
            string commandText = TrimRobocopy(command);
            ParsedSourceDest paths = ParsedSourceDest.Parse(commandText);

            // Filters SHOULD be immediately following the source/destination string at the very beginning of the text
            // Also Ensure white space at end of string because all constants have it
            var roboCommand = ParseOptionsInternal(paths, factory);
            Debugger.Instance.DebugMessage("RoboCommandParser.Parse completed successfully.\n");
            return roboCommand;
        }

        /// <summary>
        /// Parse a string of text that represents a set of robocopy options into a new IRoboCommand. Source and Destination values are ignored.
        /// </summary>
        /// <param name="commandOptions">The robocopy options to parse. Must not contain the phrase 'robocopy'. Must also not contain source/destination info.</param>
        /// <param name="factory">The factory used to generate the robocommand. <br/>If not specified, uses <see cref="RoboCommandFactory.Default"/></param>
        /// <returns>An IRoboCommand that represents the specified options</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="RoboCommandParserException"/>
        public static IRoboCommand ParseOptions(string commandOptions, IRoboCommandFactory factory = default)
        {
            // Sanity Checks to ensure correct method is being utilized:
            if (string.IsNullOrWhiteSpace(commandOptions)) throw new RoboCommandParserException("Input string is null or empty!", nameof(commandOptions));
            Debugger.Instance.DebugMessage($"RoboCommandParser.ParseOptions - Begin parsing input string : {commandOptions}");
            var roboCommand = ParseOptionsInternal(ParsedSourceDest.ParseOptionsOnly(commandOptions), factory ?? RoboCommandFactory.Default);
            Debugger.Instance.DebugMessage("RoboCommandParser.ParseOptions completed successfully.\n");
            return roboCommand;
        }

        /// <summary> Parse the options text into a new IRoboCommand object. </summary>
        /// <param name="factory"/><param name="sourceDest">struct containing the source/destination data to pass into the factory</param>
        private static IRoboCommand ParseOptionsInternal(ParsedSourceDest sourceDest, IRoboCommandFactory factory)
        {
            StringBuilder sanitizedCmd = sourceDest.SanitizedString.Trim().Append(' ');
            var filters = ExtractFileFilters(sanitizedCmd);

            // Get the command
            var roboCommand = factory.GetRoboCommand(sourceDest.Source, sourceDest.Destination, ParseCopyFlags(sanitizedCmd), ParseSelectionFlags(sanitizedCmd));

            // apply the file filters, if any were discovered
            if (filters != null) roboCommand.CopyOptions.AddFileFilter(filters);

            // apply the remaining options
            return roboCommand
                .ParseCopyOptions(sanitizedCmd)
                .ParseLoggingOptions(sanitizedCmd)
                .ParseSelectionOptions(sanitizedCmd)
                .ParseRetryOptions(sanitizedCmd);
        }

        #endregion

        #region < Copy Options Parsing >

        private static CopyActionFlags ParseCopyFlags(StringBuilder cmd)
        {
            CopyActionFlags flags = CopyActionFlags.Default;
            cmd.RemoveString(CopyOptions.NETWORK_COMPRESSION, () => flags |= CopyActionFlags.Compress);
            cmd.RemoveString(CopyOptions.COPY_SUBDIRECTORIES, () => flags |= CopyActionFlags.CopySubdirectories);
            cmd.RemoveString(CopyOptions.COPY_SUBDIRECTORIES_INCLUDING_EMPTY, () => flags |= CopyActionFlags.CopySubdirectoriesIncludingEmpty);
            cmd.RemoveString(CopyOptions.CREATE_DIRECTORY_AND_FILE_TREE, () => flags |= CopyActionFlags.CreateDirectoryAndFileTree);
            cmd.RemoveString(CopyOptions.MIRROR, () => flags |= CopyActionFlags.Mirror);
            cmd.RemoveString(CopyOptions.MOVE_FILES, () => flags |= CopyActionFlags.MoveFiles);
            cmd.RemoveString(CopyOptions.MOVE_FILES_AND_DIRECTORIES, () => flags |= CopyActionFlags.MoveFilesAndDirectories);
            cmd.RemoveString(CopyOptions.PURGE, () => flags |= CopyActionFlags.Purge);
            return flags;
        }

        /// <summary>
        /// File Filters to INCLUDE - These are always be at the beginning of the input string
        /// </summary>
        /// <param name="command">An input string with the source and destination removed. 
        /// <br/>Valid : *.*  ""text"" /XF  -- Reads up until the first OPTION switch
        /// <br/>Not Valid : robocopy Source destination -- these will be consdidered 3 seperate filters.
        /// <br/>Not Valid : Source/destination -- these will be considered as file filters.
        /// </param>
        internal static IEnumerable<string> ExtractFileFilters(StringBuilder command)
        {
            const string debugFormat = "--> Found File Filter : {0}";
            Debugger.Instance.DebugMessage($"Parsing Copy Options - Extracting File Filters");

            var input = command.ToString();
            var match = FileFilter_Regex(input);
            string foundFilters = match.Groups["filter"].Value;
            command.RemoveString(foundFilters);

            if (match.Success && !string.IsNullOrWhiteSpace(foundFilters))
            {
                return ParseFilters(foundFilters, debugFormat).Where(ExtensionMethods.IsNotEmpty);
            }
            else
            {
                Debugger.Instance.DebugMessage($"--> No file filters found.");
                return null;
            }
        }

        /// <summary>
        /// Parse the Copy Options not discovered by ParseCopyFlags
        /// </summary>
        private static IRoboCommand ParseCopyOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Copy Options");
            var options = roboCommand.CopyOptions;

            options.CheckPerFile |= command.RemoveString(CopyOptions.CHECK_PER_FILE);
            options.CopyAll |= command.RemoveString(CopyOptions.COPY_ALL);
            options.CopyFilesWithSecurity |= command.RemoveString(CopyOptions.COPY_FILES_WITH_SECURITY);
            options.CopySymbolicLink |= command.RemoveString(CopyOptions.COPY_SYMBOLIC_LINK);
            options.DoNotCopyDirectoryInfo |= command.RemoveString(CopyOptions.DO_NOT_COPY_DIRECTORY_INFO);
            options.DoNotUseWindowsCopyOffload |= command.RemoveString(CopyOptions.DO_NOT_USE_WINDOWS_COPY_OFFLOAD);
            options.EnableBackupMode |= command.RemoveString(CopyOptions.ENABLE_BACKUP_MODE);
            options.EnableEfsRawMode |= command.RemoveString(CopyOptions.ENABLE_EFSRAW_MODE);
            options.EnableRestartMode |= command.RemoveString(CopyOptions.ENABLE_RESTART_MODE);
            options.EnableRestartModeWithBackupFallback |= command.RemoveString(CopyOptions.ENABLE_RESTART_MODE_WITH_BACKUP_FALLBACK);
            options.FatFiles |= command.RemoveString(CopyOptions.FAT_FILES);
            options.FixFileSecurityOnAllFiles |= command.RemoveString(CopyOptions.FIX_FILE_SECURITY_ON_ALL_FILES);
            options.FixFileTimesOnAllFiles |= command.RemoveString(CopyOptions.FIX_FILE_TIMES_ON_ALL_FILES);
            options.RemoveFileInformation |= command.RemoveString(CopyOptions.REMOVE_FILE_INFORMATION);
            options.TurnLongPathSupportOff |= command.RemoveString(CopyOptions.TURN_LONG_PATH_SUPPORT_OFF);
            options.UseUnbufferedIo |= command.RemoveString(CopyOptions.USE_UNBUFFERED_IO);

            // Non-Boolean Options

            if (TryExtractParameter(command, CopyOptions.ADD_ATTRIBUTES, out string param))
            {
                options.AddAttributes = param;
            }

            _ = TryExtractParameter(command, CopyOptions.COPY_FLAGS, out param); // Always set this value
            options.CopyFlags = param;
            
            if (TryExtractParameter(command, CopyOptions.DEPTH, out param) && int.TryParse(param, out int value))
            {
                options.Depth = value;
            }
            
            _ = TryExtractParameter(command, CopyOptions.DIRECTORY_COPY_FLAGS, out param); // Always set this value
            options.DirectoryCopyFlags = param;

            if (TryExtractParameter(command, CopyOptions.INTER_PACKET_GAP, out param) && int.TryParse(param, out value))
            {
                options.InterPacketGap = value;
            }
            if (TryExtractParameter(command, CopyOptions.MONITOR_SOURCE_CHANGES_LIMIT, out param) && int.TryParse(param, out value))
            {
                options.MonitorSourceChangesLimit = value;
            }
            if (TryExtractParameter(command, CopyOptions.MONITOR_SOURCE_TIME_LIMIT, out param) && int.TryParse(param, out value))
            {
                options.MonitorSourceTimeLimit = value;
            }
            if (TryExtractParameter(command, CopyOptions.MULTITHREADED_COPIES_COUNT, out param) && int.TryParse(param, out value))
            {
                options.MultiThreadedCopiesCount = value;
            }
            if (TryExtractParameter(command, CopyOptions.REMOVE_ATTRIBUTES, out param))
            {
                options.RemoveAttributes = param;
            }
            if (TryExtractParameter(command, CopyOptions.RUN_HOURS, out param) && CopyOptions.IsRunHoursStringValid(param))
            {
                options.RunHours = param;
            }
            return roboCommand;
        }

        #endregion

        #region < Selection Options Parsing  >
        private static SelectionFlags ParseSelectionFlags(StringBuilder cmd)
        {
            SelectionFlags flags = SelectionFlags.Default;
            cmd.RemoveString(SelectionOptions.EXCLUDE_CHANGED, () => flags |= SelectionFlags.ExcludeChanged);
            cmd.RemoveString(SelectionOptions.EXCLUDE_EXTRA, () => flags |= SelectionFlags.ExcludeExtra);
            cmd.RemoveString(SelectionOptions.EXCLUDE_JUNCTION_POINTS, () => flags |= SelectionFlags.ExcludeJunctionPoints);
            cmd.RemoveString(SelectionOptions.EXCLUDE_JUNCTION_POINTS_FOR_DIRECTORIES, () => flags |= SelectionFlags.ExcludeJunctionPointsForDirectories);
            cmd.RemoveString(SelectionOptions.EXCLUDE_JUNCTION_POINTS_FOR_FILES, () => flags |= SelectionFlags.ExcludeJunctionPointsForFiles);
            cmd.RemoveString(SelectionOptions.EXCLUDE_LONELY, () => flags |= SelectionFlags.ExcludeLonely);
            cmd.RemoveString(SelectionOptions.EXCLUDE_NEWER, () => flags |= SelectionFlags.ExcludeNewer);
            cmd.RemoveString(SelectionOptions.EXCLUDE_OLDER, () => flags |= SelectionFlags.ExcludeOlder);
            cmd.RemoveString(SelectionOptions.INCLUDE_SAME, () => flags |= SelectionFlags.IncludeSame);
            cmd.RemoveString(SelectionOptions.INCLUDE_TWEAKED, () => flags |= SelectionFlags.IncludeTweaked);
            cmd.RemoveString(SelectionOptions.INCLUDE_MODIFIED, () => flags |= SelectionFlags.IncludeModified);
            cmd.RemoveString(SelectionOptions.ONLY_COPY_ARCHIVE_FILES, () => flags |= SelectionFlags.OnlyCopyArchiveFiles);
            cmd.RemoveString(SelectionOptions.ONLY_COPY_ARCHIVE_FILES_AND_RESET_ARCHIVE_FLAG, () => flags |= SelectionFlags.OnlyCopyArchiveFilesAndResetArchiveFlag);
            return flags;
        }

        /// <summary>
        /// Parse the Selection Options not discovered by ParseSelectionFlags
        /// </summary>
        private static IRoboCommand ParseSelectionOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Selection Options");
            var options = roboCommand.SelectionOptions;
            options.CompensateForDstDifference |= command.RemoveString(SelectionOptions.COMPENSATE_FOR_DST_DIFFERENCE);
            options.UseFatFileTimes |= command.RemoveString(SelectionOptions.USE_FAT_FILE_TIMES);

            if (TryExtractParameter(command, SelectionOptions.INCLUDE_ATTRIBUTES, out string param))
            {
                options.IncludeAttributes = param;
            }
            if (TryExtractParameter(command, SelectionOptions.EXCLUDE_ATTRIBUTES, out param))
            {
                options.ExcludeAttributes = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MAX_FILE_AGE, out param))
            {
                options.MaxFileAge = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MAX_FILE_SIZE, out param) && long.TryParse(param, out var value))
            {
                options.MaxFileSize = value;
            }
            if (TryExtractParameter(command, SelectionOptions.MIN_FILE_AGE, out param))
            {
                options.MinFileAge = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MIN_FILE_SIZE, out param) && long.TryParse(param, out value))
            {
                options.MinFileSize = value;
            }
            if (TryExtractParameter(command, SelectionOptions.MAX_LAST_ACCESS_DATE, out param))
            {
                options.MaxLastAccessDate = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MIN_LAST_ACCESS_DATE, out param))
            {
                options.MinLastAccessDate = param;
            }

            options.ExcludedDirectories.AddRange(ExtractExclusionDirectories(command));
            options.ExcludedFiles.AddRange(ExtractExclusionFiles(command));

            return roboCommand;
        }

        //lang=regex
        internal const string FileFilter = @"^\s*(?<filter>((?<Quotes>""[^""]+"") | (?<NoQuotes>((?<!\/)[^\/""])+) )+)"; // anything up until the first standalone option 
        //lang=regex
        internal const string XF_Pattern = @"(?<filter>\/XF\s*( ((?<Quotes>""(\/\/[a-zA-Z]|[A-Z]:|[^/:\s])?[\w\*$\-\/\\.\s]+"") | (?<NoQuotes>(\/\/[a-zA-Z]|[A-Z]:|[^\/\:\s])?[\w*$\-\/\\.]+)) (\s*(?!\/[a-zA-Z])) )+)";
        //lang=regex
        internal const string XD_Pattern = @"(?<filter>\/XD\s*(( (?<Quotes>""(\/\/[a-zA-Z]|[A-Z]:|[^/:\s])?[\w\*$\-\/\\.\s]+"") | (?<NoQuotes>(\/\/[a-zA-Z]|[A-Z]:|[^\/\:\s])?[\w*$\-\/\\.]+)) (\s*(?!\/[a-zA-Z])) )+)";
        private const RegexOptions X_PatternOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled;

#if NET7_0_OR_GREATER
        [GeneratedRegex(FileFilter, X_PatternOptions, 1000)]
        private static partial Regex FileFilter_Regex();
        private static Match FileFilter_Regex(string input) => FileFilter_Regex().Match(input);

        [GeneratedRegex(XD_Pattern, X_PatternOptions, 1000)]
        private static partial Regex XD_Regex();
        private static MatchCollection XD_Regex(string input) => XD_Regex().Matches(input);
        
        [GeneratedRegex(XF_Pattern, X_PatternOptions, 1000)]
        private static partial Regex XF_Regex();
        private static MatchCollection XF_Regex(string input) => XF_Regex().Matches(input);
#else
        private static Match FileFilter_Regex(string input) => Regex.Match(input, FileFilter, X_PatternOptions, TimeSpan.FromMilliseconds(1000));
        private static MatchCollection XD_Regex(string input) => Regex.Matches(input, XD_Pattern, X_PatternOptions, TimeSpan.FromMilliseconds(1000));
        private static MatchCollection XF_Regex(string input) => Regex.Matches(input, XF_Pattern, X_PatternOptions, TimeSpan.FromMilliseconds(1000));
#endif

        internal static IEnumerable<string> ExtractExclusionFiles(StringBuilder command)
        {
            // Get Excluded Files
            Debugger.Instance.DebugMessage($"Parsing Selection Options - Extracting Excluded Files");
            string input = command.ToString();
            var matchCollection = XF_Regex(input);
            if (matchCollection.Count == 0) Debugger.Instance.DebugMessage($"--> No File Exclusions found.");
            foreach (Match c in matchCollection)
            {
                string s = c.Groups["filter"].Value;
                command.RemoveString(s);
                s = s.TrimStart("/XF").Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    foreach (var item in ParseFilters(s, "---> Excluded File : {0}").Where(ExtensionMethods.IsNotEmpty))
                    {
                        yield return item;
                    };
                }
            }
        }

        internal static IEnumerable<string> ExtractExclusionDirectories(StringBuilder command)
        {
            // Get Excluded Dirs
            Debugger.Instance.DebugMessage($"Parsing Selection Options - Extracting Excluded Directories");
            string input = command.ToString();
            var matchCollection = XD_Regex(input);
            if (matchCollection.Count == 0) Debugger.Instance.DebugMessage($"--> No Directory Exclusions found.");
            foreach (Match c in matchCollection)
            {
                string s = c.Groups["filter"].Value;
                command.RemoveString(s);
                s = s.TrimStart("/XD").Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    foreach (var item in ParseFilters(s, "---> Excluded Directory : {0}").Where(ExtensionMethods.IsNotEmpty))
                    {
                        yield return item;
                    }
                }
            }
        }

#endregion

        private static IRoboCommand ParseLoggingOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Logging Options");
            var options = roboCommand.LoggingOptions;

            options.IncludeFullPathNames |= command.RemoveString(LoggingOptions.INCLUDE_FULL_PATH_NAMES);
            options.IncludeSourceTimeStamps |= command.RemoveString(LoggingOptions.INCLUDE_SOURCE_TIMESTAMPS);
            options.ListOnly |= command.RemoveString(LoggingOptions.LIST_ONLY);
            options.NoDirectoryList |= command.RemoveString(LoggingOptions.NO_DIRECTORY_LIST);
            options.NoFileClasses |= command.RemoveString(LoggingOptions.NO_FILE_CLASSES);
            options.NoFileList |= command.RemoveString(LoggingOptions.NO_FILE_LIST);
            options.NoFileSizes |= command.RemoveString(LoggingOptions.NO_FILE_SIZES);
            options.NoJobHeader |= command.RemoveString(LoggingOptions.NO_JOB_HEADER);
            options.NoJobSummary |= command.RemoveString(LoggingOptions.NO_JOB_SUMMARY);
            options.NoProgress |= command.RemoveString(LoggingOptions.NO_PROGRESS);
            options.OutputAsUnicode |= command.RemoveString(LoggingOptions.OUTPUT_AS_UNICODE);
            options.OutputToRoboSharpAndLog |= command.RemoveString(LoggingOptions.OUTPUT_TO_ROBOSHARP_AND_LOG);
            options.PrintSizesAsBytes |= command.RemoveString(LoggingOptions.PRINT_SIZES_AS_BYTES);
            options.ReportExtraFiles |= command.RemoveString(LoggingOptions.REPORT_EXTRA_FILES);
            options.ShowEstimatedTimeOfArrival |= command.RemoveString(LoggingOptions.SHOW_ESTIMATED_TIME_OF_ARRIVAL   );
            options.VerboseOutput |= command.RemoveString(LoggingOptions.VERBOSE_OUTPUT);

            options.LogPath = ExtractLogPath(LoggingOptions.LOG_PATH, command);
            options.AppendLogPath = ExtractLogPath(LoggingOptions.APPEND_LOG_PATH, command);
            options.UnicodeLogPath = ExtractLogPath(LoggingOptions.UNICODE_LOG_PATH, command);
            options.AppendUnicodeLogPath = ExtractLogPath(LoggingOptions.APPEND_UNICODE_LOG_PATH, command);
            
            return roboCommand;

            static string ExtractLogPath(string filter, StringBuilder input)
            {
                if (TryExtractParameter(input, filter, out string path))
                {
                    return path.Trim('\"');
                }
                return string.Empty;
            }
        }

        private static IRoboCommand ParseRetryOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Retry Options");
            var options = roboCommand.RetryOptions;
            
            options.SaveToRegistry |= command.RemoveString(RetryOptions.SAVE_TO_REGISTRY);
            options.WaitForSharenames |= command.RemoveString(RetryOptions.WAIT_FOR_SHARENAMES);

            if (TryExtractParameter(command, RetryOptions.RETRY_COUNT, out string param) && int.TryParse(param, out int value))
            {
                options.RetryCount = value;
            }
            if (TryExtractParameter(command, RetryOptions.RETRY_WAIT_TIME, out param) && int.TryParse(param, out value))
            {
                options.RetryWaitTime = value;
            }
            return roboCommand;
        }

        #region < Helper Methods / Parsing & Trimming >

        /// <summary>
        /// Check if the path is full qualified as far as robocopy is concerned
        /// </summary>
        /// <remarks>
        /// Strings less than 3 characters are always false.
        /// Strings such as 'C:' should be avoided because it can cause robocopy to use the CURRENT DIRECTORY instead of the root directory
        /// </remarks>
        public static bool IsPathFullyQualified(this string path)
        {
            if (path is null) return false;
            if (path.Length < 3) return false; //There is no way to specify a fixed path with one character (or less).
            if (path.Length >= 3 && IsValidDriveChar(path[0]) && path[1] == System.IO.Path.VolumeSeparatorChar && IsDirectorySeperator(path[2])) return true; //Check for standard paths. C:\
            if (path.Length >= 3 && IsDirectorySeperator(path[0]) && IsDirectorySeperator(path[1])) return true; //This is start of a UNC path
            return false; //Default
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirectorySeperator(char c) => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidDriveChar(char c) => c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';

        /// <summary>
        /// Trim robocopy from that beginning of the input string
        /// </summary>
        /// <returns>A StringBuilder instance that represents the trimmed string</returns>
        internal static string TrimRobocopy(string input)
        {
            var match = TrimRobocopyRegex(input);
            return match.Success ? input.TrimStart(match.Groups[0].Value) : input;
        }

        //lang=regex 
        const string _trimRobocopyRegex = @"^(?<rc>\s*((?<sQuote>"".+?[:$].+?robocopy(\.exe)?"")|(?<sNoQuote>([^:*?""<>|\s]+?[:$][^:*?<>|\s]+?)?robocopy(\.exe)?))\s+)";
#if NET7_0_OR_GREATER
        [GeneratedRegex(_trimRobocopyRegex, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, 1000)]
        private static partial Regex TrimRobocopyRegex();
        private static Match TrimRobocopyRegex(string input) => TrimRobocopyRegex().Match(input);
#else
        private static Match TrimRobocopyRegex(string input) => Regex.Match(input, _trimRobocopyRegex, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(1000));
#endif

        /// <summary>
        /// Parse the string, extracting individual filters out to an IEnumerable string
        /// </summary>
        internal static IEnumerable<string> ParseFilters(string stringToParse, string debugFormat)
        {
            StringBuilder filterBuilder = new StringBuilder();
            bool isQuoted = false;
            bool isBuilding = false;
            foreach (char c in stringToParse)
            {
                if (isQuoted && c == '"')
                {
                    filterBuilder.Append(c);
                    yield return NextFilter();
                }
                else if (isQuoted)
                {
                    filterBuilder.Append(c);
                }
                else if (c == '"')
                {
                    isQuoted = true;
                    isBuilding = true;
                    filterBuilder.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (isBuilding) // unquoted white space indicates end of one filter and start of next. Otherwise ignore whitepsace.
                        yield return NextFilter();
                }
                else
                {
                    isBuilding = true;
                    filterBuilder.Append(c);
                }
            }
            
            if (isBuilding) yield return NextFilter();
            yield break;

            string NextFilter()
            {
                isQuoted = false;
                isBuilding = false;
                string value = filterBuilder.ToString();
                Debugger.Instance.DebugMessage(string.Format(debugFormat, value));
                filterBuilder.Clear();
                return value;
            }
        }

        /// <summary> Attempt to extract the parameter from a format pattern string </summary>
        /// <param name="inputText">The stringbuilder to evaluate and remove the substring from</param>
        /// <param name="parameterFormatString">the parameter to extract. Example :   /LEV:{0}</param>
        /// <param name="value">The extracted value. (only if returns true)</param>
        /// <returns>True if the value was extracted, otherwise false.</returns>
        internal static bool TryExtractParameter(StringBuilder inputText, string parameterFormatString, out string value)
        {
            value = string.Empty;
            string prefix = parameterFormatString.Substring(0, parameterFormatString.IndexOf('{')).TrimEnd('{').Trim(); // Turn /LEV:{0} into /LEV:

            int prefixIndex = inputText.IndexOf(prefix, false);
            if (prefixIndex < 0)
            {
                Debugger.Instance.DebugMessage($"--> Switch {prefix} not detected.");
                return false;
            }

            int lastIndex = inputText.IndexOf(" /", false, prefixIndex + 1);
            int substringLength = lastIndex < 0 ? inputText.Length : lastIndex - prefixIndex + 1;
            var result = inputText.SubString(prefixIndex, substringLength);
            value = result.RemoveSafe(0, prefix.Length).Trim().ToString();
            Debugger.Instance.DebugMessage($"--> Switch {prefix} found. Value : {value}");
            inputText.RemoveSafe(prefixIndex, substringLength);
            return true;
        }

        /// <summary>
        /// Helper object that reports the result from <see cref="Parse(string)"/>
        /// </summary>
        internal readonly partial struct ParsedSourceDest
        {
            public readonly string Source;
            public readonly string Destination;
            public readonly string InputString;
            public readonly StringBuilder SanitizedString;

            private ParsedSourceDest(string source, string dest, string input, StringBuilder sanitized)
            {
                Source = source;
                Destination = dest;
                InputString = input;
                SanitizedString = sanitized;
            }

            //SomeServer/HiddenDrive$/RootFolder   //SomeServer\RootFolder
            //lang=regex                        no quotes                                               quotes
            internal const string uncRegex = @"([\\\/]{2}[^*:?""<>|$\s]+?[$]?[\\\/]?([^*:?""<>|\s]+)?) | (""[\\\/]{2}[^*:?""<>|$]+?[$]?[\\\/]?([^*:?""<>|]+)?"")";

            // c:\  D:/SomeFolder  "X:\Spaced Folder Name"
            //lang=regex                          no quotes                           quotes
            internal const string localRegex = @"([a-zA-Z][:][\\\/]([^:*?""<>|\s]+)?) | (""([a-zA-Z][:][\\\/]([^:*?""<>|]+)?)"")";
            private const string allowedPathsRegex = @"""\s*"" | " + localRegex + " | " + uncRegex;

            internal const string fullRegex = @"^\s* (?<source>" + allowedPathsRegex + @") \s+ (?<dest>" + allowedPathsRegex + @")";
            internal const string destinationUndefinedRegex = @"^\s*(?<source>" + allowedPathsRegex + @")$";
            internal const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace;

#if NET7_0_OR_GREATER
            [GeneratedRegex(fullRegex, regexOptions, 1000)]
            private static partial Regex AllowedPathsFullRegex();

            [GeneratedRegex($"^({allowedPathsRegex})", regexOptions, 1000)]
            private static partial Regex AllowedPathsPartialRegex();

            private static Match AllowedPathsFullRegex(string data) => AllowedPathsFullRegex().Match(data);
            private static Match AllowedPathsPartialRegex(string data) => AllowedPathsPartialRegex().Match(data);
#else
            private static Match AllowedPathsFullRegex(string data) => Regex.Match(data, fullRegex, regexOptions | RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
            private static Match AllowedPathsPartialRegex(string data) => Regex.Match(data, $"^({allowedPathsRegex})", regexOptions | RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
#endif

            /// <returns>Strips out any Source/Dest information, then returns a new object</returns>
            public static ParsedSourceDest ParseOptionsOnly(string inputText)
            {
                string trimmedText = TrimRobocopy(inputText).TrimStart();
                StringBuilder builder = new StringBuilder(trimmedText);
                
                Match fullMatch = AllowedPathsFullRegex(trimmedText);
                if (fullMatch.Success)
                {
                    builder.Remove(0, fullMatch.Length).TrimStart();
                }
                else
                {
                    Match partialMatch = AllowedPathsPartialRegex(trimmedText);
                    if (partialMatch.Success)
                    {
                        builder.Remove(0, partialMatch.Length).TrimStart();
                    }
                }
                return new ParsedSourceDest(string.Empty, string.Empty, inputText, builder);
            }

            /// <summary>
            /// Parse the input text, extracting the Source and Destination info.
            /// </summary>
            /// <param name="inputText">The input text to parse. 
            /// <br/>The expected pattern is :  robocopy "SOURCE" "DESTINATION" 
            /// <br/> - 'robocopy' is optional, but the source/destination must appear in the specified order at the beginning of the text. 
            /// <br/> - Quotes are only required if the path has whitespace.
            /// </param>
            /// <returns>A new <see cref="ParsedSourceDest"/> struct with the results</returns>
            /// <exception cref="RoboCommandParserException"/>
            public static ParsedSourceDest Parse(string inputText)
            {
                var match = AllowedPathsFullRegex(inputText);
                RoboCommandParserException ex;
                if (!match.Success)
                {
                    if (Regex.IsMatch(inputText, destinationUndefinedRegex, regexOptions, TimeSpan.FromMilliseconds(1000)))
                    {
                        ex = new RoboCommandParserException("Invalid command string - Source was provided without providing Destination");
                    }
                    else if (Regex.IsMatch(inputText, $"^\\s* .+? \\s* {allowedPathsRegex}", regexOptions, TimeSpan.FromMilliseconds(1000)))
                    {
                        ex = new RoboCommandParserException("Invalid data present in command options prior to a source/destination paths");
                    }
                    else
                    {
                        ex = new RoboCommandParserException("Source and Destination were unable to be parsed.");
                    }
                    ex.AddData("input", inputText);
                    throw ex;
                }

                string rawSource = match.Groups["source"].Value;
                string rawDest = match.Groups["dest"].Value;
                string source = rawSource.CleanDirectoryPath();
                string dest = rawDest.CleanDirectoryPath();

                // Validate source and destination - both must be empty, or both must be fully qualified
                bool sourceEmpty = string.IsNullOrWhiteSpace(source);
                bool destEmpty = string.IsNullOrWhiteSpace(dest);
                bool sourceQualified = IsPathFullyQualified(source);
                bool destQualified = IsPathFullyQualified(dest);

                StringBuilder commandBuilder = new StringBuilder(inputText);

                if (sourceQualified && destQualified)
                {
                    Debugger.Instance.DebugMessage($"--> Source and Destination Pattern Match Success:");
                    Debugger.Instance.DebugMessage($"----> Source : {source}");
                    Debugger.Instance.DebugMessage($"----> Destination : {dest}");
                    commandBuilder.Remove(0, rawSource.Length).TrimStart();
                    commandBuilder.Remove(0, rawDest.Length).TrimStart();
                    return new ParsedSourceDest(source, dest, inputText, commandBuilder);
                }
                else
                {
                    Debugger.Instance.DebugMessage($"--> Unable to detect a valid Source/Destination pattern match -- Input text : {inputText}");
                    Debugger.Instance.DebugMessage($"----> Source : {source}");
                    Debugger.Instance.DebugMessage($"----> Destination : {dest}");
                    ex = new RoboCommandParserException(message: true switch
                    {
                        true when sourceEmpty && destEmpty => "Source and Destination are missing!",
                        true when sourceEmpty && destQualified => "Destination is fully qualified, but Source is empty. See exception data.",
                        true when destEmpty && sourceQualified => "Source is fully qualified, but Destination is empty. See exception data.",
                        true when !sourceQualified && !destQualified => "Source and Destination are not fully qualified. See exception data.",
                        true when !sourceQualified => "Source is not fully qualified. See exception data. ",
                        true when !destQualified => "Destination is not fully qualified. See exception data.",
                        _ => "Source / Destination Parsing Error",
                    });
                    ex.AddData("Input Text", inputText);
                    ex.AddData("Parsed Source", rawSource);
                    ex.AddData("Parsed Destination", rawDest);
                    throw ex;
                }
            }
        }
#endregion
    }
}
