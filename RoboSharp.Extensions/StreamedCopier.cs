﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// <see cref="IFileCopier"/> that uses a <see cref="System.IO.FileStream"/> to perform the copy operation
    /// <br/>This class is platform agnostic.
    /// </summary>
    public sealed class StreamedCopier : AbstractFileCopier, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The default buffer size used by FileStream.CopyToAsync()
        /// </summary>
        public const int DefaultBufferSize = 81920;

        CancellationTokenSource _cancellationSource;
        bool _isMoving;
        bool _wasCancelled;

        /// <inheritdoc cref="StreamedCopier(FileInfo, FileInfo, IDirectoryPair)"/>
        public static new StreamedCopier CreatePair(FileInfo source, FileInfo destination, IProcessedDirectoryPair parent = null) => new StreamedCopier(source, destination, parent);

        /// <inheritdoc cref="StreamedCopier(IFilePair, IDirectoryPair)"/>
        public static new StreamedCopier CreatePair(IFilePair filePair, IProcessedDirectoryPair parent = null) => new StreamedCopier(filePair, parent);

        /// <inheritdoc/>
        public StreamedCopier(IFilePair filePair, IDirectoryPair parent = null) : base(filePair, parent)
        {
        }

        /// <inheritdoc/>
        public StreamedCopier(FileInfo source, FileInfo destination, IDirectoryPair parent = null) : base(source, destination, parent)
        {
        }

        /// <inheritdoc/>
        public StreamedCopier(string source, string destination, IDirectoryPair parent = null) : base(source, destination, parent)
        {

        }

        /// <summary>
        /// Set the buffer size used for the copy operation
        /// </summary>
        public int BufferSize { get; set; } = DefaultBufferSize;

        /// <inheritdoc/>
        public bool WasCancelled
        {
            get { return _wasCancelled; }
            private set { SetProperty(ref _wasCancelled, value, nameof(WasCancelled)); }
        }

        /// <inheritdoc/>
        public override void Cancel()
        {
            if (IsCopying && !(_cancellationSource?.IsCancellationRequested ?? true))
            {
                _cancellationSource?.Cancel();
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> CopyAsync(bool overwrite, CancellationToken token)
        {
            if (IsCopying) throw new InvalidOperationException("Copy Operation already in progress");
            if (_isMoving) throw new InvalidOperationException("Move Operation already in progress");
            token.ThrowIfCancellationRequested();
            Refresh();
            if (!Source.Exists) throw new FileNotFoundException("Source File Not Found.", Source.FullName);
            if (!overwrite && Destination.Exists) throw new IOException("The destination already file exists");


            IsCopying = true;
            _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            Task updateTask = null;
            long totalBytesRead = 0;
            Progress = 0;
            WasCancelled = false;
            StartDate = DateTime.Now;

            try
            {
                int bSize = BufferSize;
                using var reader = new FileStream(Source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bSize, true);
                Destination.Directory.Create();
                using var writer = new FileStream(Destination.FullName, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, bSize, true);
                int bytesRead = 0;

                try
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
                    Memory<byte> buffer = new byte[bSize];
                    while ((bytesRead = await reader.ReadAsync(buffer, _cancellationSource.Token).ConfigureAwait(false)) > 0)
                    {
                        await writer.WriteAsync(buffer, _cancellationSource.Token).ConfigureAwait(false);
                        totalBytesRead += bytesRead;
                        updateTask ??= Task.Run(ProgressTask, _cancellationSource.Token);
                        while (IsPaused && !_cancellationSource.IsCancellationRequested)
                            await Task.Delay(75, _cancellationSource.Token).ConfigureAwait(false);
                    }
#else
                    byte[] buffer = new byte[bSize];
                    while ((bytesRead = await reader.ReadAsync(buffer, 0, bSize, _cancellationSource.Token).ConfigureAwait(false)) > 0)
                    {
                        await writer.WriteAsync(buffer, 0, bytesRead, _cancellationSource.Token).ConfigureAwait(false);
                        totalBytesRead += bytesRead;
                        updateTask ??= Task.Run(ProgressTask, _cancellationSource.Token);
                        while (IsPaused && !_cancellationSource.IsCancellationRequested)
                            await Task.Delay(75, _cancellationSource.Token).ConfigureAwait(false);
                    }
#endif
                    writer.Dispose();
                    reader.Dispose();
                }
                catch (OperationCanceledException)
                {
                    WasCancelled = true;
                    reader.Dispose();
                    await writer.FlushAsync(CancellationToken.None).CatchCancellation(false);
                    writer.Dispose();
                    if (totalBytesRead < Source.Length && File.Exists(Destination.FullName))
                        Destination.Delete();
                    throw;
                }
            }
            finally
            {
                IsCopying = false;
                IsPaused = false;
                EndDate = DateTime.Now;
                _cancellationSource.Cancel();
                if (updateTask != null) await updateTask.CatchCancellation(false);
                var finalProg = CalcProgress();
                if (finalProg != base.Progress) OnProgressUpdated(finalProg);
                IsCopying = false;
                _cancellationSource.Dispose();
                _cancellationSource = null;
                Refresh();
            }
            return Progress == 100;

            double CalcProgress() => (double)100 * totalBytesRead / Source.Length;

            async Task ProgressTask()
            {
                while (!_cancellationSource.IsCancellationRequested && totalBytesRead < Source.Length)
                {
                    OnProgressUpdated(CalcProgress());
                    await Task.Delay(100, _cancellationSource.Token).CatchCancellation(false);
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>This function has an optimization where if the two files are determined to be on the same drive, they will be moved via <see cref="File.Move(string, string)"/> instead.</remarks>
        public override async Task<bool> MoveAsync(bool overwrite, CancellationToken token)
        {
            try
            {
                //Check if Source & Destination are on same physical drive
                if (this.IsLocatedOnSameDrive())
                {
                    if (IsCopying) throw new InvalidOperationException("Copy Operation already in progress");
                    if (_isMoving) throw new InvalidOperationException("Move Operation already in progress");
                    IsCopying = true;
                    _isMoving = true;
                    Refresh();
                    if (!Source.Exists) throw new FileNotFoundException("Source File Not Found.", Source.FullName);
                    if (!overwrite && Destination.Exists) throw new IOException("The destination already file exists");
                    token.ThrowIfCancellationRequested();

                    StartDate = DateTime.Now;
                    
                    Directory.CreateDirectory(Destination.DirectoryName);
                    if (overwrite && Destination.Exists) Destination.Delete();
                    File.Move(Source.FullName, Destination.FullName);

                    EndDate = DateTime.Now;
                    OnProgressUpdated(100);
                }
                else
                {
                    if (await CopyAsync(overwrite, token))
                    {
                        Source.Delete();
                    }
                }
                Refresh();
                return !Source.Exists && Destination.Exists;
            }
            finally
            {
                _isMoving = false;
                IsCopying = false;
            }
        }

        /// <inheritdoc/>
        public override void Pause()
        {
            if (IsCopying && !IsPaused)
                IsPaused = true;
        }

        /// <inheritdoc/>
        public override void Resume()
        {
            if (IsCopying && IsPaused)
                IsPaused = false;
        }

        /// <summary>
        /// If the operation is in progress, cancel the operation.
        /// </summary>
        /// <returns>Returns when the Copy/Move operation is no long executing</returns>
#if NETSTANDARD2_0 || NETFRAMEWORK
        public async Task DisposeAsync()
#else
        public async ValueTask DisposeAsync()
#endif
        {
            Dispose();
            while (_cancellationSource != null)
            {
                await Task.Delay(25);
            }
        }

        /// <summary>
        /// Ensure that the operation is cancelled if it is running
        /// </summary>
        public void Dispose()
        {
            _cancellationSource?.Cancel();
        }
    }
}
