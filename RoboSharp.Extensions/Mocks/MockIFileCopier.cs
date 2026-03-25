using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace RoboSharp.Extensions.Mocks
{
    /// <summary>
    /// A mock <see cref="IFileCopier"/> that is designed for unit testing scenarios.
    /// <para/> - No actual copy operation takes place. Source/Destination are not required to be set.
    /// <para/> - Customize with <see cref="MockIFileCopier.CopyDelay"/> and <see cref="MockIFileCopier.ReturnValue"/>
    /// </summary>
    public class MockIFileCopier : IFileCopier, IFileSource
    {
        /// <summary>
        /// The timespan to use with <see cref="Task.Delay(TimeSpan)"/> to simulate a copy or move
        /// </summary>
        /// <remarks>Default = 3ms</remarks>
        public TimeSpan CopyDelay { get; set; } = new TimeSpan(0, 0, 0, 0, 3);

        /// <summary>
        /// What to return from the Copy/Move tasks
        /// </summary>
        public bool ReturnValue { get; set; } = true;


        public bool IsCopying { get; private set; }

        public bool IsPaused { get; private set; }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public IProcessedDirectoryPair? Parent { get; set; }

        public ProcessedFileInfo? ProcessedFileInfo { get; set; }

        public bool ShouldCopy { get; set; } = true;

        public bool ShouldPurge { get; set; }

        /// <remarks>
        /// Not Required to be set
        /// </remarks>
        /// <inheritdoc/>
        public FileInfo? Source { get; set; }

        /// <remarks>
        /// Not Required to be set
        /// </remarks>
        /// <inheritdoc/>
        public FileInfo? Destination { get; set; }

        string IFileSource.FilePath => Source?.FullName ?? throw new InvalidOperationException($"{nameof(MockIFileCopier)}.{nameof(Source)} property is not set.");

        public event EventHandler<CopyProgressEventArgs>? ProgressUpdated;

        private CancellationTokenSource? mockCts;

        public void Cancel()
        {
            mockCts?.Cancel();
        }

        public Task<bool> CopyAsync() => CopyAsync(false, default);

        public Task<bool> CopyAsync(CancellationToken token) => CopyAsync(false, default);

        public Task<bool> CopyAsync(bool overwrite) => CopyAsync(overwrite, default);

        public async Task<bool> CopyAsync(bool overwrite, CancellationToken token)
        {
            if (ShouldCopy)
            {
                IsCopying = true;
                IsPaused = false;
                StartDate = DateTime.Now;
                mockCts = new CancellationTokenSource();
                var cts = CancellationTokenSource.CreateLinkedTokenSource(token, mockCts.Token);
                await Task.Delay(CopyDelay, cts.Token);
                IsCopying = false;
                cts.Dispose();
                ProgressUpdated?.Invoke(this, new CopyProgressEventArgs(100));
                EndDate = DateTime.Now;
                return ReturnValue;
            }
            return false;
        }

        public Task<bool> MoveAsync() => MoveAsync(false, default);

        public Task<bool> MoveAsync(CancellationToken token) => CopyAsync(false, token);

        public Task<bool> MoveAsync(bool overwrite) => MoveAsync(overwrite, default);

        public Task<bool> MoveAsync(bool overwrite, CancellationToken token) => CopyAsync(overwrite, token);

        public void Pause()
        {
            
        }

        public void Resume()
        {
            
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member