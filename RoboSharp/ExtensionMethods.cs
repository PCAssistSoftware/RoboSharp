using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RoboSharp;

namespace RoboSharp
{
    internal static class ExtensionMethods
    {
#if NETSTANDARD2_0 || NET452_OR_GREATER

        // Adds the TryDequeue method to Net452 & NetStandard, since it was not introduced until .NetStandard2.1
        internal static bool TryDequeue<T>(this Queue<T> queue, out T result)
        {
            try
            {
                result = queue.Dequeue();
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        internal static bool Contains(this string outerString, string innerString, StringComparison stringComparison)
        {
            switch (stringComparison)
            {
                case StringComparison.CurrentCultureIgnoreCase:
                    return outerString.ToLower(CultureInfo.CurrentCulture).Contains(innerString.ToLower(CultureInfo.CurrentCulture));
                case StringComparison.InvariantCultureIgnoreCase:
                    return outerString.ToLowerInvariant().Contains(innerString.ToLowerInvariant());
                default:
                    return outerString.Contains(innerString);
            }
        }

        internal static string Trim(this string text, char character)
        {
            return text.Trim(trimChars: new char[] { character });
        }

        internal static bool IsPathFullyQualified(this string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path = path.UnwrapQuotes();
            if (path.Length < 2) return false; //There is no way to specify a fixed path with one character (or less).
            if (path.Length == 2 && IsValidDriveChar(path[0]) && path[1] == System.IO.Path.VolumeSeparatorChar) return true; //Drive Root C:
            if (path.Length >= 3 && IsValidDriveChar(path[0]) && path[1] == System.IO.Path.VolumeSeparatorChar && IsDirectorySeperator(path[2])) return true; //Check for standard paths. C:\
            if (path.Length >= 3 && IsDirectorySeperator(path[0]) && IsDirectorySeperator(path[1])) return true; //This is start of a UNC path
            return false; //Default
        }

        private static bool IsDirectorySeperator(char c) => c == System.IO.Path.DirectorySeparatorChar | c == System.IO.Path.AltDirectorySeparatorChar;
        private static bool IsValidDriveChar(char c) => c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';

#else
        internal static bool IsPathFullyQualified(this string path) => System.IO.Path.IsPathFullyQualified(path.UnwrapQuotes());
#endif

        internal static string UnwrapQuotes(this string path)
        {
            if (path.StartsWith("\"") && path.EndsWith("\"")) return path.Substring(1, path.Length - 1);
            return path;
        }
        
        internal static string RemoveFirstOccurrence(this string text, string removal, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrWhiteSpace(text) | string.IsNullOrWhiteSpace(removal) || !text.Contains(removal, comparison))
                return text;
            return text.Remove(text.IndexOf(removal, comparison), removal.Length);
        }

        internal static string TrimStart(this string text, string trim, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrWhiteSpace(text) | string.IsNullOrWhiteSpace(trim) || !text.StartsWith(trim, comparison))
                return text;
            return text.Remove(0, trim.Length);
        }

        /// <summary> Encase the LogPath in quotes if needed </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static string WrapPath(this string pathToWrap)
        {
            if (string.IsNullOrWhiteSpace(pathToWrap))return string.Empty;
            return new StringBuilder().AppendWrappedPath(pathToWrap).ToString();
        }

        /// <remarks> Extension method provided by RoboSharp package </remarks>
        /// <inheritdoc cref="System.String.IsNullOrWhiteSpace(string)"/>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        internal static bool IsNotEmpty(this string value) => !string.IsNullOrWhiteSpace(value);

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static long TryConvertLong(this string val)
        {
            try
            {
                return Convert.ToInt64(val);
            }
            catch
            {
                return 0;
            }
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static int TryConvertInt(this string val)
        {
            try
            {
                return Convert.ToInt32(val);
            }
            catch
            {
                return 0;
            }

        }
        
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        public static string CleanOptionInput(this string option)
        {
            // Get rid of forward slashes
            option = option.Replace("/", "");
            // Get rid of padding
            option = option.Trim();
            return option;
        }

        public static string CleanDirectoryPath(this string path)
        {
            // Get rid of leading/trailing for single and double quotes
            path = path?.Trim('\'', '\"');

            //Validate against null / empty strings. 
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            // Get rid of padding
            path = path.Trim();

            // Get rid of trailing Directory Seperator Chars
            path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            //Sanitize invalid paths -- Convert E: to E:\
            if (path.Length <= 2)
            {
                if (DriveRootRegex.IsMatch(path))
                    return path.ToUpper() + '\\';
                else
                    return path;
            }

            // Fix UNC paths that are the root directory of a UNC drive
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri URI) && URI.IsUnc)
            {
                if (path.EndsWith("$"))
                {
                    path += '\\';
                }
            }

            return path;
        }

        private static readonly Regex DriveRootRegex = new Regex("[A-Za-z]:", RegexOptions.Compiled);

        /// <summary>
        /// Check if the string ends with a directory seperator character
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        public static bool EndsWithDirectorySeperator(this string path) => path.EndsWith(Path.DirectorySeparatorChar.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString());

        /// <summary>
        /// Convert <paramref name="StrTwo"/> into a char[]. Perform a ForEach( Char in strTwo) loop, and append any characters in Str2 to the end of this string if they don't already exist within this string.
        /// </summary>
        /// <param name="StrOne"></param>
        /// <param name="StrTwo"></param>
        /// <returns></returns>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static string CombineCharArr(this string StrOne, string StrTwo)
        {
            if (String.IsNullOrWhiteSpace(StrTwo)) return StrOne;
            if (String.IsNullOrWhiteSpace(StrOne)) return StrTwo ?? StrOne;
            string ret = StrOne;
            char[] S2 = StrTwo.ToArray();
            foreach (char c in S2)
            {
                if (!ret.Contains(c))
                    ret += c;
            }
            return ret;
        }

        /// <summary>
        /// Compare the current value to that of the supplied value, and take the greater of the two.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="i2"></param>
        /// <returns></returns>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static int GetGreaterVal(this int i, int i2) => i >= i2 ? i : i2;

        /// <summary>
        /// Evaluate this string. If this string is null or empty, replace it with the supplied string.
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static string ReplaceIfEmpty(this string str1, string str2) => String.IsNullOrWhiteSpace(str1) ? str2 ?? String.Empty : str1;
    }

    internal static class StringBuilderExtensions
    {
        /// <summary> Encase the LogPath in quotes if needed </summary>
        public static StringBuilder AppendWrappedPath(this StringBuilder builder, string pathToWrap)
        {

            if (string.IsNullOrWhiteSpace(pathToWrap)) return builder;

            string trimmedPath = pathToWrap.Trim();
            if (trimmedPath.Length > 3 && trimmedPath[0] == '"' && trimmedPath.Last() == '"')
            {
                return builder.Append(trimmedPath);
            }
            else if (trimmedPath.Any(Char.IsWhiteSpace))
            {
                builder.Append('"').Append(trimmedPath);
                if (trimmedPath.Last() == '\\')
                {
                    // Fix for trailing slashes acting as escape characters and erroring out robocopy - Trim all trailing slashes, ensure 2 occur
                    builder.TrimEnd('\\').Append(@"\\");
                }
                return builder.Append('"');
            }
            else
                return builder.Append(trimmedPath);
        }

        public static IEnumerable<char> AsEnumerable(this StringBuilder builder)
        {
            int i = 0;
            while (i < builder.Length)
            {
                yield return builder[i];
                i++;
            }
            yield break;
        }

        /// <inheritdoc cref="IndexOf(StringBuilder, string, bool, int)"/>
        public static int IndexOf(this StringBuilder builder, string text, bool isCaseSensitive = false)
            => IndexOf(builder, text, isCaseSensitive, 0);

        /// <returns>-1 if the text does not exist within the builder, otherwise the starting index</returns>
        public static int IndexOf(this StringBuilder builder, string text, bool isCaseSensitive, int startIndex)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (startIndex > builder.Length) throw new ArgumentException("startIndex value greater than input string length");
            if (string.IsNullOrEmpty(text)) return -1;
            int builderIndex = startIndex;
            int searchIndex = 0;
            int? foundIndex = null;
            char builderChar;
            char searchChar;

            while (builderIndex < builder.Length)
            {
                // Get Char
                if (isCaseSensitive)
                {
                    builderChar = builder[builderIndex];
                    searchChar = text[searchIndex];
                }
                else
                {
                    builderChar = char.ToLowerInvariant(builder[builderIndex]);
                    searchChar = char.ToLowerInvariant(text[searchIndex]);
                }
                // Compare
                if (builderChar.Equals(searchChar))
                {
                    searchIndex++;
                    foundIndex ??= builderIndex;
                    if (searchIndex >= text.Length)
                        return foundIndex.Value;
                }
                else
                {
                    searchIndex = 0;
                    foundIndex = null;
                }
                builderIndex++;
            }
            return -1;
        }

        public static StringBuilder SubString(this StringBuilder builder, int startIndex) => SubString(builder, startIndex, -1);
        public static StringBuilder SubString(this StringBuilder builder, int startIndex, int length)
        {
            StringBuilder result = new StringBuilder();
            int i = startIndex;
            while (i < builder.Length && result.Length < length)
            {
                result.Append(builder[i]);
                i++;
            }
            return result;
        }
        public static bool StartsWith(this StringBuilder builder, char c) => builder[0] == c;
        public static bool StartsWith(this StringBuilder builder, string value, bool caseSensitive = false)
        {
            int index = 0;
            foreach (char c in value)
            {
                if (caseSensitive)
                {
                    if (!c.Equals(builder[index]))
                        return false;
                }
                else if (!char.ToLowerInvariant(c).Equals(char.ToLowerInvariant(builder[index])))
                {
                    return false;
                }
                index++;
            }
            return true;
        }

        public static bool EndsWith(this StringBuilder builder, char c) => builder[builder.Length - 1] == c;
        public static bool EndsWith(this StringBuilder builder, string text)
        {
            if (builder.Length < text.Length) return false;
            int i = 1;
            foreach (char c in text.Reverse())
                if (builder[builder.Length - i] == c)
                    i++;
                else
                    return false;
            return true;
        }

        public static StringBuilder Trim(this StringBuilder builder) => builder.TrimStart().TrimEnd();
        public static StringBuilder TrimStart(this StringBuilder builder)
        {
            if (builder.Length < 1) return builder;
            while (builder.Length > 0 && char.IsWhiteSpace(builder[0]))
                builder.Remove(0, 1);
            return builder;
        }
        public static StringBuilder TrimEnd(this StringBuilder builder)
        {
            if (builder.Length < 1) return builder;
            int lastIndex;
            while (builder.Length > 0 && char.IsWhiteSpace(builder[lastIndex = builder.Length - 1]))
                builder.Remove(lastIndex, 1);
            return builder;
        }

        public static StringBuilder Trim(this StringBuilder builder, params char[] c) => builder.TrimStart(c).TrimEnd(c);
        public static StringBuilder TrimStart(this StringBuilder builder, params char[] c)
        {
            if (builder.Length < 1) return builder;
            while (builder.Length > 0 && c.Contains(builder[0]))
                builder.Remove(0, 1);
            return builder;
        }
        public static StringBuilder TrimEnd(this StringBuilder builder, params char[] c)
        {
            if (builder.Length < 1) return builder;
            int lastIndex;
            while (builder.Length > 0 && c.Contains(builder[lastIndex = builder.Length - 1]))
                builder.Remove(lastIndex, 1);
            return builder;
        }

        public static StringBuilder TrimStart(this StringBuilder builder, string textToRemove)
        {
            if (builder.StartsWith(textToRemove))
                builder.Remove(0, textToRemove.Length);
            return builder;
        }

        /// <summary>
        /// Trims, unwraps quotes, then trims again
        /// </summary>
        public static StringBuilder UnwrapQuotes(this StringBuilder builder)
        {
            builder.Trim();
            if (builder.Length > 2 && builder[0] == '"' && builder[builder.Length - 1] == '0')
                builder.Trim('"');
            return builder.Trim();
        }
    }
}

namespace System.Threading

{
    /// <summary>
    /// Contains methods for CancelleableSleep and WaitUntil
    /// </summary>
    internal static class ThreadEx
    {

        /// <summary>
        /// Wait synchronously until this task has reached the specified <see cref="TaskStatus"/>
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static void WaitUntil(this Task t, TaskStatus status)
        {
            while (t.Status < status)
                Thread.Sleep(100);
        }

        /// <summary>
        /// Wait asynchronously until this task has reached the specified <see cref="TaskStatus"/> <br/>
        /// Checks every 100ms
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static async Task WaitUntilAsync(this Task t, TaskStatus status)
        {
            while (t.Status < status)
                await Task.Delay(100);
        }

        /// <summary>
        /// Wait synchronously until this task has reached the specified <see cref="TaskStatus"/><br/>
        /// Checks every <paramref name="interval"/> milliseconds
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static async Task WaitUntilAsync(this Task t, TaskStatus status, int interval)
        {
            while (t.Status < status)
                await Task.Delay(interval);
        }

        /// <param name="timeSpan">TimeSpan to sleep the thread</param>
        /// <param name="token"><inheritdoc cref="CancellationToken"/></param>
        /// <inheritdoc cref="CancellableSleep(int, CancellationToken)"/>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        internal static Task<bool> CancellableSleep(TimeSpan timeSpan, CancellationToken token)
        {
            return CancellableSleep((int)timeSpan.TotalMilliseconds, token);
        }

        /// <inheritdoc cref="CancellableSleep(int, CancellationToken[])"/>
        /// <inheritdoc cref="CancellableSleep(int, CancellationToken)"/>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        internal static Task<bool> CancellableSleep(TimeSpan timeSpan, CancellationToken[] tokens)
        {
            return CancellableSleep((int)timeSpan.TotalMilliseconds, tokens);
        }

        /// <summary>
        /// Use await Task.Delay to sleep the thread. <br/>
        /// </summary>
        /// <returns>True if timer has expired (full duration slep), otherwise false.</returns>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait"/></param>
        /// <param name="token"><inheritdoc cref="CancellationToken"/></param>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        internal static Task<bool> CancellableSleep(int millisecondsTimeout, CancellationToken token)
        {
            return Task.Delay(millisecondsTimeout, token).ContinueWith(t => t.Exception == default);
        }

        /// <summary>
        /// Use await Task.Delay to sleep the thread. <br/>
        /// Supplied tokens are used to create a LinkedToken that can cancel the sleep at any point.
        /// </summary>
        /// <returns>True if slept full duration, otherwise false.</returns>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait"/></param>
        /// <param name="tokens">Use <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken[])"/> to create the token used to cancel the delay</param>
        /// <inheritdoc cref="CancellableSleep(int, CancellationToken)"/>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        internal static Task<bool> CancellableSleep(int millisecondsTimeout, CancellationToken[] tokens)
        {
            var token = CancellationTokenSource.CreateLinkedTokenSource(tokens).Token;
            return Task.Delay(millisecondsTimeout, token).ContinueWith(t => t.Exception == default);
        }
    }

}
