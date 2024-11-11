using RoboSharp.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Extensions.Options;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// This IRoboCommand is a collection of <see cref="IFileCopier"/> objects that will be copied to their respective destinations in the order they were added.
    /// </summary>
    /// <remarks>
    /// IRoboCommand is partially implemented. 
    /// <br/> - CopyOptions is partially Implemented
    /// <br/> - RetryOptions and LoggingOptions are implemented.
    /// <br/> - SelectionOptions is not implemented because the list of files to copy/move are explicitly passed in.
    /// <br/> - JobOptions is not implemented.
    /// </remarks>
    public class BatchCommand : AbstractIRoboCommand, INotifyPropertyChanged, IRoboCommand, IEnumerable<IFileCopier>, IDisposable
    {
        private readonly IFileCopierFactory _copierFactory;
        private readonly List<IFileCopier> _fileCopiers;
        private CancellationTokenSource _cancellationSource;
        private bool disposedValue;

        /// <summary>
        /// Create a new <see cref="BatchCommand"/>.
        /// <br/>When running on a windows platform, uses <see cref="Windows.CopyFileExFactory"/>. 
        /// <br/>Otherwise falls back to <see cref="StreamedCopierFactory.DefaultFactory"/>.
        /// </summary>
        public BatchCommand() : this(VersionManager.IsPlatformWindows ? new Windows.CopyFileExFactory() : StreamedCopierFactory.DefaultFactory) { }

        /// <summary>
        /// Create a new <see cref="IFileCopierFactory"/> using the specified <paramref name="copierFactory"/>
        /// </summary>
        /// <param name="copierFactory">The factory used to create additional copiers</param>`
        public BatchCommand(IFileCopierFactory copierFactory) : base()
        {
            if (copierFactory is null) throw new ArgumentNullException(nameof(copierFactory));
            _copierFactory = copierFactory;
            _fileCopiers = new List<IFileCopier>();
            base.CopyOptions.MultiThreadedCopiesCount = 1;
        }

        /// <summary>
        /// Create a new FileCopierCommand with the provided copiers
        /// </summary>
        /// <param name="copierFactory">The factory used to create additional copiers</param>
        /// <param name="copiers">the collection of copiers to queue</param>
        public BatchCommand(IFileCopierFactory copierFactory, params IFileCopier[] copiers) : this(copierFactory)
        {
            AddCopiers(copiers);
        }

        /// <inheritdoc cref="BatchCommand.BatchCommand(IFileCopierFactory, IFileCopier[])"/>
        public BatchCommand(IFileCopierFactory copierFactory, IEnumerable<IFileCopier> copiers) : this(copierFactory)
        {
            AddCopiers(copiers);
        }

        #region < Properties >

        private ReadOnlyCollection<IFileCopier> ReadOnlyFileCopiers;

        /// <summary>
        /// The FileCopier objects that get run with this Start method is called
        /// </summary>
        public ReadOnlyCollection<IFileCopier> FileCopiers
        {
            get
            {
                if (ReadOnlyFileCopiers is null)
                    ReadOnlyFileCopiers = new ReadOnlyCollection<IFileCopier>(_fileCopiers);
                return ReadOnlyFileCopiers;
            }
        }

        /// <summary>
        /// Factory to be used by <see cref="AddCopier(string, string)"/> and <see cref="AddCopier(FileInfo, DirectoryInfo)"/>
        /// </summary>
        /// <remarks>
        /// If not specified, will use the default factory
        /// </remarks>
        public IFileCopierFactory FileCopierFactory
        {
            get => _copierFactory;
        }

        /// <summary>
        /// Not fully implemented. Relevant options include:
        /// <br/> - MultiThreadedCopiesCount (Number of files that can copy at once)
        /// <br/> - Mirror ( Forces to copy files, ignored MoveFiles option)
        /// <br/> - MoveFiles ( Enable Moving instead of Copying )
        /// <br/> - Exclude Newer / Exclude Older
        /// </summary>
        /// <inheritdoc/>
        new public CopyOptions CopyOptions
        {
            get => base.CopyOptions;
            set => base.CopyOptions = value;
        }

        /// <summary>
        /// Not Implemented - Files are manually added to the queue.
        /// </summary>
        new public SelectionOptions SelectionOptions { get => base.SelectionOptions; set => base.SelectionOptions = value; }

        #endregion

        private int CopyOperationsActive => this.Where(o => o.IsCopying).Count();


        /// <summary>
        /// Add the copiers to the list
        /// </summary>
        public void AddCopiers(params IFileCopier[] copier)
        {
            _fileCopiers.AddRange(copier);
        }

        /// <summary>
        /// Add the copiers to the list
        /// </summary>
        public void AddCopiers(params IFilePair[] copiers)
        {
            AddCopiers(copiers.AsEnumerable());
        }

        /// <summary>
        /// Add the file pairs to the list
        /// </summary>
        public void AddCopiers(IEnumerable<IFilePair> filePairs)
        {
            foreach (var fp in filePairs)
            {
                if (fp is IFileCopier cp)
                    _fileCopiers.Add(cp);
                else if (fp != null)
                {
                    cp = _copierFactory.Create(fp);
                    _fileCopiers.Add(cp);
                }
            }
        }

        /// <summary>
        /// Add the copiers to the list
        /// </summary>
        /// <param name="copier"></param>
        public void AddCopiers(IEnumerable<IFileCopier> copier)
        {
            _fileCopiers.AddRange(copier);
        }

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> and add it to the list
        /// </summary>
        /// <inheritdoc cref="IFileCopierFactory.Create(string, string)"/>
        /// <returns>The newly created <see cref="IFileCopier"/></returns>
        public IFileCopier AddCopier(string source, string destination)
        {
            IFileCopier f = _copierFactory.Create(source, destination);
            this.AddCopiers(f);
            return f;
        }

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> and add it to the list
        /// </summary>
        /// <inheritdoc cref="IFileCopierFactory.Create(string, string)"/>
        /// <returns>The newly created <see cref="IFileCopier"/></returns>
        public IFileCopier AddCopier(IFilePair filePair)
        {
            if (filePair is IFileCopier cp)
            {
                AddCopier(cp);
                return cp;
            }
            else
            {
                IFileCopier f = _copierFactory.Create(filePair);
                this.AddCopiers(f);
                return f;
            }

        }

        /// <summary>
        /// Create a new <see cref="IFileCopier"/> and add it to the list
        /// </summary>
        /// <inheritdoc cref="IFileCopierFactory.Create(string, string)"/>
        /// <returns>The newly created <see cref="IFileCopier"/></returns>
        public IFileCopier AddCopier(FileInfo source, DirectoryInfo destinationDirectory)
        {
            IFileCopier f = _copierFactory.Create(source, destinationDirectory);
            this.AddCopiers(f);
            return f;
        }

        /// <summary>
        /// Gets the results builder that will be used by the <see cref="Start(string, string, string)"/> method
        /// </summary>
        /// <returns></returns>
        protected virtual ResultsBuilder GetResultsBuilder() { return new ResultsBuilder(this); }

        /// <inheritdoc/>
        public override Task Start(string domain = "", string username = "", string password = "")
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(BatchCommand));
            if (IsRunning) throw new InvalidOperationException("Already Running!");
            IsCancelled = false;
            IsRunning = true;
            _cancellationSource = new CancellationTokenSource();
            var resultsBuilder = GetResultsBuilder();
            RaiseOnProgressEstimatorCreated(resultsBuilder.ProgressEstimator);
            
            var moveOp = Task.Run(async () =>
            {
                LoggingOptions.DeleteLogFiles();
                LoggingOptions.EnsureLogFileDirectoriesCreated();
                if (LoggingOptions.NoJobHeader is false) CreateHeader(resultsBuilder);

                List<Task> queue = new List<Task>();
                Task copyTask = null;
                var evaluator = new PairEvaluator(this);

                bool move = !CopyOptions.Mirror && (CopyOptions.MoveFiles | CopyOptions.MoveFilesAndDirectories);
                int retries = RetryOptions.RetryCount < 0 ? 0 : RetryOptions.RetryCount;
                TimeSpan retryWaitTime = new TimeSpan(0, 0, RetryOptions.RetryWaitTime <= 0 ? 1 : RetryOptions.RetryWaitTime);

                foreach (IFileCopier copier in _fileCopiers)
                {
                    if (_cancellationSource.IsCancellationRequested) 
                        break;

                    copier.Destination.Refresh();
                    copier.ShouldCopy = evaluator.ShouldCopyFile(copier);
                    //if (copier.Parent.ProcessedFileInfo is null)
                    //    copier.Parent.ProcessedFileInfo = new ProcessedFileInfo(Path.GetDirectoryName(copier.Source.FullName), FileClassType.NewDir, this.Configuration.GetDirectoryClass(ProcessedDirectoryFlag.NewDir), 1);

                    //Check if it can copy, or if there is a need to copy.
                    bool canCopy = !copier.IsExtra() && (copier.IsLonely() || !(copier.IsSameDate() && copier.Source.Length == copier.Destination.Length));
                    if (canCopy && SelectionOptions.ExcludeNewer)
                        canCopy = !copier.IsDestinationNewer();
                    if (canCopy && SelectionOptions.ExcludeOlder)
                        canCopy = !copier.IsSourceNewer();

                    //Add the task to the list
                    if (canCopy)
                    {
                        copyTask = PerformCopyOperation(copier, move, true, retries, retryWaitTime, resultsBuilder);
                        queue.Add(copyTask);
                    }
                    else
                    {
                        resultsBuilder.AddFile(copier.ProcessedFileInfo);    
                    }
                    RaiseOnFileProcessed(copier.ProcessedFileInfo);

                    // wait for copy operations to do their thing, up to the max multithreaded copies count
                    bool wasPaused = false;
                    while (copier != this.Last() && (CopyOperationsActive >= CopyOptions.MultiThreadedCopiesCount | IsPaused))
                    {
                        if (_cancellationSource.IsCancellationRequested)
                        {
                            break;
                        }
                        else if (IsPaused)
                        {
                            wasPaused = true;
                        }
                        else if (wasPaused)
                        {
                            wasPaused = false;
                        }
                        await Task.Delay(100, _cancellationSource.Token).CatchCancellation(false);
                    }
                }
                await Task.WhenAll(queue).CatchCancellation(false);
            }, _cancellationSource.Token);

            return moveOp.ContinueWith(MoveOpContinuation).Unwrap();

            Task MoveOpContinuation(Task moveOperation)
            {
                IsCancelled = _cancellationSource?.IsCancellationRequested ?? true;
                IsRunning = false;
                _cancellationSource = null;

                if (IsCancelled) resultsBuilder.SetExitStatus(Results.RoboCopyExitCodes.Cancelled);
                var results = resultsBuilder.GetResults();
                resultsBuilder.Dispose();
                resultsBuilder = null;
                SaveResults(results);
                RaiseOnCommandCompleted(results);

                return moveOperation;
            }

        }

        private void CreateHeader(ResultsBuilder resultsBuilder)
        {
            var div = ResultsBuilder.Divider;

            string[] header = new string[] {
                    div,
                    $"\t      IRoboCommand : '{GetType()}'",
                    $"\t   Results Builder : '{resultsBuilder.GetType()}'",
                    $"\tIFileCopierFactory : {this._copierFactory.GetType()}",
                    div, 
                    Environment.NewLine,
                    $"            Started : {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}",
                    $"         File Count : {_fileCopiers.Count}",
                    $" Copy/Retry Options : {CopyOptions.Parse(true)} {RetryOptions}",
                    $"    Logging Options : {LoggingOptions}",
                    Environment.NewLine,
                    div,
                    Environment.NewLine,
            };

            resultsBuilder.Print(header);
            foreach (string line in header)
                RaiseOnFileProcessed(line);
        }

        private async Task PerformCopyOperation(IFileCopier copier, bool isMoving, bool overWrite, int numberOfRetries, TimeSpan retryWaitTime, ResultsBuilder resultsBuilder)
        {
            int tries = 0;
            bool success = false;
            copier.ProgressUpdated += RaiseOnCopyProgressChanged;
            try
            {
                while (!success && tries <= numberOfRetries && !_cancellationSource.IsCancellationRequested)
                {
                    tries++;
                    try
                    {
                        if (!LoggingOptions.ListOnly)
                        {
                            copier.Destination.Directory.Create();
                            if (isMoving)
                                await copier.MoveAsync(overWrite, _cancellationSource.Token);
                            else
                                await copier.CopyAsync(overWrite, _cancellationSource.Token);
                            success = true;
                            resultsBuilder.AverageSpeed.Average(copier.Destination.Length, copier.EndDate - copier.StartDate);
                        }
                        resultsBuilder.ProgressEstimator.AddFileCopied(copier.ProcessedFileInfo); // Add directly to results, already written to logs
                    }
                    catch (OperationCanceledException)
                    {
                        resultsBuilder.AddSystemMessage($@"Copy Operation Cancelled --> {copier.Destination.FullName}");
                    }
                    catch (Exception e)
                    {
                        resultsBuilder.AddFileFailed(copier.ProcessedFileInfo, e);
                        RaiseOnError(new ErrorEventArgs(e, copier.Destination.FullName, DateTime.Now));
                        if (tries < numberOfRetries) await Task.Delay(retryWaitTime);
                    }
                }
            }
            finally
            {
                copier.ProgressUpdated -= RaiseOnCopyProgressChanged;
            }
        }

        /// <inheritdoc/>
        public override void Stop()
        {
            if (IsRunning && !IsCancelled)
            {
                foreach (var c in _fileCopiers)
                    c.Cancel();
                this._cancellationSource?.Cancel();
                IsCancelled = true;
            }
        }

        private void RaiseOnCopyProgressChanged(object sender, CopyProgressEventArgs e) => RaiseOnCopyProgressChanged(e);

        #region < IEnumerable >

        /// <inheritdoc/>
        public IEnumerator<IFileCopier> GetEnumerator()
        {
            return ((IEnumerable<IFileCopier>)_fileCopiers).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_fileCopiers).GetEnumerator();
        }

        #endregion

        #region < IDisposable >

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (StopIfDisposing && !(_cancellationSource?.IsCancellationRequested ?? true))
                        _cancellationSource?.Cancel();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FileCopierCommand()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        /// <inheritdoc/>
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
        }

        #endregion
    }

}
