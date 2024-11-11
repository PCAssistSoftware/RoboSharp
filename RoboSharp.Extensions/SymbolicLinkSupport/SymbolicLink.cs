#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using MSWin = Windows;
using Win32 = Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using REPARSE_DATA_BUFFER = Windows.Wdk.Storage.FileSystem.REPARSE_DATA_BUFFER;
using System.Runtime.CompilerServices;
using Windows.Win32.Foundation;
using RoboSharp.Extensions.Options;

namespace RoboSharp.Extensions.SymbolicLinkSupport
{
    /// <summary>
    /// Provides extension methods for <see cref="FileSystemInfo"/> to creating and reading symbolic links.
    /// </summary>
    public static class SymbolicLink
    {
        const string PlatformErrorMessage = "This function relies on Windows P/Invoke. Use .Net 6 or newer for platform comaptibility.";
        const string NotFoundErrorMessage = "Could not find a part of the path '{0}'.";

        /// <summary>
        /// Determines whether this <see cref="FileSystemInfo"/> represents a symbolic link or junction.
        /// </summary>
        /// <param name="info">the system object in question.</param>
        /// <returns><see langword="true"/> if the <paramref name="info"/> is a symbolic link, otherwise <see langword="false"/>.</returns>
        /// <remarks>.Net6 and newer uses the native FileSystemInfo.LinkTarget property</remarks>
        public static bool IsSymbolicLink(this FileSystemInfo info)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));
#if NET6_0_OR_GREATER
            return info.LinkTarget != null;
#else
            if (VersionManager.IsPlatformWindows)
                return info.Attributes.HasFlag(FileAttributes.ReparsePoint) && SymbolicLink.GetReparseDataTarget(info) != null;
            else
                return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
#endif
        }

        /// <summary>
        /// Evaluate the REPARSE_DATA_BUFFER to determine if this path represents a symbolic link or junction
        /// </summary>
        /// <returns><see langword="true"/> if the link has <see cref="FileAttributes.ReparsePoint"/> and a link was able to be resolved. Otherwise <see langword="false"/></returns>
        /// <inheritdoc cref="SymbolicLink.GetReparseDataTarget(string, bool)"/>
        public static bool IsSymbolicLink(string link, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(link)) throw new ArgumentNullException(nameof(link));
            if (!File.GetAttributes(link).HasFlag(FileAttributes.ReparsePoint)) return false;
            if (!VersionManager.IsPlatformWindows) return true;
            return SymbolicLink.GetReparseDataTarget(link, isDirectory) != null;
        }

        /// <summary>
        /// Create a new Symbolic Link at the specified location. 
        /// <br/>This uses the Windows API <see href="https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-createsymboliclinkw#">CreateSymbolicLinkW</see>
        /// to create the symbolic link.
        /// </summary>
        /// <param name="link"><inheritdoc cref="Win32.PInvoke.CreateSymbolicLink(string, string, SYMBOLIC_LINK_FLAGS)" path="/param[@name='lpSymlinkFileName']"/></param>
        /// <param name="pathToTarget"><inheritdoc cref="Win32.PInvoke.CreateSymbolicLink(string, string, SYMBOLIC_LINK_FLAGS)" path="/param[@name='lpTargetFileName']"/></param>
        /// <param name="isDirectory">set TRUE if creating a link to a directory, otherwise this will be a link to a file.</param>
        /// <param name="makeTargetRelative">
        /// Make the link relative to the target. Example:
        /// <br/>-- Target File Path = "D:\Root\SomeDir\SomeFile.txt"
        /// <br/>-- Link File Path = "D:\"
        /// <br/>-- Resulting Link = ".\Root\SomeDir\SomeFile.txt"
        /// </param>
        /// <exception cref="IOException"/>
        /// <exception cref="PlatformNotSupportedException"/>
        /// <returns/>
        public static void CreateAsSymbolicLink(string link, string pathToTarget, bool isDirectory, bool makeTargetRelative = false)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (string.IsNullOrWhiteSpace(link)) throw new ArgumentException("linkPath must not be empty", nameof(link));
            if (string.IsNullOrWhiteSpace(pathToTarget)) throw new ArgumentException("targetPath must not be empty", nameof(pathToTarget));

            if (makeTargetRelative)
            {
                pathToTarget = GetTargetPathRelativeToLink(link, pathToTarget, isDirectory);
            }

            var success = Win32.PInvoke.CreateSymbolicLink(link, pathToTarget, isDirectory ? SYMBOLIC_LINK_FLAGS.SYMBOLIC_LINK_FLAG_DIRECTORY : 0);
            if (!success)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }
        }

        /// <inheritdoc cref="CreateAsSymbolicLink(string, string, bool, bool)"/>
        public static void CreateAsSymbolicLink(FileSystemInfo link, string pathToTarget, bool makeTargetRelative)
            => CreateAsSymbolicLink(link?.FullName ?? throw new ArgumentNullException(nameof(link)), pathToTarget, link is DirectoryInfo, makeTargetRelative);

#if NET6_0_OR_GREATER

        /// <remarks>Exposed here for binary compatibility.<br/>Calls the native method <see cref="FileSystemInfo.ResolveLinkTarget(bool)"/>.</remarks>
        /// <inheritdoc cref="FileSystemInfo.CreateAsSymbolicLink(string)"/>
        public static void CreateAsSymbolicLink(FileSystemInfo link, string pathToTarget) => link.CreateAsSymbolicLink(pathToTarget);

        /// <remarks>Exposed here for binary compatibility.<br/>Calls the native method <see cref="FileSystemInfo.ResolveLinkTarget(bool)"/>.</remarks>
        /// <inheritdoc cref="FileSystemInfo.ResolveLinkTarget(bool)"/>
        public static FileSystemInfo? ResolveLinkTarget(FileSystemInfo link, bool returnFinalTarget)
        {
            return link?.ResolveLinkTarget(returnFinalTarget);
        }
#else

        /// <summary>
        /// Creates a symbolic link at the <see cref="FileSystemInfo.FullName"/> that points to the <paramref name="pathToTarget"/>
        /// </summary>
        /// <param name="link">The file or directory to create as a link</param>
        /// <param name="pathToTarget">
        /// The path of the symbolic link target.
        /// <para/><inheritdoc cref="Win32.PInvoke.CreateSymbolicLink(string, string, SYMBOLIC_LINK_FLAGS)" path="/param[@name='lpTargetFileName']"/>
        /// </param>
        /// <remarks>Requires administractive privileges. Windows only.</remarks>
        /// <inheritdoc cref="CreateAsSymbolicLink(string, string, bool, bool)"/>
        public static void CreateAsSymbolicLink(this FileSystemInfo link, string pathToTarget)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (link is null) throw new ArgumentNullException(nameof(link));
            if (string.IsNullOrWhiteSpace(pathToTarget)) throw new ArgumentException("target path can not be empty", nameof(pathToTarget));
            SymbolicLink.CreateAsSymbolicLink(link.FullName, pathToTarget, link is DirectoryInfo, false);
        }

        /// <summary>
        /// Gets the target of the specified link.
        /// </summary>
        /// <remarks>
        /// This is an extension method to emulate the .Net6 native method of the same name.
        /// <br/> When accessed in a non-windows environment, always returns null.
        /// </remarks>
        /// <param name="link">The <see cref="FileSystemInfo"/> that represents the link to some target</param>
        /// <param name="returnFinalTarget"></param>
        /// <returns>A <see cref="FileSystemInfo"/> that represents the target. If the <paramref name="link"/> is not a target, return <see langword="null"/> </returns>
        /// <exception cref="PlatformNotSupportedException"/>
        public static FileSystemInfo? ResolveLinkTarget(this FileSystemInfo link, bool returnFinalTarget)
        {
            if (link is null | !VersionManager.IsPlatformWindows) return null;
            string? target = returnFinalTarget ? GetReparseDataFinalTarget(link) : GetReparseDataTarget(link);
            return true switch
            {
                true when target is null => null,
                true when link.FullName.Equals(target, StringComparison.CurrentCultureIgnoreCase) => link,
                true when link is DirectoryInfo => new DirectoryInfo(target),
                _ => new FileInfo(target)
            };
        }
#endif

        private static string GetTargetPathRelativeToLink(string linkPath, string targetPath, bool linkAndTargetAreDirectories = false)
        {
            if (string.IsNullOrWhiteSpace(linkPath)) throw new ArgumentException("linkPath must not be empty", nameof(linkPath));
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentException("targetPath must not be empty", nameof(targetPath));

            FileAttributes relativePathAttribute = 0;
            if (linkAndTargetAreDirectories)
            {
                relativePathAttribute = FileAttributes.Directory;

                // set the link path to the parent directory, so that PathRelativePathToW returns a path that works
                // for directory symlink traversal
                linkPath = Path.GetDirectoryName(linkPath.TrimEnd(Path.DirectorySeparatorChar));
            }

            Span<char> relativePath = new Span<char>(new char[Win32.PInvoke.MAX_PATH]);
            bool result;
            unsafe
            {
                fixed (char* builder = relativePath)
                {
                    result = Win32.PInvoke.PathRelativePathTo(builder, linkPath, (uint)relativePathAttribute, targetPath, (uint)relativePathAttribute);
                }
            }
            if (result is false)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return targetPath;
            }
            else
            {
                return relativePath.ToString();
            }
        }

        private static SafeFileHandle? GetSafeFileHandle(string path, bool isDirectory, bool openReparsePoint)
        {
            const FILE_FLAGS_AND_ATTRIBUTES dirAttr = FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY;
            const FILE_FLAGS_AND_ATTRIBUTES fileAttr = FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL;
            const FILE_FLAGS_AND_ATTRIBUTES reparseDir = dirAttr | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT;
            const FILE_FLAGS_AND_ATTRIBUTES reparseFile = fileAttr | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT;

            var handle = Win32.PInvoke.CreateFile(
                    lpFileName: path,
                    dwDesiredAccess: (uint)GENERIC_ACCESS_RIGHTS.GENERIC_READ,
                    dwShareMode: FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
                    lpSecurityAttributes: default,
                    dwCreationDisposition: FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                    dwFlagsAndAttributes: true switch
                    {
                        true when isDirectory && openReparsePoint => reparseDir,
                        true when isDirectory => dirAttr,
                        true when openReparsePoint => reparseFile,
                        _ => fileAttr
                    },
                    hTemplateFile: default);
            
            if (handle.IsInvalid)
            {
                handle.Dispose();
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
                if (isDirectory && !Directory.Exists(path)) throw new DirectoryNotFoundException(NotFoundErrorMessage.Format(path));
                if (!isDirectory && !File.Exists(path)) throw new FileNotFoundException(NotFoundErrorMessage.Format(path));
                throw new IOException("Invalid File Handle");
            }
            return handle;
        }

        /// <inheritdoc cref="GetFinalPathNameByHandle(string, bool)"/>
        public static string GetFinalPathNameByHandle(FileSystemInfo link)
            => GetFinalPathNameByHandle(link?.FullName, link is DirectoryInfo);

        /// <summary>
        /// Windows-Only API Call to <see href="https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlea">GetFinalPathNameByHandle</see>.
        /// <br/>- This can be used to discover the mount point of a mapped network drive ( J:\ == //SomeServer/Share$%/ )
        /// </summary>
        /// <returns>
        /// A string representing the target of the <paramref name="link"/>
        /// </returns>
        public unsafe static string GetFinalPathNameByHandle(string link, bool isDirectory)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (string.IsNullOrWhiteSpace(link)) throw new ArgumentNullException(nameof(link));

            using SafeFileHandle fileHandle = GetSafeFileHandle(link, isDirectory, false);

            uint result;
            Span<char> text = new char[Win32.PInvoke.MAX_PATH];
            text.Clear();
            fixed (char* builder = text)
            {
                result = Win32.PInvoke.GetFinalPathNameByHandle(fileHandle, builder, Win32.PInvoke.MAX_PATH, GETFINALPATHNAMEBYHANDLE_FLAGS.FILE_NAME_NORMALIZED);
            }
            if (result > Win32.PInvoke.MAX_PATH) // if not enough characters alloted, retry with required character count (supplied by the previous call's result)
            {   
                text = new char[result];
                text.Clear();
                fixed (char* builder = text)
                {
                    result = Win32.PInvoke.GetFinalPathNameByHandle(fileHandle, builder, result, GETFINALPATHNAMEBYHANDLE_FLAGS.FILE_NAME_NORMALIZED);
                }
            }

            if (result == 0)
            {
                int errCode = Marshal.GetHRForLastWin32Error();
                switch ((WIN32_ERROR)errCode)
                {
                    case WIN32_ERROR.ERROR_SUCCESS:
                    case WIN32_ERROR.ERROR_FSFILTER_OP_COMPLETED_SUCCESSFULLY:
                        break;
                    default:
                        Marshal.ThrowExceptionForHR(errCode);
                        return null;
                }
            }

            // remove the appended prefix from the result, and ensure UNC paths get their slashes
            const string prefix = @"\\?\";
            const string uncPrefix = @"\\?\UNC\";

            return true switch
            {
#if NETCOREAPP3_0_OR_GREATER
                true when text.StartsWith(uncPrefix) =>  string.Concat("\\", text.Slice(7, (int)result - 7)),
#else
                true when text.StartsWith(uncPrefix.AsSpan()) => string.Concat("\\", text.Slice(7, (int)result - 7).ToString()),
#endif
                true when text.StartsWith(prefix.AsSpan()) => text.Slice(4, (int)result - 4).ToString(),
                _ => text.Slice(0, (int)result).ToString()
            };
        }



        private static string? GetReparseDataFinalTarget(FileSystemInfo link)
        {
            // first link
            string targetPath = GetReparseDataTarget(link.FullName, link is DirectoryInfo);
            if (targetPath is null) return null;
            // follow the trail
            while(File.GetAttributes(targetPath).HasFlag(FileAttributes.ReparsePoint) && GetReparseDataTarget(targetPath, link is DirectoryInfo) is string nextTarget)
            {
                targetPath = nextTarget;
            }
            return targetPath;
        }

        /// <inheritdoc cref="GetReparseDataTarget(string, bool)"/>
        public static string? GetReparseDataTarget(FileSystemInfo link) => GetReparseDataTarget(link?.FullName, link is DirectoryInfo);

        /// <summary>
        /// Retrieve the target for the specified <paramref name="link"/> using the Windows api for REPARSE_DATA_BUFFER
        /// </summary>
        /// <param name="link">The path to the link, whose target shall be retrieved.</param>
        /// <param name="isDirectory">set <see langword="true"/> if this is path represents a Directory</param>
        /// <returns>
        /// If the <paramref name="link"/> represents a symbolic link, this will return the target path.
        /// Otherwise returns <see langword="null"/> .
        /// </returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public unsafe static string? GetReparseDataTarget(string link, bool isDirectory)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (string.IsNullOrWhiteSpace(link)) throw new ArgumentNullException(nameof(link));
            
            using SafeFileHandle fileHandle = GetSafeFileHandle(link, isDirectory, true);

            int bufferSize;
            bufferSize = (int)Win32.PInvoke.MAXIMUM_REPARSE_DATA_BUFFER_SIZE;

            Span<sbyte> outBuffer = new sbyte[bufferSize];
            outBuffer.Clear();

            fixed (sbyte* outBufferPtr = outBuffer)
            {
                uint bytes;
                bool success = Win32.PInvoke.DeviceIoControl(fileHandle, Win32.PInvoke.FSCTL_GET_REPARSE_POINT, null, 0U, outBufferPtr, (uint)outBuffer.Length, &bytes, null);
                if (!success)
                {
                    int errCode = Marshal.GetHRForLastWin32Error();

                    switch ((WIN32_ERROR)errCode)
                    {
                        case WIN32_ERROR.ERROR_SUCCESS:
                        case WIN32_ERROR.ERROR_FSFILTER_OP_COMPLETED_SUCCESSFULLY:
                            break;
                        case WIN32_ERROR.ERROR_NOT_A_REPARSE_POINT:
                        case (WIN32_ERROR)0x80071126: //alternate not reparse point
                            return null;

                        default:
                            Marshal.ThrowExceptionForHR(errCode);
                            return null;
                    }
                }

                ref var data = ref *(REPARSE_DATA_BUFFER*)outBufferPtr;

                Span<char> targetSpan;
                switch (data.ReparseTag)
                {
                    case Win32.PInvoke.IO_REPARSE_TAG_SYMLINK:
                        ref var symReparse = ref data.Anonymous.SymbolicLinkReparseBuffer;

                        targetSpan = symReparse.PathBuffer.AsSpan((int)bytes / sizeof(char)).Slice(symReparse.SubstituteNameOffset / sizeof(char), symReparse.SubstituteNameLength/sizeof(char));
                        return true switch
                        {
                            true when symReparse.Flags == MSWin.Wdk.PInvoke.SYMLINK_FLAG_RELATIVE => targetSpan.ToString(),
                            true when targetSpan.Length > 4 && targetSpan.StartsWith(@"\??\".AsSpan()) => targetSpan.Slice(4).ToString(),
                            _ => throw new Exception("Invalid SymLink data was detected")
                        };

                    case Win32.PInvoke.IO_REPARSE_TAG_MOUNT_POINT:
                        ref var mountReparse = ref data.Anonymous.MountPointReparseBuffer;
                        return mountReparse.PathBuffer.AsSpan((int)bytes / sizeof(char)).Slice(mountReparse.SubstituteNameOffset / sizeof(char), mountReparse.SubstituteNameLength / sizeof(char)).ToString();
                }
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UintHasFlag(this uint value, uint flag) => value == flag || flag == (value & flag);

    }
}

#pragma warning restore CA1416 // Validate patform compatibility
#pragma warning restore IDE0079 // Remove unnecessary suppression
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.