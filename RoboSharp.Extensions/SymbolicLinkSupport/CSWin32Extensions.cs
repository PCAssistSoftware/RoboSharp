using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace RoboSharp.Extensions.SymbolicLinkSupport
{
    /// <summary>
    /// Extension Methods for CSWin32 Source Generated classes
    /// </summary>
    internal static class CSWin32Extensions
    {

        /*
        * These extensions were derived from CSWin32 0.3.106, since they were removed in 0.3.183
        * AsSpan exists in .net8
        */

#if !NET8_0_OR_GREATER
        /// <inheritdoc cref="Span{T}.Span(void*, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<T> AsSpan<T>(this VariableLengthInlineArray<T> array, int length)
            where T : unmanaged
        {
            unsafe
            {
                return new ReadOnlySpan<T>(&array.e0, length);
            }
        }

        /*
         * VariableLengthInlineArray<T, TBlittable> was added in CSWin32 0.3.183
         */

        /// <inheritdoc cref="AsSpan{T}(VariableLengthInlineArray{T}, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<T> AsSpan<T, TBlittable>(this ref VariableLengthInlineArray<T, TBlittable> array, int length)
            where T : unmanaged
            where TBlittable : unmanaged
        {
            ref T _e0 = ref Unsafe.As<TBlittable, T>(ref array.e0); // same as net8 cswin32 0.3.183
#if NETCOREAPP || NET5_0_OR_GREATER
            return MemoryMarshal.CreateReadOnlySpan(ref _e0, length);
#else
            ReadOnlySpan<T> span;
            unsafe
            {
                fixed (T* ptr = &_e0)
                {
                    span = new ReadOnlySpan<T>(ptr, length);
                }
            }
            return span;
#endif
        }
#endif

        }
}

