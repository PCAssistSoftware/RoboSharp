using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoboSharp.Results;

namespace RoboSharp.Interfaces
{
    /// <summary>
    /// Object that provides <see cref="IStatistic"/> objects whose events can be bound to report estimated RoboCommand / RoboQueue progress periodically.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/IProgressEstimator"/>
    /// </remarks>
    public interface IProgressEstimator : IResults
    {

        /// <summary>
        /// Estimate of current number of directories processed while the job is still running. <br/>
        /// Estimate is provided by parsing of the LogLines produces by RoboCopy.
        /// </summary>
        new IStatistic DirectoriesStatistic { get; }

        /// <summary>
        /// Estimate of current number of files processed while the job is still running. <br/>
        /// Estimate is provided by parsing of the LogLines produces by RoboCopy.
        /// </summary>
        new IStatistic FilesStatistic { get; }

        /// <summary>
        /// Estimate of current number of bytes processed while the job is still running. <br/>
        /// Estimate is provided by parsing of the LogLines produces by RoboCopy.
        /// </summary>
        new IStatistic BytesStatistic { get; }

        /// <summary>
        /// Parse this object's stats into a <see cref="RoboCopyExitCodes"/> enum.
        /// </summary>
        RoboCopyExitCodes GetExitCode();

        /// <summary> Event that occurs when this IProgressEstimatorObject's IStatistic values have been updated. </summary>
        event ProgressEstimator.UIUpdateEventHandler ValuesUpdated;

    }
}
