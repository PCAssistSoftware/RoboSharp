using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.IO;
using System.ComponentModel;

namespace RoboSharp
{
    /// <summary>
    /// RoboCopy Switches that determine which folders and files are selected for copying/moving
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/SelectionOptions"/>
    /// </remarks>
    public partial class SelectionOptions : ICloneable
    {
        #region Constructors 

        /// <summary>
        /// Create new SelectionOptions with Default Settings
        /// </summary>
        public SelectionOptions() { }

        /// <summary>
        /// Create new SelectionOptions using the provided <paramref name="selectionFlags"/>
        /// </summary>
        public SelectionOptions(SelectionFlags selectionFlags)
        {
            ApplySelectionFlags(selectionFlags);
        }

        /// <summary>
        /// Clone a SelectionOptions Object
        /// </summary>
        public SelectionOptions(SelectionOptions options)
        {
            OnlyCopyArchiveFiles = options.OnlyCopyArchiveFiles;
            OnlyCopyArchiveFilesAndResetArchiveFlag = options.OnlyCopyArchiveFilesAndResetArchiveFlag;
            IncludeAttributes = options.IncludeAttributes;
            ExcludeAttributes = options.ExcludeAttributes;
            ExcludedFiles.AddRange(options.ExcludedFiles);
            ExcludedDirectories.AddRange(options.ExcludedDirectories);
            ExcludeChanged = options.ExcludeChanged;
            ExcludeNewer = options.ExcludeNewer;
            ExcludeOlder = options.ExcludeOlder;
            ExcludeExtra = options.ExcludeExtra;
            ExcludeLonely = options.ExcludeLonely;
            IncludeSame = options.IncludeSame;
            IncludeTweaked = options.IncludeTweaked;
            IncludeModified = options.IncludeModified;
            MaxFileSize = options.MaxFileSize;
            MinFileSize = options.MinFileSize;
            MaxFileAge = options.MaxFileAge;
            MinFileAge = options.MinFileAge;
            MaxLastAccessDate = options.MaxLastAccessDate;
            MinLastAccessDate = options.MinLastAccessDate;
            ExcludeJunctionPoints = options.ExcludeJunctionPoints;
            UseFatFileTimes = options.UseFatFileTimes;
            CompensateForDstDifference = options.CompensateForDstDifference; ;
            ExcludeJunctionPointsForFiles = options.ExcludeJunctionPointsForFiles;

        }

        /// <summary>
        /// Clone this SelectionOptions Object
        /// </summary>
        public SelectionOptions Clone() => new SelectionOptions(this);

        object ICloneable.Clone() => Clone();

        #endregion

        #region Option Constants

        internal const string ONLY_COPY_ARCHIVE_FILES = "/A ";
        internal const string ONLY_COPY_ARCHIVE_FILES_AND_RESET_ARCHIVE_FLAG = "/M ";
        internal const string INCLUDE_ATTRIBUTES = "/IA:{0} ";
        internal const string EXCLUDE_ATTRIBUTES = "/XA:{0} ";
        internal const string EXCLUDE_FILES = "/XF {0} ";
        internal const string EXCLUDE_DIRECTORIES = "/XD {0} ";
        internal const string EXCLUDE_CHANGED = "/XC ";
        internal const string EXCLUDE_NEWER = "/XN ";
        internal const string EXCLUDE_OLDER = "/XO ";
        internal const string EXCLUDE_EXTRA = "/XX ";
        internal const string EXCLUDE_LONELY = "/XL ";
        internal const string INCLUDE_SAME = "/IS ";
        internal const string INCLUDE_TWEAKED = "/IT ";
        internal const string INCLUDE_MODIFIED = "/IM ";
        internal const string MAX_FILE_SIZE = "/MAX:{0} ";
        internal const string MIN_FILE_SIZE = "/MIN:{0} ";
        internal const string MAX_FILE_AGE = "/MAXAGE:{0} ";
        internal const string MIN_FILE_AGE = "/MINAGE:{0} ";
        internal const string MAX_LAST_ACCESS_DATE = "/MAXLAD:{0} ";
        internal const string MIN_LAST_ACCESS_DATE = "/MINLAD:{0} ";
        internal const string EXCLUDE_JUNCTION_POINTS = "/XJ ";
        internal const string USE_FAT_FILE_TIMES = "/FFT ";
        internal const string COMPENSATE_FOR_DST_DIFFERENCE = "/DST ";
        internal const string EXCLUDE_JUNCTION_POINTS_FOR_DIRECTORIES = "/XJD ";
        internal const string EXCLUDE_JUNCTION_POINTS_FOR_FILES = "/XJF ";

        #endregion Option Constants

        #region < ExcludedDirs and ExcludedFiles >

        /// <summary>
        /// This regex is used when the { <see cref="ExcludeFiles"/> } and { <see cref="ExcludeDirectories"/> } properties are set in order to split the input string to a List{string}
        /// </summary>
        /// <remarks>
        /// Regex Tester to use with <see cref="Regex.Matches(string)"/> to get all the matches from a string.
        /// </remarks>
        public static readonly Regex FileFolderNameRegexSplitter = GetFileFolderNameRegex();

#if NET7_0_OR_GREATER
        [GeneratedRegex("(?<VALUE>\".+?\"|[^\\s\\,\"\\|]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, 1000)]
        private static partial Regex GetFileFolderNameRegex();
#else
        private static Regex GetFileFolderNameRegex() => new Regex("(?<VALUE>\".+?\"|[^\\s\\,\"\\|]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));
#endif

        /// <summary>
        /// Use { <see cref="FileFolderNameRegexSplitter"/> } to split the <paramref name="inputString"/>, then add the matches to the suppplied <paramref name="list"/>.
        /// </summary>
        /// <param name="inputString">String to perform <see cref="Regex.Matches(string)"/> against</param>
        /// <param name="list">List to add regex matches to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ParseAndAddToList(string inputString, List<string> list)
        {
            MatchCollection collection = FileFolderNameRegexSplitter.Matches(inputString);
            if (collection.Count == 0) return;
            foreach (Match c in collection)
            {
                string s = c.Groups["VALUE"].Value;
                list.Add(s);
            }
        }

#endregion

        #region Public Properties

        /// <summary>
        /// Copies only files for which the Archive attribute is set.
        /// [/A]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool OnlyCopyArchiveFiles { get; set; }

        /// <summary>
        /// Copies only files for which the Archive attribute is set, and resets the Archive attribute.
        /// [/M]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool OnlyCopyArchiveFilesAndResetArchiveFlag { get; set; }

        /// <summary>
        /// This property should be set to a string consisting of all the attributes to include (eg. AH; RASHCNETO).
        /// Includes only files for which any of the specified attributes are set.
        /// [/IA:attributes]
        /// </summary>
        [DefaultValue("")]
        public virtual string IncludeAttributes
        {
            get => ConvertFileAttrToString(IncludedAttributesValue);
            set => SetIncludedAttributes(ConvertFileAttrStringToEnum(value));
        }
        private FileAttributes? IncludedAttributesValue { get; set; }

        /// <summary>
        /// This property should be set to a string consisting of all the attributes to exclude (eg. AH; RASHCNETO).
        /// Excludes files for which any of the specified attributes are set.
        /// [/XA:attributes]
        /// </summary>
        [DefaultValue("")]
        public virtual string ExcludeAttributes
        {
            get => ConvertFileAttrToString(ExcludedAttributesValue);
            set => SetExcludedAttributes(ConvertFileAttrStringToEnum(value));
        }
        private FileAttributes? ExcludedAttributesValue { get; set; }

        /// <summary>
        /// Files should be separated by spaces.
        /// Excludes files that match the specified names or paths. Note that FileName can include wildcard characters (* and ?).
        /// [/XF File File ...]
        /// </summary>
        /// <remarks>
        /// This property is now backed by the ExcludedFiles List{String} property. <br/>
        /// Get -> Ensures all strings in { <see cref="ExcludedFiles"/> } are wrapped in quotes if needed, and concats the items into a single string. <br/>
        /// Set -> Clears ExcludedFiles and splits this list using a regex to populate the list.
        /// </remarks>
        [Obsolete("This property is now backed by the ExcludedFiles List<String> property. \n Both Get/Set accessors still work similar to previous:\n" +
            "- 'Get' sanitizies then Joins all strings in the list into a single output string that is passed into RoboCopy.\n" +
            "- 'Set' clears the ExcludedFiles list, then splits the input string using regex to repopulate the list."
            )]
        public string ExcludeFiles
        {
            get
            {
                StringBuilder RetString = new StringBuilder();
                foreach (string s in ExcludedFiles)
                {
                    RetString.AppendWrappedPath(s).Append(' ');
                }
                return RetString.Trim().ToString();
            }
            set
            {
                ExcludedFiles.Clear();
                if (value.IsNullOrWhiteSpace()) return;
                ParseAndAddToList(value, ExcludedFiles);
            }
        }

        /// <summary>
        /// Allows you to supply a set of files to copy or use wildcard characters (* or ?). <br/>
        /// JobOptions file saves these into the /XF (Exclude Files) section
        /// </summary>
        /// <remarks>
        /// Example: *.txt will exclude any .txt files 
        /// </remarks>
        public List<string> ExcludedFiles { get; } = new List<string>();

        /// <summary>
        /// Directories should be separated by spaces.
        /// Excludes directories that match the specified names or paths.
        /// [/XD Directory Directory ...]
        /// </summary>
        /// <remarks>
        /// This property is now backed by the ExcludedDirectories List{String} property. <br/>
        /// Get -> Ensures all strings in { <see cref="ExcludedDirectories"/> } are wrapped in quotes if needed, and concats the items into a single string. <br/>
        /// Set -> Clears ExcludedDirs and splits this list using a regex to populate the list.
        /// </remarks>
        [Obsolete("This property is now backed by the ExcludedDirectories List<String> property. \n Both Get/Set accessors still work similar to previous:\n" +
            "- 'Get' sanitizies then Joins all strings in the list into a single output string that is passed into RoboCopy.\n" +
            "- 'Set' clears the ExcludedDirectories list, then splits the input string using regex to repopulate the list."
            )]
        public string ExcludeDirectories
        {
            get
            {
                StringBuilder RetString = new StringBuilder();
                foreach (string s in ExcludedDirectories)
                {
                    RetString.AppendWrappedPath(s).Append(' ');
                }
                return RetString.Trim().ToString();
            }
            set
            {
                ExcludedDirectories.Clear();
                if (value.IsNullOrWhiteSpace()) return;
                ParseAndAddToList(value, ExcludedDirectories);
            }
        }
        /// <summary>
        /// Allows you to supply a set of files to copy or use wildcard characters (* or ?). <br/>
        /// JobOptions file saves these into the /XD (Exclude Directories) section
        /// </summary>
        /// <remarks>
        /// Examples: 
        /// <br/> - 'Archive' -> Exclude a folder named 'Archive' from the root directory.  Source/Archive (excluded)   --- Source/SubFolder/Archive (not excluded)
        /// <br/> - 'C:\My\Archive' -> Exclude the fully qualified directory path.
        /// </remarks>
        public List<string> ExcludedDirectories { get; } = new List<string>();

        /// <summary>
        /// Excludes changed files.
        /// [/XC]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool ExcludeChanged { get; set; }

        /// <summary>
        /// Excludes newer files.
        /// [/XN]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool ExcludeNewer { get; set; }

        /// <summary>
        /// Excludes older files.
        /// [/XO]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool ExcludeOlder { get; set; }

        /// <summary>
        /// Prevent logging of extra files and directories (items that exist in the destination but not the source)
        /// [/XX]
        /// </summary>
        /// <remarks>
        /// When used the /PURGE or /MIRROR, this will prevent deletion of the extra files/directories. ( default robocopy behavior )
        /// </remarks>
        [DefaultValue(false)]
        public virtual bool ExcludeExtra { get; set; }

        /// <summary>
        /// Excludes lonely files and directories.
        /// [/XL]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool ExcludeLonely { get; set; }

        /// <summary>
        /// Includes the same files.
        /// [/IS]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool IncludeSame { get; set; }

        /// <summary>
        /// Includes tweaked files.
        /// [/IT]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool IncludeTweaked { get; set; }

        /// <summary>
        /// Includes modified files (differing change times).
        /// [/IM]
        /// </summary>
        [DefaultValue(false)]
        public virtual bool IncludeModified { get; set; }

        /// <summary>
        /// Zero indicates that this feature is turned off.
        /// Specifies the maximum file size (to exclude files bigger than N bytes).
        /// [/MAX:N]
        /// </summary>
        [DefaultValue((long)0)]
        public virtual long MaxFileSize { get; set; }

        /// <summary>
        /// Zero indicates that this feature is turned off.
        /// Specifies the minimum file size (to exclude files smaller than N bytes).
        /// [/MIN:N]
        /// </summary>
        [DefaultValue((long)0)]
        public virtual long MinFileSize { get; set; }

        /// <summary>
        /// Specifies the maximum file age (to exclude files older than N days or date).
        /// [/MAXAGE:N OR YYYYMMDD]
        /// </summary>
        [DefaultValue("")]
        public virtual string MaxFileAge { get; set; }

        /// <summary>
        /// Specifies the minimum file age (exclude files newer than N days or date).
        /// [/MINAGE:N OR YYYYMMDD]
        /// </summary>
        [DefaultValue("")]
        public virtual string MinFileAge { get; set; }

        /// <summary>
        /// Specifies the maximum last access date (excludes files unused since Date).
        /// [/MAXLAD:YYYYMMDD]
        /// </summary>
        [DefaultValue("")]
        public virtual string MaxLastAccessDate { get; set; }

        /// <summary>
        /// Specifies the minimum last access date (excludes files used since N) If N is less 
        /// than 1900, N specifies the number of days. Otherwise, N specifies a date 
        /// in the format YYYYMMDD.
        /// [/MINLAD:N or YYYYMMDD]
        /// </summary>
        [DefaultValue("")]
        public virtual string MinLastAccessDate { get; set; }

        /// <summary>
        /// Excludes junction points, which are normally included by default.
        /// [/XJ]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool ExcludeJunctionPoints { get; set; }
        /// <summary>
        /// Assumes FAT file times (two-second precision).
        /// [/FFT]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool UseFatFileTimes { get; set; }

        /// <summary>
        /// Compensates for one-hour DST time differences.
        /// [/DST]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool CompensateForDstDifference { get; set; }

        /// <summary>
        /// Excludes junction points for directories.
        /// [/XJD]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool ExcludeJunctionPointsForDirectories { get; set; }

        /// <summary>
        /// Excludes junction points for files.
        /// [/XJF]
        /// </summary>
        [DefaultValue(false)] 
        public virtual bool ExcludeJunctionPointsForFiles { get; set; }

        #endregion Public Properties

        private const FileAttributes AcceptedAttributes =
            FileAttributes.ReadOnly |
            FileAttributes.Archive |
            FileAttributes.System |
            FileAttributes.Hidden |
            FileAttributes.Compressed |
            FileAttributes.NotContentIndexed |
            FileAttributes.Encrypted |
            FileAttributes.Temporary |
            FileAttributes.Offline;

        /// <summary>Set the <see cref="IncludeAttributes"/> property </summary>
        /// <param name="AttributesToInclude"><inheritdoc cref="ConvertFileAttrToString(FileAttributes?)"/></param>
        public void SetIncludedAttributes(FileAttributes? AttributesToInclude)
        {
            if (AttributesToInclude is null)
                IncludedAttributesValue = AttributesToInclude;
            else
                IncludedAttributesValue = AttributesToInclude &= AcceptedAttributes;
        }

        /// <summary>Set the <see cref="ExcludeAttributes"/> property </summary>
        /// <param name="AttributesToExclude"><inheritdoc cref="ConvertFileAttrToString(FileAttributes?)"/></param>
        public void SetExcludedAttributes(FileAttributes? AttributesToExclude)
        {
            if (AttributesToExclude is null)
                ExcludedAttributesValue = AttributesToExclude;
            else
                ExcludedAttributesValue = AttributesToExclude &= AcceptedAttributes;
        }

        /// <summary> Get the FileAttributes enum representation of <see cref="SelectionOptions.IncludeAttributes"/></summary>
        /// <returns>If not specified, return null. Otherwise return the file attributes to include.</returns>
        public FileAttributes? GetIncludedAttributes() => this.IncludedAttributesValue;

        /// <summary> Get the FileAttributes enum representation of <see cref="SelectionOptions.ExcludeAttributes"/></summary>
        /// <returns>If not specified, return null. Otherwise return the file attributes to include.</returns>
        public FileAttributes? GetExcludedAttributes() => this.ExcludedAttributesValue;

        /// <summary>
        /// Converts a <see cref="FileAttributes"/> enum to its RASHCNETO string.
        /// </summary>
        /// <param name="attributes">
        /// Accepts: ReadOnly, Archive, System, Hidden, Compressed, NotContentIndexed, Encrypted, Temporary, Offline <br/>
        /// Ignores: All Other Attributes <br/>
        /// Pass in NULL value to return empty string.
        /// </param>
        /// <returns>RASHCNETO depending on submitted enum</returns>
        public static string ConvertFileAttrToString(FileAttributes? attributes)
        {
            if (attributes is null) return String.Empty;
            string s = "";
            var Attr = (FileAttributes)attributes;
            if (Attr.HasFlag(FileAttributes.ReadOnly)) s += "R";
            if (Attr.HasFlag(FileAttributes.Archive)) s += "A";
            if (Attr.HasFlag(FileAttributes.System)) s += "S";
            if (Attr.HasFlag(FileAttributes.Hidden)) s += "H";
            if (Attr.HasFlag(FileAttributes.Compressed)) s += "C";
            if (Attr.HasFlag(FileAttributes.NotContentIndexed)) s += "N";
            if (Attr.HasFlag(FileAttributes.Encrypted)) s += "E";
            if (Attr.HasFlag(FileAttributes.Temporary)) s += "T";
            if (Attr.HasFlag(FileAttributes.Offline)) s += "O";
            return s;
        }

        /// <summary>
        /// Converts a RASHCNETO string to its <see cref="FileAttributes"/> enum.
        /// </summary>
        /// <param name="attributes">
        /// Accepts: ReadOnly, Archive, System, Hidden, Compressed, NotContentIndexed, Encrypted, Temporary, Offline <br/>
        /// Ignores: All Other Attributes <br/>
        /// Pass in NULL value to return empty string.
        /// </param>
        /// <returns>If the string is parsable, returns the enum. Otherwise returns null.</returns>
        public static FileAttributes? ConvertFileAttrStringToEnum(string attributes)
        {
            if (string.IsNullOrWhiteSpace(attributes)) return null;
            attributes = new StringBuilder(attributes).RemoveWhiteSpace().ToUpper().ToString();
            if (!IsRASHCENTO(attributes)) throw new ArgumentException("Invalid RASHCNETO string!");
            FileAttributes? attr = null;
            if (attributes.Contains('R')) attr = attr.HasValue ? attr |= FileAttributes.ReadOnly : FileAttributes.ReadOnly;
            if (attributes.Contains('A')) attr = attr.HasValue ? attr |= FileAttributes.Archive : FileAttributes.Archive;
            if (attributes.Contains('S')) attr = attr.HasValue ? attr |= FileAttributes.System : FileAttributes.System;
            if (attributes.Contains('H')) attr = attr.HasValue ? attr |= FileAttributes.Hidden : FileAttributes.Hidden;
            if (attributes.Contains('C')) attr = attr.HasValue ? attr |= FileAttributes.Compressed : FileAttributes.Compressed;
            if (attributes.Contains('N')) attr = attr.HasValue ? attr |= FileAttributes.NotContentIndexed : FileAttributes.NotContentIndexed;
            if (attributes.Contains('E')) attr = attr.HasValue ? attr |= FileAttributes.Encrypted : FileAttributes.Encrypted;
            if (attributes.Contains('T')) attr = attr.HasValue ? attr |= FileAttributes.Temporary : FileAttributes.Temporary;
            if (attributes.Contains('O')) attr = attr.HasValue ? attr |= FileAttributes.Offline : FileAttributes.Offline;
            return attr;
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex("^[RASHCNETO]{0,9}$")]
        private static partial Regex RashcnetoRegex();
        private static bool IsRASHCENTO(string data) => RashcnetoRegex().IsMatch(data);
#else
        private static bool IsRASHCENTO(string data) => Regex.IsMatch(data, "^[RASHCNETO]{0,9}$");
#endif

        /// <summary>
        /// Adds the supplied <paramref name="fileExclusions"/> to the <see cref="ExcludedFiles"/> collection
        /// </summary>
        /// <param name="fileExclusions"><inheritdoc cref="ExcludedFiles" path="/remarks"/></param>
        public void AddFileExclusion(params string[] fileExclusions)
        {
            this.ExcludedFiles.AddRange(fileExclusions);
        }

        /// <summary>
        /// Adds the supplied <paramref name="directoryExclusions"/> to the <see cref="ExcludedDirectories"/> collection.
        /// </summary>
        /// <param name="directoryExclusions"><inheritdoc cref="ExcludedDirectories" path="/remarks"/></param>
        public void AddDirectoryExclusion(params string[] directoryExclusions)
        {
            this.ExcludedDirectories.AddRange(directoryExclusions);
        }

        /// <summary>
        /// Parse the class properties and generate the command arguments
        /// </summary>
        /// <param name="optionsOnly">When <see langword="true"/> only returns the options tags, similar to how robocopy would display them. (omits the source/destination)</param>
        public string Parse(bool optionsOnly = false)
        {
            var options = new StringBuilder();

            #region Set Options

            if (OnlyCopyArchiveFiles)
                options.Append(ONLY_COPY_ARCHIVE_FILES);
            if (OnlyCopyArchiveFilesAndResetArchiveFlag)
                options.Append(ONLY_COPY_ARCHIVE_FILES_AND_RESET_ARCHIVE_FLAG);
            if (!IncludeAttributes.IsNullOrWhiteSpace())
                options.Append(string.Format(INCLUDE_ATTRIBUTES, IncludeAttributes.CleanOptionInput()));
            if (!ExcludeAttributes.IsNullOrWhiteSpace())
                options.Append(string.Format(EXCLUDE_ATTRIBUTES, ExcludeAttributes.CleanOptionInput()));
#pragma warning disable CS0618 // Marked as Obsolete for consumers, but it originally functionality is still intact, so this still works properly.
            if (!optionsOnly)
            {
                if (!ExcludeFiles.IsNullOrWhiteSpace())
                    options.Append(string.Format(EXCLUDE_FILES, ExcludeFiles));
                if (!ExcludeDirectories.IsNullOrWhiteSpace())
                    options.Append(string.Format(EXCLUDE_DIRECTORIES, ExcludeDirectories));
            }
#pragma warning restore CS0618
            if (ExcludeChanged)
                options.Append(EXCLUDE_CHANGED);
            if (ExcludeNewer)
                options.Append(EXCLUDE_NEWER);
            if (ExcludeOlder)
                options.Append(EXCLUDE_OLDER);
            if (ExcludeExtra)
                options.Append(EXCLUDE_EXTRA);
            if (ExcludeLonely)
                options.Append(EXCLUDE_LONELY);
            if (IncludeSame)
                options.Append(INCLUDE_SAME);
            if (IncludeTweaked)
                options.Append(INCLUDE_TWEAKED);
            if (IncludeModified)
                options.Append(INCLUDE_MODIFIED);
            if (MaxFileSize > 0)
                options.Append(string.Format(MAX_FILE_SIZE, MaxFileSize));
            if (MinFileSize > 0)
                options.Append(string.Format(MIN_FILE_SIZE, MinFileSize));
            if (!MaxFileAge.IsNullOrWhiteSpace())
                options.Append(string.Format(MAX_FILE_AGE, MaxFileAge.CleanOptionInput()));
            if (!MinFileAge.IsNullOrWhiteSpace())
                options.Append(string.Format(MIN_FILE_AGE, MinFileAge.CleanOptionInput()));
            if (!MaxLastAccessDate.IsNullOrWhiteSpace())
                options.Append(string.Format(MAX_LAST_ACCESS_DATE, MaxLastAccessDate.CleanOptionInput()));
            if (!MinLastAccessDate.IsNullOrWhiteSpace())
                options.Append(string.Format(MIN_LAST_ACCESS_DATE, MinLastAccessDate.CleanOptionInput()));
            if (ExcludeJunctionPoints)
                options.Append(EXCLUDE_JUNCTION_POINTS);
            if (ExcludeJunctionPointsForDirectories)
                options.Append(EXCLUDE_JUNCTION_POINTS_FOR_DIRECTORIES);
            if (ExcludeJunctionPointsForFiles)
                options.Append(EXCLUDE_JUNCTION_POINTS_FOR_FILES);
            if (UseFatFileTimes)
                options.Append(USE_FAT_FILE_TIMES);
            if (CompensateForDstDifference)
                options.Append(COMPENSATE_FOR_DST_DIFFERENCE);

            #endregion Set Options

            return options.ToString();
        }

        /// <summary>
        /// Returns the Parsed Options as it would be applied to RoboCopy
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Parse();
        }

        /// <summary>
        /// Combine this object with another RetryOptions object. <br/>
        /// Any properties marked as true take priority. IEnumerable items are combined. <br/>
        /// String\Long Values will only be replaced if the primary object has a null/empty value for that property.
        /// </summary>
        /// <param name="options"></param>
        public void Merge(SelectionOptions options)
        {
            //File Attributes
            IncludeAttributes = IncludeAttributes.CombineCharArr(options.IncludeAttributes);
            ExcludeAttributes = ExcludeAttributes.CombineCharArr(options.ExcludeAttributes);

            //File Age
            MaxFileAge = MaxFileAge.ReplaceIfEmpty(options.MaxFileAge);
            MinFileAge = MaxFileAge.ReplaceIfEmpty(options.MinFileAge);
            MaxLastAccessDate = MaxFileAge.ReplaceIfEmpty(options.MaxLastAccessDate);
            MinLastAccessDate = MaxFileAge.ReplaceIfEmpty(options.MinLastAccessDate);

            //Long
            MaxFileSize |= options.MaxFileSize;
            MinFileSize |= options.MinFileSize;

            //Lists
            ExcludedFiles.AddRange(options.ExcludedFiles);
            ExcludedDirectories.AddRange(options.ExcludedDirectories);

            //Bools
            OnlyCopyArchiveFiles |= options.OnlyCopyArchiveFiles;
            OnlyCopyArchiveFilesAndResetArchiveFlag |= options.OnlyCopyArchiveFilesAndResetArchiveFlag;
            ExcludeChanged |= options.ExcludeChanged;
            ExcludeNewer |= options.ExcludeNewer;
            ExcludeOlder |= options.ExcludeOlder;
            ExcludeExtra |= options.ExcludeExtra;
            ExcludeLonely |= options.ExcludeLonely;
            IncludeSame |= options.IncludeSame;
            IncludeTweaked |= options.IncludeTweaked;
            IncludeModified |= options.IncludeModified;
            ExcludeJunctionPoints |= options.ExcludeJunctionPoints;
            ExcludeJunctionPointsForFiles |= options.ExcludeJunctionPointsForFiles;
            ExcludeJunctionPointsForDirectories |= options.ExcludeJunctionPointsForDirectories;

            UseFatFileTimes |= options.UseFatFileTimes;
            CompensateForDstDifference |= options.CompensateForDstDifference;

        }

        /// <summary>
        /// Apply the <see cref="SelectionFlags"/> to this command
        /// </summary>
        /// <param name="flags">Options to apply</param>
        public virtual void ApplySelectionFlags(SelectionFlags flags)
        {
            this.ExcludeChanged = flags.HasFlag(SelectionFlags.ExcludeChanged);
            this.ExcludeExtra = flags.HasFlag(SelectionFlags.ExcludeExtra);
            this.ExcludeJunctionPoints = flags.HasFlag(SelectionFlags.ExcludeJunctionPoints);
            this.ExcludeJunctionPointsForDirectories = flags.HasFlag(SelectionFlags.ExcludeJunctionPointsForDirectories);
            this.ExcludeJunctionPointsForFiles = flags.HasFlag(SelectionFlags.ExcludeJunctionPointsForFiles);
            this.ExcludeLonely = flags.HasFlag(SelectionFlags.ExcludeLonely);
            this.ExcludeNewer = flags.HasFlag(SelectionFlags.ExcludeNewer);
            this.ExcludeOlder = flags.HasFlag(SelectionFlags.ExcludeOlder);
            this.IncludeSame = flags.HasFlag(SelectionFlags.IncludeSame);
            this.IncludeTweaked = flags.HasFlag(SelectionFlags.IncludeTweaked);
            this.IncludeModified = flags.HasFlag(SelectionFlags.IncludeModified);
            this.OnlyCopyArchiveFiles = flags.HasFlag(SelectionFlags.OnlyCopyArchiveFiles);
            this.OnlyCopyArchiveFilesAndResetArchiveFlag = flags.HasFlag(SelectionFlags.OnlyCopyArchiveFilesAndResetArchiveFlag);
        }

        /// <summary>
        /// Translate the selection bools of this object to its <see cref="SelectionFlags"/> representation
        /// </summary>
        /// <returns>The <see cref="SelectionFlags"/> representation of this object.</returns>
        public SelectionFlags GetSelectionFlags()
        {
            var flags = SelectionFlags.Default;

            if (this.ExcludeChanged) flags |= SelectionFlags.ExcludeChanged;
            if (this.ExcludeExtra) flags |= SelectionFlags.ExcludeExtra;
            if (this.ExcludeJunctionPoints) flags |= SelectionFlags.ExcludeJunctionPoints;
            if (this.ExcludeJunctionPointsForDirectories) flags |= SelectionFlags.ExcludeJunctionPointsForDirectories;
            if (this.ExcludeJunctionPointsForFiles) flags |= SelectionFlags.ExcludeJunctionPointsForFiles;
            if (this.ExcludeLonely) flags |= SelectionFlags.ExcludeLonely;
            if (this.ExcludeNewer) flags |= SelectionFlags.ExcludeNewer;
            if (this.ExcludeOlder) flags |= SelectionFlags.ExcludeOlder;
            if (this.IncludeSame) flags |= SelectionFlags.IncludeSame;
            if (this.IncludeTweaked) flags |= SelectionFlags.IncludeTweaked;
            if (this.IncludeModified) flags |= SelectionFlags.IncludeModified;
            if (this.OnlyCopyArchiveFiles) flags |= SelectionFlags.OnlyCopyArchiveFiles;
            if (this.OnlyCopyArchiveFilesAndResetArchiveFlag) flags |= SelectionFlags.OnlyCopyArchiveFilesAndResetArchiveFlag;

            return flags;
        }
    }
}
