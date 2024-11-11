using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RoboSharp.Interfaces;

namespace RoboSharp.Results
{
    /// <summary>
    /// Contains information regarding average Transfer Speed. <br/>
    /// Note: Runs that do not perform any copy operations or that exited prematurely ( <see cref="RoboCopyExitCodes.Cancelled"/> ) will result in a null <see cref="SpeedStatistic"/> object.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/tjscience/RoboSharp/wiki/SpeedStatistic"/>
    /// </remarks>
    public partial class SpeedStatistic : INotifyPropertyChanged, ISpeedStatistic
    {
        internal const long MegaBytePerMinDivisor = 1024 * 1024;

        /// <summary>
        /// Create new SpeedStatistic
        /// </summary>
        public SpeedStatistic() { }

        /// <summary>
        /// Clone a SpeedStatistic
        /// </summary>
        public SpeedStatistic(ISpeedStatistic stat)
        {
            BytesPerSec = stat?.BytesPerSec ?? 0;
            MegaBytesPerMin = stat?.MegaBytesPerMin ?? 0;
        }

        /// <summary>
        /// Create a new SpeesdStatistic from a file length and a copy time
        /// </summary>
        /// <param name="fileLength">The file length in Bytes</param>
        /// <param name="copyTime">How long the file took top copy</param>
        public SpeedStatistic(long fileLength, TimeSpan copyTime)
        {
            if (fileLength < 0) throw new ArgumentException("File Length cannot be less than 0", nameof(fileLength));
            if (copyTime.TotalSeconds <= 0) throw new ArgumentException("Copy Time cannot be less than or equal to 0", nameof(copyTime));

            BytesPerSec = Decimal.Round(fileLength / (decimal)copyTime.TotalSeconds);
            MegaBytesPerMin = Decimal.Round(fileLength / MegaBytePerMinDivisor / (decimal)copyTime.TotalMinutes, 3);
        }

        #region < Private & Protected Members >

        private decimal _bytesPerSecond;
        private decimal _megabytesPerMinute;

        /// <summary> This toggle Enables/Disables firing the <see cref="PropertyChanged"/> Event to avoid firing it when doing multiple consecutive changes to the values </summary>
        protected bool EnablePropertyChangeEvent { get; set; } = true;

        #endregion

        #region < Public Properties & Events >

        /// <summary>This event will fire when the value of the SpeedStatistic is updated </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Raise Property Change Event</summary>
        protected void OnPropertyChange(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        /// <inheritdoc cref="ISpeedStatistic.BytesPerSec"/>
        public virtual decimal BytesPerSec
        {
            get => _bytesPerSecond;
            protected set
            {
                if (_bytesPerSecond != value)
                {
                    _bytesPerSecond = value;
                    if (EnablePropertyChangeEvent) OnPropertyChange(nameof(BytesPerSec));
                }
            }
        }

        /// <inheritdoc cref="ISpeedStatistic.MegaBytesPerMin"/>
        public virtual decimal MegaBytesPerMin
        {
            get => _megabytesPerMinute;
            protected set
            {
                if (_megabytesPerMinute != value)
                {
                    _megabytesPerMinute = value;
                    if (EnablePropertyChangeEvent) OnPropertyChange(nameof(MegaBytesPerMin));
                }
            }
        }

        #endregion

        #region < Methods >

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"Speed: {BytesPerSec} Bytes/sec{Environment.NewLine}Speed: {MegaBytesPerMin} MegaBytes/min";
        }

        /// <summary>
        /// Gets the BytesPerSec as a string
        /// </summary>
        /// <returns>$"{BytesPerSec} Bytes/sec"</returns>
        public string GetBytesPerSecond() => $"{BytesPerSec} Bytes/sec";

        /// <summary>
        /// Gets the MegaBytesPerMin as a string
        /// </summary>
        /// <returns>$"{MegaBytesPerMin} MegaBytes/min"</returns>
        public string GetMegaBytesPerMin() => $"{MegaBytesPerMin} MegaBytes/min";

        /// <inheritdoc cref="ISpeedStatistic.Clone"/>
        public virtual SpeedStatistic Clone() => new SpeedStatistic(this);

        object ICloneable.Clone() => Clone();

        internal static SpeedStatistic Parse(string line1, string line2)
        {
            var res = new SpeedStatistic();
            res.BytesPerSec = ParseBytesPerSecond(line1);
            res.MegaBytesPerMin = ParseMegabytesPerMinute(line2);
            return res;
        }

        //lang = regex
        private const string _speedPattern = "(\\d+[.,\\s]?)+";

#if NET7_0_OR_GREATER
        [GeneratedRegex(_speedPattern, RegexOptions.CultureInvariant, 1000)]
        private static partial Regex SpeedStatisticRegex();
        private static Match SpeedStatisticRegex(string data) => SpeedStatisticRegex().Match(data);
#else
        private static Match SpeedStatisticRegex(string data) => Regex.Match(data, _speedPattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(1000));
#endif
        internal static decimal ParseBytesPerSecond(string line)
        {
            Match match = SpeedStatisticRegex(line);
            if (match.Success)
            {
                if (decimal.TryParse(match.Value, out decimal value))
                    return value;
                else
                {
                    // parsing in default culture failed, convert to invariant culture
                    StringBuilder sanitizer = new StringBuilder(match.Value).RemoveChars('.', ',', ' ');
                    if (decimal.TryParse(sanitizer.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out value))
                        return value;
                }
            }
            return 0;
        }

        internal static decimal ParseMegabytesPerMinute(string line)
        {
            Match match = SpeedStatisticRegex(line);
            if (match.Success)
            {
                if (decimal.TryParse(match.Value, out decimal value))
                    return value;
                else
                {
                    // parsing in default culture failed, convert to invariant culture
                    StringBuilder sanitizer = new StringBuilder(match.Value).RemoveWhiteSpace();
                    int decPoint = sanitizer.LastIndexOf('.', ',');
                    // replace any decimal point group separators with a comma for use with the invariant culture
                    if (decPoint > 1)
                    {
                        sanitizer[decPoint] = '.';
                        for (int i = 0; i < decPoint; i++)
                            if (sanitizer[i] == '.')
                                sanitizer[i] = ',';
                    }
                    if (decimal.TryParse(sanitizer.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out value))
                        return value;
                }
            }
            return 0;
        }

        #endregion

    }

    /// <summary>
    /// This object represents the Average of several <see cref="SpeedStatistic"/> objects, and contains 
    /// methods to facilitate that functionality.
    /// </summary>
    public sealed class AverageSpeedStatistic : SpeedStatistic
    {
        private bool _recalcBPS;
        private bool _recalcMB;
        private long _divisor = 0; // Total number of SpeedStats that were combined to produce the Combined_* values
        private decimal _combinedBytesPerSecond = 0;        //Sum of all <see cref="SpeedStatistic.BytesPerSec"/>
        private decimal _combinedMegabytesPerMinute = 0;    //Sum of all <see cref="SpeedStatistic.MegaBytesPerMin"/>

        /// <inheritdoc/>
        public override decimal BytesPerSec
        {
            get
            {
                if (_recalcBPS)
                {
                    _recalcBPS = false;
                    base.BytesPerSec = _divisor < 1 ? 0 : Math.Round(_combinedBytesPerSecond / _divisor, 3);
                }
                return base.BytesPerSec;
            }
            protected set => base.BytesPerSec = value;
        }

        /// <inheritdoc/>
        public override decimal MegaBytesPerMin
        {
            get
            {
                if (_recalcMB)
                {
                    _recalcMB = false;
                    base.MegaBytesPerMin = _divisor < 1 ? 0 : Math.Round(_combinedMegabytesPerMinute / _divisor, 3);
                }
                return base.MegaBytesPerMin;
            }
            protected set => base.MegaBytesPerMin = value;
        }

        #region < Constructors >

        /// <summary>
        /// Initialize a new <see cref="AverageSpeedStatistic"/> object with the default values.
        /// </summary>
        public AverageSpeedStatistic() : base() { }

        /// <summary>
        /// Initialize a new <see cref="AverageSpeedStatistic"/> object. <br/>
        /// Values will be set to the return values of <see cref="SpeedStatistic.BytesPerSec"/> and <see cref="SpeedStatistic.MegaBytesPerMin"/> <br/>
        /// </summary>
        /// <param name="speedStat">
        /// Either a <see cref="SpeedStatistic"/> or a <see cref="AverageSpeedStatistic"/> object. <br/>
        /// If a <see cref="AverageSpeedStatistic"/> is passed into this constructor, it wil be treated as the base <see cref="SpeedStatistic"/> instead.
        /// </param>
        public AverageSpeedStatistic(ISpeedStatistic speedStat) : base()
        {
            _divisor = 1;
            _combinedBytesPerSecond = speedStat.BytesPerSec;
            _combinedMegabytesPerMinute = speedStat.MegaBytesPerMin;
            CalculateAverage();
        }

        /// <summary>
        /// Initialize a new <see cref="AverageSpeedStatistic"/> object using <see cref="AverageSpeedStatistic.Average(IEnumerable{ISpeedStatistic})"/>. <br/>
        /// </summary>
        /// <param name="speedStats"><inheritdoc cref="Average(IEnumerable{ISpeedStatistic})"/></param>
        /// <inheritdoc cref="Average(IEnumerable{ISpeedStatistic})"/>
        public AverageSpeedStatistic(IEnumerable<ISpeedStatistic> speedStats) : base()
        {
            Average(speedStats);
        }

        /// <summary>
        /// Clone an AverageSpeedStatistic
        /// </summary>
        public AverageSpeedStatistic(AverageSpeedStatistic stat) : base(stat)
        {
            _divisor = stat._divisor;
            _combinedBytesPerSecond = stat.BytesPerSec;
            _combinedMegabytesPerMinute = stat.MegaBytesPerMin;
        }

        #endregion

        /// <inheritdoc cref="ICloneable.Clone"/>
        public override SpeedStatistic Clone() => new AverageSpeedStatistic(this);

        /// <summary>
        /// Set the values for this object to 0
        /// </summary>
        public void Reset()
        {
            _combinedBytesPerSecond = 0;
            _combinedMegabytesPerMinute = 0;
            _divisor = 0;
            _recalcBPS = false;
            _recalcMB = false;
            BytesPerSec = 0;
            MegaBytesPerMin = 0;
        }

        /// <summary>
        /// Set the values for this object to 0
        /// </summary>
        internal void Reset(bool enablePropertyChangeEvent)
        {
            EnablePropertyChangeEvent = enablePropertyChangeEvent;
            Reset();
            EnablePropertyChangeEvent = true;
        }

        // Add / Subtract methods are internal to allow usage within the RoboCopyResultsList object.
        // The 'Average' Methods will first Add the statistics to the current one, then recalculate the average.
        // Subtraction is only used when an item is removed from a RoboCopyResultsList 
        // As such, public consumers should typically not require the use of subtract methods 

        #region < ADD & Subtract ( internal ) >

        internal void Add(IEnumerable<ISpeedStatistic> stats)
        {
            foreach (ISpeedStatistic stat in stats)
                Add(stat);
        }

        internal void Subtract(IEnumerable<SpeedStatistic> stats)
        {
            foreach (SpeedStatistic stat in stats)
                Subtract(stat);
        }

        /// <summary>
        /// Add the results of the supplied SpeedStatistic objects to this object. <br/>
        /// Does not automatically recalculate the average, and triggers no events.
        /// </summary>
        /// <remarks>
        /// If any supplied Speedstat object is actually an <see cref="AverageSpeedStatistic"/> object, default functionality will combine the private fields
        /// used to calculate the average speed instead of using the publicly reported speeds. <br/>
        /// This ensures that combining the average of multiple <see cref="AverageSpeedStatistic"/> objects returns the correct value. <br/>
        /// Ex: One object with 2 runs and one with 3 runs will return the average of all 5 runs instead of the average of two averages.
        /// </remarks>
        /// <param name="stat">SpeedStatistic Item to add</param>
        internal void Add(ISpeedStatistic stat)
        {
            if (stat is null) return;
            if (stat is AverageSpeedStatistic avg)
            {
                _divisor += avg._divisor;
                _combinedBytesPerSecond += avg._combinedBytesPerSecond;
                _combinedMegabytesPerMinute += avg._combinedMegabytesPerMinute;
            }
            else
            {
                _divisor += 1;
                _combinedBytesPerSecond += stat.BytesPerSec;
                _combinedMegabytesPerMinute += stat.MegaBytesPerMin;
            }
        }

        /// <summary>
        /// Subtract the results of the supplied SpeedStatistic objects from this object.<br/>
        /// </summary>
        /// <param name="stat">Statistics Item to subtract</param>
        internal void Subtract(ISpeedStatistic stat)
        {
            if (stat is null) return;
            if (_divisor < 1 || _combinedBytesPerSecond < 0 || _combinedMegabytesPerMinute < 0)
            {
                //Cannot have negative speeds or divisors -> Reset all values
                _combinedBytesPerSecond = 0;
                _combinedMegabytesPerMinute = 0;
                _divisor = 0;
            }
            else if (stat is AverageSpeedStatistic avg)
            {
                _divisor -= avg._divisor;
                _combinedBytesPerSecond -= avg._combinedBytesPerSecond;
                _combinedMegabytesPerMinute -= avg._combinedMegabytesPerMinute;
            }
            else
            {
                _divisor -= 1;
                _combinedBytesPerSecond -= stat.BytesPerSec;
                _combinedMegabytesPerMinute -= stat.MegaBytesPerMin;
            }
        }

        #endregion

        #region < AVERAGE ( public ) >

        /// <summary> Report the public properties have updated </summary>
        private void CalculateAverage()
        {
            _recalcBPS = true;
            _recalcMB = true;
            OnPropertyChange("BytesPerSec");
            OnPropertyChange("MegaBytesPerMin");
        }

        /// <summary>
        /// Combine the supplied <see cref="SpeedStatistic"/> objects, then get the average.
        /// </summary>
        /// <param name="stat">Stats object</param>
        /// <inheritdoc cref="Add(ISpeedStatistic)" path="/remarks"/>
        public void Average(ISpeedStatistic stat)
        {
            Add(stat);
            CalculateAverage();
        }

        /// <summary>
        /// Combine the supplied <see cref="SpeedStatistic"/> objects, then get the average.
        /// </summary>
        /// <param name="stats">Collection of <see cref="ISpeedStatistic"/> objects</param>
        /// <inheritdoc cref="Add(ISpeedStatistic)" path="/remarks"/>
        public void Average(IEnumerable<ISpeedStatistic> stats)
        {
            Add(stats);
            CalculateAverage();
        }

        /// <summary>
        /// Calculate the speeds for the specified file and time, then average them in.
        /// </summary>
        /// <param name="fileLength">the number of bytes copied</param>
        /// <param name="timeSpan">the time span over which the bytes were copied ( EndDate - StartDate )</param>
        public void Average(long fileLength, TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds <= 0) return; // instantaneous copies are ignored to prevent div/0 errors
            if (fileLength < 0) throw new ArgumentException("File Length cannot be less than 0", nameof(fileLength));

            _divisor += 1;
            _combinedBytesPerSecond += Decimal.Round(fileLength / (decimal)timeSpan.TotalSeconds, 3);
            _combinedMegabytesPerMinute += Decimal.Round(fileLength / MegaBytePerMinDivisor / (decimal)timeSpan.TotalMinutes, 3);
            CalculateAverage();
        }

        /// <returns>New Statistics Object</returns>
        /// <inheritdoc cref=" Average(IEnumerable{ISpeedStatistic})"/>
        public static AverageSpeedStatistic GetAverage(IEnumerable<ISpeedStatistic> stats)
        {
            AverageSpeedStatistic stat = new AverageSpeedStatistic();
            stat.Average(stats);
            return stat;
        }

        #endregion

    }
}
