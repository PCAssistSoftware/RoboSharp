using System;
using System.ComponentModel;
using RoboSharp.Results;


namespace RoboSharp.Interfaces
{
    /// <summary>
    /// Provide Read-Only access to a <see cref="Statistic"/> object
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/IStatistic"/>
    /// </remarks>
    public interface IStatistic : INotifyPropertyChanged, ICloneable
    {

        #region < Properties >

        /// <summary>
        /// Name of the Statistics Object
        /// </summary>
        string Name { get; }

        /// <summary>
        /// <inheritdoc cref="Statistic.StatType"/>
        /// </summary>
        Statistic.StatType Type { get; }

        /// <summary> Total Scanned during the run</summary>
        long Total { get; }

        /// <summary> Total Copied </summary>
        long Copied { get; }

        /// <summary> Total Skipped </summary>
        long Skipped { get; }

        /// <summary>  </summary>
        long Mismatch { get; }

        /// <summary> Total that failed to copy or move </summary>
        long Failed { get; }

        /// <summary> Total Extra that exist in the Destination (but are missing from the Source)</summary>
        long Extras { get; }

        /// <inheritdoc cref="Statistic.NonZeroValue"/>
        bool NonZeroValue { get; }

        #endregion

        #region < Events >

        /// <inheritdoc cref="Statistic.PropertyChanged"/>
        new event PropertyChangedEventHandler PropertyChanged;

        /// <summary> Occurs when the <see cref="Total"/> Property is updated. </summary>
        event Statistic.StatChangedHandler OnTotalChanged;

        /// <summary> Occurs when the <see cref="Copied"/> Property is updated. </summary>
        event Statistic.StatChangedHandler OnCopiedChanged;

        /// <summary> Occurs when the <see cref="Skipped"/> Property is updated. </summary>
        event Statistic.StatChangedHandler OnSkippedChanged;

        /// <summary> Occurs when the <see cref="Mismatch"/> Property is updated. </summary>
        event Statistic.StatChangedHandler OnMisMatchChanged;

        /// <summary> Occurs when the <see cref="Failed"/> Property is updated. </summary>
        event Statistic.StatChangedHandler OnFailedChanged;

        /// <summary> Occurs when the <see cref="Extras"/> Property is updated. </summary>
        event Statistic.StatChangedHandler OnExtrasChanged;

        #endregion

        #region < ToString Methods >

        /// <inheritdoc cref="SpeedStatistic.ToString"/>
        string ToString();

        /// <inheritdoc cref="Statistic.ToString(bool, bool, string, bool)"/>
        string ToString(bool IncludeType, bool IncludePrefix, string Delimiter, bool DelimiterAfterType = false);

        /// <inheritdoc cref="Statistic.ToString_Type()"/>
        string ToString_Type();

        /// <inheritdoc cref="Statistic.ToString_Total(bool, bool)"/>
        string ToString_Total(bool IncludeType = false, bool IncludePrefix = true);

        /// <inheritdoc cref="Statistic.ToString_Copied"/>
        string ToString_Copied(bool IncludeType = false, bool IncludePrefix = true);

        /// <inheritdoc cref="Statistic.ToString_Extras"/>
        string ToString_Extras(bool IncludeType = false, bool IncludePrefix = true);

        /// <inheritdoc cref="Statistic.ToString_Failed"/>
        string ToString_Failed(bool IncludeType = false, bool IncludePrefix = true);

        /// <inheritdoc cref="Statistic.ToString_Mismatch"/>
        string ToString_Mismatch(bool IncludeType = false, bool IncludePrefix = true);

        /// <inheritdoc cref="Statistic.ToString_Skipped"/>
        string ToString_Skipped(bool IncludeType = false, bool IncludePrefix = true);

        #endregion

        /// <returns>new <see cref="Statistic"/> object </returns>
        /// <inheritdoc cref="Statistic.Statistic(IStatistic)"/>
        new Statistic Clone();
    }
}
