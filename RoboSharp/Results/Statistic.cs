using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RoboSharp.Interfaces;
using RoboSharp.EventArgObjects;

namespace RoboSharp.Results
{
    /// <summary>
    /// Information about number of items Copied, Skipped, Failed, etc.
    /// <para/>
    /// <see cref="RoboCopyResults"/> will not typically raise any events, but this object is used for other items, such as <see cref="ProgressEstimator"/> and <see cref="RoboCopyResultsList"/> to present results whose values may update periodically.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/Statistic"/>
    /// </remarks>
    public partial class Statistic : IStatistic
    {
        internal static IStatistic Default_Bytes = new Statistic(type: StatType.Bytes);
        internal static IStatistic Default_Files = new Statistic(type: StatType.Files);
        internal static IStatistic Default_Dirs = new Statistic(type: StatType.Directories);

        #region < Constructors >

        /// <summary> Create a new Statistic object of <see cref="StatType"/> </summary>
        [Obsolete("Statistic Types require Initialization with a StatType")]
        private Statistic() { }

        /// <summary> Create a new Statistic object </summary>
        public Statistic(StatType type) { Type = type; }

        /// <summary> Create a new Statistic object </summary>
        public Statistic(StatType type, string name) { Type = type; Name = name; }

        /// <summary> Clone an existing Statistic object</summary>
        public Statistic(IStatistic stat)
        {
            Type = stat.Type;
            NameField = stat.Name;
            TotalField = stat.Total;
            CopiedField = stat.Copied;
            SkippedField = stat.Skipped;
            MismatchField = stat.Mismatch;
            FailedField = stat.Failed;
            ExtrasField = stat.Extras;
        }

        /// <summary> Clone an existing Statistic object</summary>
        internal  Statistic(StatType type, string name, long total, long copied, long skipped, long mismatch, long failed, long extras)
        {
            Type = type;
            NameField = name;
            TotalField = total;
            CopiedField = copied;
            SkippedField = skipped;
            MismatchField = mismatch;
            FailedField = failed;
            ExtrasField = extras;
        }

        /// <summary> Describe the Type of Statistics Object </summary>
        public enum StatType
        {
            /// <summary> Statistics object represents count of Directories </summary>
            Directories,
            /// <summary> Statistics object represents count of Files </summary>
            Files,
            /// <summary> Statistics object represents a Size ( number of bytes )</summary>
            Bytes
        }

        #endregion 

        #region < Fields >

        private string NameField = "";
        private long TotalField = 0;
        private long CopiedField = 0;
        private long SkippedField = 0;
        private long MismatchField = 0;
        private long FailedField = 0;
        private long ExtrasField = 0;

        #endregion

        #region < Events >

        /// <summary> This toggle Enables/Disables firing the <see cref="PropertyChanged"/> Event to avoid firing it when doing multiple consecutive changes to the values </summary>
        internal bool EnablePropertyChangeEvent = true;
        private bool PropertyChangedListener => EnablePropertyChangeEvent && PropertyChanged != null;
        private bool Listener_TotalChanged => EnablePropertyChangeEvent && OnTotalChanged != null;
        private bool Listener_CopiedChanged => EnablePropertyChangeEvent && OnCopiedChanged != null;
        private bool Listener_SkippedChanged => EnablePropertyChangeEvent && OnSkippedChanged != null;
        private bool Listener_MismatchChanged => EnablePropertyChangeEvent && OnMisMatchChanged != null;
        private bool Listener_FailedChanged => EnablePropertyChangeEvent && OnFailedChanged != null;
        private bool Listener_ExtrasChanged => EnablePropertyChangeEvent && OnExtrasChanged != null;

        /// <summary>
        /// This event will fire when the value of the statistic is updated via Adding / Subtracting methods. <br/>
        /// Provides <see cref="StatisticPropertyChangedEventArgs"/> object.
        /// </summary>
        /// <remarks>
        /// Allows use with both binding to controls and <see cref="ProgressEstimator"/> binding. <br/>
        /// EventArgs can be passed into <see cref="AddStatistic(PropertyChangedEventArgs)"/> after casting.
        /// </remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Handles any value changes </summary>
        public delegate void StatChangedHandler(Statistic sender, StatChangedEventArg e);

        /// <summary> Occurs when the <see cref="Total"/> Property is updated. </summary>
        public event StatChangedHandler OnTotalChanged;

        /// <summary> Occurs when the <see cref="Copied"/> Property is updated. </summary>
        public event StatChangedHandler OnCopiedChanged;

        /// <summary> Occurs when the <see cref="Skipped"/> Property is updated. </summary>
        public event StatChangedHandler OnSkippedChanged;

        /// <summary> Occurs when the <see cref="Mismatch"/> Property is updated. </summary>
        public event StatChangedHandler OnMisMatchChanged;

        /// <summary> Occurs when the <see cref="Failed"/> Property is updated. </summary>
        public event StatChangedHandler OnFailedChanged;

        /// <summary> Occurs when the <see cref="Extras"/> Property is updated. </summary>
        public event StatChangedHandler OnExtrasChanged;

        #endregion

        #region < Properties >

        /// <summary>
        /// Checks all values and determines if any of them are != 0.
        /// </summary>
        public bool NonZeroValue => TotalField != 0 || CopiedField != 0 || SkippedField != 0 || MismatchField != 0 || FailedField != 0 || ExtrasField != 0;

        /// <summary>
        /// Name of the Statistics Object
        /// </summary>
        public string Name
        {
            get => NameField;
            set
            {
                if (value != NameField)
                {
                    NameField = value ?? "";
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }
        

        /// <summary>
        /// <inheritdoc cref="StatType"/>
        /// </summary>
        public StatType Type { get; }

        /// <inheritdoc cref="IStatistic.Total"/>
        public long Total { 
            get => TotalField;
            internal set
            {
                if (TotalField != value)
                {
                    var e = PrepEventArgs(TotalField, value, "Total");
                    TotalField = value;
                    if (e != null)
                    {
                        PropertyChanged?.Invoke(this, e.Value.Item2);
                        OnTotalChanged?.Invoke(this, e.Value.Item1);
                    }
                }
            }
        }

        /// <inheritdoc cref="IStatistic.Copied"/>
        public long Copied { 
            get => CopiedField;
            internal set
            {
                if (CopiedField != value)
                {
                    var e = PrepEventArgs(CopiedField, value, "Copied");
                    CopiedField = value;
                    if (e != null)
                    {
                        PropertyChanged?.Invoke(this, e.Value.Item2);
                        OnCopiedChanged?.Invoke(this, e.Value.Item1);
                    }
                }
            }
        }

        /// <inheritdoc cref="IStatistic.Skipped"/>
        public long Skipped { 
            get => SkippedField;
            internal set
            {
                if (SkippedField != value)
                {
                    var e = PrepEventArgs(SkippedField, value, "Skipped");
                    SkippedField = value;
                    if (e != null)
                    {
                        PropertyChanged?.Invoke(this, e.Value.Item2);
                        OnSkippedChanged?.Invoke(this, e.Value.Item1);
                    }
                }
            }
        }

        /// <inheritdoc cref="IStatistic.Mismatch"/>
        public long Mismatch { 
            get => MismatchField;
            internal set
            {
                if (MismatchField != value)
                {
                    var e = PrepEventArgs(MismatchField, value, "Mismatch");
                    MismatchField = value;
                    if (e != null)
                    {
                        PropertyChanged?.Invoke(this, e.Value.Item2);
                        OnMisMatchChanged?.Invoke(this, e.Value.Item1);
                    }
                }
            }
        }

        /// <inheritdoc cref="IStatistic.Failed"/>
        public long Failed { 
            get => FailedField;
            internal set
            {
                if (FailedField != value)
                {
                    var e = PrepEventArgs(FailedField, value, "Failed");
                    FailedField = value;
                    if (e != null)
                    {
                        PropertyChanged?.Invoke(this, e.Value.Item2);
                        OnFailedChanged?.Invoke(this, e.Value.Item1);
                    }
                }
            }
        }

        /// <inheritdoc cref="IStatistic.Extras"/>
        public long Extras { 
            get => ExtrasField;
            internal set
            {
                if (ExtrasField != value)
                {
                    var e = PrepEventArgs(ExtrasField, value, "Extras");
                    ExtrasField = value;
                    if (e != null)
                    {
                        PropertyChanged?.Invoke(this, e.Value.Item2);
                        OnExtrasChanged?.Invoke(this, e.Value.Item1);
                    }
                }
            }
        }

        #endregion

        #region < ToString Methods >

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString() => ToString(false, true, ", ");

        /// <summary>
        /// Customize the returned string
        /// </summary>
        /// <param name="IncludeType">Include string representation of <see cref="Type"/></param>
        /// <param name="IncludePrefix">Include "Total:" / "Copied:" / etc in the string to identify the values</param>
        /// <param name="Delimiter">Value Delimieter</param>
        /// <param name="DelimiterAfterType">
        /// Include the delimiter after the 'Type' - Only used if <paramref name="IncludeType"/> us also true. <br/>
        /// When <paramref name="IncludeType"/> is true, a space always exist after the type string. This would add delimiter instead of the space. 
        /// </param>
        /// <returns>
        /// TRUE, TRUE, "," --> $"{Type} Total: {Total}, Copied: {Copied}, Skipped: {Skipped}, Mismatch: {Mismatch}, Failed: {Failed}, Extras: {Extras}" <para/>
        /// FALSE, TRUE, "," --> $"Total: {Total}, Copied: {Copied}, Skipped: {Skipped}, Mismatch: {Mismatch}, Failed: {Failed}, Extras: {Extras}" <para/>
        /// FALSE, FALSE, "," --> $"{Total}, {Copied}, {Skipped}, {Mismatch}, {Failed}, {Extras}"
        /// </returns>
        public string ToString(bool IncludeType, bool IncludePrefix, string Delimiter, bool DelimiterAfterType = false)
        {
            return $"{ToString_Type(IncludeType, DelimiterAfterType)}" + $"{(IncludeType && DelimiterAfterType ? Delimiter : "")}" +
                $"{ToString_Total(false, IncludePrefix)}{Delimiter}" +
                $"{ToString_Copied(false, IncludePrefix)}{Delimiter}" +
                $"{ToString_Skipped(false, IncludePrefix)}{Delimiter}" +
                $"{ToString_Mismatch(false, IncludePrefix)}{Delimiter}" +
                $"{ToString_Failed(false, IncludePrefix)}{Delimiter}" +
                $"{ToString_Extras(false, IncludePrefix)}";
        }

        /// <summary> Get the <see cref="Type"/> as a string </summary>
        public string ToString_Type() => ToString_Type(true).Trim();
        private string ToString_Type(bool IncludeType, bool Trim = false) => IncludeType ? $"{Type}{(Trim? "" : " ")}" : "";

        /// <summary>Get the string describing the <see cref="Total"/></summary>
        /// <returns></returns>
        /// <inheritdoc cref="ToString(bool, bool, string, bool)"/>
        public string ToString_Total(bool IncludeType = false, bool IncludePrefix = true) => $"{ToString_Type(IncludeType)}{(IncludePrefix? "Total: " : "")}{Total}";

        /// <summary>Get the string describing the <see cref="Copied"/></summary>
        /// <inheritdoc cref="ToString_Total"/>
        public string ToString_Copied(bool IncludeType = false, bool IncludePrefix = true) => $"{ToString_Type(IncludeType)}{(IncludePrefix ? "Copied: " : "")}{Copied}";

        /// <summary>Get the string describing the <see cref="Extras"/></summary>
        /// <inheritdoc cref="ToString_Total"/>
        public string ToString_Extras(bool IncludeType = false, bool IncludePrefix = true) => $"{ToString_Type(IncludeType)}{(IncludePrefix ? "Extras: " : "")}{Extras}";

        /// <summary>Get the string describing the <see cref="Failed"/></summary>
        /// <inheritdoc cref="ToString_Total"/>
        public string ToString_Failed(bool IncludeType = false, bool IncludePrefix = true) => $"{ToString_Type(IncludeType)}{(IncludePrefix ? "Failed: " : "")}{Failed}";

        /// <summary>Get the string describing the <see cref="Mismatch"/></summary>
        /// <inheritdoc cref="ToString_Total"/>
        public string ToString_Mismatch(bool IncludeType = false, bool IncludePrefix = true) => $"{ToString_Type(IncludeType)}{(IncludePrefix ? "Mismatch: " : "")}{Mismatch}";

        /// <summary>Get the string describing the <see cref="Skipped"/></summary>
        /// <inheritdoc cref="ToString_Total"/>
        public string ToString_Skipped(bool IncludeType = false, bool IncludePrefix = true) => $"{ToString_Type(IncludeType)}{(IncludePrefix ? "Skipped: " : "")}{Skipped}";

        #endregion

        #region < Parsing Methods >

        //lang=regex
        private const string _tokenPattern = @"^.*:\s+(?<Total>[\d\.]+(\s\w)?)\s+(?<Copied>[\d\.]+(\s\w)?)\s+(?<Skipped>[\d\.]+(\s\w)?)\s+(?<Mismatch>[\d\.]+(\s\w)?)\s+(?<Failed>[\d\.]+(\s\w)?)\s+(?<Extras>[\d\.]+(\s\w)?)";
        //lang=regex
        private const string _longConversionPattern = @"(?<Whole>[\d\.,]+)(\.(?<Fraction>\d+))\s(?<Unit>\w)";

#if NET7_0_OR_GREATER
        [GeneratedRegex(_tokenPattern, RegexOptions.ExplicitCapture, 1000)]
        private static partial Regex TokenParsingRegex();
        [GeneratedRegex(_longConversionPattern, RegexOptions.ExplicitCapture, 1000)]
        private static partial Regex LongConversionRegex();

        private static Match TokenParsingRegex(string data) => TokenParsingRegex().Match(data);
        private static Match LongConversionRegex(string data) => LongConversionRegex().Match(data);
#else
        private static Match TokenParsingRegex(string data) => Regex.Match(data, _tokenPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
        private static Match LongConversionRegex(string data) => Regex.Match(data, _longConversionPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
#endif


        /// <summary>
        /// Parse a string and for the tokens reported by RoboCopy
        /// </summary>
        /// <param name="type">Statistic Type to produce</param>
        /// <param name="line">LogLine produced by RoboCopy in Summary Section</param>
        /// <param name="estimatedStat">The statistic produced from the <see cref="ProgressEstimator"/>, whose value should be updated if the line is able to parse out the values.</param>
        /// <returns>New Statistic Object</returns>
        public static Statistic Parse(StatType type, string line, Statistic estimatedStat = null)
        {
            var res = estimatedStat ?? new Statistic(type);

            var match = TokenParsingRegex(line);

            if (!match.Success)
                return res;

            res.Total = ParseTokenString(match.Groups[nameof(Total)].Value);
            res.Copied = ParseTokenString(match.Groups[nameof(Copied)].Value);
            res.Skipped = ParseTokenString(match.Groups[nameof(Skipped)].Value);
            res.Mismatch = ParseTokenString(match.Groups[nameof(Mismatch)].Value);
            res.Failed = ParseTokenString(match.Groups[nameof(Failed)].Value);
            res.Extras= ParseTokenString(match.Groups[nameof(Extras)].Value);
            
            return res;
        }

        private static long ParseTokenString(string tokenString)
        {
            if (string.IsNullOrWhiteSpace(tokenString))
                return 0;

            tokenString = tokenString.Trim();
            if (Regex.IsMatch(tokenString, @"^\d+$"))
                return long.Parse(tokenString);

            var match = LongConversionRegex(tokenString);
            if (match.Success)
            {
                var builder = new StringBuilder(match.Groups["Whole"].Value).RemoveChars(',', '.', ' ') // whole number
                    .Append('.') // decimal
                    .Append(match.Groups["Fraction"].Value); // decimal values

                var unit = match.Groups["Unit"].Value.ToLower();

                var number = double.Parse(builder.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                switch (unit)
                {
                    case "k":
                        // Byte = kBytes * 1024
                        number *= Math.Pow(1024, 1);
                        break;
                    case "m":
                        // Byte = MBytes * 1024 * 1024
                        number *= Math.Pow(1024, 2);
                        break;
                    case "g":
                        // Byte = GBytes * 1024 * 1024 * 1024
                        number *= Math.Pow(1024, 3);
                        break;
                    case "t":
                        // Byte = TBytes * 1024 * 1024 * 1024 * 1024
                        number *= Math.Pow(1024, 4);
                        break;
                }

                return Convert.ToInt64(number);
            }

            return 0;
        }

#endregion

        #region < Reset Method >

        /// <summary>
        /// Set the values for this object to 0
        /// </summary>
        public void Reset(bool enablePropertyChangeEvent)
        {
            EnablePropertyChangeEvent = enablePropertyChangeEvent;
            Reset();
            EnablePropertyChangeEvent = true;
        }


        /// <summary>
        /// Reset all values to Zero ( 0 ) -- Used by <see cref="RoboCopyResultsList"/> for the properties
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Statistic OriginalValues = PropertyChangedListener && NonZeroValue ? this.Clone() : null;

            //Total
            var eTotal = ConditionalPrepEventArgs(Listener_TotalChanged, TotalField, 0, "Total");
            TotalField = 0;
            //Copied
            var eCopied = ConditionalPrepEventArgs(Listener_CopiedChanged, CopiedField, 0, "Copied");
            CopiedField = 0;
            //Extras
            var eExtras = ConditionalPrepEventArgs(Listener_ExtrasChanged, ExtrasField, 0, "Extras");
            ExtrasField = 0;
            //Failed
            var eFailed = ConditionalPrepEventArgs(Listener_FailedChanged, FailedField, 0, "Failed");
            FailedField = 0;
            //Mismatch
            var eMismatch = ConditionalPrepEventArgs(Listener_MismatchChanged, MismatchField, 0, "Mismatch");
            MismatchField = 0;
            //Skipped
            var eSkipped = ConditionalPrepEventArgs(Listener_SkippedChanged, SkippedField, 0, "Skipped");
            SkippedField = 0;

            //Trigger Events 
            TriggerDeferredEvents(OriginalValues, eTotal, eCopied, eExtras, eFailed, eMismatch, eSkipped);
        }


        #endregion

        #region < Trigger Deferred Events >

        /// <summary>
        /// Prep Event Args for SETTERS of the properties
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private Lazy<Tuple<StatChangedEventArg, StatisticPropertyChangedEventArgs>> PrepEventArgs(long OldValue, long NewValue, string PropertyName)
        {
            if (!EnablePropertyChangeEvent) return null;
            var old = this.Clone();
            return new Lazy<Tuple<StatChangedEventArg, StatisticPropertyChangedEventArgs>>(() =>
            {
                StatChangedEventArg e1 = new StatChangedEventArg(this, OldValue, NewValue, PropertyName);
                var e2 = new StatisticPropertyChangedEventArgs(this, old, PropertyName);
                return new Tuple<StatChangedEventArg, StatisticPropertyChangedEventArgs>(e1, e2);
            });
        }

        /// <summary>
        /// Prep event args for the ADD and RESET methods
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private StatChangedEventArg ConditionalPrepEventArgs(bool Listener, long OldValue, long NewValue, string PropertyName)=> !Listener || OldValue == NewValue ? null : new StatChangedEventArg(this, OldValue, NewValue, PropertyName);

        /// <summary>
        /// Raises the events that were deferred while item was object was still being calculated by ADD / RESET
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private void TriggerDeferredEvents(Statistic OriginalValues, StatChangedEventArg eTotal, StatChangedEventArg eCopied, StatChangedEventArg eExtras, StatChangedEventArg eFailed, StatChangedEventArg eMismatch, StatChangedEventArg eSkipped)
        {
            //Perform Events
            int i = 0;
            if (eTotal != null)
            {
                i = 1;
                OnTotalChanged?.Invoke(this, eTotal);
            }
            if (eCopied != null)
            {
                i += 2;
                OnCopiedChanged?.Invoke(this, eCopied);
            }
            if (eExtras != null)
            {
                i += 4;
                OnExtrasChanged?.Invoke(this, eExtras);
            }
            if (eFailed != null)
            {
                i += 8;
                OnFailedChanged?.Invoke(this, eFailed);
            }
            if (eMismatch != null)
            {
                i += 16;
                OnMisMatchChanged?.Invoke(this, eMismatch);
            }
            if (eSkipped != null)
            {
                i += 32;
                OnSkippedChanged?.Invoke(this, eSkipped);
            }

            //Trigger PropertyChangeEvent
            if (OriginalValues != null)
            {
                switch (i)
                {
                    case 1: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, eTotal.PropertyName)); return;
                    case 2: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, eCopied.PropertyName)); return;
                    case 4: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, eExtras.PropertyName)); return;
                    case 8: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, eFailed.PropertyName)); return;
                    case 16: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, eMismatch.PropertyName)); return;
                    case 32: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, eSkipped.PropertyName)); return;
                    default: PropertyChanged?.Invoke(this, new StatisticPropertyChangedEventArgs(this, OriginalValues, String.Empty)); return;
                }
            }
        }

        #endregion

        #region < ADD Methods >

        /// <summary>
        /// Add the supplied values to this Statistic object. <br/>
        /// Events are defered until all the fields have been added together.
        /// </summary>
        /// <param name="total"></param>
        /// <param name="copied"></param>
        /// <param name="extras"></param>
        /// <param name="failed"></param>
        /// <param name="mismatch"></param>
        /// <param name="skipped"></param>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void Add(long total = 0, long copied = 0, long extras = 0, long failed = 0, long mismatch = 0, long skipped = 0)
        {
            //Store the original object values for the event args
            Statistic originalValues = PropertyChangedListener ? this.Clone() : null;

            //Total
            long i = TotalField;
            TotalField += total;
            var eTotal = ConditionalPrepEventArgs(Listener_TotalChanged, i, TotalField, "Total");

            //Copied
            i = CopiedField;
            CopiedField += copied;
            var eCopied = ConditionalPrepEventArgs(Listener_CopiedChanged, i, CopiedField, "Copied");

            //Extras
            i = ExtrasField;
            ExtrasField += extras;
            var eExtras = ConditionalPrepEventArgs(Listener_ExtrasChanged, i, ExtrasField, "Extras");

            //Failed
            i = FailedField;
            FailedField += failed;
            var eFailed = ConditionalPrepEventArgs(Listener_FailedChanged, i, FailedField, "Failed");

            //Mismatch
            i = MismatchField;
            MismatchField += mismatch;
            var eMismatch = ConditionalPrepEventArgs(Listener_MismatchChanged, i, MismatchField, "Mismatch");

            //Skipped
            i = SkippedField;
            SkippedField += skipped;
            var eSkipped = ConditionalPrepEventArgs(Listener_SkippedChanged, i, SkippedField, "Skipped");

            //Trigger Events 
            if (EnablePropertyChangeEvent)
                TriggerDeferredEvents(originalValues, eTotal, eCopied, eExtras, eFailed, eMismatch, eSkipped);
        }

        /// <summary>
        /// Add the results of the supplied Statistics object to this Statistics object. <br/>
        /// Events are defered until all the fields have been added together.
        /// </summary>
        /// <param name="stat">Statistics Item to add</param>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void AddStatistic(IStatistic stat)
        {
            if (stat != null && stat.Type == this.Type && stat.NonZeroValue) 
                Add(stat.Total, stat.Copied, stat.Extras, stat.Failed, stat.Mismatch, stat.Skipped);
        }

        
        /// <param name="enablePropertyChangedEvent"><inheritdoc cref="EnablePropertyChangeEvent" path="*"/></param>
        /// <inheritdoc cref="AddStatistic(IStatistic)"/>
        /// <param name="stats"/>
        internal void AddStatistic(IStatistic stats, bool enablePropertyChangedEvent)
        {
            EnablePropertyChangeEvent = enablePropertyChangedEvent;
            AddStatistic(stats);
            EnablePropertyChangeEvent = true;

        }

        /// <summary>
        /// Add the results of the supplied Statistics objects to this Statistics object.
        /// </summary>
        /// <param name="stats">Statistics Item to add</param>
        public void AddStatistic(IEnumerable<IStatistic> stats)
        {
            foreach (IStatistic stat in stats)
            {
                EnablePropertyChangeEvent = stat == stats.Last();
                AddStatistic(stat);
            }
        }

        /// <summary>
        /// Adds <see cref="StatChangedEventArg.Difference"/> to the appropriate property based on the 'PropertyChanged' value. <br/>
        /// Will only add the value if the <see cref="StatChangedEventArg.StatType"/> == <see cref="Type"/>.
        /// </summary>
        /// <param name="eventArgs">Arg provided by either <see cref="PropertyChanged"/> or a Statistic's object's On*Changed events</param>
        public void AddStatistic(PropertyChangedEventArgs eventArgs)
        {
            //Only process the args if of the proper type
            var e = eventArgs as IStatisticPropertyChangedEventArg;
            if (e == null) return;
            
            if (e.StatType != this.Type || (e.Is_StatisticPropertyChangedEventArgs && e.Is_StatChangedEventArg))
            {
                // INVALID!
            }
            else if (e.Is_StatisticPropertyChangedEventArgs)
            {
                var e1 = (StatisticPropertyChangedEventArgs)eventArgs;
                AddStatistic(e1.Difference);
            }
            else if (e.Is_StatChangedEventArg)
            {
                var e2 = (StatChangedEventArg)eventArgs;
                switch (e.PropertyName)
                {
                    case "": //String.Empty means all fields have changed
                        AddStatistic(e2.Sender);
                        break;
                    case "Copied":
                        this.Copied += e2.Difference;
                        break;
                    case "Extras":
                        this.Extras += e2.Difference;
                        break;
                    case "Failed":
                        this.Failed += e2.Difference;
                        break;
                    case "Mismatch":
                        this.Mismatch += e2.Difference;
                        break;
                    case "Skipped":
                        this.Skipped += e2.Difference;
                        break;
                    case "Total":
                        this.Total += e2.Difference;
                        break;
                }
            }
        }

        /// <summary>
        /// Combine the results of the supplied statistics objects of the specified type.
        /// </summary>
        /// <param name="stats">Collection of <see cref="Statistic"/> objects</param>
        /// <param name="statType">Create a new Statistic object of this type.</param>
        /// <returns>New Statistics Object</returns>
        public static Statistic AddStatistics(IEnumerable<IStatistic> stats, StatType statType)
        {
            Statistic ret = new Statistic(statType);
            ret.AddStatistic(stats.Where(s => s.Type == statType) );
            return ret;
        }


        #endregion ADD

        #region < AVERAGE Methods >

        /// <summary>
        /// Combine the supplied <see cref="Statistic"/> objects, then get the average.
        /// </summary>
        /// <param name="stats">Array of Stats objects</param>
        public void AverageStatistic(IEnumerable<IStatistic> stats)
        {
            this.AddStatistic(stats);
            int cnt = stats.Count() + 1;
            Total /= cnt;
            Copied /= cnt;
            Extras /= cnt;
            Failed /= cnt;
            Mismatch /= cnt;
            Skipped /= cnt;

        }

        /// <returns>New Statistics Object</returns>
        /// <inheritdoc cref=" AverageStatistic(IEnumerable{IStatistic})"/>
        public static Statistic AverageStatistics(IEnumerable<IStatistic> stats, StatType statType)
        {
            Statistic stat = AddStatistics(stats, statType);
            int cnt = stats.Count(s => s.Type == statType);
            if (cnt > 1)
            {
                stat.Total /= cnt;
                stat.Copied /= cnt;
                stat.Extras /= cnt;
                stat.Failed /= cnt;
                stat.Mismatch /= cnt;
                stat.Skipped /= cnt;
            }
            return stat;
        }

        #endregion AVERAGE

        #region < Subtract Methods >

        /// <summary>
        /// Subtract Method used by <see cref="RoboCopyResultsList"/> <br/>
        /// Events are deferred until all value changes have completed.
        /// </summary>
        /// <param name="stat">Statistics Item to subtract</param>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void Subtract(IStatistic stat)
        {
            if (stat.Type == this.Type && stat.NonZeroValue)
                Add(-stat.Total, -stat.Copied, -stat.Extras, -stat.Failed, -stat.Mismatch, -stat.Skipped);
        }

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
        /// <param name="enablePropertyChangedEvent"><inheritdoc cref="EnablePropertyChangeEvent" path="*"/></param>
        /// <inheritdoc cref="Subtract(IStatistic)"/>        
        internal void Subtract(IStatistic stats, bool enablePropertyChangedEvent)
        {
            EnablePropertyChangeEvent = enablePropertyChangedEvent;
            Subtract(stats);
            EnablePropertyChangeEvent = true;
        }
#pragma warning restore CS1573

        /// <summary>
        /// Subtract the results of the supplied Statistics objects to this Statistics object.
        /// </summary>
        /// <param name="stats">Statistics Item to subtract</param>
        public void Subtract(IEnumerable<IStatistic> stats)
        {
            foreach (IStatistic stat in stats)
            {
                EnablePropertyChangeEvent = stat == stats.Last();
                Subtract(stat);
            }
        }

        /// <param name="MainStat">Statistics object to clone</param>
        /// <param name="stats"><inheritdoc cref="Subtract(IStatistic)"/></param>
        /// <returns>Clone of the <paramref name="MainStat"/> object with the <paramref name="stats"/> subtracted from it.</returns>
        /// <inheritdoc cref="Subtract(IStatistic)"/>       
        public static Statistic Subtract(IStatistic MainStat, IStatistic stats)
        {
            var ret = MainStat.Clone();
            ret.Subtract(stats);
            return ret;
        }

        #endregion Subtract


        /// <inheritdoc cref="IStatistic.Clone"/>
        public Statistic Clone() => new Statistic(this);

        object ICloneable.Clone() => new Statistic(this);

    }
}
