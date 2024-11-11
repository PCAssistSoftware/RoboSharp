using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Interface that extends <see cref="IFilePair"/> to include information obtained via the <see cref="PairEvaluator"/>
    /// </summary>
    public interface IProcessedFilePair : IFilePair
    {
        /// <summary>
        /// The Parent IDirectoryPair. This may be null.
        /// </summary>
        IProcessedDirectoryPair Parent { get; }

        /// <summary>
        /// The ProcessedFileInfo that represents how the IRoboCommand has processed the pair
        /// </summary>
        ProcessedFileInfo ProcessedFileInfo { get; set; }

        /// <summary>
        /// Stores the result of <see cref="PairEvaluator.FilterAndSortSourceFiles{T}(IEnumerable{T})"/>
        /// </summary>
        bool ShouldCopy { get; set; }

        /// <summary>
        /// Stores the result of <see cref="PairEvaluator.ShouldPurge(IProcessedFilePair)"/>
        /// </summary>
        bool ShouldPurge { get; set; }
    }
}
