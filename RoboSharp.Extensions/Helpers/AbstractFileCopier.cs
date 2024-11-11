using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// Abstract base class extending <see cref="FilePair"/>, that will implement the <see cref="IFileCopier"/> interface
    /// </summary>
    public abstract class AbstractFileCopier : FilePair, INotifyPropertyChanged, IFileCopier
    {
        private double _progress;
        private bool _isCopying;
        private bool _isPaused;
        private DateTime _startDate;
        private DateTime _endDate;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The Progress Updated event
        /// </summary>
        public event EventHandler<CopyProgressEventArgs> ProgressUpdated;


        /// <summary>
        /// Create a new FileCopier from the supplied file paths
        /// </summary>
        /// <inheritdoc cref="FilePair(FileInfo, FileInfo, IDirectoryPair)"/>
        protected AbstractFileCopier(FileInfo source, FileInfo destination, IDirectoryPair parent = null) : base(source, destination, parent)
        { }

        /// <summary>
        /// Create a new FileCopier from the supplied file paths
        /// </summary>
        /// <inheritdoc cref="FilePair(string, string, IDirectoryPair)"/>
        protected AbstractFileCopier(string source, string destination, IDirectoryPair parent = null) : base(source, destination, parent)
        { }

        /// <summary>
        /// Create a new FileCopier from the provided IFilePair
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <inheritdoc cref="FilePair(IFilePair, IDirectoryPair)"/>
        protected AbstractFileCopier(IFilePair filePair, IDirectoryPair parent = null) : base(filePair, parent)
        { }

        /// <inheritdoc/>
        public double Progress
        {
            get { return _progress; }
            protected set { SetProperty(ref _progress, value); }
        }

        /// <inheritdoc/>
        public bool IsCopying
        {
            get { return _isCopying; }
            protected set { SetProperty(ref _isCopying, value); }
        }

        /// <inheritdoc/>
        public bool IsPaused
        {
            get { return _isPaused; }
            protected set { SetProperty(ref _isPaused, value); }
        }

        /// <inheritdoc/>
        public DateTime StartDate
        {
            get { return _startDate; }
            protected set { SetProperty(ref _startDate, value); }
        }

        /// <inheritdoc/>
        public DateTime EndDate
        {
            get { return _endDate; }
            protected set { SetProperty(ref _endDate, value); }
        }

        /// <summary> 
        /// Get the time it took to complete the operation. 
        /// </summary>
        /// <returns>If <see cref="EndDate"/> is greater than <see cref="StartDate"/> : returns the timespan difference. Otherwise returns null.</returns>
        public TimeSpan? GetTotalTime()
        {
            if (EndDate > StartDate) return EndDate - StartDate;
            return null;
        }

        /// <summary>
        /// Set the value for <see cref="Progress"/> property then raise the ProgressUpdated event
        /// </summary>
        /// <param name="progress"></param>
        protected virtual void OnProgressUpdated(double progress)
        {
            Progress = progress;
            if (ProgressUpdated != null)
                OnProgressUpdated(new CopyProgressEventArgs(progress, ProcessedFileInfo, Parent?.ProcessedFileInfo));
        }

        /// <summary>
        /// Raise the ProgressUpdated event
        /// </summary>
        /// <param name="e"></param>
        protected void OnProgressUpdated(CopyProgressEventArgs e)
        {
            ProgressUpdated?.Invoke(this, e);
        }

#pragma warning disable CS1591 // Interface provides descriptions

        public Task<bool> CopyAsync() => CopyAsync(false, CancellationToken.None);
        public Task<bool> CopyAsync(CancellationToken token) => CopyAsync(false, token);
        public Task<bool> CopyAsync(bool overwrite) => CopyAsync(overwrite, CancellationToken.None);
        public abstract Task<bool> CopyAsync(bool overwrite, CancellationToken token);

        public Task<bool> MoveAsync() => MoveAsync(false, CancellationToken.None);
        public Task<bool> MoveAsync(CancellationToken token) => CopyAsync(false, token);
        public Task<bool> MoveAsync(bool overwrite) => MoveAsync(overwrite, CancellationToken.None);
        public abstract Task<bool> MoveAsync(bool overwrite, CancellationToken token);

        public abstract void Cancel();
        public abstract void Pause();
        public abstract void Resume();

#pragma warning restore CS1591

        /// <summary>
        /// Raise PropertyChanged
        /// </summary>
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Set the property and raise PropertyChanged event
        /// </summary>
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
