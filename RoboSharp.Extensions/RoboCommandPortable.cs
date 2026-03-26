
using RoboSharp.EventArgObjects;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Extensions.Options;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
#if !(NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER || NET8_0_OR_GREATER)
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public event RoboCommand.FileProcessedHandler? OnFileProcessed;
        public event RoboCommand.CommandErrorHandler? OnCommandError;
        public event RoboCommand.ErrorHandler? OnError;
        public event RoboCommand.CommandCompletedHandler? OnCommandCompleted;
        public event RoboCommand.CopyProgressHandler? OnCopyProgressChanged;
        public event RoboCommand.ProgressUpdaterCreatedHandler? OnProgressEstimatorCreated;
        public event UnhandledExceptionEventHandler? TaskFaulted;
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

        private string name = string.Empty;
        private bool isPaused = false, isRunning = false, isScheduled = false, isCancelled = false, stopIfDisposing;
        private CopyOptions _CopyOptions = new();
        private SelectionOptions _SelectionOptions = new();
        private RetryOptions _RetryOptions = new();
        private LoggingOptions _LoggingOptions = new();
        private JobOptions _JobOptions = new();
        private RoboSharpConfiguration _Configuration = new();
        private IProgressEstimator? progressEstimator;
        private CancellationTokenSource? _CancellationTokenSource;
        private SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);
        private RoboCopyResults? _lastResults;

        public string Name { get => name; private set => SetProperty(ref name, value, nameof(Name)); }
        public bool IsPaused { get => isPaused; private set => SetProperty(ref isPaused, value, nameof(IsPaused)); }
        public bool IsRunning { get => isRunning; private set => SetProperty(ref isRunning, value, nameof(IsRunning)); }
        public bool IsScheduled { get => isScheduled; private set => SetProperty(ref isScheduled, value, nameof(IsScheduled)); }
        public bool IsCancelled { get => isCancelled; private set => SetProperty(ref isCancelled, value, nameof(IsCancelled)); }
        public bool StopIfDisposing { get => stopIfDisposing; private set => SetProperty(ref stopIfDisposing, value, nameof(StopIfDisposing)); }
        public IProgressEstimator? IProgressEstimator { get => progressEstimator; private set => SetProperty(ref progressEstimator, value, nameof(IProgressEstimator)); }
        public string CommandOptions => GenerateParameters();
        public CopyOptions CopyOptions { get => _CopyOptions; set => SetProperty(ref _CopyOptions, value, nameof(CopyOptions)); }
        public SelectionOptions SelectionOptions { get => _SelectionOptions; set => SetProperty(ref _SelectionOptions, value, nameof(SelectionOptions)); }
        public RetryOptions RetryOptions { get => _RetryOptions; set => SetProperty(ref _RetryOptions, value, nameof(RetryOptions)); }
        public LoggingOptions LoggingOptions { get => _LoggingOptions; set => SetProperty(ref _LoggingOptions, value, nameof(LoggingOptions)); }
        public JobOptions JobOptions { get => _JobOptions; set => SetProperty(ref _JobOptions, value, nameof(JobOptions)); }
        public RoboSharpConfiguration Configuration { get => _Configuration; set => SetProperty(ref _Configuration, value, nameof(Configuration)); }


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



#if !(NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER || NET8_0_OR_GREATER)

        private Task Run(string domain, string username, string password, Action? preRunAction = null, Action? postRunAction = null)
        {
            ThrowUnsupportedFrameworkException();
            return Task.CompletedTask;
        }

#else

        private Regex[] GetFileExclusionRegex() => excludedFiledRegex??= SelectionOptions.GetExcludedFileRegex();
        private Regex[]? excludedFiledRegex;

        private Regex[] GetFileFilterRegex() => fileFilterRegex ??= CopyOptions.GetFileFilterRegex();
        private Regex[]? fileFilterRegex;

        private DirectoryRegex[] GetDirectoryRegexes() => directoryRegexes ??= SelectionOptions.GetExcludedDirectoryRegex();
        private DirectoryRegex[]? directoryRegexes;

        private void EvaluateFilePair(IFileCopier pair) => pair.ProcessFilePairAgainstCommandOptions(this, GetFileFilterRegex(), GetFileExclusionRegex());
        private void EvaluateDirPair(DirectoryPair pair) => pair.EvaluateDirectoryPair(this, GetDirectoryRegexes());

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

            
            var progressReporter = new ProgressEstimator(this);  // live IStatistic feeds for the UI
            var resultsBuilder = new ResultsBuilder(this);  // tracks counts/bytes per category

            // Fire OnProgressEstimatorCreated so subscribers (e.g. a progress bar) can
            // attach to the estimator's IStatistic change events before work begins.
            this.IProgressEstimator = progressReporter;
            OnProgressEstimatorCreated?.Invoke(this, new ProgressEstimatorCreatedEventArgs(progressReporter));

            bool includeEmpty = this.CopyOptions.CopySubdirectoriesIncludingEmpty || CopyOptions.Mirror;
            bool recurse = this.CopyOptions.CopySubdirectories || this.CopyOptions.CopySubdirectoriesIncludingEmpty || CopyOptions.Mirror;
            int maxDepth = !recurse ? 1 : CopyOptions.Depth == 0 ? int.MaxValue : CopyOptions.Depth;
            var rootPair = new DirectoryPair(this.CopyOptions.Source, this.CopyOptions.Destination);
            bool listOnly = LoggingOptions.ListOnly;
            bool touchFiles = CopyOptions.CreateDirectoryAndFileTree;
            

            SemaphoreSlim multiThreadedController = new SemaphoreSlim(CopyOptions.MultiThreadedCopiesCount >= 128 ? 128 : CopyOptions.MultiThreadedCopiesCount <= 1 ? 1 : CopyOptions.MultiThreadedCopiesCount);
            Dictionary<string, ProcessedFileInfo> infoDict = new();
            ConcurrentDictionary<IFileCopier, Task> runningTasks = new();

            try
            {

                // ── Pass 1: pre-scan to seed ProgressEstimator ────────────────────────────
                // Robocopy reports totals before starting transfers; we replicate that here
                // so ProgressEstimator can give accurate percentage estimates from the start.

                await foreach (var dirPair in EnumerateDirectoryPairsAsync(rootPair, 1, maxDepth, cancellationToken))
                {
                    // Tell the estimator a directory exists on the source side
                    EvaluateDirPair(dirPair);
                    progressReporter.AddDir(dirPair.ProcessedFileInfo);

                    infoDict[dirPair.Source.FullName] = dirPair.ProcessedFileInfo;

                    await foreach (IFileCopier copier in CreateFileCopiers(dirPair, cancellationToken))
                    {
                        dirPair.ProcessedFileInfo.Size++;
                    }
                }

                // ── Pass 2: process each directory ───────────────────────────────────────
                await foreach (var dirPair in EnumerateDirectoryPairsAsync(rootPair, 1, maxDepth, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (infoDict.TryGetValue(dirPair.Source.FullName, out var pInfo))
                    {
                        dirPair.ProcessedFileInfo = pInfo;
                        infoDict.Remove(dirPair.Source.FullName); // key will never be read again
                    }
                    else
                    {
                        EvaluateDirPair(dirPair);
                    }

                    progressReporter.AddDir(dirPair.ProcessedFileInfo);
                    resultsBuilder.AddDir(dirPair.ProcessedFileInfo);
                    OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(dirPair.ProcessedFileInfo));

                    if (includeEmpty)
                        dirPair.Destination.Create();

                    // ── 2a. Source files ──────────────────────────────────────────────────

                    await foreach (IFileCopier copier in CreateFileCopiers(dirPair, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Evaluate populates copier.ProcessedFileInfo (FileClass, Size, Name)
                        // AND sets ShouldCopy / ShouldPurge based on this IRoboCommand's options.
                        EvaluateFilePair(copier);

                        ProcessedFileInfo fileInfo = copier.ProcessedFileInfo;

                        if (copier.ShouldCopy)
                        {
                            if (listOnly)
                            {
                                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                                progressReporter.AddFileCopied(fileInfo);
                                resultsBuilder.AddFileCopied(fileInfo);
                            }
                            else if (touchFiles)
                            {
                                dirPair.Destination.Create();
                                if (copier.Destination.Exists is false)
                                    copier.Destination.Create();

                                progressReporter.AddFileCopied(fileInfo);
                                resultsBuilder.AddFileCopied(fileInfo);
                            }
                            else
                            {
                                await multiThreadedController.WaitAsync(cancellationToken);

                                // Announce the file before the transfer (mirrors Robocopy's pre-copy log line)
                                OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                                runningTasks[copier] = Task.Run(async () =>
                                {
                                    try
                                    {
                                        Directory.CreateDirectory(dirPair.Destination.FullName);
                                        copier.ProgressUpdated += RaiseProgressUpdated;
                                        if (CopyOptions.MoveFiles || CopyOptions.MoveFilesAndDirectories)
                                            await copier.MoveAsync(true, cancellationToken).ConfigureAwait(false);
                                        else
                                            await copier.CopyAsync(true, cancellationToken).ConfigureAwait(false);

                                        progressReporter.AddFileCopied(fileInfo);
                                        resultsBuilder.AddFileCopied(fileInfo);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        throw; // let cancellation propagate cleanly
                                    }
                                    catch (Exception ex)
                                    {
                                        resultsBuilder.AddFileFailed(fileInfo);
                                        OnError?.Invoke(this, new ErrorEventArgs(ex, copier.Destination.FullName, DateTime.Now));
                                    }
                                    finally
                                    {
                                        copier.ProgressUpdated -= RaiseProgressUpdated;
                                        runningTasks.TryRemove(copier, out _);
                                        multiThreadedController.Release();
                                    }
                                }, cancellationToken);
                            }
                        }
                        else
                        {
                            // File was evaluated but not copied (skipped/extra/same/newer/older).
                            // Still report it so consumers see the full picture.
                            progressReporter.AddFileSkipped(fileInfo);
                            resultsBuilder.AddFileSkipped(fileInfo);
                            OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
                        }
                    }

                    // ── 2b. Purge candidates (destination-only files) ────────────────────

                    await foreach (IFileCopier purgeCopier in CreatePurgeCandidates(dirPair, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        EvaluateFilePair(purgeCopier);
                        ProcessedFileInfo purgeInfo = purgeCopier.ProcessedFileInfo;

                        if (purgeCopier.ShouldPurge)
                        {
                            OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(purgeInfo));

                            try
                            {
                                purgeCopier.Destination.Delete();
                                progressReporter.AddFileExtra(purgeInfo);
                                resultsBuilder.AddFilePurged(purgeInfo);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                resultsBuilder.AddFileFailed(purgeInfo);
                                OnCommandError?.Invoke(this, new CommandErrorEventArgs(ex.Message, ex));
                            }
                        }
                        else
                        {
                            // Extra file is present but purge is disabled — treat as skipped/extra
                            progressReporter.AddFileExtra(purgeInfo);
                            resultsBuilder.AddFileExtra(purgeInfo);
                            OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(purgeInfo));
                        }
                    }
                }

                if (runningTasks.IsEmpty == false)
                    await Task.WhenAll(runningTasks.Values);

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

        /// <summary>
        /// Yields the root pair and (if recurse is true) all sub-directory pairs,
        /// mirroring Robocopy's directory tree walk.
        /// </summary>
        private async  IAsyncEnumerable<DirectoryPair> EnumerateDirectoryPairsAsync(DirectoryPair root, int currentDepth, int maxDepth, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return root;

            if (currentDepth >= maxDepth)
                yield break;

            // Offload the synchronous Directory.EnumerateDirectories call onto the thread pool
            // so the caller's await loop stays non-blocking.
            IEnumerable<string> subDirs = await Task.Run(() => Directory.EnumerateDirectories(root.Source.FullName, "*", SearchOption.TopDirectoryOnly), cancellationToken)
                .ConfigureAwait(false);

            foreach (string sourceSubDir in subDirs)
            {
                while (IsPaused && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(50, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                string relative = Path.GetRelativePath(root.Source.FullName, sourceSubDir);
                string destSubDir = Path.Combine(root.Destination.FullName, relative);

                // Adjust to however your IDirectoryPair is constructed
                var subPair = new DirectoryPair(sourceSubDir, destSubDir);

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
        private async IAsyncEnumerable<IFileCopier> CreateFileCopiers(IDirectoryPair dirPair, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IEnumerable<FileInfo> sourceFiles = await Task.Run(() => dirPair.Source.EnumerateFiles("*", SearchOption.TopDirectoryOnly), cancellationToken)
                .ConfigureAwait(false);

            foreach (FileInfo sourceFile in sourceFiles)
            {
                while (IsPaused && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(50, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // Map to the corresponding destination FileInfo
                string destPath = Path.Combine(dirPair.Destination.FullName, sourceFile.Name);
                var destFile = new FileInfo(destPath);

                yield return copierFactory.Create(sourceFile, destFile, dirPair);
            }
        }

        /// <summary>
        /// Creates purge-candidate <see cref="IFileCopier"/> instances for files that
        /// exist in the destination but not the source (i.e. "extra" files).
        /// </summary>
        private async IAsyncEnumerable<IFileCopier> CreatePurgeCandidates(IDirectoryPair dirPair, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!Directory.Exists(dirPair.Destination.FullName))
                yield break;

            IEnumerable<FileInfo> destFiles = await Task.Run(() => dirPair.Destination.EnumerateFiles("*", SearchOption.TopDirectoryOnly), cancellationToken)
                .ConfigureAwait(false);

            foreach (FileInfo destFile in destFiles)
            {
                while (IsPaused && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(50, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                string sourcePath = Path.Combine(dirPair.Source.FullName, destFile.Name);

                if (File.Exists(sourcePath))
                    continue;

                var sourceFile = new FileInfo(sourcePath);
                yield return copierFactory.Create(sourceFile, destFile, dirPair);
            }
        }
#endif
    }
}
