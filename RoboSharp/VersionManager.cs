using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    
    /// <summary>
    /// Class that retrieves the OS version.
    /// </summary>
    public static class VersionManager
    {
        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(UseRtlGetVersion)]
        public enum VersionCheckType
        {
            /// <summary>
            /// Use ntdll.dll (windows only) to retrieve the OS Version information
            /// </summary>
            UseRtlGetVersion = 0,

            /// <summary>
            /// Use Microsoft.Management.Infrastructure
            /// </summary>
            UseWMI = 1,
        }

        /// <summary>
        /// True if running in a windows environment, otherwise false.
        /// </summary>
#if NETFRAMEWORK
        public static readonly bool IsPlatformWindows = true;
#else
#if NET6_0_OR_GREATER
        [SupportedOSPlatformGuard(nameof(OSPlatform.Windows))]
#endif
        public static readonly bool IsPlatformWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

        public static VersionCheckType VersionCheck { get; set; } = VersionManager.VersionCheckType.UseRtlGetVersion;


        private static double? version;
        public static double Version
        {
            get
            {
                if (version == null)
                {
                    if (!IsPlatformWindows || VersionCheck == VersionCheckType.UseWMI)
                    {
                        var v = GetOsVersion();
                        version = GetOsVersionNumber(v);
                        return version.Value;
                    }
                    else
                    {
                        var osVersionInfo = new OSVERSIONINFOEX { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX)) };
                        var hrResult = RtlGetVersion(ref osVersionInfo);
                        if (hrResult != 0) Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                        var versionString = $"{osVersionInfo.MajorVersion}.{osVersionInfo.MinorVersion}{osVersionInfo.BuildNumber}";
                        version = GetOsVersionNumber(versionString);
                        return version.Value;
                    }
                }
                else
                {
                    return version.Value;
                }
            }
        }

        static VersionManager()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
        }

        /// <exception cref="PlatformNotSupportedException"></exception>
        public static void ThrowIfNotWindowsPlatform(string message = default)
        {
            if (!IsPlatformWindows)
                throw new PlatformNotSupportedException(message ?? "This function is only available on Windows");
        }

        private static string GetOsVersion()
        {
            if (!IsPlatformWindows) return Environment.OSVersion.Version.ToString();

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            using (var session = Microsoft.Management.Infrastructure.CimSession.Create("."))

            {
                var win32OperatingSystemCimInstance = session.QueryInstances("root\\cimv2", "WQL", "SELECT Version FROM  Win32_OperatingSystem").FirstOrDefault();

                if (win32OperatingSystemCimInstance?.CimInstanceProperties["Version"] != null)
                {
                    return win32OperatingSystemCimInstance.CimInstanceProperties["Version"].Value.ToString();
                }
            }
#elif NETFRAMEWORK
            using (System.Management.ManagementObjectSearcher objMOS = new System.Management.ManagementObjectSearcher("SELECT * FROM  Win32_OperatingSystem"))
            {
                foreach (var version in from System.Management.ManagementObject objManagement in objMOS.Get()
                                        let version = objManagement.GetPropertyValue("Version")
                                        where version != null
                                        select version)
                {
                    return version.ToString();
                }
            }
#endif

            return Environment.OSVersion.Version.ToString();
        }

        private static double GetOsVersionNumber(string version)
        {
            if (version.IsNullOrWhiteSpace())
                return 0;

            var segments = version.Split(new char[] { '.' });
            var major = Convert.ToDouble(segments[0]);
            var otherSegments = segments.Skip(1).ToArray();
            var dec = Convert.ToDouble("." + string.Join("", otherSegments), CultureInfo.InvariantCulture);
            return major + dec;
        }

        /// <summary>
        /// taken from https://stackoverflow.com/a/49641055
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        [SecurityCritical]
        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);
        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            // The OSVersionInfoSize field must be set to Marshal.SizeOf(typeof(OSVERSIONINFOEX))
            internal int OSVersionInfoSize;
            internal int MajorVersion;
            internal int MinorVersion;
            internal int BuildNumber;
            internal int PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string CSDVersion;
            internal ushort ServicePackMajor;
            internal ushort ServicePackMinor;
            internal short SuiteMask;
            internal byte ProductType;
            internal byte Reserved;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
