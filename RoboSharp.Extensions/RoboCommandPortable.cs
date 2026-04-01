#if NET6_0_OR_GREATER

using Microsoft.VisualBasic;
using RoboSharp.EventArgObjects;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Extensions.Options;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


#nullable enable

namespace RoboSharp.Extensions
{
    /// <summary>
    /// This <see cref="Interfaces.IRoboCommand"/> relies on an <see cref="IFileCopierFactory"/> to generate the objects used to manage the copy operations.
    /// <br/>This class should allow use of this library in non-windows environments.
    /// </summary>
    public class RoboCommandPortable : IRoboCommand, INotifyPropertyChanged
    {
        internal static void ThrowUnsupportedFrameworkException()
        {
#if !(NET6_0_OR_GREATER)
            throw new System.NotSupportedException("This process relies on IAsyncEnumerable, which is not present for this framework.");
#endif
        }

        /// <summary>
        /// Create a new <see cref="RoboCommandPortable"/>
        /// </summary>
        /// <param name="fileCopierFactory"></param>
        /// <param name="authenticator">
        /// The <see cref="IAuthenticator"/> used to validate the robocommand prior to running. 
        /// <br/>Default uses <see cref="SourceAndDestinationAuthenticator"/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException">Not Available in .Net Framework or .NetStandard2.0</exception>
        public RoboCommandPortable(IFileCopierFactory fileCopierFactory, IAuthenticator? authenticator = null)
        {
            ThrowUnsupportedFrameworkException();
            copierFactory = fileCopierFactory ?? throw new ArgumentNullException(nameof(fileCopierFactory));
            this.authenticator = authenticator ?? SourceAndDestinationAuthenticator.Instance;
        }

        private readonly IFileCopierFactory copierFactory;
        private readonly IAuthenticator authenticator;
        private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);

        private string name = string.Empty;
        private bool isPaused = false, isRunning = false, isScheduled = false, isCancelled = false, stopIfDisposing;
        private CopyOptions? _CopyOptions;
        private SelectionOptions? _SelectionOptions;
        private RetryOptions? _RetryOptions;
        private LoggingOptions? _LoggingOptions;
        private JobOptions? _JobOptions;
        private RoboSharpConfiguration? _Configuration;
        private IProgressEstimator? progressEstimator;
        private CancellationTokenSource? _CancellationTokenSource;
        private RoboCopyResults? _lastResults;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member -- Inherits descriptions from interface.

        public event RoboCommand.FileProcessedHandler? OnFileProcessed;
        public event RoboCommand.CommandErrorHandler? OnCommandError;
        public event RoboCommand.ErrorHandler? OnError;
        public event RoboCommand.CommandCompletedHandler? OnCommandCompleted;
        public event RoboCommand.CopyProgressHandler? OnCopyProgressChanged;
        public event RoboCommand.ProgressUpdaterCreatedHandler? OnProgressEstimatorCreated;
        public event UnhandledExceptionEventHandler? TaskFaulted { add { } remove { } }
        public event PropertyChangedEventHandler? PropertyChanged;


        private void SetProperty<T>(ref T field, T value, string name)
        {
            System.Diagnostics.Debug.Assert(string.IsNullOrWhiteSpace(name) == false, "name parameter has no value");
            if ((field is not null && field.Equals(value) == false) || (field is null && value is not null))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            System.Diagnostics.Debug.Assert(field?.Equals(value) ?? (value is null && field is null), "FactoryCommand.SetProperty failed to update field.", "Field {0} value was not updated to value [{1}]'", name, value);
        }

        public string Name { get => name; private set => SetProperty(ref name, value, nameof(Name)); }
        public bool IsPaused { get => isPaused; private set => SetProperty(ref isPaused, value, nameof(IsPaused)); }
        public bool IsRunning { get => isRunning; private set => SetProperty(ref isRunning, value, nameof(IsRunning)); }
        public bool IsScheduled { get => isScheduled; private set => SetProperty(ref isScheduled, value, nameof(IsScheduled)); }
        public bool IsCancelled { get => isCancelled; private set => SetProperty(ref isCancelled, value, nameof(IsCancelled)); }
        public bool StopIfDisposing { get => stopIfDisposing; private set => SetProperty(ref stopIfDisposing, value, nameof(StopIfDisposing)); }
        public IProgressEstimator? IProgressEstimator { get => progressEstimator; private set => SetProperty(ref progressEstimator, value, nameof(IProgressEstimator)); }
        public string CommandOptions => GenerateParameters();

        public CopyOptions CopyOptions { get => _CopyOptions ??= new(); set => SetProperty(ref _CopyOptions, value, nameof(CopyOptions)); }
        public SelectionOptions SelectionOptions { get => _SelectionOptions ??= new(); set => SetProperty(ref _SelectionOptions, value, nameof(SelectionOptions)); }
        public RetryOptions RetryOptions { get => _RetryOptions ??= new(); set => SetProperty(ref _RetryOptions, value, nameof(RetryOptions)); }
        public LoggingOptions LoggingOptions { get => _LoggingOptions ??= new(); set => SetProperty(ref _LoggingOptions, value, nameof(LoggingOptions)); }
        public JobOptions JobOptions { get => _JobOptions ??= new(); set => SetProperty(ref _JobOptions, value, nameof(JobOptions)); }
        public RoboSharpConfiguration Configuration { get => _Configuration ??= new(); set => SetProperty(ref _Configuration, value, nameof(Configuration)); }


        public void Pause()
        {
            if (IsRunning)
            {
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsPaused)
            {
                IsPaused = false;
            }
        }

        public void Stop()
        {
            _CancellationTokenSource?.Cancel();
            IsCancelled = _CancellationTokenSource?.IsCancellationRequested ?? false;
        }

        public Task Start(string domain = "", string username = "", string password = "")
        {
            return Run(domain, username, password);
        }

        public Task Start_ListOnly(string domain = "", string username = "", string password = "")
        {
            return Run(domain, username, password, PreRunListOnlyAction, PostRunListOnlyAction);
        }

        public async Task<RoboCopyResults?> StartAsync(string domain = "", string username = "", string password = "")
        {
            await Run(domain, username, password);
            return GetResults();
        }

        public async Task<RoboCopyResults?> StartAsync_ListOnly(string domain = "", string username = "", string password = "")
        {
            await Run(domain, username, password, PreRunListOnlyAction, PostRunListOnlyAction);
            return GetResults();
        }

        public RoboCopyResults? GetResults()
        {
            return _lastResults;
        }

        public void Dispose()
        {
            this._CancellationTokenSource?.Cancel();
        }


        /// <summary>
        /// Generate the Parameters and Switches to execute RoboCopy with based on the configured settings
        /// </summary>
        /// <returns></returns>
        private string GenerateParameters()
        {
            var parsedCopyOptions = CopyOptions.Parse();
            var parsedSelectionOptions = SelectionOptions.Parse();
            var parsedRetryOptions = RetryOptions.ToString();
            var parsedLoggingOptions = LoggingOptions.ToString();
            var parsedJobOptions = JobOptions.ToString();
            //var systemOptions = " /V /R:0 /FP /BYTES /W:0 /NJH /NJS";
            return string.Format("{0}{1}{2}{3}{4}", parsedCopyOptions, parsedSelectionOptions,
                parsedRetryOptions, parsedLoggingOptions, parsedJobOptions);
        }

        /// <inheritdoc cref="GenerateParameters"/>
        public override string ToString()
        {
            return GenerateParameters();
        }

        /// <summary>
        /// Combine this object's options with that of some JobFile
        /// </summary>
        /// <param name="jobFile"></param>
        public void MergeJobFile(JobFile jobFile)
        {
            Name = string.IsNullOrWhiteSpace(Name) ? jobFile.Name ?? "" : Name;
            CopyOptions.Merge(jobFile.CopyOptions);
            LoggingOptions.Merge(jobFile.LoggingOptions);
            RetryOptions.Merge(jobFile.RetryOptions);
            SelectionOptions.Merge(jobFile.SelectionOptions);
            JobOptions.Merge(((IRoboCommand)jobFile).JobOptions);
            //this.StopIfDisposing |= ((IRoboCommand)jobFile).StopIfDisposing;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member


        void PreRunListOnlyAction()
        {
            LoggingOptions.ListOnly = true;
        }
        void PostRunListOnlyAction()
        {
            LoggingOptions.ListOnly = false;
        }

        private Regex[] GetFileExclusionRegex() => excludedFiledRegex??= SelectionOptions.GetExcludedFileRegex();
        private Regex[]? excludedFiledRegex;

        private Regex[] GetFileFilterRegex() => fileFilterRegex ??= CopyOptions.GetFileFilterRegex();
        private Regex[]? fileFilterRegex;

        private DirectoryRegex[] GetDirectoryRegexes() => directoryRegexes ??= SelectionOptions.GetExcludedDirectoryRegex();
        private DirectoryRegex[]? directoryRegexes;

        private void RaiseProgressUpdated(object? sender, CopyProgressEventArgs e) => OnCopyProgressChanged?.Invoke(this, e);

        private async Task Run(string domain, string username, string password, Action? preRunAction = null, Action? postRunAction = null)
        {
            await _startLock.WaitAsync(CancellationToken.None);
            if (IsRunning)
            {
                _startLock.Release();
                throw new InvalidOperationException($"{nameof(RoboCommandPortable)} is already running.");
            }
            IsRunning = true;
            IsPaused = false;
            IsCancelled = false;

            // Sanity Checks
            var authResult = authenticator.Authenticate(this, domain, username, password);
            if (!authResult.Success)
            {
                OnCommandError?.Invoke(this, authResult.CommandErrorArgs);
                isRunning = false;
                return;
            }

            _CancellationTokenSource = new CancellationTokenSource();
            var token = _CancellationTokenSource.Token;

            try
            {
                // Pre-Run Action
                preRunAction?.Invoke();
                await RunAsync(token);
            }
            finally
            {
                postRunAction?.Invoke();
            }
        }

        /*
         * Code below this point was generated with the assistance of Claude.ai for the actual robocopy implementation
         * Modified as needed
         */

        /// <summary>
        /// Core execution loop. Two-pass design mirrors Robocopy:
        ///   Pass 1 – directory scan: feed ProgressEstimator so the UI has estimates upfront.
        ///   Pass 2 – directory process: evaluate, copy/skip/purge, fire IRoboCommand events,
        ///             record every outcome in ResultsBuilder.
        /// </summary>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // ── Infrastructure setup ──────────────────────────────────────────────────

            var resultsBuilder = new ResultsBuilder(this);  // tracks counts/bytes per category

            // Fire OnProgressEstimatorCreated so subscribers (e.g. a progress bar) can
            // attach to the estimator's IStatistic change events before work begins.
            this.IProgressEstimator = resultsBuilder.ProgressEstimator;
            OnProgressEstimatorCreated?.Invoke(this, new ProgressEstimatorCreatedEventArgs(resultsBuilder.ProgressEstimator));

            bool isRecursive = this.CopyOptions.IsRecursive();
            bool includeEmptyDirs = this.CopyOptions.IsIncludingEmptyDirectories();
            bool listOnly = LoggingOptions.ListOnly;
            bool purging = !SelectionOptions.ExcludeExtra && (CopyOptions.Purge || CopyOptions.Mirror);
            bool touchFiles = CopyOptions.CreateDirectoryAndFileTree && !CopyOptions.RemoveFileInformation;
            bool moveDirs = CopyOptions.MoveFilesAndDirectories && !touchFiles && !listOnly;
            int maxDepth = isRecursive ? (CopyOptions.Depth <= 0 ? int.MaxValue : CopyOptions.Depth) : 1;

            var rootPair = new DirectoryPair(this.CopyOptions.Source, this.CopyOptions.Destination);

            SemaphoreSlim multiThreadedController = new SemaphoreSlim(CopyOptions.MultiThreadedCopiesCount >= 128 ? 128 : CopyOptions.MultiThreadedCopiesCount <= 1 ? 1 : CopyOptions.MultiThreadedCopiesCount);
            ConcurrentDictionary<IFileCopier, Task> runningTasks = new();
            ConcurrentDictionary<DirectoryPair, Task>? directoryDeletionTasks = moveDirs ? new() : null;
            
            HashSet<string> destinationDirs = new HashSet<string>();
            HashSet<string> destinationFiles = new HashSet<string>();
            HashSet<string> sourceFiles = new HashSet<string>();
            
            try
            {
                resultsBuilder.CreateHeader();

                // ── process each directory ───────────────────────────────────────
                await foreach ((DirectoryPair dirPair, HashSet<string> sourceDirs, int currentDepth) in EnumerateDirectoryPairsAsync(rootPair, 1, maxDepth, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!dirPair.EvaluateCommandOptions(this, GetDirectoryRegexes()))
                    {
                        resultsBuilder.AddDir(dirPair.ProcessedFileInfo);
                        OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(dirPair.ProcessedFileInfo));
                        continue;
                    }

                    await Task.WhenAll(
                        Task.Run(() => PopulateSourceFiles(sourceFiles, dirPair.Source), cancellationToken),
                        Task.Run(() => PopulateDestinationChidren(destinationFiles, destinationDirs, dirPair.Destination), cancellationToken)
                        ).ConfigureAwait(false);

                    dirPair.ProcessedFileInfo.Size = sourceFiles.Count;

                    if (dirPair == rootPair)
                    {
                        resultsBuilder.AddFirstDir(rootPair);
                    }
                    else
                    {
                        resultsBuilder.AddDir(dirPair.ProcessedFileInfo);
                    }
                    OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(dirPair.ProcessedFileInfo));

                    // Process Extras (files and directories in destination but not source)
                    if (destinationFiles.Count > 0 || destinationDirs.Count > 0)
                    {
                        await ProcessExtras(dirPair, purging, resultsBuilder, destinationFiles, sourceFiles, sourceDirs, destinationDirs, cancellationToken).ConfigureAwait(false);
                    }

                    // ── Process Source files for copy/move ──────────────────────────────────────────────────
                    if (currentDepth <= maxDepth && dirPair.Source.Exists)
                    {
                        if (!listOnly && (includeEmptyDirs || dirPair.ProcessedFileInfo.Size > 0))
                            dirPair.Destination.Create();

                        Task? lastCopyTask = null;
                        
                        await foreach (IFileCopier copier in CreateFileCopiers(sourceFiles, dirPair, cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Evaluate populates copier.ProcessedFileInfo (FileClass, Size, Name)
                            // AND sets ShouldCopy / ShouldPurge based on this IRoboCommand's options.
                            if (copier.EvaluateCommandOptions(this, GetFileFilterRegex(), GetFileExclusionRegex()) == IFilePairExtensions.EvaluationResult.SkippedByFilter)
                                continue;

                            ProcessedFileInfo fileInfo = copier.ProcessedFileInfo;
                            
                            if (fileInfo.GetProcessedFileFlag() == ProcessedFileFlag.MisMatch)
                            {
                                // File was evaluated but not copied (skipped/extra/same/newer/older).
                                resultsBuilder.AddFile(fileInfo);
                                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                                continue;
                            }
                            if (!copier.ShouldCopy)
                            {
                                // File was evaluated but not copied (skipped/extra/same/newer/older).
                                resultsBuilder.AddFileSkipped(fileInfo);
                                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                                continue;
                            }
                            if (listOnly)
                            {
                                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                                resultsBuilder.AddFileCopied(fileInfo);
                                continue;
                            }
                            if (touchFiles)
                            {
                                dirPair.Destination.Create();
                                if (copier.Destination.Exists is false)
                                    copier.Destination.Create();

                                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                                continue;
                            }

                            // else -> perform copy
                            await multiThreadedController.WaitAsync(cancellationToken);
                            OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                            lastCopyTask = PerformCopyOrMove(dirPair, copier, resultsBuilder, multiThreadedController, runningTasks, cancellationToken);

                            if (lastCopyTask.Status < TaskStatus.RanToCompletion)
                                runningTasks[copier] = lastCopyTask;
                            else
                                await lastCopyTask; // acknowledge completion
                        }

                        // continueWIth source dir deletion if /MOVE and all copies completed successfully
                        if (moveDirs && lastCopyTask is not null)
                        {
                            directoryDeletionTasks![dirPair] = lastCopyTask.ContinueWith(t =>
                            {
                                try
                                {
                                    Directory.Delete(dirPair.Source.FullName, false);
                                }
                                catch { }
                                finally
                                {
                                    directoryDeletionTasks?.TryRemove(dirPair, out _);
                                }
                            }, cancellationToken);
                        }
                    }
                }

                if (runningTasks.IsEmpty == false)
                    await Task.WhenAll(runningTasks.Values);

                if (directoryDeletionTasks is not null && directoryDeletionTasks.IsEmpty == false)
                    await Task.WhenAll(directoryDeletionTasks.Values);

                // ── Completion ───────────────────────────────────────────────────────────
                RoboCopyResults results = resultsBuilder.GetResults();
                _lastResults = results;
                OnCommandCompleted?.Invoke(this, new RoboCommandCompletedEventArgs(results));
            }
            catch
            {
                _lastResults = resultsBuilder.GetResults();
                throw;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────
        private static bool MatchesFileFilters(Regex[] filters, string filePath)
        {
            return filters.Length == 0 || filters.Any(r => r.IsMatch(filePath));
        }

        private void PopulateDestinationChidren(HashSet<string> fileSet, HashSet<string> dirSet, DirectoryInfo root)
        {
            var fileFilters = GetFileFilterRegex();
            var dirExclusions = GetDirectoryRegexes();

            fileSet.Clear();
            dirSet.Clear();
            if (root.Exists == false) return;
            bool recursivePurge = CopyOptions.Mirror || (CopyOptions.Purge && (CopyOptions.CopySubdirectories || CopyOptions.CopySubdirectoriesIncludingEmpty));
            foreach (var child in root.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                // ignore logging path
                if (child.FullName.Equals(LoggingOptions.LogPath, comparisonType: StringComparison.InvariantCultureIgnoreCase)
                    || child.FullName.Equals(LoggingOptions.UnicodeLogPath, comparisonType: StringComparison.InvariantCultureIgnoreCase)
                    || child.FullName.Equals(LoggingOptions.AppendLogPath, comparisonType: StringComparison.InvariantCultureIgnoreCase)
                    || child.FullName.Equals(LoggingOptions.AppendUnicodeLogPath, comparisonType: StringComparison.InvariantCultureIgnoreCase)
                    ) 
                    continue;

                if (LoggingOptions.ReportExtraFiles || recursivePurge || MatchesFileFilters(fileFilters, child.FullName))
                    fileSet.Add(child.Name);
            }
            
            foreach (var child in root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                if (LoggingOptions.ReportExtraFiles  || recursivePurge || (dirExclusions.None(r => r.ShouldExcludeDirectory(child.FullName)) && MatchesFileFilters(fileFilters, child.FullName)) )
                    dirSet.Add(child.Name);
            }
        }
        private void PopulateSourceFiles(HashSet<string> fileSet, DirectoryInfo root)
        {
            var fileFilters = GetFileFilterRegex();
            fileSet.Clear();
            foreach (var child in root.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                if (MatchesFileFilters(fileFilters, child.FullName))
                    fileSet.Add(child.Name);
            }
        }


        private async Task PerformCopyOrMove(
            DirectoryPair dirPair, 
            IFileCopier copier,  
            ResultsBuilder resultsBuilder,
            SemaphoreSlim multiThreadedController,
            ConcurrentDictionary<IFileCopier, Task> runningTasks,
            CancellationToken cancellationToken)
        {
            if (CopyOptions.RemoveFileInformation) // [ /NOCOPY ]
                return;

            bool success = false;
            int tries = 0;
            int maxTries = RetryOptions.RetryCount <= 1 ? 1 : RetryOptions.RetryCount;
            TimeSpan retryWaitTime = maxTries > 1 ? RetryOptions.GetRetryWaitTime() : TimeSpan.Zero;

            while (success == false && tries < maxTries)
            {
                var attr = copier.Source.Attributes;
                var dt = copier.Source.LastWriteTimeUtc;
                
                tries++;
                try
                {
                    Directory.CreateDirectory(dirPair.Destination.FullName);
                    copier.ProgressUpdated += RaiseProgressUpdated;
                    if (CopyOptions.MoveFiles || CopyOptions.MoveFilesAndDirectories)
                            success = await copier.MoveAsync(true, cancellationToken).ConfigureAwait(false);
                        else
                            success = await copier.CopyAsync(true, cancellationToken).ConfigureAwait(false);
                        
                    if (success)
                        ApplyAttributes(copier.Destination.FullName, attr, dt);

                    success = true;
                    resultsBuilder.AddFileCopied(copier.ProcessedFileInfo);
                }
                catch (OperationCanceledException)
                {
                    throw; // let cancellation propagate cleanly
                }
                catch (Exception ex) when (success == false) // don't catch errors from the progress reporter or results builder.
                {
                    if (tries >= maxTries)
                    {
                        resultsBuilder.AddFileFailed(copier.ProcessedFileInfo, ex);
                    }
                    OnError?.Invoke(this, new ErrorEventArgs(ex, copier.Destination.FullName, DateTime.Now));
                    await Task.Delay(retryWaitTime, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    copier.ProgressUpdated -= RaiseProgressUpdated;
                    runningTasks.TryRemove(copier, out _);
                    multiThreadedController.Release();
                }
            }
        }

        private void ApplyAttributes(string destinationFile, FileAttributes sourceAttributes, DateTime sourceLastWriteTime )
        {
            bool d = CopyOptions.CopyAll || CopyOptions.CopyFilesWithSecurity || CopyOptions.CopyFlags.Contains('d', StringComparison.InvariantCultureIgnoreCase);
            bool a = CopyOptions.CopyAll || CopyOptions.CopyFilesWithSecurity || CopyOptions.CopyFlags.Contains('a', StringComparison.InvariantCultureIgnoreCase);
            bool t = d || CopyOptions.CopyAll || CopyOptions.CopyFilesWithSecurity || CopyOptions.CopyFlags.Contains('t', StringComparison.InvariantCultureIgnoreCase);
            //bool s = CopyOptions.CopyAll || CopyOptions.CopyFlags.Contains('s', StringComparison.InvariantCultureIgnoreCase);
            //bool o = CopyOptions.CopyAll || CopyOptions.CopyFlags.Contains('o', StringComparison.InvariantCultureIgnoreCase);
            //bool u = CopyOptions.CopyAll || CopyOptions.CopyFlags.Contains('u', StringComparison.InvariantCultureIgnoreCase);

            var attributeToRemove = CopyOptions.GetRemoveAttributes() ?? FileAttributes.None;
            var attributesToAdd = CopyOptions.GetAddAttributes() ?? FileAttributes.None;
            var fAttributes = (File.GetAttributes(destinationFile) | attributesToAdd);
            if (a)
                fAttributes |= sourceAttributes;
            File.SetAttributes(destinationFile, fAttributes & ~attributeToRemove);
                
            if (t)
                File.SetLastWriteTimeUtc(destinationFile, sourceLastWriteTime);

        }

        /// <summary>
        /// Yields the root pair and (if isRecursive is true) all sub-directory pairs, mirroring Robocopy's directory tree walk.
        /// <br/> Only yields items from the Source tree
        /// </summary>
        private async  IAsyncEnumerable<(DirectoryPair dirPair, HashSet<string> subDirNames, int currentDepth)> EnumerateDirectoryPairsAsync(DirectoryPair root, int currentDepth, int maxDepth, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var subDirs = await Task.Run(() =>
            {
                var children = Directory.EnumerateDirectories(root.Source.FullName, "*", SearchOption.TopDirectoryOnly).ToArray();
                var set = children.Select(Path.GetFileName).ToHashSet();
                return (children, set);
            }, cancellationToken);

            yield return (root, subDirs.set, currentDepth);

            if (currentDepth >= maxDepth && SelectionOptions.ExcludeLonely == false)
                yield break;

            foreach (string sourceSubDir in subDirs.children)
            {
                while (IsPaused && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(50, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                string relative = Path.GetRelativePath(root.Source.FullName, sourceSubDir);
                string destSubDir = Path.Combine(root.Destination.FullName, relative);

                // Adjust to however your IDirectoryPair is constructed
                var subPair = new DirectoryPair(sourceSubDir, destSubDir);

                // accomodates reporting of extra directories without recursing into them
                if (SelectionOptions.ExcludeLonely && subPair.IsLonely())
                {
                    yield return (subPair, [], currentDepth + 1);
                    continue;
                }

                // recurse into the child directory
                await foreach (var child in EnumerateDirectoryPairsAsync(subPair, currentDepth + 1, maxDepth, cancellationToken))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Creates <see cref="IFileCopier"/> instances for every source file in
        /// <paramref name="dirPair"/> using each factory in <c>_copierFactories</c>.
        /// The factory decides the copier implementation; we just enumerate source files
        /// and hand each <see cref="FileInfo"/> pair to the factory.
        /// </summary>
        private async IAsyncEnumerable<IFileCopier> CreateFileCopiers(HashSet<string> fileNames, DirectoryPair dirPair, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var name in fileNames)
            {
                while (IsPaused && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(50, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // Map to the corresponding destination FileInfo
                string destPath = Path.Combine(dirPair.Destination.FullName, name);
                string sourcePath = Path.Combine(dirPair.Source.FullName, name);

                yield return copierFactory.Create(new FileInfo(sourcePath), new FileInfo(destPath), dirPair);
            }
        }

        private async Task ProcessExtras(DirectoryPair dirPair, bool purging, ResultsBuilder resultsBuilder, HashSet<string> destinationFiles, HashSet<string> sourceFiles, HashSet<string> sourceDirs, HashSet<string> destinationDirs, CancellationToken token)
        {
            // process extra files
            ProcessedFileInfo? pfInfo;
            bool misMatch;
            foreach (var name in destinationFiles)
            {
                // dont report as extra if was found in source pairs
                if (sourceFiles.Contains(name))
                    continue;

                misMatch = sourceDirs.Contains(name);

                var finfo = new FileInfo(Path.Combine(dirPair.Destination.FullName, name));
                pfInfo = new ProcessedFileInfo(finfo, this, status: misMatch ? ProcessedFileFlag.MisMatch : ProcessedFileFlag.ExtraFile);

                if (misMatch && !purging)
                    resultsBuilder.ReportMismatch(pfInfo);
                else
                    resultsBuilder.AddFileExtra(pfInfo);

                if (purging)
                    finfo.Delete();
            }

            // Detect Extra Directories (dest dirs not in source tree)
            foreach (var name in destinationDirs)
            {
                if (sourceDirs.Contains(name))
                    continue;

                string fullPath = Path.Combine(dirPair.Destination.FullName, name);
                pfInfo = new ProcessedFileInfo(fullPath, FileClassType.NewDir, fileClass: Configuration.LogParsing_ExtraDir, purging ? -1 : 0);

                if (misMatch = sourceFiles.Contains(name))
                {
                    pfInfo.SetDirectoryClass(ProcessedDirectoryFlag.MisMatch, Configuration);
                    resultsBuilder.ReportMismatch(pfInfo);
                }
                else
                {
                    resultsBuilder.AddDirExtra(pfInfo);
                }

                if (purging)
                {
                    PurgeExtraDirectory(fullPath, resultsBuilder, token);
                }
            }
        }

        /// <summary>
        /// Recursively reports and deletes an extra destination directory and all its contents.
        /// Mirrors RoboCopy's behaviour: files are counted as Extra/Purged before the directory is deleted.
        /// </summary>
        /// <returns>true is purged, otherwise false</returns>
        private bool PurgeExtraDirectory(string destDir, ResultsBuilder resultsBuilder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileInclusions = GetFileExclusionRegex();
            var fileExclusions = GetFileExclusionRegex();
            var dirExclusions = GetDirectoryRegexes();

            bool success = true;

            // Report and count each file inside the extra directory before deleting
            foreach (var file in Directory.EnumerateFiles(destDir, "*", SearchOption.TopDirectoryOnly))
            {
                if (fileExclusions.Any(r => r.IsMatch(file)))
                {
                    success = false;
                    continue;
                }
                    
                if (MatchesFileFilters(fileInclusions, file))
                {
                    var fileInfo = new FileInfo(file);
                    var rPath = Path.GetRelativePath(CopyOptions.Destination, file);
                    var pfi = new ProcessedFileInfo(rPath, FileClassType.File, fileClass: Configuration.LogParsing_ExtraFile, size: fileInfo.Length);
                    // Mark as extra/purged in results
                    resultsBuilder.AddFilePurged(pfi);
                    OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(pfi));
                    continue;
                }
                success = false;
            }

            

            // Recurse into subdirectories of this extra dir
            foreach (var subDir in Directory.EnumerateDirectories(destDir, "*", SearchOption.TopDirectoryOnly).Where(path => dirExclusions.None(r => r.ShouldExcludeDirectory(path))))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extraInfo = new ProcessedFileInfo(subDir, FileClassType.NewDir, fileClass: Configuration.LogParsing_ExtraDir, size: -1);
                resultsBuilder.AddDir(extraInfo);
                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(extraInfo));
                success &= PurgeExtraDirectory(subDir, resultsBuilder, cancellationToken);
            }

            // Now delete the whole tree
            if (success)
            {
                try
                {
                    Directory.Delete(destDir, success);
                }
                catch (Exception e)
                {
                    OnCommandError?.Invoke(this, new CommandErrorEventArgs($"Unable to purge directory: {destDir}", e));
                    return false;
                }
            }
            return success;
        }

    }
}
#endif