using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using RoboSharp.EventArgObjects;
using RoboSharp.Interfaces;
using StatType = RoboSharp.Results.Statistic.StatType;
using System.Collections;

namespace RoboSharp.Results
{
    /// <summary>
    /// Object used to represent results from multiple <see cref="RoboCommand"/>s. <br/>
    /// As <see cref="RoboCopyResults"/> are added to this object, it will update the Totals and Averages accordingly.<para/>
    /// Implements:
    /// <br/><see cref="IRoboCopyResultsList"/>
    /// <br/><see cref="IList{RoboCopyResults}"/> where T = RoboCopyResults
    /// <br/><see cref="INotifyCollectionChanged"/>
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/RoboCopyResultsList"/>
    /// </remarks>
    public sealed class RoboCopyResultsList : IRoboCopyResultsList, IList<RoboCopyResults>, INotifyCollectionChanged
    {
        #region < Constructors >

        /// <inheritdoc cref="List{T}.List()"/>
        public RoboCopyResultsList() { InitCollection(null); Init(); }

        /// <param name="result">Populate the new List object with this result as the first item.</param>
        /// <inheritdoc cref="List{T}.List(IEnumerable{T})"/>
        public RoboCopyResultsList(RoboCopyResults result) { ResultsList.Add(result); InitCollection(null); Init(); }

        /// <inheritdoc cref="List{T}.List(IEnumerable{T})"/>
        public RoboCopyResultsList(IEnumerable<RoboCopyResults> collection) { InitCollection(collection); Init(); }

        /// <summary>
        /// Clone a RoboCopyResultsList into a new object
        /// </summary>
        public RoboCopyResultsList(RoboCopyResultsList resultsList)
        {
            InitCollection(resultsList);
            Total_DirStatsField = GetLazyStat(resultsList.Total_DirStatsField, GetLazyFunc(GetDirectoriesStatistics, StatType.Directories));
            Total_FileStatsField = GetLazyStat(resultsList.Total_FileStatsField, GetLazyFunc(GetFilesStatistics, StatType.Files));
            Total_ByteStatsField = GetLazyStat(resultsList.Total_ByteStatsField, GetLazyFunc(GetByteStatistics, StatType.Bytes));
            Average_SpeedStatsField = GetLazyStat(resultsList.Average_SpeedStatsField, GetLazyAverageSpeedFunc());
            ExitStatusSummaryField = GetLazyStat(resultsList.ExitStatusSummaryField, GetLazCombinedStatusFunc());
        }

        #region < Constructor Helper Methods >

        private void InitCollection(IEnumerable<RoboCopyResults> collection)
        {
            ResultsList.AddRange(collection);
            ResultsList.CollectionChanged += OnCollectionChanged;
        }

        private void Init()
        {
            Total_DirStatsField = new Lazy<Statistic>(GetLazyFunc(GetDirectoriesStatistics, StatType.Directories));
            Total_FileStatsField = new Lazy<Statistic>(GetLazyFunc(GetFilesStatistics, StatType.Files));
            Total_ByteStatsField = new Lazy<Statistic>(GetLazyFunc(GetByteStatistics, StatType.Bytes));
            Average_SpeedStatsField = new Lazy<AverageSpeedStatistic>(GetLazyAverageSpeedFunc());
            ExitStatusSummaryField = new Lazy<RoboCopyCombinedExitStatus>(GetLazCombinedStatusFunc());
        }

        private Func<Statistic> GetLazyFunc(Func<IStatistic[]> Action, StatType StatType) => new Func<Statistic>(() => Statistic.AddStatistics(Action.Invoke(), StatType));
        private Func<AverageSpeedStatistic> GetLazyAverageSpeedFunc() => new Func<AverageSpeedStatistic>(() => AverageSpeedStatistic.GetAverage(GetSpeedStatistics()));
        private Func<RoboCopyCombinedExitStatus> GetLazCombinedStatusFunc() => new Func<RoboCopyCombinedExitStatus>(() => RoboCopyCombinedExitStatus.CombineStatuses(GetStatuses()));
        private Lazy<T> GetLazyStat<T>(Lazy<T> lazyStat, Func<T> action) where T : ICloneable
        {
            if (lazyStat.IsValueCreated)
            {
                var clone = lazyStat.Value.Clone();
                return new Lazy<T>(() => (T)clone);
            }
            else
            {
                return new Lazy<T>(action);
            }
        }

        #endregion
        
        #endregion

        #region < Fields >

        //These objects are the underlying Objects that may be bound to by consumers.
        //The values are updated upon request of the associated property. 
        //This is so that properties are not returning new objects every request (which would break bindings)
        //If the statistic is never requested, then Lazy<> allows the list to skip performing the math against that statistic.

        private Lazy<Statistic> Total_DirStatsField;
        private Lazy<Statistic> Total_ByteStatsField;
        private Lazy<Statistic> Total_FileStatsField;
        private Lazy<AverageSpeedStatistic> Average_SpeedStatsField;
        private Lazy<RoboCopyCombinedExitStatus> ExitStatusSummaryField;
        private readonly ObservableList<RoboCopyResults> ResultsList = new ObservableList<RoboCopyResults>();

        #endregion

        #region < Events >

        /// <summary>
        /// Delegate for objects to send notification that the list behind an <see cref="IRoboCopyResultsList"/> interface has been updated
        /// </summary>
        public delegate void ResultsListUpdated(object sender, ResultListUpdatedEventArgs e);

        #endregion

        #region < Public Properties >

        /// <summary> Sum of all DirectoryStatistics objects </summary>
        /// <remarks>Underlying value is Lazy{Statistic} object - Initial value not calculated until first request. </remarks>
        public IStatistic DirectoriesStatistic => Total_DirStatsField?.Value;

        /// <summary> Sum of all ByteStatistics objects </summary>
        /// <remarks>Underlying value is Lazy{Statistic} object - Initial value not calculated until first request. </remarks>
        public IStatistic BytesStatistic => Total_ByteStatsField?.Value;

        /// <summary> Sum of all FileStatistics objects </summary>
        /// <remarks>Underlying value is Lazy{Statistic} object - Initial value not calculated until first request. </remarks>
        public IStatistic FilesStatistic => Total_FileStatsField?.Value;

        /// <summary> Average of all SpeedStatistics objects </summary>
        /// <remarks>Underlying value is Lazy{SpeedStatistic} object - Initial value not calculated until first request. </remarks>
        public ISpeedStatistic SpeedStatistic => Average_SpeedStatsField?.Value;

        /// <summary> Sum of all RoboCopyExitStatus objects </summary>
        /// <remarks>Underlying value is Lazy object - Initial value not calculated until first request. </remarks>
        public IRoboCopyCombinedExitStatus Status => ExitStatusSummaryField?.Value;

        RoboCopyExitStatus IResults.Status => ExitStatusSummaryField?.Value;

        /// <summary> The Collection of RoboCopy Results. Add/Removal of <see cref="RoboCopyResults"/> objects must be performed through this object's methods, not on the list directly. </summary>
        public IReadOnlyList<RoboCopyResults> Collection => ResultsList;

        /// <inheritdoc cref="List{T}.Count"/>
        public int Count => ResultsList.Count;

        /// <summary>
        /// Get or Set the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to Get or Set.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public RoboCopyResults this[int index] { get => ResultsList[index]; set => ResultsList[index] = value; }

        #endregion

        #region < Get Array Methods ( Public ) >

        /// <summary>
        /// Get a snapshot of the ByteStatistics objects from this list.
        /// </summary>
        /// <returns>New array of the ByteStatistic objects</returns>
        public IStatistic[] GetByteStatistics()
        {
            List<Statistic> tmp = new List<Statistic> { };
            foreach (RoboCopyResults r in this)
                tmp.Add(r?.BytesStatistic);
            return tmp.ToArray();
        }

        /// <summary>
        /// Get a snapshot of the DirectoriesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the DirectoriesStatistic objects</returns>
        public IStatistic[] GetDirectoriesStatistics()
        {
            List<Statistic> tmp = new List<Statistic> { };
            foreach (RoboCopyResults r in this)
                tmp.Add(r?.DirectoriesStatistic);
            return tmp.ToArray();
        }

        /// <summary>
        /// Get a snapshot of the FilesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the FilesStatistic objects</returns>
        public IStatistic[] GetFilesStatistics()
        {
            List<Statistic> tmp = new List<Statistic> { };
            foreach (RoboCopyResults r in this)
                tmp.Add(r?.FilesStatistic);
            return tmp.ToArray();
        }

        /// <summary>
        /// Get a snapshot of the FilesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the FilesStatistic objects</returns>
        public RoboCopyExitStatus[] GetStatuses()
        {
            List<RoboCopyExitStatus> tmp = new List<RoboCopyExitStatus> { };
            foreach (RoboCopyResults r in this)
                tmp.Add(r?.Status);
            return tmp.ToArray();
        }

        /// <summary>
        /// Get a snapshot of the FilesStatistic objects from this list.
        /// </summary>
        /// <returns>New array of the FilesStatistic objects</returns>
        public ISpeedStatistic[] GetSpeedStatistics()
        {
            List<SpeedStatistic> tmp = new List<SpeedStatistic> { };
            foreach (RoboCopyResults r in this)
                tmp.Add(r?.SpeedStatistic);
            return tmp.ToArray();
        }

        /// <summary>
        /// Combine the <see cref="RoboCopyResults.RoboCopyErrors"/> into a single array of errors
        /// </summary>
        /// <returns>New array of the ErrorEventArgs objects</returns>
        public ErrorEventArgs[] GetErrors()
        {
            List<ErrorEventArgs> tmp = new List<ErrorEventArgs> { };
            foreach (RoboCopyResults r in this)
                tmp.AddRange(r?.RoboCopyErrors);
            return tmp.ToArray();
        }

        #endregion

        #region < INotifyCollectionChanged >

        /// <summary>
        /// <inheritdoc cref="ObservableList{T}.CollectionChanged"/>
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>Process the Added/Removed items, then fire the event</summary>
        /// <inheritdoc cref="ObservableList{T}.OnCollectionChanged(NotifyCollectionChangedEventArgs)"/>
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move) goto RaiseEvent; // Sorting causes no change in math -> Simply raise the event

            //Reset means a drastic change -> Recalculate everything, then goto RaiseEvent
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                //Bytes
                if (Total_ByteStatsField.IsValueCreated)
                {
                    Total_ByteStatsField.Value.Reset(false);
                    Total_ByteStatsField.Value.AddStatistic(GetByteStatistics());
                }
                //Directories
                if (Total_DirStatsField.IsValueCreated)
                {
                    Total_DirStatsField.Value.Reset(false);
                    Total_DirStatsField.Value.AddStatistic(GetDirectoriesStatistics());
                }
                //Files
                if (Total_FileStatsField.IsValueCreated)
                {
                    Total_FileStatsField.Value.Reset(false);
                    Total_FileStatsField.Value.AddStatistic(GetFilesStatistics());
                }
                //Exit Status
                if (ExitStatusSummaryField.IsValueCreated)
                {
                    ExitStatusSummaryField.Value.Reset(false);
                    ExitStatusSummaryField.Value.CombineStatus(GetStatuses());
                }
                //Speed
                if (Average_SpeedStatsField.IsValueCreated)
                {
                    Average_SpeedStatsField.Value.Reset(false);
                    Average_SpeedStatsField.Value.Average(GetSpeedStatistics());
                }

                goto RaiseEvent;
            }

            //Process New Items
            if (e.NewItems != null)
            {
                int i = 0;
                int i2 = e.NewItems.Count;
                foreach (RoboCopyResults r in e?.NewItems)
                {
                    i++;
                    bool RaiseValueChangeEvent = (e.OldItems == null || e.OldItems.Count == 0) && (i == i2); //Prevent raising the event if more calculation needs to be performed either from NewItems or from OldItems
                    //Bytes
                    if (Total_ByteStatsField.IsValueCreated)
                        Total_ByteStatsField.Value.AddStatistic(r?.BytesStatistic, RaiseValueChangeEvent);
                    //Directories
                    if (Total_DirStatsField.IsValueCreated)
                        Total_DirStatsField.Value.AddStatistic(r?.DirectoriesStatistic, RaiseValueChangeEvent);
                    //Files
                    if (Total_FileStatsField.IsValueCreated)
                        Total_FileStatsField.Value.AddStatistic(r?.FilesStatistic, RaiseValueChangeEvent);
                    //Exit Status
                    if (ExitStatusSummaryField.IsValueCreated)
                        ExitStatusSummaryField.Value.CombineStatus(r?.Status, RaiseValueChangeEvent);
                    //Speed
                    if (Average_SpeedStatsField.IsValueCreated)
                    {
                        Average_SpeedStatsField.Value.Add(r?.SpeedStatistic);
                    }
                }
            }

            //Process Removed Items
            if (e.OldItems != null)
            {
                int i = 0;
                int i2 = e.OldItems.Count;
                foreach (RoboCopyResults r in e?.OldItems)
                {
                    i++;
                    bool RaiseValueChangeEvent = i == i2;
                    //Bytes
                    if (Total_ByteStatsField.IsValueCreated)
                        Total_ByteStatsField.Value.Subtract(r?.BytesStatistic, RaiseValueChangeEvent);
                    //Directories
                    if (Total_DirStatsField.IsValueCreated)
                        Total_DirStatsField.Value.Subtract(r?.DirectoriesStatistic, RaiseValueChangeEvent);
                    //Files
                    if (Total_FileStatsField.IsValueCreated)
                        Total_FileStatsField.Value.Subtract(r?.FilesStatistic, RaiseValueChangeEvent);
                    //Exit Status
                    if (ExitStatusSummaryField.IsValueCreated && RaiseValueChangeEvent)
                    {
                        ExitStatusSummaryField.Value.Reset(false);
                        ExitStatusSummaryField.Value.CombineStatus(GetStatuses());
                    }
                    //Speed
                    if (Average_SpeedStatsField.IsValueCreated)
                    {
                        if (this.Count == 0)
                            Average_SpeedStatsField.Value.Reset();
                        else
                            Average_SpeedStatsField.Value.Subtract(r.SpeedStatistic);
                    }
                }
            }

        RaiseEvent:
            //Raise the CollectionChanged event
            CollectionChanged?.Invoke(this, e);
        }

        #endregion

        #region < ICloneable >

        /// <summary> Clone this object to a new RoboCopyResultsList </summary>
        public RoboCopyResultsList Clone() => new RoboCopyResultsList(this);

        #endregion

        #region < IList{T} Implementation >

        bool ICollection<RoboCopyResults>.IsReadOnly => false;

        /// <inheritdoc cref="IList{T}.IndexOf(T)"/>
        public int IndexOf(RoboCopyResults item) => ResultsList.IndexOf(item);

        /// <inheritdoc cref="ObservableList{T}.Insert(int, T)"/>
        public void Insert(int index, RoboCopyResults item) => ResultsList.Insert(index, item);

        /// <inheritdoc cref="ObservableList{T}.RemoveAt(int)"/>
        public void RemoveAt(int index) => ResultsList.RemoveAt(index);

        /// <inheritdoc cref="ObservableList{T}.Add(T)"/>
        public void Add(RoboCopyResults item) => ResultsList.Add(item);

        /// <inheritdoc cref="ObservableList{T}.Clear"/>
        public void Clear() => ResultsList.Clear();

        /// <inheritdoc cref="IList.Contains(object)"/>
        public bool Contains(RoboCopyResults item) => ResultsList.Contains(item);

        /// <inheritdoc cref="ICollection.CopyTo(Array, int)"/>
        public void CopyTo(RoboCopyResults[] array, int arrayIndex) => ResultsList.CopyTo(array, arrayIndex);

        /// <inheritdoc cref="ObservableList{T}.Remove(T)"/>
        public bool Remove(RoboCopyResults item) => ResultsList.Remove(item);

        /// <inheritdoc cref="List{T}.GetEnumerator"/>
        public IEnumerator<RoboCopyResults> GetEnumerator() => ResultsList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ResultsList.GetEnumerator();

        #endregion

    }

}

