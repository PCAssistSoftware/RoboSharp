using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using RoboSharp.Results;

namespace RoboSharp.Interfaces
{
    /// <summary>
    /// Interface to provide Read-Only access to a <see cref="RoboCopyResultsList"/>
    /// <para/>Implements: <br/>
    /// <see cref="IEnumerable{T}"/> where T = <see cref="RoboCopyResults"/> <br/>
    /// <see cref="ICloneable"/>
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/IRoboCopyResultsList"/>
    /// </remarks>
    public interface IRoboCopyResultsList : IResults, IEnumerable<RoboCopyResults>, INotifyCollectionChanged
    {
        #region < Properties >

        /// <summary>
        /// Get the <see cref="RoboCopyResults"/> objects at the specified index.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"/>
        RoboCopyResults this[int i] { get; }
        
        /// <inheritdoc cref="RoboCopyResultsList.SpeedStatistic"/>
        ISpeedStatistic SpeedStatistic { get; }

        /// <inheritdoc cref="RoboCopyResultsList.Status"/>
        new IRoboCopyCombinedExitStatus Status { get; }

        /// <inheritdoc cref="RoboCopyResultsList.Collection"/>
        IReadOnlyList<RoboCopyResults> Collection { get; }

        /// <inheritdoc cref="RoboCopyResultsList.Count"/>
        int Count { get; }

        #endregion

        #region < Methods >

        /// <summary>
        /// Get a snapshot of the ByteStatistics objects from this list.
        /// </summary>
        /// <returns>New array of the ByteStatistic objects</returns>
        IStatistic[] GetByteStatistics();

        /// <summary>
        /// Get a snapshot of the DirectoriesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the DirectoriesStatistic objects</returns>
        IStatistic[] GetDirectoriesStatistics();

        /// <summary>
        /// Get a snapshot of the FilesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the FilesStatistic objects</returns>
        IStatistic[] GetFilesStatistics();

        /// <summary>
        /// Get a snapshot of the FilesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the FilesStatistic objects</returns>
        RoboCopyExitStatus[] GetStatuses();

        /// <summary>
        /// Get a snapshot of the FilesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the FilesStatistic objects</returns>
        ISpeedStatistic[] GetSpeedStatistics();

        /// <summary>
        /// Combine the <see cref="RoboCopyResults.RoboCopyErrors"/> into a single array of errors
        /// </summary>
        /// <returns>New array of the ErrorEventArgs objects</returns>
        ErrorEventArgs[] GetErrors();

        #endregion
    }
}
