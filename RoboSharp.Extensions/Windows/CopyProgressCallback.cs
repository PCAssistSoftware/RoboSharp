using RoboSharp.Extensions.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Storage.FileSystem;
using Win32 = Windows.Win32;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// Handle the CallBack requested by CopyFileEx
    /// </summary>
    /// <param name="totalFileSize">Total File Size to be copied (bytes)</param>
    /// <param name="totalBytesTransferred">Total number of bytes transfered</param>
    /// <param name="streamSize">The total size of the current file stream, in bytes.</param>
    /// <param name="streamBytesTransferred">The total number of bytes in the current stream that have been transferred from the source file to the destination file since the copy operation began.</param>
    /// <param name="streamID">A handle to the current stream. The first time CopyProgressRoutine is called, the stream number is 1.</param>
    /// <param name="reason"><inheritdoc cref="CopyProgressCallbackReason" path="*"/>
    /// User-Defined data that will be passed into the callback.
    /// <para/> Example : CopyProgressData progressData = GCHandle.FromIntPtr(lpData).Target as CopyProgressData;
    /// </param>
    /// <returns><inheritdoc cref="CopyProgressCallbackResult"/></returns>
    /// <remarks>
    /// Signature : Func{long, long, long ,long, uint, CopyProgressCallbackReason, IntPtr, IntPtr, IntPtr, CopyProgressCallbackResult}
    /// <br/>
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winbase/nc-winbase-lpprogress_routine"/>
    /// </remarks>
    /// <seealso cref="Win32.Storage.FileSystem.LPPROGRESS_ROUTINE"/>
    public delegate CopyProgressCallbackResult CopyProgressCallback(
        long totalFileSize,
        long totalBytesTransferred,
        long streamSize,
        long streamBytesTransferred,
        uint streamID,
        CopyProgressCallbackReason reason
        //,IntPtr hSourceFile
        //,IntPtr hDestinationFile
        //,IntPtr data
        );

    /// <summary>
    /// Event description from CopyFileEx (why its performing the callback)
    /// </summary>
    /// <seealso cref="Win32.Storage.FileSystem.LPPROGRESS_ROUTINE_CALLBACK_REASON"/>
    public enum CopyProgressCallbackReason : uint
    {
        /// <summary>
        /// Copy Progress Updated
        /// </summary>
        CALLBACK_CHUNK_FINISHED = LPPROGRESS_ROUTINE_CALLBACK_REASON.CALLBACK_CHUNK_FINISHED,

        /// <summary>
        /// A stream was created and is about to be copied. This is the callback reason given when the callback routine is first invoked. 
        /// </summary>
        CALLBACK_STREAM_SWITCH = LPPROGRESS_ROUTINE_CALLBACK_REASON.CALLBACK_STREAM_SWITCH
    }

    /// <summary>
    /// The result of the callback to be evaluated by CopyFileEx
    /// </summary>
    public enum CopyProgressCallbackResult : uint
    {
        /// <summary>
        /// Continue the copy operation. 
        /// </summary>
        CONTINUE = 0U,

        /// <summary>
        /// Cancel the copy operation.
        /// The partially copied destination file is deleted.
        /// </summary>
        CANCEL = 1U,

        /// <summary>
        /// Stop the copy operation. It can be restarted at a later time.
        /// The partially copied destination file is left intact.
        /// </summary>
        STOP = 2U,

        /// <summary>
        /// Continue the copy operation, but prevent additional callbacks. 
        /// </summary>
        QUIET = 3U
    }

    public static partial class FileFunctions
    {
        /// <summary>
        /// Create a new <see cref="LPPROGRESS_ROUTINE"/> that wraps the <paramref name="callback"/>
        /// </summary>
        /// <param name="callback">The callback to wrap</param>
        /// <returns>If <paramref name="callback"/> is null, returns null. Otherwise returns a <see cref="LPPROGRESS_ROUTINE"/></returns>
        internal static unsafe LPPROGRESS_ROUTINE CreateCallbackInternal(CopyProgressCallback callback)
        {
            if (callback is null) return null;
            return new LPPROGRESS_ROUTINE((totalFileSize, totalBytesTransferred, streamSize, streamBytesTransferred, dwStreamNumber, dwCallbackReason, _, _, _) =>
            {
                return (uint)callback(totalFileSize, totalBytesTransferred, streamSize, streamBytesTransferred, dwStreamNumber, (CopyProgressCallbackReason)dwCallbackReason);
            });
        }

        /// <summary>
        /// Create a callback new callback that wraps the <paramref name="token"/> in order to detected a cancellation request and pass it back to CopyFileEx
        /// </summary>
        /// <param name="token">The token to wrap</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        internal static unsafe LPPROGRESS_ROUTINE CreateCallbackInternal(CancellationToken token)
        {
            if (!token.CanBeCanceled) return null;
            return new LPPROGRESS_ROUTINE((_, _, _, _, _, _, _, _, _) =>
            {
                return (uint)(token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE);
            });
        }

        /// <summary>
        /// Wrap an existing callback method, combinining with the the token to detect if the copy operation should be cancelled.
        /// </summary>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        internal static unsafe LPPROGRESS_ROUTINE CreateCallbackInternal(CopyProgressCallback callback, CancellationToken token)
        {
            if (callback is null && !token.CanBeCanceled)
                return null;

            else if (callback is null)
                return CreateCallbackInternal(token);

            else if (token.CanBeCanceled)
            {
                return new LPPROGRESS_ROUTINE((a, b, c, d, e, f, _, _, _) =>
                {
                    var result = callback.Invoke(a, b, c, d, e, (CopyProgressCallbackReason)f);
                    if (token.IsCancellationRequested)
                        result = CopyProgressCallbackResult.CANCEL;
                    return (uint)result;
                });
            }
            else
                return CreateCallbackInternal(callback);
        }

        /// <summary>
        /// Create a new CopyProgressCallback
        /// </summary>
        /// <param name="action">The action to perform. First parameter is total file size, second parameter is number of bytes copied.</param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        internal unsafe static LPPROGRESS_ROUTINE CreateCallbackInternal(Action<long, long> action, CancellationToken token)
        {
            if (action is null && !token.CanBeCanceled) return null;
            if (action is null) return token.CanBeCanceled ? CreateCallbackInternal(token) : null;

            return new LPPROGRESS_ROUTINE((size, totalRead, _, _, _, _, _, _, _) =>
            {
                action(size, totalRead);
                return (uint)(token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE);
            });
        }

        /// <summary>
        /// Create a new callback that will calculate the current progress and report it to the <paramref name="progress"/> object
        /// </summary>
        /// <param name="progress">The object to report progress to</param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(IProgress<double> progress, CancellationToken token = default)
        {
            if (progress is null) throw new ArgumentNullException(nameof(progress));
            return new CopyProgressCallback((total, processed, _, _, _, _) =>
            {
                progress.Report((double)100 * processed / total);
                return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE;
            });
        }

        /// <summary>
        /// Create a new callback that will calculate the current progress and report it to the <paramref name="progress"/> object
        /// </summary>
        /// <param name="progress">The object to report progress to</param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <param name="source">Source information for the <see cref="ProgressUpdate"/> objects</param>
        /// <param name="destination">Destination information for the <see cref="ProgressUpdate"/> objects</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        /// <remarks>Source and Destination information are not provided by the CopyFileEx or MoveFileWithProgress callbacks, so it must be supplied here if desired.</remarks>
        public static CopyProgressCallback CreateCallback(IProgress<ProgressUpdate> progress, string source = "", string destination = "", CancellationToken token = default)
        {
            if (progress is null) throw new ArgumentNullException(nameof(progress));
            return new CopyProgressCallback((total, processed, _, _, _, _) =>
            {
                progress.Report(new ProgressUpdate(total, processed, source, destination));
                return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE;
            });
        }

        /// <summary>
        /// Create a new callback that will calculate the current progress and report it to the <paramref name="progress"/> function
        /// </summary>
        /// <param name="progress">The object to report progress to</param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <param name="source">Source information for the <see cref="ProgressUpdate"/> objects</param>
        /// <param name="destination">Destination information for the <see cref="ProgressUpdate"/> objects</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>\
        /// <remarks>Source and Destination information are not provided by the CopyFileEx or MoveFileWithProgress callbacks, so it must be supplied here if desired.</remarks>
        public static CopyProgressCallback CreateCallback(Func<ProgressUpdate, CopyProgressCallbackResult> progress, string source = "", string destination = "", CancellationToken token = default)
        {
            if (progress is null) throw new ArgumentNullException(nameof(progress));
            if (token.CanBeCanceled)
            {
                return new CopyProgressCallback((total, processed, _, _, _, _) =>
                {
                    var result = progress(new ProgressUpdate(total, processed, source, destination));
                    return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : result;
                });
            }
            else
            {
                return new CopyProgressCallback((a, b, _, _, _, _) => progress(new ProgressUpdate(a, b, source, destination)));
            }
        }
    }
}
