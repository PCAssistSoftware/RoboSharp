using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;

namespace RoboSharp
{
    /// <summary>
    /// Source, Destination, and options for how to move or copy files.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/tjscience/RoboSharp/wiki/CopyOptions"/>
    /// </remarks>
    public class CopyOptions : ICloneable
    {
        #region Constructors

        /// <summary>
        /// Create new CopyOptions with Default Settings
        /// </summary>
        public CopyOptions() 
        {
            this.Source = string.Empty;
            this.Destination = string.Empty;
            this.runHours = string.Empty;
        }

        /// <summary>
        /// Create a new CopyOptions object with the provided settings
        /// </summary>
        /// <param name="source"><inheritdoc cref="Source" path="*"/></param>
        /// <param name="destination"><inheritdoc cref="Destination" path="*"/></param>
        /// <param name="flags"><inheritdoc cref="CopyActionFlags" path="*"/></param>
        public CopyOptions(string source, string destination, CopyActionFlags flags) 
        {
            this.Source = source ?? string.Empty;
            this.Destination = destination ?? string.Empty;
            this.ApplyActionFlags(flags); 
        }

        /// <summary>
        /// Clone a CopyOptions Object
        /// </summary>
        /// <param name="copyOptions">CopyOptions object to clone</param>
        /// <param name="NewSource">Specify a new source if desired. If left as null, will use Source from <paramref name="copyOptions"/></param>
        /// <param name="NewDestination">Specify a new source if desired. If left as null, will use Destination from <paramref name="copyOptions"/></param>
        public CopyOptions(CopyOptions copyOptions, string NewSource = null, string NewDestination = null)
        {
            Source = NewSource ?? copyOptions.Source;
            Destination = NewDestination ?? copyOptions.Destination;

            AddAttributes = copyOptions.AddAttributes;
            CheckPerFile = copyOptions.CheckPerFile;
            CopyAll = copyOptions.CopyAll;
            CopyFilesWithSecurity = copyOptions.CopyFilesWithSecurity;
            CopyFlags = copyOptions.CopyFlags;
            CopySubdirectories = copyOptions.CopySubdirectories;
            CopySubdirectoriesIncludingEmpty = copyOptions.CopySubdirectoriesIncludingEmpty;
            CopySymbolicLink = copyOptions.CopySymbolicLink;
            CreateDirectoryAndFileTree = copyOptions.CreateDirectoryAndFileTree;
            Depth = copyOptions.Depth;
            DirectoryCopyFlags = copyOptions.DirectoryCopyFlags;
            DoNotCopyDirectoryInfo = copyOptions.DoNotCopyDirectoryInfo;
            DoNotUseWindowsCopyOffload = copyOptions.DoNotUseWindowsCopyOffload;
            EnableBackupMode = copyOptions.EnableBackupMode;
            EnableEfsRawMode = copyOptions.EnableEfsRawMode;
            EnableRestartMode = copyOptions.EnableRestartMode;
            EnableRestartModeWithBackupFallback = copyOptions.EnableRestartModeWithBackupFallback;
            FatFiles = copyOptions.FatFiles;
            FatFileTimes = copyOptions.FatFileTimes;
            FileFilter = copyOptions.FileFilter;
            FixFileSecurityOnAllFiles = copyOptions.FixFileSecurityOnAllFiles;
            FixFileTimesOnAllFiles = copyOptions.FixFileTimesOnAllFiles;
            InterPacketGap = copyOptions.InterPacketGap;
            Mirror = copyOptions.Mirror;
            MonitorSourceChangesLimit = copyOptions.MonitorSourceChangesLimit;
            MonitorSourceTimeLimit = copyOptions.MonitorSourceTimeLimit;
            MoveFiles = copyOptions.MoveFiles;
            MoveFilesAndDirectories = copyOptions.MoveFilesAndDirectories;
            MultiThreadedCopiesCount = copyOptions.MultiThreadedCopiesCount;
            Purge = copyOptions.Purge;
            RemoveAttributes = copyOptions.RemoveAttributes;
            RemoveFileInformation = copyOptions.RemoveFileInformation;
            RunHours = copyOptions.RunHours;
            TurnLongPathSupportOff = copyOptions.TurnLongPathSupportOff;
            UseUnbufferedIo = copyOptions.UseUnbufferedIo;
        }

        /// <inheritdoc cref="CopyOptions.CopyOptions(CopyOptions, string, string)"/>
        public CopyOptions Clone(string NewSource = null, string NewDestination = null) => new CopyOptions(this, NewSource, NewDestination);
        object ICloneable.Clone() => Clone();

        #endregion

        #region Option Constants

        internal const string COPY_SUBDIRECTORIES = "/S ";
        internal const string COPY_SUBDIRECTORIES_INCLUDING_EMPTY = "/E ";
        internal const string DEPTH = "/LEV:{0} ";
        internal const string ENABLE_RESTART_MODE = "/Z ";
        internal const string ENABLE_BACKUP_MODE = "/B ";
        internal const string ENABLE_RESTART_MODE_WITH_BACKUP_FALLBACK = "/ZB ";
        internal const string USE_UNBUFFERED_IO = "/J ";
        internal const string ENABLE_EFSRAW_MODE = "/EFSRAW ";
        internal const string COPY_FLAGS = "/COPY:{0} ";
        internal const string COPY_FILES_WITH_SECURITY = "/SEC ";
        internal const string COPY_ALL = "/COPYALL ";
        internal const string REMOVE_FILE_INFORMATION = "/NOCOPY ";
        internal const string FIX_FILE_SECURITY_ON_ALL_FILES = "/SECFIX ";
        internal const string FIX_FILE_TIMES_ON_ALL_FILES = "/TIMFIX ";
        internal const string PURGE = "/PURGE ";
        internal const string MIRROR = "/MIR ";
        internal const string MOVE_FILES = "/MOV ";
        internal const string MOVE_FILES_AND_DIRECTORIES = "/MOVE ";
        internal const string ADD_ATTRIBUTES = "/A+:{0} ";
        internal const string REMOVE_ATTRIBUTES = "/A-:{0} ";
        internal const string CREATE_DIRECTORY_AND_FILE_TREE = "/CREATE ";
        internal const string FAT_FILES = "/FAT ";
        internal const string FAT_FILE_TIMES = "/FFT ";
        internal const string TURN_LONG_PATH_SUPPORT_OFF = "/256 ";
        internal const string MONITOR_SOURCE_CHANGES_LIMIT = "/MON:{0} ";
        internal const string MONITOR_SOURCE_TIME_LIMIT = "/MOT:{0} ";
        internal const string RUN_HOURS = "/RH:{0} ";
        internal const string CHECK_PER_FILE = "/PF ";
        internal const string INTER_PACKET_GAP = "/IPG:{0} ";
        internal const string COPY_SYMBOLIC_LINK = "/SL ";
        internal const string MULTITHREADED_COPIES_COUNT = "/MT:{0} ";
        internal const string DIRECTORY_COPY_FLAGS = "/DCOPY:{0} ";
        internal const string DO_NOT_COPY_DIRECTORY_INFO = "/NODCOPY ";
        internal const string DO_NOT_USE_WINDOWS_COPY_OFFLOAD = "/NOOFFLOAD ";
        internal const string NETWORK_COMPRESSION = "/COMPRESS ";

        #endregion Option Constants

        #region Option Defaults

        /// <summary>
        /// The Default File Filter used that will allow copying of all files
        /// </summary>
        public const string DefaultFileFilter = "*.*" ;
        
        private IEnumerable<string> fileFilter = new[] { DefaultFileFilter };
        private string copyFlags = "DAT";
        private string directoryCopyFlags = !VersionManager.IsPlatformWindows ? "DA" : VersionManager.Version >= 6.2 ? "DA" : "T";

        #endregion Option Defaults

        #region Public Properties

        /// <summary>
        /// The source folder path where the RoboCommand is copying files from.
        /// </summary>
        public virtual string Source { get { return _source; } set { _source = value.CleanDirectoryPath(); } }
        private string _source;

        /// <summary> 
        /// The destination folder path where the RoboCommand is copying files to. 
        /// </summary>
        public virtual string Destination { get { return _destination; } set { _destination = value.CleanDirectoryPath(); } }
        private string _destination;

        /// <summary>
        /// Allows you to supply a set of files to copy or use wildcard characters (* or ?).
        /// <para/> * = wildcard, any number of characters.
        /// <br/> ? = wildcard, single character.
        /// </summary>
        /// <remarks>JobOptions file saves these into the /IF (Include Files) section</remarks>
        public IEnumerable<string> FileFilter
        {
            get
            {
                return fileFilter;
            }
            set
            {
                fileFilter = value;
            }
        }

        /// <summary>
        /// Copies subdirectories. Note that this option excludes empty directories.
        /// [/S]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool CopySubdirectories { get; set; }

        /// <summary>
        /// Copies subdirectories. Note that this option includes empty directories.
        /// [/E]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool CopySubdirectoriesIncludingEmpty { get; set; }

        /// <summary>
        /// Copies only the top N levels of the source directory tree. The default is
        /// zero which does not limit the depth.
        /// [/LEV:N]
        /// </summary>
        [DefaultValue(0)]
        public virtual int Depth { get; set; }

        /// <summary>
        /// Copies files in Restart mode.
        /// [/Z]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool EnableRestartMode { get; set; }

        /// <summary>
        /// Copies files in Backup mode.
        /// [/B]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool EnableBackupMode { get; set; }

        /// <summary>
        /// Uses Restart mode. If access is denied, this option uses Backup mode.
        /// [/ZB]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool EnableRestartModeWithBackupFallback { get; set; }

        /// <summary>
        /// Copy using unbuffered I/O (recommended for large files).
        /// [/J]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool UseUnbufferedIo { get; set; }

        /// <summary>
        /// Copies all encrypted files in EFS RAW mode.
        /// [/EFSRAW]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool EnableEfsRawMode { get; set; }

        /// <summary>
        /// Requests network compression during file transfer, if applicable.
        /// [/COMPRESS]
        /// </summary>
        /// <remarks>
        /// Compression became available in Windows 10 / Server2019 build 20206. Earlier than that and this flag will cause robocopy to report an Invalid Parameter.
        /// <br/>Due to that, this option has been safeguarded by the static <see cref="CanEnableCompression"/> property.</remarks>
        [DefaultValue(false)]
        public virtual bool Compress 
        { 
            get => CanEnableCompression && compress;
            set => compress = value;
        }
        private bool compress;

        /// <summary>
        /// Value indicating if the current system supports robocopy using the <see cref="Compress"/> function. 
        /// </summary>
        /// <remarks>Value can be forced via <see cref="SetCanEnableCompression"/>, or tested via <see cref="TestCompressionFlag"/> </remarks>
        public static bool CanEnableCompression => canEnableCompression;
        private static bool canEnableCompression;

        /// <summary>Update the value of <see cref="CanEnableCompression"/></summary>
        /// <param name="value"><see langword="true"/> if you wish to permit using the /COMPRESS flag. <see langword="false"/> if you wish to prevent usage of the flag.</param>
        public static void SetCanEnableCompression(bool value) => canEnableCompression = value;

        /// <summary>
        /// Run a RoboCommand that has the /COMPRESS /ListOnly /QUIT options enabled as to test the ability to enable the /COMPRESS flag in other commands. 
        /// <br/>No items will be moved or copied as part of this test.
        /// </summary>
        /// <param name="source">The source supplied to the test command</param>
        /// <param name="dest">The destination supplied to the test command</param>
        /// <param name="updateCanEnableCompression">When <see langword="true"/>, updates <see cref="CanEnableCompression"/> with the result.</param>
        /// <param name="configuration">The configuration to use for the test. If not specified, uses the default configuration.</param>
        /// <returns>A task that returns <see langword="true"/> if the command supported compression, otherwise false.</returns>
        /// <inheritdoc cref="Authentication.Authenticate(string, string, string, Interfaces.IRoboCommand, Authentication.AuthenticationDelegate)"/>
        /// <param name="domain"/><param name="username"/><param name="password"/>
        public static async Task<bool> TestCompressionFlag(
            string source = @"C:\", 
            string dest = @"C:\", 
            bool updateCanEnableCompression = true, 
            RoboSharpConfiguration configuration = null,
            string domain = "",
            string username = "",
            string password = ""
            )
        {
            bool result = false;
            RoboCommand cmd = new RoboCommand("TestCompressionFlag", configuration: configuration)
            {
                CopyOptions = new CompressionTestSettings()
                {
                    Source = source,
                    Destination = dest,
                    FileFilter = new string[] { "*.ABCDEF" },
                    Depth = 1
                },
                LoggingOptions = new LoggingOptions(LoggingFlags.ListOnly),
                JobOptions = new JobOptions() { PreventCopyOperation = true }
            };
            if (!cmd.CopyOptions.Parse().Contains(NETWORK_COMPRESSION)) throw new InvalidOperationException("Compression Test failed to enable" + NETWORK_COMPRESSION);
            var results = await cmd.StartAsync(domain, username, password);
            result = !results.RoboCopyErrors.Any(n => n.ErrorDescription.Contains("Invalid Parameter"));
            if (updateCanEnableCompression) SetCanEnableCompression(result);
            return result;
        }

        /// <summary>
        /// This property should be set to a string consisting of all the flags to include (eg. DAT; DATSOU)
        /// Specifies the file properties to be copied. The following are the valid values for this option:
        ///D Data
        ///A Attributes
        ///T Time stamps
        ///S NTFS access control list (ACL)
        ///O Owner information
        ///U Auditing information
        ///The default value for copyflags is DAT (data, attributes, and time stamps).
        ///[/COPY:copyflags]
        /// </summary>
        [DefaultValue("DAT")]
        public string CopyFlags
        {
            get
            {
                return copyFlags;
            }
            set => copyFlags = value;
        }

        /// <summary>
        /// Copies files with security (equivalent to /copy:DAT).
        /// [/SEC]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool CopyFilesWithSecurity { get; set; }

        /// <summary>
        /// Copies all file information (equivalent to /copy:DATSOU).
        /// [/COPYALL]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool CopyAll { get; set; }

        /// <summary>
        /// Copies no file information (useful with Purge option).
        /// [/NOCOPY]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool RemoveFileInformation { get; set; }

        /// <summary>
        /// Fixes file security on all files, even skipped ones.
        /// [/SECFIX]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool FixFileSecurityOnAllFiles { get; set; }

        /// <summary>
        /// Fixes file times on all files, even skipped ones.
        /// [/TIMFIX]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool FixFileTimesOnAllFiles { get; set; }

        /// <summary>
        /// Deletes destination files and directories that no longer exist in the source.
        /// [/PURGE]
        /// </summary>
        /// <remarks>
        /// Using this option with the <see cref="CopySubdirectoriesIncludingEmpty"/> option allows the destination directory security settings to not be overwritten.
        /// </remarks>
        [DefaultValue(false)]
        public virtual bool Purge { get; set; }

        /// <summary>
        /// Mirrors a directory tree (equivalent to CopySubdirectoriesIncludingEmpty plus Purge).
        /// [/MIR]
        /// </summary>
        /// <remarks>Using this option with the <see cref="CopySubdirectoriesIncludingEmpty"/> overwrites the destination directory security settings.</remarks>
        [DefaultValue(false)]
        public virtual bool Mirror { get; set; }

        /// <summary>
        /// Moves files, and deletes them from the source after they are copied.
        /// [/MOV]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool MoveFiles { get; set; }

        /// <summary>
        /// Moves files and directories, and deletes them from the source after they are copied.
        /// [/MOVE]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool MoveFilesAndDirectories { get; set; }

        /// <summary>
        /// This property should be set to a string consisting of all the attributes to add (eg. AH; RASHCNET).
        /// Adds the specified attributes to copied files.
        /// [/A+:attributes]
        /// </summary>
        [DefaultValue("")]
        public string AddAttributes
        {
            get => SelectionOptions.ConvertFileAttrToString(AddAttributesValue);
            set => SetAddAttributes(SelectionOptions.ConvertFileAttrStringToEnum(value));
        }
        private FileAttributes? AddAttributesValue { get; set; }

        /// <summary>
        /// This property should be set to a string consisting of all the attributes to remove (eg. AH; RASHCNET).
        /// Removes the specified attributes from copied files.
        /// [/A-:attributes]
        /// </summary>
        [DefaultValue("")]
        public string RemoveAttributes
        {
            get => SelectionOptions.ConvertFileAttrToString(RemoveAttributesValue);
            set => SetRemoveAttributes(SelectionOptions.ConvertFileAttrStringToEnum(value));
        }
        private FileAttributes? RemoveAttributesValue { get; set; }

        /// <summary>
        /// Creates a directory tree and zero-length files only.
        /// [/CREATE]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool CreateDirectoryAndFileTree { get; set; }

        /// <summary>
        /// Creates destination files by using 8.3 character-length FAT file names only.
        /// [/FAT]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool FatFiles { get; set; }

        /// <summary>
        /// Assumes FAT file times (two-second precision).
        /// [/FFT]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool FatFileTimes { get; set; }

        /// <summary>
        /// Turns off support for very long paths (longer than 256 characters).
        /// [/256]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool TurnLongPathSupportOff { get; set; }

        /// <summary>
        /// The default value of zero indicates that you do not wish to monitor for changes.
        /// Monitors the source, and runs again when more than N changes are detected.
        /// [/MON:N]
        /// </summary>
        [DefaultValue(0)]
        public virtual int MonitorSourceChangesLimit { get; set; }

        /// <summary>
        /// The default value of zero indicates that you do not wish to monitor for changes.
        /// Monitors source, and runs again in M minutes if changes are detected.
        /// [/MOT:M]
        /// </summary>
        [DefaultValue(0)]
        public virtual int MonitorSourceTimeLimit { get; set; }

        /// <summary>
        /// Specifies run times when new copies may be started. ( Copy Operation is scheduled to only operate within specified timeframe )
        /// [/rh:hhmm-hhmm] <br/>
        /// If copy operation is unfinished, robocopy will remain active in idle state until the specified time, at which it will resume copying.<br/>
        /// Must be in correct format. Incorrectly formatted strings will be ignored.  <para/>
        /// Examples:<br/>
        /// 1500-1800 -> Robocopy will only copy between 3 PM and 5 PM <br/>
        /// 0015-0530 -> Robocopy will only copy between 12:15 AM and 5:30 AM <br/>
        /// </summary>
        /// <remarks>
        /// If this is set up, then the robocopy process will remain active after the program exits if the calling asemmbly does not call <see cref="RoboCommand.Stop()"/> prior to exiting the application.
        /// </remarks>
        [DefaultValue("")]
        public string RunHours
        {
            get => runHours;
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    runHours = value?.Trim() ?? string.Empty;
                else if (CheckRunHoursString(value))
                    runHours = value.Trim();
            }
        }
        private string runHours;

        /// <summary>
        /// Checks the scheduled /RH (run hours) per file instead of per pass.
        /// [/PF]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool CheckPerFile { get; set; }

        /// <summary>
        /// The default value of zero indicates that this feature is turned off.
        /// Specifies the inter-packet gap to free bandwidth on slow lines.
        /// [/IPG:N]
        /// </summary>
        [DefaultValue(0)] 
        public virtual int InterPacketGap { get; set; }

        /// <summary>
        /// Copies the symbolic link instead of the target.
        /// [/SL]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool CopySymbolicLink { get; set; }

        /// <summary>
        /// The default value of zero indicates that this feature is turned off.
        /// Creates multi-threaded copies with N threads. Must be an integer between 1 and 128.
        /// The MultiThreadedCopiesCount parameter cannot be used with the /IPG and EnableEfsRawMode parameters.
        /// [/MT:N]
        /// </summary>
        /// <remarks>
        /// Settings this value to anything other than 0 causes RoboCopy to force the following options: 
        /// <br/> - <see cref="LoggingOptions.NoDirectoryList"/>
        /// <br/> - <see cref="LoggingOptions.IncludeFullPathNames"/>
        /// </remarks>
        [DefaultValue(0)]
        public virtual int MultiThreadedCopiesCount { 
            get => MultiThreadedCopiesCountField; 
            set {
                if (value >= 0 && value <= 128)
                    MultiThreadedCopiesCountField = value;
                else
                    throw new ArgumentOutOfRangeException(message: "Value must be a value between 0-128", null);
            } 
        }
        private int MultiThreadedCopiesCountField = 0;

        /// <summary>
        /// What to copy for directories (default is DA).
        /// (copyflags: D=Data, A=Attributes, T=Timestamps).
        /// [/DCOPY:copyflags]
        /// </summary>
        [DefaultValue("DA")]
        public string DirectoryCopyFlags
        {
            get { return directoryCopyFlags; }
            set { directoryCopyFlags = value; }
        }
        /// <summary>
        /// Do not copy any directory info.
        /// [/NODCOPY]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool DoNotCopyDirectoryInfo { get; set; }
        /// <summary>
        /// Copy files without using the Windows Copy Offload mechanism.
        /// [/NOOFFLOAD]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool DoNotUseWindowsCopyOffload { get; set; }

        #endregion Public Properties

        #region < Parse (INTERNAL) >

        /// <summary>
        /// Used by the Parse method to sanitize path for the command options.<br/>
        /// Evaluate the path. If needed, wrap it in quotes.  <br/>
        /// If the path ends in a DirectorySeperatorChar, santize it to work as expected. <br/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Each return string includes a space at the end of the string to seperate it from the next option variable.</returns>
        private static string WrapPath(string path)
        {
            if (!path.Contains(" ")) return $"{path} "; //No spaces, just return the path
            //Below this line, the path contains a space, so it must be wrapped in quotes.
            if (path.EndsWithDirectorySeperator()) return $"\"{path}.\" "; // Ends with a directory seperator - Requires a '.' to denote using that directory. ex: "F:\."
            return $"\"{path}\" ";
        }

        /// <summary>
        /// Parse the class properties and generate the command arguments
        /// </summary>
        /// <param name="optionsOnly">When <see langword="true"/> only returns the options tags, similar to how robocopy would display them. (omits the source/destination)</param>
        public string Parse(bool optionsOnly = false)
        {
            Debugger.Instance.DebugMessage("Parsing CopyOptions...");
            bool isNotWindows = !VersionManager.IsPlatformWindows;
            var version = VersionManager.Version;
            var options = new StringBuilder();

            if (!optionsOnly)
            {
                // Set Source and Destination
                options.Append(WrapPath(Source));
                options.Append(WrapPath(Destination));
            }
            
            // Set FileFilter
            // Quote each FileFilter item. The quotes are trimmed first to ensure that they are applied only once.
            var fileFilterQuotedItems = FileFilter.Select(word => "\"" + word.Trim('"') + "\"");
            string fileFilter = String.Join(" ", fileFilterQuotedItems);
            options.Append($"{fileFilter} ");
            Debugger.Instance.DebugMessage(string.Format("Parsing CopyOptions progress ({0}).", options.ToString()));

            #region Set Options
            var cleanedCopyFlags = CopyFlags.CleanOptionInput();
            var cleanedDirectoryCopyFlags = DirectoryCopyFlags.CleanOptionInput();

            if (!cleanedCopyFlags.IsNullOrWhiteSpace())
            {
                options.Append(string.Format(COPY_FLAGS, cleanedCopyFlags));
                Debugger.Instance.DebugMessage(string.Format("Parsing CopyOptions progress ({0}).", options.ToString()));
            }
            if (!cleanedDirectoryCopyFlags.IsNullOrWhiteSpace() && (isNotWindows | version >= 5.1260026))
            {
                options.Append(string.Format(DIRECTORY_COPY_FLAGS, cleanedDirectoryCopyFlags));
                Debugger.Instance.DebugMessage(string.Format("Parsing CopyOptions progress ({0}).", options.ToString()));
            }
            if (CopySubdirectories)
            {
                options.Append(COPY_SUBDIRECTORIES);
                Debugger.Instance.DebugMessage(string.Format("Parsing CopyOptions progress ({0}).", options.ToString()));
            }
            if (CopySubdirectoriesIncludingEmpty)
                options.Append(COPY_SUBDIRECTORIES_INCLUDING_EMPTY);
            if (Depth > 0)
                options.Append(string.Format(DEPTH, Depth));
            if (EnableRestartMode)
                options.Append(ENABLE_RESTART_MODE);
            if (EnableBackupMode)
                options.Append(ENABLE_BACKUP_MODE);
            if (EnableRestartModeWithBackupFallback)
                options.Append(ENABLE_RESTART_MODE_WITH_BACKUP_FALLBACK);
            if (UseUnbufferedIo && (isNotWindows | version >= 6.2))
                options.Append(USE_UNBUFFERED_IO);
            if (EnableEfsRawMode)
                options.Append(ENABLE_EFSRAW_MODE);
            if (CopyFilesWithSecurity)
                options.Append(COPY_FILES_WITH_SECURITY);
            if (CopyAll)
                options.Append(COPY_ALL);
            if (RemoveFileInformation)
                options.Append(REMOVE_FILE_INFORMATION);
            if (FixFileSecurityOnAllFiles)
                options.Append(FIX_FILE_SECURITY_ON_ALL_FILES);
            if (FixFileTimesOnAllFiles)
                options.Append(FIX_FILE_TIMES_ON_ALL_FILES);
            if (Purge)
                options.Append(PURGE);
            if (Mirror)
                options.Append(MIRROR);
            if (MoveFiles)
                options.Append(MOVE_FILES);
            if (MoveFilesAndDirectories)
                options.Append(MOVE_FILES_AND_DIRECTORIES);
            if (!AddAttributes.IsNullOrWhiteSpace())
                options.Append(string.Format(ADD_ATTRIBUTES, AddAttributes.CleanOptionInput()));
            if (!RemoveAttributes.IsNullOrWhiteSpace())
                options.Append(string.Format(REMOVE_ATTRIBUTES, RemoveAttributes.CleanOptionInput()));
            if (CreateDirectoryAndFileTree)
                options.Append(CREATE_DIRECTORY_AND_FILE_TREE);
            if (FatFiles)
                options.Append(FAT_FILES);
            if (FatFileTimes)
                options.Append(FAT_FILE_TIMES);
            if (TurnLongPathSupportOff)
                options.Append(TURN_LONG_PATH_SUPPORT_OFF);
            if (MonitorSourceChangesLimit > 0)
                options.Append(string.Format(MONITOR_SOURCE_CHANGES_LIMIT, MonitorSourceChangesLimit));
            if (MonitorSourceTimeLimit > 0)
                options.Append(string.Format(MONITOR_SOURCE_TIME_LIMIT, MonitorSourceTimeLimit));
            if (!RunHours.IsNullOrWhiteSpace())
                options.Append(string.Format(RUN_HOURS, RunHours.CleanOptionInput()));
            if (CheckPerFile)
                options.Append(CHECK_PER_FILE);
            if (InterPacketGap > 0)
                options.Append(string.Format(INTER_PACKET_GAP, InterPacketGap));
            if (CopySymbolicLink)
                options.Append(COPY_SYMBOLIC_LINK);
            if (MultiThreadedCopiesCount > 0)
                options.Append(string.Format(MULTITHREADED_COPIES_COUNT, MultiThreadedCopiesCount));
            if (DoNotCopyDirectoryInfo && (isNotWindows | version >= 6.2))
                options.Append(DO_NOT_COPY_DIRECTORY_INFO);
            if (DoNotUseWindowsCopyOffload && (isNotWindows | version >= 6.2))
                options.Append(DO_NOT_USE_WINDOWS_COPY_OFFLOAD);
            if (Compress)
                options.Append(NETWORK_COMPRESSION);
            #endregion Set Options

            var parsedOptions = options.ToString();
            Debugger.Instance.DebugMessage(string.Format("CopyOptions parsed ({0}).", parsedOptions));
            return parsedOptions;
        }

        /// <summary>
        /// Returns the Parsed Options as it would be applied to RoboCopy
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Parse();
        }

        #endregion

        #region < RunHours (Public) >

        private static readonly Regex RunHours_OverallRegex = new Regex("^(?<StartTime>[0-2][0-9][0-5][0-9])-(?<EndTime>[0-2][0-9][0-5][0-9])$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex RunHours_Check1 = new Regex("^[0-1][0-9][0-5][0-9]$", RegexOptions.Compiled);  // Checks 0000 - 1959
        private static readonly Regex RunHours_Check2 = new Regex("^[2][0-3][0-5][0-9]$", RegexOptions.Compiled);    // Checks 2000 - 2359
        private GroupCollection RunHoursGroups => RunHours_OverallRegex.Match(RunHours).Groups;

        /// <summary>
        /// Get the StartTime portion of <see cref="RunHours"/>
        /// </summary>
        /// <returns>hhmm or String.Empty</returns>
        public string GetRunHours_StartTime()
        {
            if (RunHours.IsNullOrWhiteSpace()) return string.Empty;
            return RunHoursGroups["StartTime"]?.Value ?? String.Empty;
        }

        /// <summary>
        /// Get the EndTime portion of <see cref="RunHours"/>
        /// </summary>
        /// <returns>hhmm or String.Empty</returns>
        public string GetRunHours_EndTime()
        {
            if (RunHours.IsNullOrWhiteSpace()) return string.Empty;
            return RunHoursGroups["EndTime"]?.Value ?? String.Empty;
        }

        /// <summary>
        /// Method to check if some string is valid for use as with the <see cref="RunHours"/> property.
        /// </summary>
        /// <param name="runHours"></param>
        /// <returns>True if correct format, otherwise false</returns>
        public static bool IsRunHoursStringValid(string runHours)
        {
            if (string.IsNullOrWhiteSpace(runHours)) return true;
            if (!RunHours_OverallRegex.IsMatch(runHours.Trim())) return false;
            var times = RunHours_OverallRegex.Match(runHours.Trim());
            bool StartMatch = RunHours_Check1.IsMatch(times.Groups["StartTime"].Value) || RunHours_Check2.IsMatch(times.Groups["StartTime"].Value);
            bool EndMatch = RunHours_Check1.IsMatch(times.Groups["EndTime"].Value) || RunHours_Check2.IsMatch(times.Groups["EndTime"].Value);
            return StartMatch && EndMatch;
        }

        /// <inheritdoc cref="IsRunHoursStringValid(string)"/>
        public bool CheckRunHoursString(string runHours) => IsRunHoursStringValid(runHours);

        #endregion

        /// <summary>
        /// Add file filters to the <see cref="FileFilter"/> property.
        /// <para/> Note: If the FileFilter contains only <see cref="DefaultFileFilter"/>, it will be replaced with the new collection.
        /// <br/>Otherwise, the <paramref name="filters"/> will be added to the collection.
        /// </summary>
        /// <param name="filters"><inheritdoc cref="FileFilter" path="/summary"/></param>
        public void AddFileFilter(params string[] filters)
        {
            if (filters.Length == 0) return;
            if (FileFilter is null | !FileFilter.Any())
                FileFilter = filters;
            else
            {
                var cur = FileFilter.ToArray();
                if (cur.Length == 1 && cur[0] == DefaultFileFilter)
                    FileFilter = filters;
                else
                    FileFilter = cur.Concat(filters);
            }
        }

        #region < Flags >

        /// <summary>
        /// Apply the <see cref="CopyActionFlags"/> to the command
        /// </summary>
        /// <param name="flags">Options to apply</param>
        public virtual void ApplyActionFlags(CopyActionFlags flags)
        {
            this.CopySubdirectories = flags.HasFlag(CopyActionFlags.CopySubdirectories);
            this.CopySubdirectoriesIncludingEmpty = flags.HasFlag(CopyActionFlags.CopySubdirectoriesIncludingEmpty);
            this.Purge = flags.HasFlag(CopyActionFlags.Purge);
            this.Mirror = flags.HasFlag(CopyActionFlags.Mirror);
            this.MoveFiles = flags.HasFlag(CopyActionFlags.MoveFiles);
            this.MoveFilesAndDirectories = flags.HasFlag(CopyActionFlags.MoveFilesAndDirectories);
            this.CreateDirectoryAndFileTree = flags.HasFlag(CopyActionFlags.CreateDirectoryAndFileTree);
            this.Compress = flags.HasFlag(CopyActionFlags.Compress);
        }

        /// <summary>
        /// Get the <see cref="CopyActionFlags"/> representation of this object
        /// </summary>
        public virtual CopyActionFlags GetCopyActionFlags()
        {
            var flags = CopyActionFlags.Default;
            if (this.CopySubdirectories) flags |=CopyActionFlags.CopySubdirectories;
            if (this.CopySubdirectoriesIncludingEmpty) flags |=CopyActionFlags.CopySubdirectoriesIncludingEmpty;
            if (this.Purge) flags |=CopyActionFlags.Purge;
            if (this.Mirror) flags |=CopyActionFlags.Mirror;
            if (this.MoveFiles) flags |=CopyActionFlags.MoveFiles;
            if (this.MoveFilesAndDirectories) flags |=CopyActionFlags.MoveFilesAndDirectories;
            if (this.CreateDirectoryAndFileTree) flags |= CopyActionFlags.CreateDirectoryAndFileTree;
            if (this.Compress) flags |= CopyActionFlags.Compress;
            return flags;
        }

        #endregion

        #region < Other Public Methods >

        private const FileAttributes AcceptedAttributes =
            FileAttributes.ReadOnly |
            FileAttributes.Archive |
            FileAttributes.System |
            FileAttributes.Hidden |
            FileAttributes.Compressed |
            FileAttributes.NotContentIndexed |
            FileAttributes.Encrypted |
            FileAttributes.Temporary;

        /// <summary>Set the <see cref="AddAttributes"/> property </summary>
        /// <param name="AttributesToAdd"><inheritdoc cref="SelectionOptions.ConvertFileAttrToString(FileAttributes?)"/></param>
        public void SetAddAttributes(FileAttributes? AttributesToAdd)
        {
            if (AttributesToAdd is null)
                AddAttributesValue = AttributesToAdd;
            else
                AddAttributesValue = AttributesToAdd &= AcceptedAttributes;
        }

        /// <summary>Set the <see cref="RemoveAttributes"/> property </summary>
        /// <param name="AttributesToRemove"><inheritdoc cref="SelectionOptions.ConvertFileAttrToString(FileAttributes?)"/></param>
        public void SetRemoveAttributes(FileAttributes? AttributesToRemove)
        {
            if (AttributesToRemove is null)
                RemoveAttributesValue = AttributesToRemove;
            else
                RemoveAttributesValue = AttributesToRemove &= AcceptedAttributes;
        }

        /// <summary> Get the FileAttributes enum representation of <see cref="SelectionOptions.IncludeAttributes"/></summary>
        /// <returns>If not specified, return null. Otherwise return the file attributes to include.</returns>
        public FileAttributes? GetAddAttributes() => this.AddAttributesValue;

        /// <summary> Get the FileAttributes enum representation of <see cref="SelectionOptions.ExcludeAttributes"/></summary>
        /// <returns>If not specified, return null. Otherwise return the file attributes to include.</returns>
        public FileAttributes? GetRemoveAttributes() => this.RemoveAttributesValue;

        /// <summary>
        /// Combine this object with another CopyOptions object. <br/>
        /// Any properties marked as true take priority. IEnumerable items are combined. 
        /// </summary>
        /// <remarks>
        /// Source and Destination are only taken from the merged item if this object's Source/Destination values are null/empty. <br/>
        /// RunHours follows the same rules.
        /// 
        /// </remarks>
        /// <param name="copyOptions"></param>
        public void Merge(CopyOptions copyOptions)
        {
            Source = Source.ReplaceIfEmpty(copyOptions.Source);
            Destination = Destination.ReplaceIfEmpty(copyOptions.Destination);
            RunHours = RunHours.ReplaceIfEmpty(copyOptions.RunHours);

            //int -> Take Greater Value
            Depth = Depth.GetGreaterVal(copyOptions.Depth);
            InterPacketGap = InterPacketGap.GetGreaterVal(copyOptions.InterPacketGap);
            MonitorSourceChangesLimit = MonitorSourceChangesLimit.GetGreaterVal(copyOptions.MonitorSourceChangesLimit);
            MonitorSourceTimeLimit = MonitorSourceTimeLimit.GetGreaterVal(copyOptions.MonitorSourceTimeLimit);
            MultiThreadedCopiesCount = MultiThreadedCopiesCount.GetGreaterVal(copyOptions.MultiThreadedCopiesCount);

            //Flags
            AddAttributes = AddAttributes.CombineCharArr(copyOptions.AddAttributes);
            CopyFlags = CopyFlags.CombineCharArr(copyOptions.CopyFlags);
            DirectoryCopyFlags = DirectoryCopyFlags.CombineCharArr(copyOptions.DirectoryCopyFlags);
            RemoveAttributes = RemoveAttributes.CombineCharArr(copyOptions.RemoveAttributes);

            //IEnumerable
            var list = new List<String>(FileFilter);
            list.AddRange(copyOptions.FileFilter);
            FileFilter = list;

            //Bool
            CheckPerFile |= copyOptions.CheckPerFile;
            Compress |= copyOptions.Compress;
            CopyAll |= copyOptions.CopyAll;
            CopyFilesWithSecurity |= copyOptions.CopyFilesWithSecurity;
            CopySubdirectories |= copyOptions.CopySubdirectories;
            CopySubdirectoriesIncludingEmpty |= copyOptions.CopySubdirectoriesIncludingEmpty;
            CopySymbolicLink |= copyOptions.CopySymbolicLink;
            CreateDirectoryAndFileTree |= copyOptions.CreateDirectoryAndFileTree;
            DoNotCopyDirectoryInfo |= copyOptions.DoNotCopyDirectoryInfo;
            DoNotUseWindowsCopyOffload |= copyOptions.DoNotUseWindowsCopyOffload;
            EnableBackupMode |= copyOptions.EnableBackupMode;
            EnableEfsRawMode |= copyOptions.EnableEfsRawMode;
            EnableRestartMode |= copyOptions.EnableRestartMode;
            EnableRestartModeWithBackupFallback |= copyOptions.EnableRestartModeWithBackupFallback;
            FatFiles |= copyOptions.FatFiles;
            FatFileTimes |= copyOptions.FatFileTimes;
            FixFileSecurityOnAllFiles |= copyOptions.FixFileSecurityOnAllFiles;
            FixFileTimesOnAllFiles |= copyOptions.FixFileTimesOnAllFiles;
            Mirror |= copyOptions.Mirror;
            MoveFiles |= copyOptions.MoveFiles;
            MoveFilesAndDirectories |= copyOptions.MoveFilesAndDirectories;
            Purge |= copyOptions.Purge;
            RemoveFileInformation |= copyOptions.RemoveFileInformation;
            TurnLongPathSupportOff |= copyOptions.TurnLongPathSupportOff;
            UseUnbufferedIo |= copyOptions.UseUnbufferedIo;
        }

        #endregion

        private class CompressionTestSettings : CopyOptions
        {
            public override bool Compress { get => true; set { } }
        }
    }
}
