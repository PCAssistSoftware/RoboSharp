﻿using System;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using RoboSharp.Extensions.Windows;
using System.Text;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// Class that extends <see cref="FilePair"/> to implement Copy/Move async methods via CopyFileEx.
    /// <br/>This class is only usable on a Windows platform!
    /// </summary>
    public partial class CopyFileEx : AbstractFileCopier
    {
        private bool _isCopied;
        private bool _isMoving;
        private bool _wasCancelled;
        private bool _disposed;
        private CancellationTokenSource _cancellationSource;

        /// <summary>
        /// Create a new FileCopier from the supplied file paths
        /// </summary>
        /// <inheritdoc cref="FilePair.FilePair(FileInfo, FileInfo, IDirectoryPair)"/>
        public CopyFileEx(FileInfo source, FileInfo destination, IDirectoryPair parent = null) : base(source, destination, parent)
        { }

        /// <summary>
        /// Create a new FileCopier from the supplied file paths
        /// </summary>
        /// <inheritdoc cref="FilePair.FilePair(string, string, IDirectoryPair)"/>
        public CopyFileEx(string source, string destination, IDirectoryPair parent = null) : base(source, destination, parent)
        { }

        /// <summary>
        /// Create a new FileCopier from the provided IFilePair
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <inheritdoc cref="FilePair.FilePair(IFilePair, IDirectoryPair)"/>
        public CopyFileEx(IFilePair filePair, IDirectoryPair parent = null) : base(filePair, parent)
        { }

        /// <summary>
        /// The options to use when performing the copy operation - Note : Does not apply to move operations.
        /// </summary>
        /// <remarks>
        ///  - <see cref="CopyFileExOptions.FAIL_IF_EXISTS"/> is set by the 'overwrite' parameter of the <see cref="CopyAsync(bool, CancellationToken)"/> method
        ///  <br/> - <see cref="CopyFileExOptions.RESTARTABLE"/> must be set here if you wish to enable the <see cref="Pause"/> functionality.
        /// </remarks>
        public CopyFileExOptions CopyOptions { get; set; }

        /// <summary>
        /// Copied Status -> True if the copy action has been performed.
        /// </summary>
        public bool IsCopied
        {
            get { return _isCopied; }
            private set { SetProperty(ref _isCopied, value, nameof(IsCopied)); }
        }

        /// <inheritdoc/>
        public bool WasCancelled
        {
            get { return _wasCancelled; }
            private set { SetProperty(ref _wasCancelled, value, nameof(WasCancelled)); }
        }

        #region < Pause / Resume / Cancel >

        /// <summary>
        /// Stops the COPY operation using the 'STOP' argument. The copy task then enters an 'await Task.Delay(100)' loop until resumed or cancelled. Warning : During this period the file is not locked as CopyFileEx has released its hold.
        /// Upon being resumed, CopyFileEx will attempt to resume where it left off. 
        /// <para/> Only possible when <see cref="CopyFileExOptions.RESTARTABLE"/> is specified in the <see cref="CopyOptions"/>
        /// <br/> No effect on the MOVE operation.
        /// </summary>
        public override void Pause()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CopyFileEx));
            if (IsCopying && !_isMoving)
                IsPaused = CopyOptions.HasFlag(CopyFileExOptions.RESTARTABLE);
        }

        /// <summary>
        /// Resume a COPY operation
        /// </summary>
        public override void Resume()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CopyFileEx));
            if (IsCopying && IsPaused)
            {
                IsPaused = false;
            }
        }

        /// <summary>
        /// Request Cancellation immediately.
        /// </summary>
        public override void Cancel()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CopyFileEx));
            if (IsCopying && !(_cancellationSource?.IsCancellationRequested ?? true))
            {
                _cancellationSource?.Cancel();
            }
        }

        #endregion

        /// <summary> Sets up the flags that allow the read/write tasks to run. </summary>
        private void SetStarted()
        {
            StartDate = DateTime.Now;
            IsCopied = false;
            IsCopying = true;
            IsPaused = false;
            WasCancelled = false;
            Progress = 0;
        }

        /// <summary>
        /// Set IsCopying to FALSE <br/>
        /// Set EndDate <br/>
        /// Dispose of cancellation token
        /// </summary>
        /// <param name="isCopied">set <see cref="IsCopied"/></param>
        private void SetEnded(bool isCopied)
        {
            IsCopying = false;
            WasCancelled = _cancellationSource?.IsCancellationRequested ?? false;
            IsCopied = isCopied;
            IsPaused = false;
            EndDate = DateTime.Now;
            _cancellationSource?.Dispose();
            _cancellationSource = null;
        }

        /// <inheritdoc cref="CopyFileEx.CopyFileAsync(string, string, IProgress{double}, CopyFileExOptions, int, CancellationToken)"/>
        public override async Task<bool> CopyAsync(bool overwrite, CancellationToken token)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CopyFileEx));
            if (IsCopying) throw new InvalidOperationException("Copy/Move Operation Already in progress!");
            token.ThrowIfCancellationRequested();
            Refresh();
            if (!Source.Exists) throw new FileNotFoundException("Source File Not Found.", Source.FullName);
            if (!overwrite && Destination.Exists) throw new IOException("Destination file already exists");

            bool copied = false;
            Task updateTask = null;
            long fileSize = Source.Length;
            long totalBytesTransferred = 0;
            var options = overwrite ? CopyOptions &= ~CopyFileExOptions.FAIL_IF_EXISTS : CopyOptions | CopyFileExOptions.FAIL_IF_EXISTS;

            try
            {
                SetStarted();
                _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                Destination.Directory.Create();

                while (!copied && !_cancellationSource.IsCancellationRequested)
                {
                    if (IsPaused)
                    {
                        await Task.Delay(100, _cancellationSource.Token); // Do nothing while waiting to unpause - This only applies if started in restartable mode
                    }
                    else
                    {
                        //Writer - consumes a thread while not paused
                        try
                        {
                            copied = await Task.Run(() =>
                            {
                                var result = CopyFileEx.InvokeCopyFileEx(Source.FullName, Destination.FullName, progressRecorder, options);
                                if (!result) Win32Error.ThrowLastError(Source.FullName, Destination.FullName);
                                return result;
                            }, _cancellationSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (IsPaused) { }
                    }
                }
            }
            finally
            {
                _cancellationSource?.Cancel();
                if (updateTask != null) await updateTask.CatchCancellation(false);
                Destination.Refresh();
                if (copied && Progress != 100) OnProgressUpdated(100);
                SetEnded(copied);
            }
            return IsCopied;

            // Helper Routines
            double CalcProgress() => (double)100 * totalBytesTransferred / fileSize;

            CopyProgressCallbackResult progressRecorder(long totalFileSize, long byteCount, long streamSize, long streamBytesTransferred, uint streamID, CopyProgressCallbackReason reason)
            {
                if (byteCount < totalBytesTransferred) // account for resuming a stopped operation
                    totalBytesTransferred += byteCount;
                else
                    totalBytesTransferred = byteCount;

                updateTask ??= Task.Run(ProgressReportingTask, _cancellationSource.Token);

                if (_cancellationSource.Token.IsCancellationRequested)
                    return CopyProgressCallbackResult.CANCEL;
                return IsPaused ? CopyProgressCallbackResult.STOP : CopyProgressCallbackResult.CONTINUE;
            }

            // Updater - asynchronous background task
            async Task ProgressReportingTask()
            {
                while (totalBytesTransferred < Source.Length)
                {
                    if (!IsPaused) OnProgressUpdated(CalcProgress());
                    _cancellationSource.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100, _cancellationSource.Token);
                }
            }
        }


        /// <inheritdoc cref="FileFunctions.MoveFileAsync(string, string, IProgress{double}, int, bool, CancellationToken)"/>
        public override async Task<bool> MoveAsync(bool overWrite, CancellationToken token)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CopyFileEx));
            if (IsCopying) throw new InvalidOperationException("Copy/Move Operation Already in progress!");

            if (!File.Exists(Source.FullName))
            {
                throw new FileNotFoundException("File Not Found", Source.FullName);
            }
            bool destExists = File.Exists(Destination.FullName);
            if (destExists && !overWrite)
            {
                throw new IOException("Destination already exists");
            }

            CancellationTokenRegistration tokenRegister = token.Register(Cancel);
            SetStarted();
            _isMoving = true;
            bool moved = false;
            try
            {
                //Check if Source & Destination are on same physical drive
                if (this.IsLocatedOnSameDrive())
                {
                    Directory.CreateDirectory(Destination.DirectoryName);
                    if (destExists) Destination.Delete();
                    File.Move(Source.FullName, Destination.FullName);
                    moved = true;
                }
                else
                {
                    moved = await FileFunctions.MoveFileAsync(Source.FullName, Destination.FullName, new Progress<double>(OnProgressUpdated), 100, overWrite, _cancellationSource.Token);
                }
            }
            finally
            {
                tokenRegister.Dispose();
                _isMoving = false;
                SetEnded(isCopied: moved);
                if (moved)
                {
                    Source.Refresh();
                    Destination.Refresh();
                    if (Progress != 100) OnProgressUpdated(100);
                }
            }
            return moved;
        }

#region < Dispose >

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                Cancel();
                _cancellationSource?.Dispose();
                _cancellationSource = null;

                // TODO: set large fields to null
                _disposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /// <summary>
        /// 
        /// </summary>
        ~CopyFileEx()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}