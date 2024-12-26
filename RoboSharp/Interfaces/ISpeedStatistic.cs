using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using RoboSharp.Results;

namespace RoboSharp.Interfaces
{
    /// <summary>
    /// Provide Read-Only access to a SpeedStatistic
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/ISpeedStatistic"/>
    /// </remarks>
    public interface ISpeedStatistic : INotifyPropertyChanged, ICloneable
    {
        /// <summary> Average Transfer Rate in Bytes/Second </summary>
        decimal BytesPerSec { get; }

        /// <summary> Average Transfer Rate in MB/Minute</summary>
        decimal MegaBytesPerMin { get; }

        /// <inheritdoc cref="SpeedStatistic.ToString"/>
        string ToString();

        /// <returns>new <see cref="SpeedStatistic"/> object </returns>
        /// <inheritdoc cref="SpeedStatistic.SpeedStatistic(ISpeedStatistic)"/>
        new SpeedStatistic Clone();
    }
}
