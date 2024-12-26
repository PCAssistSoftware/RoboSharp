using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using RoboSharp.Results;

namespace RoboSharp.Interfaces
{
    /// <summary>
    /// Read-Only interface for <see cref="RoboCopyCombinedExitStatus"/>
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/IRoboCopyCombinedExitStatus"/>
    /// </remarks>
    public interface IRoboCopyCombinedExitStatus : INotifyPropertyChanged, ICloneable
    {
        /// <inheritdoc cref="RoboCopyCombinedExitStatus.WasCancelled"/>
        bool WasCancelled { get; }

        /// <inheritdoc cref="RoboCopyCombinedExitStatus.AnyNoCopyNoError"/>
        bool AnyNoCopyNoError { get; }

        /// <inheritdoc cref="RoboCopyCombinedExitStatus.AnyWasCancelled"/>
        bool AnyWasCancelled { get; }

        /// <inheritdoc cref="RoboCopyCombinedExitStatus.AllSuccessful"/>
        bool AllSuccessful { get; }

        /// <inheritdoc cref="RoboCopyCombinedExitStatus.AllSuccessful_WithWarnings"/>
        bool AllSuccessful_WithWarnings { get; }

    }
}
