using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// Static class to marshal the last Win32 Error, and generate the appropriate exception.
    /// </summary>
    public static class Win32Error
    {
#if Net6OrGreater
        /// <inheritdoc cref="Marshal.GetLastPInvokeError"/>
        /// <remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.getlastpinvokeerror?view=net-8.0"/>
        /// </remarks>
        public static int GetLastErrorCode() => Marshal.GetLastPInvokeError();
#else
        /// <inheritdoc cref="Marshal.GetLastWin32Error"/>
        /// <remarks>
        /// On .NET Framework, the GetLastWin32Error method can retain error information from one P/Invoke call to the next. 
        /// <br/>On .NET Core, error information is cleared before P/Invoke call, and the GetLastWin32Error represents only error information from the last method call
        /// <para/><seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.getlastwin32error?view=net-8.0"/>
        /// </remarks>
        public static int GetLastErrorCode() => Marshal.GetLastWin32Error();
#endif

        /// <inheritdoc cref="ThrowLastError(string,string)"/>
        [DebuggerHidden]
        public static void ThrowLastError()
        {
            var e = GetLastError();
            if (e is Exception er) throw er;
        }

        /// <summary>Gets the last Exception then throws it. If no exception was detected, returns to caller.</summary>
        /// <returns/>
        /// <inheritdoc cref="GenerateException(string, string, int)"/>
        [DebuggerHidden]
        public static void ThrowLastError(string source, string destinaation)
        {
            var e = GetLastError(source, destinaation);
            if (e is Exception er) throw er;
        }


        /// <inheritdoc cref="GetLastError(string, string)"/>
        public static Exception GetLastError() => GenerateException(string.Empty, string.Empty, GetLastErrorCode());

        ///<summary>
        /// Get the last Win32 error code and generate the appropriate exception.
        /// </summary>
        /// <inheritdoc cref="GenerateException(string, string, int)"/>
        public static Exception GetLastError(string source, string destination) => GenerateException(source, destination, GetLastErrorCode());

        ///<summary>
        /// Look up the Win32 error code and generate the appropriate exception.
        /// <br/>
        /// <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d"/>
        /// </summary>
        /// <remarks>
        /// Errors will have the following dictionary entries:
        /// <br/> - Source
        /// <br/> - Destination
        /// <br/> - Win32ErrorCode
        /// <br/> - Help Page
        /// </remarks>
        /// <returns>null if the <paramref name="errorCode"/> == 0, otherwise an Exception object</returns>
        public static Exception GenerateException(string sourceFile, string destFile, int errorCode)
        {

            if (errorCode == 0) return null; //No Error
            Exception e = errorCode switch
            {
                80 => new IOException("The destination file already exists."),
                0x000004D3 => new OperationCanceledException("The operation was cancelled."),
                1 => new InvalidOperationException("Invalid Operation"),
                2 => new FileNotFoundException(message: "Unable to locate the file", fileName: sourceFile),
                3 => new DirectoryNotFoundException("Unable to locate the directory: " + Path.GetDirectoryName(destFile)),
                4 => new FileNotFoundException("Too Many Open Files. Unable to open the file: ", sourceFile),
                5 => new UnauthorizedAccessException("Access Denied"),
                6 => new IOException("File Handle is Invalid"),
                7 => new IOException("The Storage Control Blocks were destroyed"),
                8 => new InsufficientMemoryException("Not enough storage is available to process this command."),
                0x0000000E => new InsufficientMemoryException("Not enough storage is available to complete this operation."),
                0x0000000F => new DriveNotFoundException("The system cannot find the drive specified."),
                0x00000013 => new UnauthorizedAccessException("The media is write-protected."),
                0x00000014 => new DriveNotFoundException("The system cannot find the device specified."),
                0x00000015 => new IOException("The device is not ready."),
                0x00000021 => new IOException("The process cannot access the file because another process has locked a portion of the file."),
                0x00000027 => new IOException("The destination disk is full: " + Path.GetPathRoot(destFile)),
                0x00000032 => new IOException("The destination file is locked"), // Occurs when the file is locked
                0x0000006E => new IOException("The system cannot open the device or file specified"),
                0x0000006F => new IOException("The file name is too long."),
                0x00000070 => new IOException("There is not enough space on the disk."),
                _ => new IOException($@"CopyFileEx Error Code: {errorCode}{Environment.NewLine} See https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d for details")
            };

            e.Data.Add("Source", sourceFile);
            e.Data.Add("Destination", destFile);
            e.Data.Add("Win32ErrorCode", errorCode);
            e.Data.Add("Help Page", @"https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d");
            return e;
        }
    }
}