using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RoboSharp.Extensions.Windows;
using Windows.Win32.Storage.FileSystem;
using Win32 = Windows.Win32;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// Windows-Specific Functionality and Extensions for Moving Files
    /// </summary>
    public static partial class FileFunctions
    {
        /// <summary>
        /// Gets the relevant settings from the <paramref name="options"/> object for use with CopyFileEx
        /// </summary>
        /// <param name="options"></param>
        /// <returns>
        /// A set of <see cref="CopyFileExOptions"/> that may have the following options enabled : 
        /// <br/> - <see cref="CopyFileExOptions.NO_BUFFERING"/>
        /// <br/> - <see cref="CopyFileExOptions.RESTARTABLE"/>
        /// <br/> - <see cref="CopyFileExOptions.COPY_SYMLINK"/>
        /// <br/> - <see cref="CopyFileExOptions.REQUEST_COMPRESSED_TRAFFIC"/>
        /// </returns>
        public static CopyFileExOptions GetCopyFileExOptions(this CopyOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            CopyFileExOptions copyOptions = CopyFileExOptions.NONE;
            if (options.EnableRestartMode) copyOptions |= CopyFileExOptions.RESTARTABLE;
            if (options.CopySymbolicLink) copyOptions |= CopyFileExOptions.COPY_SYMLINK;
            if (options.Compress) copyOptions |= CopyFileExOptions.REQUEST_COMPRESSED_TRAFFIC;
            if (options.UseUnbufferedIo) copyOptions |= CopyFileExOptions.NO_BUFFERING;
            return copyOptions;
        }

        /// <param name="source"><inheritdoc cref="Win32.PInvoke.MoveFileWithProgress(string, string, LPPROGRESS_ROUTINE, void*, MOVE_FILE_FLAGS)" path="/param[@name='lpExistingFileName']"/></param>
        /// <param name="destination"><inheritdoc cref="Win32.PInvoke.MoveFileWithProgress(string, string, LPPROGRESS_ROUTINE, void*, MOVE_FILE_FLAGS)" path="/param[@name='lpNewFileName']"/></param>
        /// <param name="progressCallback"><inheritdoc cref="Win32.PInvoke.MoveFileWithProgress(string, string, LPPROGRESS_ROUTINE, void*, MOVE_FILE_FLAGS)" path="/param[@name='lpProgressRoutine']"/></param>
        /// <param name="options"><inheritdoc cref="Win32.PInvoke.MoveFileWithProgress(string, string, LPPROGRESS_ROUTINE, void*, MOVE_FILE_FLAGS)" path="/param[@name='dwFlags']"/></param>
        /// <inheritdoc cref="Win32.PInvoke.MoveFileWithProgress(string, string, LPPROGRESS_ROUTINE, void*, MOVE_FILE_FLAGS)"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Throws if not windows")]
        internal static unsafe bool InvokeMoveFileWithProgress(string source, string destination, LPPROGRESS_ROUTINE progressCallback, MoveFileOptions options)
        {
            //Prepare
            VersionManager.ThrowIfNotWindowsPlatform("P/Invoke.MoveFileWithProgress is only available on Windows");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination));

            //Execute
            bool returnValue = Win32.PInvoke.MoveFileWithProgress(source, destination, progressCallback, lpData: null, dwFlags: (MOVE_FILE_FLAGS)options);
            if (!returnValue) Win32Error.ThrowLastError(source, destination);
            return returnValue;
        }

        /// <param name="source">The path to the source file.<para/><inheritdoc cref="InvokeMoveFileWithProgress" path="/param[@name='source']"/></param>
        /// <param name="destination">The path to the destination file.<para/><inheritdoc cref="InvokeMoveFileWithProgress" path="/param[@name='destination']"/></param>
        /// <param name="progressCallback">Callback function for progress notifications during the copy operation. Can be null.
        ///  <para/>If the <paramref name="progressCallback"/> returns either <see cref="CopyProgressCallbackResult.CANCEL"/> or <see cref="CopyProgressCallbackResult.STOP"/>, 
        ///  this method will throw a <see cref="OperationCanceledException"/>. The <paramref name="source"/> file is left intact.
        /// </param>
        /// <param name="options">Flags specifying how the file should be moved.</param>
        /// <returns>True if the file was moved successfully, otherwise false.</returns>
        /// <inheritdoc cref="Win32.PInvoke.MoveFileWithProgress(string, string, LPPROGRESS_ROUTINE, void*, MOVE_FILE_FLAGS)"/>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="Exception"/>
        public static bool MoveFileWithProgress(string source, string destination, CopyProgressCallback progressCallback, MoveFileOptions options = MoveFileOptions.Default )
        {
            // Prepare
            VersionManager.ThrowIfNotWindowsPlatform("P/Invoke.MoveFileWithProgress is only available on Windows");
            if (!File.Exists(source)) throw new FileNotFoundException("Source File Not Found.", source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            // Invoke
            return InvokeMoveFileWithProgress(source, destination, CreateCallbackInternal(progressCallback), options);
        }

        ///<inheritdoc cref="MoveFileWithProgress(string, string, CopyProgressCallback, MoveFileOptions)"/>
        public static bool MoveFileWithProgress(string source, string destination, MoveFileOptions options)
            => MoveFileWithProgress(source, destination, null, options);

        /// <summary>
        /// Executes the MoveFileWithProgress function via Task.Run()
        /// </summary>
        /// <inheritdoc cref="MoveFileWithProgress(string, string, CopyProgressCallback, MoveFileOptions)"/>
        public static Task<bool> MoveFileWithProgressAsync(string source,
            string destination,
            CopyProgressCallback progressCallback = null,
            MoveFileOptions options = MoveFileOptions.Default,
            CancellationToken token = default
            )
        {
            // Prepare
            VersionManager.ThrowIfNotWindowsPlatform("P/Invoke.MoveFileWithProgress is only available on Windows");
            if (!File.Exists(source)) throw new FileNotFoundException("Source File Not Found.", source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            // Invoke
            var callback = CreateCallbackInternal(progressCallback, token);
            return Task.Run(() => InvokeMoveFileWithProgress(source, destination, callback, options), token);
        }

        /// <inheritdoc cref="MoveFileWithProgressAsync(string, string, bool, CancellationToken)"/>
        public static Task<bool> MoveFileWithProgressAsync(string source, string destination)
            => MoveFileWithProgressAsync(source, destination, false, CancellationToken.None);

        /// <inheritdoc cref="MoveFileWithProgressAsync(string, string, bool, CancellationToken)"/>
        public static Task<bool> MoveFileWithProgressAsync(string source, string destination, CancellationToken token)
            => MoveFileWithProgressAsync(source, destination, false, token);

        /// <summary> Copy the file asynchronously. If the operation is successfull, delete the source file. </summary>
        /// <inheritdoc cref="MoveFileWithProgressAsync(string, string, CopyProgressCallback, MoveFileOptions, CancellationToken)"/>
        public static async Task<bool> MoveFileWithProgressAsync(string source, string destination, bool overwrite, CancellationToken token = default)
        {
            MoveFileOptions options = MoveFileOptions.COPY_ALLOWED | MoveFileOptions.WRITE_THROUGH;
            if (overwrite) options |= MoveFileOptions.REPLACE_EXISTSING;
            return await MoveFileWithProgressAsync(source, destination, null, options, token).ConfigureAwait(false);
        }

        /// <inheritdoc cref="MoveFileProgressAsync"/>
        public static async Task<bool> MoveFileWithProgressAsync(string source, string destination, IProgress<ProgressUpdate> progress, int updateInterval = 100, bool overwrite = false, CancellationToken token = default)
        {
            return await MoveFileProgressAsync(source, destination, overwrite, updateInterval, progress: progress, token: token);
        }

        /// <inheritdoc cref="MoveFileProgressAsync"/>
        public static async Task<bool> MoveFileWithProgressAsync(string source, string destination, IProgress<long> progress, int updateInterval = 100, bool overwrite = false, CancellationToken token = default)
        {
            return await MoveFileProgressAsync(source, destination, overwrite, updateInterval, sizeProgress: progress, token: token);
        }

        /// <inheritdoc cref="MoveFileProgressAsync"/>
        public static async Task<bool> MoveFileWithProgressAsync(string source, string destination, IProgress<double> progress, int updateInterval = 100, bool overwrite = false, CancellationToken token = default)
        {
            return await MoveFileProgressAsync(source, destination, overwrite, updateInterval, percentProgress: progress, token: token);
        }

        /// <summary>
        /// Move the file asynchronously with a progress reporter. If the operation is successfull, delete the soure file.
        /// </summary>
        /// <param name="progress">An IProgress object that will accept a progress notification</param>
        /// <param name="updateInterval">Time interval in milliseconds to update the <paramref name="progress"/> object</param>
        /// <returns>A task that returns when the operation has completed successfully or has been cancelled.</returns>
        /// <inheritdoc cref="MoveFileWithProgress(string, string, CopyProgressCallback, MoveFileOptions)"/>
        /// <param name="source"/><param name="destination"/><param name="overwrite"/><param name="token"/>
        /// <param name="percentProgress"/><param name="sizeProgress"/>
        internal static async Task<bool> MoveFileProgressAsync(
            string source, string destination, bool overwrite,
            int updateInterval = 100,
            IProgress<ProgressUpdate> progress = null,
            IProgress<long> sizeProgress = null,
            IProgress<double> percentProgress = null,
            CancellationToken token = default)
        {
            FileInfo sourceFile = new FileInfo(source);
            if (!sourceFile.Exists) throw new FileNotFoundException("Source file does not exist", source);
            if (!overwrite && File.Exists(destination)) throw new IOException("The destination already file exists");
            token.ThrowIfCancellationRequested();
            bool result = false;

            // Updater
            Task updateTask = null;
            long fileSize = 0;
            long totalBytesRead = 0;
            updateInterval = updateInterval > 25 ? updateInterval : 100;
            var updateToken = CancellationTokenSource.CreateLinkedTokenSource(token);
            if (sourceFile.Length > 0 && (progress != null | sizeProgress != null | percentProgress != null))
            {
                updateTask = Task.Run(async () =>
                {
                    while (totalBytesRead < sourceFile.Length)
                    {
                        Report();
                        await Task.Delay(updateInterval, updateToken.Token);
                        updateToken.Token.ThrowIfCancellationRequested();
                    }
                }, updateToken.Token);
            }

            try
            {
                var callback = FileFunctions.CreateCallbackInternal(ProgressHandler, token);
                MoveFileOptions options = MoveFileOptions.COPY_ALLOWED | MoveFileOptions.WRITE_THROUGH;
                if (overwrite) options |= MoveFileOptions.REPLACE_EXISTSING;
                result = await Task.Run(() => InvokeMoveFileWithProgress(source, destination, callback, options), token).ConfigureAwait(false);
            }
            finally
            {
                updateToken.Cancel();
                await updateTask.CatchCancellation(false);
            }
            Report();
            return result;

            void Report()
            {
                progress?.Report(new ProgressUpdate(sourceFile.Length, totalBytesRead, sourceFile.FullName, destination));
                sizeProgress?.Report(totalBytesRead);
                percentProgress?.Report((double)100 * totalBytesRead / sourceFile.Length);
            }

            void ProgressHandler(long size, long copied)
            {
                fileSize = size;
                totalBytesRead = copied;
            }
        }

    }
}
