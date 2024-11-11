using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Interface for objects that can be used within custom IRoboCommands
    /// </summary>
    public interface IFileCopier : IProcessedFilePair
    {
        /// <summary>
        /// Notify of Progress updates
        /// </summary>
        event EventHandler<CopyProgressEventArgs> ProgressUpdated;

        /// <summary>
        /// The status of the object - Reports if an operation is in progress or not.
        /// </summary>
        bool IsCopying { get; }

        /// <summary>
        /// TRUE is the copier was paused while it was running, otherwise false.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// The time the operation was started
        /// </summary>
        DateTime StartDate { get; }

        /// <summary>
        /// The time the operation was stopped or cancelled
        /// </summary>
        DateTime EndDate { get; }

        /// <summary>
        /// Cancel the operation
        /// </summary>
        void Cancel();

        /// <inheritdoc cref="CopyAsync(bool, CancellationToken)"/>
        /// <remarks>Will not overwrite an existing destination file.</remarks>
        Task<bool> CopyAsync();

        /// <inheritdoc cref="CopyAsync(bool, CancellationToken)"/>
        /// <remarks>Will not overwrite an existing destination file.</remarks>
        Task<bool> CopyAsync(CancellationToken token);

        /// <inheritdoc cref="CopyAsync(bool, CancellationToken)"/>
        Task<bool> CopyAsync(bool overwrite);

        /// <summary>
        /// Start a task that copies from the source to the destination
        /// </summary>
        /// <param name="overwrite">Overwrite the destination file if it exists</param>
        /// <param name="token">The associated Cancellation Token</param>
        /// <returns>True if the copy was successful</returns>
        Task<bool> CopyAsync(bool overwrite, CancellationToken token);

        /// <inheritdoc cref="MoveAsync(bool, CancellationToken)"/>
        /// <remarks>Will not overwrite an existing destination file.</remarks>
        Task<bool> MoveAsync();

        /// <inheritdoc cref="MoveAsync(bool, CancellationToken)"/>
        /// <remarks>Will not overwrite an existing destination file.</remarks>
        Task<bool> MoveAsync(CancellationToken token);

        /// <inheritdoc cref="MoveAsync(bool, CancellationToken)"/>
        Task<bool> MoveAsync(bool overwrite);

        /// <summary>
        /// Start a task that moves from the source to the destination
        /// </summary>
        /// <param name="overwrite">Overwrite the destination file if it exists</param>
        /// <param name="token">The associated Cancellation Token</param>
        /// <returns>True if the copy was successful</returns>
        Task<bool> MoveAsync(bool overwrite, CancellationToken token);

        /// <summary>
        /// Pause the current operation
        /// </summary>
        void Pause();

        /// <summary>
        /// Resume the paused operation
        /// </summary>
        void Resume();
    }
}