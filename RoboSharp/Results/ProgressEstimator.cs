using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using RoboSharp.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using RoboSharp.EventArgObjects;

namespace RoboSharp.Results
{
    /// <summary>
    /// Object that provides <see cref="IStatistic"/> objects whose events can be bound to report estimated RoboCommand progress periodically.
    /// <br/>
    /// Note: Only works properly with /V verbose set TRUE.
    /// </summary>
    /// <remarks>
    /// Subscribe to <see cref="RoboCommand.OnProgressEstimatorCreated"/> or <see cref="RoboQueue.OnProgressEstimatorCreated"/> to be notified when the ProgressEstimator becomes available for binding <br/>
    /// Create event handler to subscribe to the Events you want to handle: <para/>
    /// <code>
    /// private void OnProgressEstimatorCreated(object sender, Results.ProgressEstimatorCreatedEventArgs e) { <br/>
    /// e.ResultsEstimate.ByteStats.PropertyChanged += ByteStats_PropertyChanged;<br/>
    /// e.ResultsEstimate.DirStats.PropertyChanged += DirStats_PropertyChanged;<br/>
    /// e.ResultsEstimate.FileStats.PropertyChanged += FileStats_PropertyChanged;<br/>
    /// }<br/>
    /// </code>
    /// <para/>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/ProgressEstimator"/>
    /// </remarks>
    public class ProgressEstimator : IProgressEstimator, IResults
    {

        #region < Constructors >

        private ProgressEstimator() { }

        /// <summary>
        /// Create a new ProgressEstimator object
        /// </summary>
        /// <param name="cmd"></param>
        public ProgressEstimator(IRoboCommand cmd)
        {
            Command = cmd;
            DirStatField = new Statistic(Statistic.StatType.Directories, "Directory Stats Estimate");
            FileStatsField = new Statistic(Statistic.StatType.Files, "File Stats Estimate");
            ByteStatsField = new Statistic(Statistic.StatType.Bytes, "Byte Stats Estimate");

            tmpByte.EnablePropertyChangeEvent = false;
            tmpFile.EnablePropertyChangeEvent = false;
            tmpDir.EnablePropertyChangeEvent = false;
            this.StartUpdateTask(out UpdateTaskCancelSource);
        }

        #endregion

        #region < Private Members >

        internal IRoboCommand Command { get; }
        private bool SkippingFile { get; set; }
        private bool CopyOpStarted { get; set; }
        private bool IsFinalized { get; set; }
        internal bool FileFailed { get; set; }
        private bool DirMarkedAsCopied { get; set; }

        private RoboSharpConfiguration Config => Command?.Configuration;

        // Stat Objects that will be publicly visible
        private readonly Statistic DirStatField;
        private readonly Statistic FileStatsField;
        private readonly Statistic ByteStatsField;

        internal enum WhereToAdd { Copied, Skipped, Extra, MisMatch, Failed }

        // Storage for last entered Directory and File objects 
        /// <summary>Used for providing Source Directory in CopyProgressChanged args</summary>
        internal ProcessedFileInfo CurrentDir { get; private set; }
        /// <summary>Used for providing Source Directory in CopyProgressChanged args AND for byte Statistic</summary>
        internal ProcessedFileInfo CurrentFile { get; private set; }
        /// <summary> Marked as TRUE if this is LIST ONLY mode or the file is 0KB  -- Value set during 'AddFile' method </summary>
        private bool CurrentFile_SpecialHandling { get; set; }

        //Stat objects to house the data temporarily before writing to publicly visible stat objects
        readonly Statistic tmpDir =new Statistic(type: Statistic.StatType.Directories);
        readonly Statistic tmpFile = new Statistic(type: Statistic.StatType.Files);
        readonly Statistic tmpByte = new Statistic(type: Statistic.StatType.Bytes);

        ///<summary> Update Period in milliseconds to push Updates to a UI or RoboQueueProgressEstimator </summary>
        private const int UpdatePeriod = 150; 
        ///<summary>Thread Lock for CurrentFile</summary>
        private readonly object CurrentFileLock = new object();     
        ///<summary>Thread Lock for CurrentDir</summary>
        private readonly object CurrentDirLock = new object();     
        /// <summary>Thread Lock for tmpDir</summary>
        private readonly object DirLock = new object();     
        /// <summary>Thread Lock for tmpFile and tmpByte</summary>
        private readonly object FileLock = new object();
        /// <summary>Thread Lock for NextUpdatePush and UpdateTaskTrgger</summary>
        private readonly object UpdateLock = new object();
        /// <summary>Time the next update will be pushed to the UI </summary>
        private DateTime NextUpdatePush = DateTime.Now.AddMilliseconds(UpdatePeriod);
        /// <summary>TCS that the UpdateTask awaits on </summary>
        private TaskCompletionSource<object> UpdateTaskTrigger;
        /// <summary>While !Cancelled, UpdateTask continues looping </summary>
        private CancellationTokenSource UpdateTaskCancelSource;

        #endregion

        #region < Public Properties > 

        /// <summary>
        /// Estimate of current number of directories processed while the job is still running. <br/>
        /// Estimate is provided by parsing of the LogLines produces by RoboCopy.
        /// </summary>
        public IStatistic DirectoriesStatistic => DirStatField;

        /// <summary>
        /// Estimate of current number of files processed while the job is still running. <br/>
        /// Estimate is provided by parsing of the LogLines produces by RoboCopy.
        /// </summary>
        public IStatistic FilesStatistic => FileStatsField;

        /// <summary>
        /// Estimate of current number of bytes processed while the job is still running. <br/>
        /// Estimate is provided by parsing of the LogLines produces by RoboCopy.
        /// </summary>
        public IStatistic BytesStatistic => ByteStatsField;

        RoboCopyExitStatus IResults.Status => new RoboCopyExitStatus((int)GetExitCode());

        /// <summary>  </summary>
        public delegate void UIUpdateEventHandler(IProgressEstimator sender, IProgressEstimatorUpdateEventArgs e);

        /// <inheritdoc cref="IProgressEstimator.ValuesUpdated"/>
        public event UIUpdateEventHandler ValuesUpdated;

        #endregion

        #region < Public Methods >

        /// <summary>
        /// Parse this object's stats into a <see cref="RoboCopyExitCodes"/> enum.
        /// </summary>
        /// <returns></returns>
        public RoboCopyExitCodes GetExitCode() => GetExitCode(FileStatsField, DirStatField);

        /// <summary>
        /// Parse the Statistics into a <see cref="RoboCopyExitCodes"/> enum.
        /// </summary>
        /// <returns></returns>
        public static RoboCopyExitCodes GetExitCode(IStatistic files, IStatistic dirs)
        {
            Results.RoboCopyExitCodes code = 0;

            //Files Copied
            if (files.Copied > 0)
                code |= Results.RoboCopyExitCodes.FilesCopiedSuccessful;

            //Extra
            if (dirs.Extras > 0 | files.Extras > 0)
                code |= Results.RoboCopyExitCodes.ExtraFilesOrDirectoriesDetected;

            //MisMatch
            if (dirs.Mismatch > 0 | files.Mismatch > 0)
                code |= Results.RoboCopyExitCodes.MismatchedDirectoriesDetected;

            //Failed
            if (dirs.Failed > 0 | files.Failed > 0)
                code |= Results.RoboCopyExitCodes.SomeFilesOrDirectoriesCouldNotBeCopied;

            return code;

        }

        #endregion

        #region < Get RoboCopyResults Object ( Internal ) >

        /// <summary>
        /// Repackage the statistics into a new <see cref="RoboCopyResults"/> object
        /// </summary>
        /// <remarks>
        /// Used by ResultsBuilder as starting point for the results. 
        /// </remarks>
        /// <returns></returns>
        public RoboCopyResults GetResults()
        {
            FinalizeResults();

            // Package up
            return new RoboCopyResults()
            {
                BytesStatistic = (Statistic)BytesStatistic,
                DirectoriesStatistic = (Statistic)DirectoriesStatistic,
                FilesStatistic = (Statistic)FilesStatistic,
                SpeedStatistic = new SpeedStatistic(),
                Status = new RoboCopyExitStatus(GetExitCode())
            };
        }

        /// <summary>
        /// Tabulate the final results - should only be called when no more processed files will be added to the estimator
        /// </summary>
        /// <remarks>
        /// Should not be used anywhere else, as it kills the worker thread that calculates the Statistics objects.
        /// </remarks>
        public void FinalizeResults()
        {
            if (!IsFinalized)
            {
                //Stop the Update Task
                UpdateTaskCancelSource?.Cancel();
                UpdateTaskTrigger?.TrySetResult(null);

                // - if copy operation wasn't completed, register it as failed instead.
                // - if file was to be marked as 'skipped', then register it as skipped.

                ProcessPreviousFile();
                PushUpdate(); // Perform Final calculation before generating the Results Object
                IsFinalized = true;
            }
        }

        #endregion

        #region < Calculate Dirs (Internal) >

        /// <summary>Increment <see cref="DirectoriesStatistic"/></summary>
        public void AddDir(ProcessedFileInfo currentDir)
        {
            if (currentDir.FileClassType != FileClassType.NewDir) return;
            if (currentDir == CurrentDir) return;

            WhereToAdd? whereTo = null;
            bool SetCurrentDir = false;
            if (currentDir.FileClass.Equals(Config.LogParsing_ExistingDir, StringComparison.CurrentCultureIgnoreCase))  // Existing Dir
            { 
                whereTo = WhereToAdd.Skipped;
                SetCurrentDir = true;
            }   
            else if (currentDir.FileClass.Equals(Config.LogParsing_NewDir, StringComparison.CurrentCultureIgnoreCase))  //New Dir
            { 
                whereTo = WhereToAdd.Copied;
                SetCurrentDir = true;
            }    
            else if (currentDir.FileClass.Equals(Config.LogParsing_ExtraDir, StringComparison.CurrentCultureIgnoreCase)) //Extra Dir
            { 
                whereTo = WhereToAdd.Extra;
                SetCurrentDir = false;
            }   
            else if (currentDir.FileClass.Equals(Config.LogParsing_DirectoryExclusion, StringComparison.CurrentCultureIgnoreCase)) //Excluded Dir
            { 
                whereTo = WhereToAdd.Skipped;
                SetCurrentDir = false;
            }
            //Store CurrentDir under various conditions
            if (SetCurrentDir)
            {
                lock (CurrentDirLock)
                {
                    CurrentDir = currentDir;
                    DirMarkedAsCopied = whereTo == WhereToAdd.Copied;
                }
            }

            lock (DirLock)
            {
                switch (whereTo)
                {
                    case WhereToAdd.Copied: tmpDir.Total++; tmpDir.Copied++; break;
                    case WhereToAdd.Extra: tmpDir.Extras++; break;  //Extras do not count towards total
                    case WhereToAdd.Failed: tmpDir.Total++; tmpDir.Failed++; break;
                    case WhereToAdd.MisMatch: tmpDir.Total++; tmpDir.Mismatch++; break;
                    case WhereToAdd.Skipped: tmpDir.Total++; tmpDir.Skipped++; break;
                }
            }
            
            
            //Check if the UpdateTask should push an update to the public fields
            if (Monitor.TryEnter(UpdateLock))
            {
                if (NextUpdatePush <= DateTime.Now) 
                    UpdateTaskTrigger?.TrySetResult(null);
                Monitor.Exit(UpdateLock);
            }
            return;
        }

        /// <summary>
        /// Sets the <see cref="CurrentDir"/> and adds 1 to the directories Copied stat
        /// </summary>
        /// <param name="dir"></param>
        public void AddDirCopied(ProcessedFileInfo dir)
        {
            lock (CurrentDirLock)
                lock (DirLock)
                {
                    if (dir != CurrentDir)
                    {
                        tmpDir.Total++;
                        CurrentDir = dir;
                        DirMarkedAsCopied = false;
                    }
                    if (!DirMarkedAsCopied)
                    {
                        this.tmpDir.Copied++;
                        DirMarkedAsCopied = true;
                    }
                }
        }

        /// <summary>
        /// Adds 1 to the directories Skipped stat
        /// </summary>
        /// <param name="dir"></param>
        public void AddDirSkipped(ProcessedFileInfo dir)
        {
            lock (DirLock)
            {
                if (dir != CurrentDir)
                {
                    tmpDir.Total++;
                    tmpDir.Skipped++;
                }
            }
        }

        #endregion

        #region < Calculate Files (Internal) >

        /// <summary>
        /// Performs final processing of the previously added file if needed.
        /// </summary>
        /// <remarks>
        /// This is already called inside of <see cref="AddFile(ProcessedFileInfo)"/>, but is not called by any method with a WhereTo suffix, such as <see cref="AddFileCopied(ProcessedFileInfo)"/>
        /// </remarks>
        public void ProcessPreviousFile()
        {
            bool shouldRelease = false;
            if (!Monitor.IsEntered(CurrentFileLock))
            {
                shouldRelease = true;
                Monitor.Enter(CurrentFileLock);
            }
            if (CurrentFile != null)
            {
                if (FileFailed)
                {
                    PerformByteCalc(CurrentFile, WhereToAdd.Failed);
                }
                else if (CopyOpStarted && CurrentFile_SpecialHandling)
                {
                    PerformByteCalc(CurrentFile, WhereToAdd.Copied);
                }
                else if (SkippingFile)
                {
                    PerformByteCalc(CurrentFile, WhereToAdd.Skipped);
                }
                else if (UpdateTaskCancelSource?.IsCancellationRequested ?? true)
                {
                    //Default marks as failed - This should only occur during the 'GetResults()' method due to the if statement above.
                    PerformByteCalc(CurrentFile, WhereToAdd.Failed);
                }
            }
            if (shouldRelease)
                Monitor.Exit(CurrentFileLock);
        }

        /// <summary>
        /// Call this method after determining what to do with a file, but before starting to copy it.<br/>
        /// Compares the <see cref="ProcessedFileInfo.FileClass"/> against the configuration strings to identify how to process the file. <br/>
        /// Also compares against the current <see cref="SelectionOptions"/> and <see cref="CopyOptions"/> to determine if the file should be copied or skipped.
        /// </summary>
        /// <remarks>
        /// Standard Operation: Robocopy reports that it has determined what to do with a file, reports that determination, then starts reporting copy progress. <br/> <br/>
        /// Notes for Custom Implementations: 
        /// <br/> - <see cref="SetCopyOpStarted"/> and <see cref="AddFileCopied(ProcessedFileInfo)"/> should be used if using this method.
        /// <br/> - Should not be used if <see cref="PerformByteCalc(ProcessedFileInfo, WhereToAdd)"/> is used.
        /// </remarks>
        /// <param name="currentFile">The file that was just added to the stack</param>
        public void AddFile(ProcessedFileInfo currentFile)
        {
            if (currentFile.FileClassType != FileClassType.File) return;
            
            Monitor.Enter(CurrentFileLock);
            ProcessPreviousFile();

            CurrentFile = currentFile;
            SkippingFile = false;
            CopyOpStarted = false;
            FileFailed = false;

            // Flag to perform checks during a ListOnly operation OR for 0kb files (They won't get Progress update, but will be created)
            bool ListOperation = Command.LoggingOptions.ListOnly;
            bool SpecialHandling = ListOperation || currentFile.Size == 0;
            CurrentFile_SpecialHandling = SpecialHandling;
            Monitor.Exit(CurrentFileLock);

            // EXTRA FILES
            if (currentFile.FileClass.Equals(Config.LogParsing_ExtraFile, StringComparison.CurrentCultureIgnoreCase))
            {
                PerformByteCalc(currentFile, WhereToAdd.Extra);
            }
            //MisMatch
            else if (currentFile.FileClass.Equals(Config.LogParsing_MismatchFile, StringComparison.CurrentCultureIgnoreCase))
            {
                PerformByteCalc(currentFile, WhereToAdd.MisMatch);
            }
            //Failed Files
            else if (currentFile.FileClass.Equals(Config.LogParsing_FailedFile, StringComparison.CurrentCultureIgnoreCase))
            {
                PerformByteCalc(currentFile, WhereToAdd.Failed);
            }

            //Files to be Copied/Skipped
            else
            {
                SkippingFile = !ListOperation;//Assume Skipped, adjusted when CopyProgress is updated
                if (currentFile.FileClass.Equals(Config.LogParsing_NewFile, StringComparison.CurrentCultureIgnoreCase)) // New File
                {
                    //Special handling for 0kb files & ListOnly -> They won't get Progress update, but will be created
                    if (SpecialHandling)
                    {
                        SetCopyOpStarted();
                    }
                }
                else if (currentFile.FileClass.Equals(Config.LogParsing_SameFile, StringComparison.CurrentCultureIgnoreCase))    //Identical Files
                {
                    if (Command.SelectionOptions.IncludeSame)
                    {
                        if (SpecialHandling) SetCopyOpStarted();   // Only add to Copied if ListOnly / 0-bytes
                    }
                    else
                        PerformByteCalc(currentFile, WhereToAdd.Skipped);
                }
                else if (SpecialHandling) // These checks are always performed during a ListOnly operation
                {

                    switch (true)
                    {
                        //Skipped Or Copied Conditions
                        case true when currentFile.FileClass.Equals(Config.LogParsing_NewerFile, StringComparison.CurrentCultureIgnoreCase):    // ExcludeNewer
                            SkippedOrCopied(currentFile, Command.SelectionOptions.ExcludeNewer);
                            break;
                        case true when currentFile.FileClass.Equals(Config.LogParsing_OlderFile, StringComparison.CurrentCultureIgnoreCase):    // ExcludeOlder
                            SkippedOrCopied(currentFile, Command.SelectionOptions.ExcludeOlder);
                            break;
                        case true when currentFile.FileClass.Equals(Config.LogParsing_ChangedExclusion, StringComparison.CurrentCultureIgnoreCase):  //ExcludeChanged
                            SkippedOrCopied(currentFile, Command.SelectionOptions.ExcludeChanged);
                            break;
                        case true when currentFile.FileClass.Equals(Config.LogParsing_TweakedInclusion, StringComparison.CurrentCultureIgnoreCase):  //IncludeTweaked
                            SkippedOrCopied(currentFile, !Command.SelectionOptions.IncludeTweaked);
                            break;
                        case true when currentFile.FileClass.Equals(Config.LogParsing_ModifiedInclusion, StringComparison.CurrentCultureIgnoreCase):  //IncludeModified
                            SkippedOrCopied(currentFile, !Command.SelectionOptions.IncludeModified);
                            break;

                        //Mark As Skip Conditions
                        case true when currentFile.FileClass.Equals(Config.LogParsing_FileExclusion, StringComparison.CurrentCultureIgnoreCase):    //FileExclusion
                        case true when currentFile.FileClass.Equals(Config.LogParsing_AttribExclusion, StringComparison.CurrentCultureIgnoreCase):  //AttributeExclusion
                        case true when currentFile.FileClass.Equals(Config.LogParsing_MaxFileSizeExclusion, StringComparison.CurrentCultureIgnoreCase):     //MaxFileSizeExclusion
                        case true when currentFile.FileClass.Equals(Config.LogParsing_MinFileSizeExclusion, StringComparison.CurrentCultureIgnoreCase):     //MinFileSizeExclusion
                        case true when currentFile.FileClass.Equals(Config.LogParsing_MaxAgeOrAccessExclusion, StringComparison.CurrentCultureIgnoreCase):  //MaxAgeOrAccessExclusion
                        case true when currentFile.FileClass.Equals(Config.LogParsing_MinAgeOrAccessExclusion, StringComparison.CurrentCultureIgnoreCase):  //MinAgeOrAccessExclusion
                            PerformByteCalc(currentFile, WhereToAdd.Skipped);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Method meant only to be called from AddFile method while SpecialHandling is true - helps normalize code and avoid repetition
        /// </summary>
        private void SkippedOrCopied(ProcessedFileInfo currentFile, bool MarkSkipped)
        {
            if (MarkSkipped)
                PerformByteCalc(currentFile, WhereToAdd.Skipped);
            else
            {
                SetCopyOpStarted();
                //PerformByteCalc(currentFile, WhereToAdd.Copied);
            }
        }

        /// <summary>Catch start copy progress of large files ( Called when progress less than 100% )</summary>
        /// <remarks>
        /// For Custom Implementations: <br/>
        /// - <see cref="AddFile(ProcessedFileInfo)"/> should have been called prior to this being called. <br/>
        /// - Should use <see cref="AddFileCopied(ProcessedFileInfo)"/> after progress reaches 100% <br/><br/>
        /// !! Should not be used if <see cref="PerformByteCalc(ProcessedFileInfo, WhereToAdd)"/> is used.
        /// </remarks>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void SetCopyOpStarted()
        {
            lock (CurrentFileLock)
            {
                SkippingFile = false;
                CopyOpStarted = true;
            }
        }

        private void CheckToResetCurrentFile(ProcessedFileInfo compareTo)
        {
            lock (CurrentFileLock)
            {
                if (CurrentFile == compareTo)
                    CurrentFile = null;
            }
        }

        /// <summary>Increment <see cref="FileStatsField"/>.Copied ( Triggered when copy progress = 100% ) </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void AddFileCopied(ProcessedFileInfo currentFile)
        {
            CheckToResetCurrentFile(currentFile);
            PerformByteCalc(currentFile, WhereToAdd.Copied);
        }

        /// <summary>Increment <see cref="FileStatsField"/>.Failed </summary>
        /// <param name="currentFile"/>
        /// <param name="DecrementCopied">Set this TRUE if you also wish to decrease the <see cref="Statistic.Copied"/> values accordingly </param>
        public void AddFileFailed(ProcessedFileInfo currentFile, bool DecrementCopied = false)
        {
            if (DecrementCopied)
            {
                lock(FileLock)
                {
                    tmpFile.Copied--;
                    tmpByte.Copied -= currentFile.Size;
                }
            }
            CheckToResetCurrentFile(currentFile);
            PerformByteCalc(currentFile, WhereToAdd.Failed);
        }

        /// <summary>Increment <see cref="FileStatsField"/>.Skipped</summary>
        public void AddFileSkipped(ProcessedFileInfo currentFile)
        {
            CheckToResetCurrentFile(currentFile);
            PerformByteCalc(currentFile, WhereToAdd.Skipped);
        }

        /// <summary>Increment <see cref="FileStatsField"/>.MisMatch</summary>
        public void AddFileMisMatch(ProcessedFileInfo currentFile)
        {
            CheckToResetCurrentFile(currentFile);
            PerformByteCalc(currentFile, WhereToAdd.MisMatch);
        }

        /// <summary>Increment <see cref="FileStatsField"/>.Extra</summary>
        public void AddFileExtra(ProcessedFileInfo currentFile)
        {
            CheckToResetCurrentFile(currentFile);
            PerformByteCalc(currentFile, WhereToAdd.Extra);
        }

        /// <summary>
        /// Adds the file statistics to the <see cref="BytesStatistic"/> and the <see cref="FilesStatistic"/> internal counters.
        /// </summary>
        /// <remarks>
        /// This method resets internal flags that other methods set to log how the current file should be handled. <br/>
        /// As such, if using this method for pushing updates to the estimator, no other methods that accept files should be used.
        /// </remarks>
        private void PerformByteCalc(ProcessedFileInfo file, WhereToAdd where)
        {
            if (file == null) return;
            if (file.FileClassType != FileClassType.File) return;

            //Reset Flags
            Monitor.Enter(CurrentFileLock);
                SkippingFile = false;
                CopyOpStarted = false;
                FileFailed = false;
                CurrentFile = null;
                CurrentFile_SpecialHandling = false;
            Monitor.Exit(CurrentFileLock);

            //Perform Math
            lock (FileLock)
            {
                //Extra files do not contribute towards Copy Total.
                if (where == WhereToAdd.Extra)
                {
                    tmpFile.Extras++;
                    tmpByte.Extras += file.Size;
                }
                else
                {
                    tmpFile.Total++;
                    tmpByte.Total += file.Size;

                    switch (where)
                    {
                        case WhereToAdd.Copied:
                            tmpFile.Copied++;
                            tmpByte.Copied += file.Size;
                            break;
                        case WhereToAdd.Extra:
                            break;
                        case WhereToAdd.Failed:
                            tmpFile.Failed++;
                            tmpByte.Failed += file.Size;
                            break;
                        case WhereToAdd.MisMatch:
                            tmpFile.Mismatch++;
                            tmpByte.Mismatch += file.Size;
                            break;
                        case WhereToAdd.Skipped:
                            tmpFile.Skipped++;
                            tmpByte.Skipped += file.Size;
                            break;
                    }
                }
            }
            //Check if the UpdateTask should push an update to the public fields
            if (Monitor.TryEnter(UpdateLock))
            {
                if (NextUpdatePush <= DateTime.Now)
                    UpdateTaskTrigger?.TrySetResult(null);
                Monitor.Exit(UpdateLock);
            }
        }

        #endregion

        #region < PushUpdate to Public Stat Objects >

        /// <summary>
        /// Creates a LongRunning task that is meant to periodically push out Updates to the UI on a thread isolated from the event thread.
        /// </summary>
        /// <param name="CancelSource"></param>
        /// <returns></returns>
        private Task StartUpdateTask(out CancellationTokenSource CancelSource)
        {
            CancelSource = new CancellationTokenSource();
            var CS = CancelSource;
            return Task.Run(async () =>
            {
                while (!CS.IsCancellationRequested)
                {
                    lock(UpdateLock)
                    {
                        PushUpdate();
                        UpdateTaskTrigger = new TaskCompletionSource<object>();
                        NextUpdatePush = DateTime.Now.AddMilliseconds(UpdatePeriod);
                    }
                    await UpdateTaskTrigger.Task;
                }
                //Cleanup
                CS?.Dispose();
                UpdateTaskTrigger = null;
                UpdateTaskCancelSource = null;
            }, CS.Token);
        }

        /// <summary>
        /// Push the update to the public Stat Objects
        /// </summary>
        private void PushUpdate()
        {
            //Lock the Stat objects, clone, reset them, then push the update to the UI.
            Statistic TD = null;
            Statistic TB = null;
            Statistic TF = null;
            lock (DirLock)
            {
                if (tmpDir.NonZeroValue)
                {
                    TD = tmpDir.Clone();
                    tmpDir.Reset();
                }
            }
            lock (FileLock)
            {
                if (tmpFile.NonZeroValue)
                {
                    TF = tmpFile.Clone();
                    tmpFile.Reset();
                }
                if (tmpByte.NonZeroValue)
                {
                    TB = tmpByte.Clone();
                    tmpByte.Reset();
                }
            }
            //Push UI update after locks are released, to avoid holding up the other thread for too long
            if (TD != null) DirStatField.AddStatistic(TD);
            if (TB != null) ByteStatsField.AddStatistic(TB);
            if (TF != null) FileStatsField.AddStatistic(TF);
            //Raise the event if any of the values have been updated
            if (TF != null || TD != null || TB != null)
            {
                ValuesUpdated?.Invoke(this, new IProgressEstimatorUpdateEventArgs(this, TB, TF, TD));
            }
        }

        #endregion
    }
}
