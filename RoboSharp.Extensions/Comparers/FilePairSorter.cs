using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Comparers
{
    /// <summary>
    /// Sort FilePairs by their <see cref="ProcessedFileInfo.FileClass"/> ( the <see cref="ProcessedFileFlag"/> enum )
    /// </summary>
    public sealed class FilePairSorter<T> : IComparer<T> where T : IProcessedFilePair
    {
        /// <summary>
        /// The default constructor
        /// </summary>
        /// <param name="config">The <see cref="RoboSharpConfiguration"/> used to get the <see cref="ProcessedFileFlag"/> for each <see cref="IProcessedFilePair"/> being compared. </param>
        public FilePairSorter(RoboSharpConfiguration config)
        {
            Config = config;
        }

        private RoboSharpConfiguration Config { get; }

        /// <summary>
        /// Evaluate the <see cref="IProcessedFilePair.ProcessedFileInfo"/> and sort accordingly
        /// </summary>
        /// <inheritdoc/>
        public int Compare(T x, T y)
        {
            return Compare(x?.ProcessedFileInfo, y?.ProcessedFileInfo);
        }

        /// <inheritdoc cref="Compare(T, T)"/>
        public int Compare<T1>(T1 x, T1 y) where T1 : IProcessedFilePair
        {
            return Compare(x?.ProcessedFileInfo, y?.ProcessedFileInfo);
        }

        /// <inheritdoc cref="IComparer{T}.Compare(T, T)"/>
        public int Compare(ProcessedFileInfo x, ProcessedFileInfo y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            _ = x.TryGetFileClass(Config, out var XF);
            _ = y.TryGetFileClass(Config, out var YF);
            int result = XF.CompareTo(YF);
            return result;
        }
    }
}
