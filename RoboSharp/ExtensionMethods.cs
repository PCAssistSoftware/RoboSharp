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

namespace RoboSharp
{
    internal static class ExtensionMethods
    {
#if NETSTANDARD2_0 || NETFRAMEWORK

        // Adds the TryDequeue method to NetStandard2.0, since it was not introduced until .NetStandard2.1
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


#endif

        internal static IEnumerable<T> WhereNot<T>(this IEnumerable<T> collection, Predicate<T> where)
        {
            foreach(T item in collection)
                if (!where(item))
                    yield return item;
        }

        internal static bool EndsWith(this string text, char c) => text.Length >= 1 && text[text.Length - 1] == c;

        /// <summary>
        /// Trims the string, removes any double-quotes, trims again
        /// </summary>
        internal static string UnwrapQuotes(this string path)
        {
            return path.Trim().Trim('"').Trim();
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

        /// <remarks> Extension method provided by RoboSharp package </remarks>
        /// <inheritdoc cref="System.String.IsNullOrWhiteSpace(string)"/>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        internal static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        /// <summary> Check if string is null or whitespace. </summary>
        /// <returns>Opposite of <see cref="string.IsNullOrWhiteSpace(string?)"/></returns>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
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

        /// <summary>
        /// Check if the string ends with a directory seperator character
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden()]
        public static bool EndsWithDirectorySeperator(this string path) => path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);

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
            return new StringBuilder().Append(StrOne.Union(StrTwo).WhereNot(Char.IsWhiteSpace).ToArray()).ToString(); // Union the two strings
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
        internal static void Add(this List<string> list, StringBuilder builder) => list.Add(builder.ToString());

        /// <summary>
        /// Evaluates the <paramref name="directoryPath"/> and sanitizes it if necessary.
        /// </summary>
        /// <returns>
        /// - If the string is null or empty : returns string.empty
        /// <br/> - If the string contains no double-quotes and no white-space : returns input string (assumes valid input)
        /// <br/> - Otherwise trims all white-space, and double-quotes from the beginning and end.
        /// </returns>
        public static string CleanDirectoryPath(this string directoryPath)
        {
            //Validate against null / empty strings. 
            if (string.IsNullOrWhiteSpace(directoryPath)) return string.Empty;

            // if path does not contain quotes or white-space, no processing required (assume ok)
            if (!(directoryPath.Any(Char.IsWhiteSpace) || directoryPath.Contains('"'))) return directoryPath;

            StringBuilder sanitizer = new StringBuilder(directoryPath).Trim(' ', '"');

            // Handle Local Path Roots -- c: -> C:\
            if (sanitizer.Length == 2 && char.IsLetter(sanitizer[0]) && sanitizer[1] == ':')
            {
                sanitizer[0] = char.ToUpper(sanitizer[0]);
                sanitizer.Append(Path.DirectorySeparatorChar);
            }
            
            return sanitizer.ToString();
        }

        /// <summary> Encase the <paramref name="pathToWrap"/> in quotes if needed </summary>
        /// <inheritdoc cref="AppendWrappedPath(StringBuilder, string)"/>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        internal static string WrapPath(this string pathToWrap)
        {
            if (string.IsNullOrWhiteSpace(pathToWrap)) return string.Empty;
            var trimmedPath = pathToWrap.Trim();
            if (trimmedPath.Length > 3 && trimmedPath[0] == '"' && trimmedPath.EndsWith('"'))
            {
                return trimmedPath;
            }else if (trimmedPath.Any(Char.IsWhiteSpace))
            {
                return $@"""{trimmedPath.TrimEnd('\\', '/')}""";
            }
            else
            {
                return trimmedPath;
            }
        }

        /// <summary> 
        /// Append the <paramref name="pathToWrap"/> to the <paramref name="builder"/>, surrounding it with quotes if needed.
        /// <br/>If wrapping is required, sanitizes to prevent escaping the quotations ( see remarks )
        /// </summary>
        /// <remarks>
        /// - Directory Paths should have already been sanitizied via <see cref="CleanDirectoryPath(string)"/> 
        /// <br/> - Clean the directory to prevent this issue : <see href="https://superuser.com/questions/1544437/why-does-the-trailing-slash-on-the-target-confuse-robocopy"/>
        /// </remarks>
        /// Issue being resolved:
        ///     robocopy D:\SomeFolder\ "Q:\SomeOtherFolder\"
        ///     Resulting Source = D:\SomeFolder\
        ///     Resulting Dest = Q:\SomeOtherFolder"  ( Note quote is present due to escape character \ )
        public static StringBuilder AppendWrappedPath(this StringBuilder builder, string pathToWrap)
        {
            if (string.IsNullOrWhiteSpace(pathToWrap)) return builder;

            // Check if already wrapped
            var trimmedPath = pathToWrap.Trim();
            if (trimmedPath.Length > 3 && trimmedPath[0] == '"' && trimmedPath.EndsWith('"'))
            {
                return builder.Append(trimmedPath);
            }
            // Wrap if contains whitespace
            else if (trimmedPath.Any(Char.IsWhiteSpace))
            {
                builder.Append('"').Append(trimmedPath);
                
                // Trim any directory separators from the end of the path, then apply the '/' character, which does not act as an escape character
                if (builder.EndsWith('\\')) 
                    builder.TrimEnd('\\', '/').Append('/');
                
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

        internal static int LastIndexOf(this StringBuilder builder, params char[] chars)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            for (int i = builder.Length - 1; i >= 0; i--)
            {
                if (chars.Contains(builder[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// <see cref="StringBuilder.Remove(int, int)"/> but handles the <paramref name="length"/> exceeding character count
        /// </summary>
        internal static StringBuilder RemoveSafe(this StringBuilder builder, int index, int length)
        {
            if (index + length > builder.Length)
                return builder.Remove(index, builder.Length - index);
            else
                return builder.Remove(index, length);
        }

        /// <inheritdoc cref="RemoveString(StringBuilder, string, Action)"/>
        internal static bool RemoveString(this StringBuilder builder, string searchText)
        {
            return RemoveString(builder, searchText, null);
        }

        /// <summary>Removes all white space from the builder</summary>
        internal static StringBuilder RemoveWhiteSpace(this StringBuilder builder)
        {
            for (int i = builder.Length - 1; i >= 0; i--)
                if (Char.IsWhiteSpace(builder[i]))
                    builder.Remove(i, 1);
            return builder;
        }

        /// <summary>Removes any characters in the collection from the builder.</summary>
        internal static StringBuilder RemoveChars(this StringBuilder builder, params char[] chars)
        {
            bool trimWhiteSpace = chars.Contains(' ');
            for (int i = builder.Length - 1; i >= 0; i--)
                if (chars.Contains(builder[i]) || (trimWhiteSpace && Char.IsWhiteSpace(builder[i])))
                    builder.Remove(i, 1);
            return builder;
        }

        /// <returns>True if the first occurence was removed, otherwise false</returns>
        internal static bool RemoveString(this StringBuilder builder, string searchText, Action actionIfTrue)
        {
            int index = builder.IndexOf(searchText, false);
            Debugger.Instance.DebugMessage($"--> Switch {searchText}{(index >= 0 ? "" : " not")} detected.");
            if (index >= 0)
            {
                builder.Remove(index, searchText.Length);
                actionIfTrue?.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        public static StringBuilder SubString(this StringBuilder builder, int startIndex)
        {
            if (startIndex < 0 | startIndex > builder.Length) throw new ArgumentException("index out of range", nameof(startIndex));
            StringBuilder subBuilder = new StringBuilder();
            foreach(char c in builder.AsEnumerable().Skip(startIndex))
                subBuilder.Append(c);
            return subBuilder;
        }
        public static StringBuilder SubString(this StringBuilder builder, int startIndex, int length)
        {
            if (startIndex < 0 | startIndex > builder.Length) throw new ArgumentException("index out of range", nameof(startIndex));
            StringBuilder result = new StringBuilder();
            int i = startIndex;
            while (i < builder.Length && result.Length < length)
            {
                result.Append(builder[i]);
                i++;
            }
            return result;
        }
        public static bool StartsWith(this StringBuilder builder, char c) => builder.Length >= 1 && builder[0] == c;
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

        public static bool EndsWith(this StringBuilder builder, char c) => builder.Length >= 1 && builder[builder.Length - 1] == c;
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

        public static StringBuilder ToUpper(this StringBuilder builder)
        {
            for (int i = 0; i < builder.Length; i++)
                builder[i] = char.ToUpper(builder[i]);
            return builder;
        }

        public static StringBuilder Trim(this StringBuilder builder) => builder.TrimStart().TrimEnd();
        public static StringBuilder Trim(this StringBuilder builder, params char[] c) => builder.TrimStart(c).TrimEnd(c);
        
        public static StringBuilder TrimStart(this StringBuilder builder)
        {
            // adapted from System.String.Trim
            if (builder is null || builder.Length < 1) return builder;
            int i;
            for (i = 0; i < builder.Length && char.IsWhiteSpace(builder[i]); i++) { } 
            return builder.Remove(0, i);
        }
        
        public static StringBuilder TrimStart(this StringBuilder builder, params char[] c)
        {
            // adapted from System.String.Trim(char)
            if (builder is null || builder.Length < 1) return builder;
            bool trimWhiteSpace = c.Contains(' ');
            int i;
            for (i = 0;
                i < builder.Length && (c.Contains(builder[i]) || (trimWhiteSpace && Char.IsWhiteSpace(builder[i])));
                i++) 
            { }
            return builder.Remove(0, i);
        }

        public static StringBuilder TrimEnd(this StringBuilder builder)
        {
            // adapted from System.String.Trim
            if (builder is null || builder.Length < 1) return builder;
            int i = builder.Length - 1;
            while (i >= 0 && char.IsWhiteSpace(builder[i])) { i--; }

            if (i < 0)
                return builder.Clear();
            else
                return builder.Remove(i + 1, builder.Length - i - 1);
        }

        public static StringBuilder TrimEnd(this StringBuilder builder, params char[] c)
        {
            // adapted from System.String.Trim(char)
            if (builder is null || builder.Length < 1) return builder;
            bool trimWhiteSpace = c.Contains(' ');
            int i = builder.Length - 1;
            while(i >= 0 && (c.Contains(builder[i]) || (trimWhiteSpace && Char.IsWhiteSpace(builder[i])))) { i--; }

            if (i < 0 )
                return builder.Clear();
            else
                return builder.Remove(i + 1, builder.Length - i - 1);
        }

        public static StringBuilder TrimStart(this StringBuilder builder, string textToRemove)
        {
            if (builder.StartsWith(textToRemove))
                builder.Remove(0, textToRemove.Length);
            return builder;
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
