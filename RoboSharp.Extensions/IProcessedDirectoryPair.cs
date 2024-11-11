using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Interface that extends <see cref="IDirectoryPair"/> to include information from <see cref="PairEvaluator"/>
    /// </summary>
    public interface IProcessedDirectoryPair : IDirectoryPair
    {
        /// <summary>
        /// The ProcessedFileInfo that represents how the IRoboCommand has processed the pair
        /// </summary>
        ProcessedFileInfo ProcessedFileInfo { get; set; }
    }
}
