using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoboSharp.Results;

namespace RoboSharp.Interfaces
{
    /// <summary>
    /// Provides <see cref="IStatistic"/> objects for File, Directory, and Bytes to allow comparison between ProgressEstimator and RoboCopyResults objects
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/IResults"/>
    /// </remarks>
    public interface IResults
    {
        /// <summary> Information about number of Directories Copied, Skipped, Failed, etc.</summary>
        IStatistic DirectoriesStatistic { get; }

        /// <summary> Information about number of Files Copied, Skipped, Failed, etc.</summary>
        IStatistic FilesStatistic { get; }

        /// <summary> Information about number of Bytes processed.</summary>
        IStatistic BytesStatistic { get; }

        /// <inheritdoc cref="RoboCopyExitStatus"/>
        RoboCopyExitStatus Status { get; }

    }
}
