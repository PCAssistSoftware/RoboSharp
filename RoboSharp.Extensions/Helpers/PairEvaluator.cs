using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RoboSharp.Extensions.Comparers;
using RoboSharp.Extensions.Options;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// Class that can be instantiated to cache the various values that get checked against when deciding to copy a file or folder.
    /// <br/> Custom Implementations can instantiate this class and use it to assist with evaluating if they need to copy a file, generate the ProcessedFileInfo objects, etc.
    /// </summary>
    public class PairEvaluator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public PairEvaluator(IRoboCommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            FileSorter = new FilePairSorter<IProcessedFilePair>(command.Configuration);
            FileAttributesToApplyField = new Lazy<FileAttributes?>(Command.CopyOptions.GetAddAttributes);
            FileAttributesToRemoveField = new Lazy<FileAttributes?>(Command.CopyOptions.GetRemoveAttributes);
            FileFilterRegexField = new Lazy<Regex[]>(Command.CopyOptions.GetFileFilterRegex);
            ExcludeFileNameRegexField = new Lazy<Regex[]>(Command.SelectionOptions.GetExcludedFileRegex);
            ExcludeDirectoryRegexField = new Lazy<DirectoryRegex[]>(Command.SelectionOptions.GetExcludedDirectoryRegex);
        }

        #region < Properties >

        private readonly Lazy<FileAttributes?> FileAttributesToApplyField;
        private readonly Lazy<FileAttributes?> FileAttributesToRemoveField;
        private readonly Lazy<Regex[]> FileFilterRegexField;
        private readonly Lazy<Regex[]> ExcludeFileNameRegexField;
        private readonly Lazy<DirectoryRegex[]> ExcludeDirectoryRegexField;
        private readonly FilePairSorter<IProcessedFilePair> FileSorter;

        /// <summary>
        /// The IRoboCommand object this evaluator is tied to
        /// </summary>
        public IRoboCommand Command { get; }

        /// <summary>
        /// Regex objects used by <see cref="ShouldCopyFile"/> - Generated via <see cref="CopyOptions.FileFilter"/>
        /// </summary>
        public IEnumerable<Regex> FileFilterRegex => FileFilterRegexField.Value;

        /// <summary>
        /// Regex objects used by <see cref="ShouldCopyFile"/> - Generated via <see cref="SelectionOptions.ExcludedFiles"/>
        /// </summary>
        public IEnumerable<Regex> ExcludedFileNamesRegex => ExcludeFileNameRegexField.Value;

        /// <summary>
        /// Regex objects used by <see cref="ShouldCopyFile"/> - Generated via <see cref="SelectionOptions.ExcludedDirectories"/>
        /// </summary>
        public IEnumerable<DirectoryRegex> ExcludedDirectoriesRegex => ExcludeDirectoryRegexField.Value;

        /// <summary>
        /// File Attributes to add - Gathered from <see cref="CopyOptions.AddAttributes"/>
        /// </summary>
        public FileAttributes? FileAttributesToApply => FileAttributesToApplyField.Value;


        /// <summary>
        /// File Attributes to remove - Gathered from <see cref="CopyOptions.RemoveAttributes"/>
        /// </summary>
        public FileAttributes? FileAttributesToRemove => FileAttributesToRemoveField.Value;


        #endregion

        /// <summary>
        /// Evaluate the depth and determine if subdirectories should be evaluated.
        /// </summary>
        /// <param name="currentDepth"></param>
        /// <returns>true if the subdirectories should be evaluated, otherwise false.</returns>
        public bool CanDigDeeper(int currentDepth)
        {
            if (!Command.CopyOptions.IsRecursive()) return false;
            return !Command.CopyOptions.ExceedsAllowedDepth(currentDepth + 1);
        }

        /// <summary>
        /// Evaluate the current depth and determine if the Extra Subfolders contained within should be evaluated.
        /// <br/>This should should not be used when evaluating within a purge loop.
        /// </summary>
        /// <returns>true if the subdirectories should be evaluated, otherwise false.</returns>
        public bool CanProcessExtraDirs(int currentDepth)
        {
            bool evalExtra = Command.CopyOptions.Purge | Command.LoggingOptions.ReportExtraFiles;
            if (currentDepth == 1) return Command.CopyOptions.Depth != 1 && evalExtra | !Command.CopyOptions.ExceedsAllowedDepth(currentDepth + 1);
            if (evalExtra) return true;
            return CanDigDeeper(currentDepth);
        }

        /// <summary>
        /// EXTRA files are files that exist in the directory but are not selected for the operation.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="selectedFiles">Files selected for the operation</param>
        /// <returns></returns>
        public IEnumerable<FilePair> GetExtraFiles(DirectoryPair parent, IEnumerable<FilePair> selectedFiles)
        {
            var coll = parent.DestinationFiles;
            if (Command.LoggingOptions.ReportExtraFiles)
                return coll.Where(d => selectedFiles.None(s => PairEqualityComparer.AreEqual(s, d)));
            else
                return coll.Where(IFilePairExtensions.IsExtra);
        }

        #region < ShouldCopyDir >

        /// <summary>
        /// Compare the Source/Destination directories, and decide if the directory should be copied down.
        /// </summary>
        /// <param name="pair">the pair to evaluate</param>
        /// <param name="dirClass">The dirClass applied to the <see cref="IProcessedDirectoryPair.ProcessedFileInfo"/></param>
        /// <param name="ExcludeDirectoryName">Result of <see cref="ShouldExcludeDirectoryName(IDirectoryPair)"/></param>
        /// <param name="ExcludeJunctionDirectory">Result of <see cref="ShouldExcludeJunctionDirectory(IDirectoryPair)"/></param>
        /// <returns>TRUE if the directory would be excluded based on the current IROboCommand settings, otherwise false</returns>
        public virtual bool ShouldCopyDir(IProcessedDirectoryPair pair, out ProcessedDirectoryFlag dirClass, out bool ExcludeJunctionDirectory, out bool ExcludeDirectoryName)
        {
            ExcludeDirectoryName = ShouldExcludeDirectoryName(pair);
            ExcludeJunctionDirectory = ShouldExcludeJunctionDirectory(pair);

            bool shouldExclude = ExcludeJunctionDirectory | ExcludeDirectoryName;
            if (!shouldExclude && pair.Source.Exists && pair.Destination.Exists)
            {
                dirClass = ProcessedDirectoryFlag.ExistingDir;
                pair.ProcessedFileInfo = new ProcessedFileInfo(pair.Source, Command, ProcessedDirectoryFlag.ExistingDir, 0);
            }
            else if (!shouldExclude && pair.Source.Exists && !Command.SelectionOptions.ExcludeLonely)
            {
                dirClass = ProcessedDirectoryFlag.NewDir;
                pair.ProcessedFileInfo = new ProcessedFileInfo(pair.Source, Command, ProcessedDirectoryFlag.NewDir, 0);
            }
            else if (pair.Destination.Exists && !Command.SelectionOptions.ExcludeExtra)
            {
                dirClass = ProcessedDirectoryFlag.ExtraDir;
                pair.ProcessedFileInfo = new ProcessedFileInfo(pair.Destination, Command, ProcessedDirectoryFlag.ExtraDir, 0);
                return false;
            }
            else
            {
                dirClass = ProcessedDirectoryFlag.Exclusion;
                pair.ProcessedFileInfo = new ProcessedFileInfo(pair.Source, Command, ProcessedDirectoryFlag.Exclusion, 0);
            }
            return !shouldExclude;
        }

        /// <inheritdoc cref="ShouldCopyDir(IProcessedDirectoryPair , out ProcessedDirectoryFlag, out bool, out bool)"/>
        public bool ShouldCopyDir(IProcessedDirectoryPair pair) => ShouldCopyDir(pair, out _, out _, out _);

        /// <inheritdoc cref="SelectionExtensions.ShouldExcludeJunctionDirectory(SelectionOptions, IDirectoryPair)"/>
        public bool ShouldExcludeJunctionDirectory(IDirectoryPair pair) => Command.SelectionOptions.ShouldExcludeJunctionDirectory(pair.Source);

        /// <inheritdoc cref="SelectionExtensions.ShouldExcludeDirectoryName(SelectionOptions, IDirectoryPair, IEnumerable{DirectoryRegex})"/>
        public bool ShouldExcludeDirectoryName(IDirectoryPair pair) => Command.SelectionOptions.ShouldExcludeDirectoryName(pair.Source.FullName, ExcludedDirectoriesRegex);

        #endregion

        #region < ShouldCopyFile >

        /// <summary>
        /// Evaluate RoboCopy Options of the command, the source, and destination and compute a ProcessedFileInfo object
        /// <br/> - Ignores <see cref="LoggingOptions.ListOnly"/>
        /// <br/> - The generated <see cref="ProcessedFileInfo"/> object will be applied to the <see cref="IProcessedFilePair.ProcessedFileInfo"/> property.
        /// <br/> - The <see cref="bool"/> result is not automatically applied to the <see cref="IProcessedFilePair.ShouldCopy"/> or <see cref="IProcessedFilePair.ShouldPurge"/> properties!
        /// </summary>
        /// <param name="pair">the pair of Source/Destination to compare</param>
        /// <returns>
        /// TRUE if the file should be copied/moved, FALSE if the file should be skiped
        /// </returns>
        /// <remarks>
        /// Note: Does not evaluate the FileName inclusions from CopyOptions, since RoboCopy appears to use those to filter prior to performing these evaluations. <br/>
        /// Use <see cref="FilterAndSortSourceFiles{T}(IEnumerable{T})"/> as a pre-filter for this.
        /// </remarks>
        public virtual bool ShouldCopyFile(IProcessedFilePair pair)
        {
            bool SourceExists = pair.Source.Exists;
            bool DestExists = pair.Destination.Exists;
            string Name = Command.LoggingOptions.IncludeFullPathNames ?
                DestExists & !SourceExists ? pair.Destination.FullName : pair.Source.FullName :
                DestExists & !SourceExists ? pair.Destination.Name : pair.Source.Name;
            pair.ProcessedFileInfo = new ProcessedFileInfo()
            {
                FileClassType = FileClassType.File,
                Name = Name,
                Size = SourceExists ? pair.Source.Length : DestExists ? pair.Destination.Length : 0,
            };
            var info = pair.ProcessedFileInfo;
            var SO = Command.SelectionOptions;
            bool result = false;

            // Order of the following checks was done to allow what are likely the fastest checks to go first. More complex checks (such as DateTime parsing) are towards the bottom.

            //EXTRA
            if (pair.IsExtra())// SO.ShouldExcludeExtra(pair))
            {
                info.SetFileClass(ProcessedFileFlag.ExtraFile, Command);
            }
            //Lonely
            else if (SO.ShouldExcludeLonely(pair))
            {
                info.SetFileClass(ProcessedFileFlag.ExtraFile, Command); // TO-DO: Does RoboCopy identify Lonely seperately? If so, we need a token for it!
            }
            //Exclude Newer
            else if (SO.ShouldExcludeNewer(pair))
            {
                info.SetFileClass(ProcessedFileFlag.NewerFile, Command);
            }
            //Exclude Older
            else if (SO.ShouldExcludeOlder(pair))
            {
                info.SetFileClass(ProcessedFileFlag.OlderFile, Command);
            }
            //MaxFileSize
            else if (SO.ShouldExcludeMaxFileSize(pair.Source.Length))
            {
                info.SetFileClass(ProcessedFileFlag.MaxFileSizeExclusion, Command);
            }
            //MinFileSize
            else if (SO.ShouldExcludeMinFileSize(pair.Source.Length))
            {
                info.SetFileClass(ProcessedFileFlag.MinFileSizeExclusion, Command);
            }
            //FileAttributes
            else if (!SO.ShouldIncludeAttributes(pair) || SO.ShouldExcludeFileAttributes(pair))
            {
                info.SetFileClass(ProcessedFileFlag.AttribExclusion, Command);
            }
            //Max File Age
            else if (SO.ShouldExcludeMaxFileAge(pair))
            {
                info.SetFileClass(ProcessedFileFlag.MaxAgeSizeExclusion, Command);
            }
            //Min File Age
            else if (SO.ShouldExcludeMinFileAge(pair))
            {
                info.SetFileClass(ProcessedFileFlag.MinAgeSizeExclusion, Command);
            }
            //Max Last Access Date
            else if (SO.ShouldExcludeMaxLastAccessDate(pair))
            {
                info.SetFileClass(ProcessedFileFlag.MaxAgeSizeExclusion, Command);
            }
            //Min Last Access Date
            else if (SO.ShouldExcludeMinLastAccessDate(pair))
            {
                info.SetFileClass(ProcessedFileFlag.MinAgeSizeExclusion, Command); // TO-DO: Does RoboCopy iddentify Last Access Date exclusions seperately? If so, we need a token for it!
            }
            // Name Filters - These are last check since Regex will likely take the longest to evaluate
            else if (SO.ShouldExcludeFileName(pair.Source.Name, ExcludedFileNamesRegex))
            {
                info.SetFileClass(ProcessedFileFlag.FileExclusion, Command);
            }
            else // Only the following conditions may return true 
            {
                // Check for symbolic links
                bool xjf = SO.ExcludeSymbolicFile(pair.Source); // TO-DO: Likely needs its own 'FileClass' set up for proper evaluation by ProgressEstimator

                // File passed all checks - It should be copied!
                if (pair.IsLonely())
                {
                    info.SetFileClass(ProcessedFileFlag.NewFile, Command);
                    result = !xjf && !Command.SelectionOptions.ExcludeLonely;
                }
                else if (pair.IsSourceNewer())
                {
                    info.SetFileClass(ProcessedFileFlag.NewerFile, Command);
                    result = !xjf && !Command.SelectionOptions.ExcludeNewer;
                }
                else if (pair.IsDestinationNewer())
                {
                    info.SetFileClass(ProcessedFileFlag.OlderFile, Command);
                    result = !xjf && !Command.SelectionOptions.ExcludeOlder;
                }
                else
                {
                    info.SetFileClass(ProcessedFileFlag.SameFile, Command);
                    result = !xjf && Command.SelectionOptions.IncludeSame;
                }
            }
            return result;
        }

        /// <summary>
        /// - Filter the filenames according to the filters specified by <see cref="CopyOptions.FileFilter"/>
        /// <br/> - Generate the <see cref="IProcessedFilePair.ProcessedFileInfo"/> via <see cref="ShouldCopyFile(IProcessedFilePair)"/>
        /// <br/> - Sort the collection, then return it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public IEnumerable<T> FilterAndSortSourceFiles<T>(IEnumerable<T> collection) where T : IProcessedFilePair
        {
            List<T> coll;
            if (Command.CopyOptions.HasDefaultFileFilter())
                coll = collection.ToList();
            else
                coll = collection.Where(ShouldIncludeFileName).ToList();

            // Generate the ProcessedFileInto and sort
            foreach (var obj in coll)
                obj.ShouldCopy = ShouldCopyFile(obj);
            coll.Sort(FileSorter.Compare);
            return coll;
        }


        /// <inheritdoc cref="CopyExtensions.ShouldIncludeFileName(CopyOptions, IFilePair, IEnumerable{Regex})"/>
        public bool ShouldIncludeFileName<T>(T pair) where T : IFilePair
        {
            return Command.CopyOptions.ShouldIncludeFileName(pair, FileFilterRegex);
        }

        /// <inheritdoc cref="CopyExtensions.ShouldIncludeFileName(CopyOptions, FileInfo, IEnumerable{Regex})"/>
        public bool ShouldIncludeFileName(FileInfo file)
        {
            return Command.CopyOptions.ShouldIncludeFileName(file, FileFilterRegex);
        }

        /// <inheritdoc cref="CopyExtensions.ShouldIncludeFileName(CopyOptions, string, IEnumerable{Regex})"/>
        public bool ShouldIncludeFileName(string file)
        {
            return Command.CopyOptions.ShouldIncludeFileName(file, FileFilterRegex);
        }

        #endregion

        #region < Purge >

        /// <inheritdoc cref="CopyExtensions.ShouldPurge(IRoboCommand, IProcessedFilePair)"/>
        public bool ShouldPurge(IProcessedFilePair pair)
        {
            return Command.ShouldPurge(pair);
        }

        /// <inheritdoc cref="CopyExtensions.ShouldPurge(IRoboCommand, IDirectoryPair)"/>
        public bool ShouldPurge(IDirectoryPair pair) => Command.ShouldPurge(pair);

        #endregion

        #region < Apply Attributes >

        /// <inheritdoc cref="CopyExtensions.SetFileAttributes(CopyOptions, FileInfo)"/>
        public void ApplyAttributes(FileInfo destination)
        {
            if (FileAttributesToApply.HasValue)
                destination.Attributes &= FileAttributesToApply.Value;
            if (FileAttributesToRemove.HasValue)
                destination.Attributes &= ~FileAttributesToRemove.Value;
        }

        /// <inheritdoc cref="CopyExtensions.SetFileAttributes(CopyOptions, FileInfo)"/>
        public void ApplyAttributes(IFilePair pair)
            => ApplyAttributes(pair.Destination);

        #endregion

    }
}
