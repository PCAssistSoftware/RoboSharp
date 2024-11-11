using CommunityToolkit.Mvvm.ComponentModel;
using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RoboSharp.BackupApp.ViewModels
{
    internal partial class JobResultsViewModel : ObservableObject
    {
        public JobResultsViewModel() { }

        public JobResultsViewModel(IResults results)
        {
            if (results is IRoboCopyResultsList list)
                BindToResultsList(list);
            else if (results is Results.RoboCopyResults res)
                BindToResults(res);
        }

        private bool _areResultsBound;

        [ObservableProperty] private IResults _results;
        [ObservableProperty] private IStatistic _byteStat;
        [ObservableProperty] private IStatistic _dirStat;
        [ObservableProperty] private IStatistic _fileStat;
        [ObservableProperty] private string _jobName;
        [ObservableProperty] private string _logLines;
        [ObservableProperty] private string _dirStatString;
        [ObservableProperty] private string _fileStatString;
        [ObservableProperty] private string _byteStatString;
        [ObservableProperty] private string _summary;

        /// <summary>
        /// Bind to a static ResultsObject
        /// </summary>
        /// <param name="resultsObj"></param>
        public void BindToResults(RoboSharp.Results.RoboCopyResults resultsObj)
        {
            Unbind();

            //Set starting values 
            Results = resultsObj;
            ByteStat = resultsObj?.BytesStatistic;
            DirStat = resultsObj?.DirectoriesStatistic;
            FileStat = resultsObj?.FilesStatistic;
            StringBuilder log = new StringBuilder();
            if (resultsObj != null)
            {
                resultsObj.LogLines.ToList().ForEach(s =>
                {
                    log.AppendLine(s);
                });
            }
            LogLines = log.ToString();
            JobName = resultsObj?.JobName;
            _areResultsBound = false;

            DirectoriesStatistic_PropertyChanged(null, null);
            FilesStatistic_PropertyChanged(null, null);
            BytesStatistic_PropertyChanged(null, null);

        }


        /// <summary>
        /// Bind to a ResultsList
        /// </summary>
        /// <param name="list"></param>
        public void BindToResultsList(RoboSharp.Interfaces.IRoboCopyResultsList list)
        {
            Unbind();

            //Set starting values 
            Results = list;
            ByteStat = list.BytesStatistic;
            DirStat = list.DirectoriesStatistic;
            FileStat = list.FilesStatistic;
            LogLines = "";
            JobName = "";
            _areResultsBound = true;

            // Initialize the values
            DirectoriesStatistic_PropertyChanged(null, null);
            FilesStatistic_PropertyChanged(null, null);
            BytesStatistic_PropertyChanged(null, null);

            //Update Summary Event
            ShowResultsListSummary(null, new System.ComponentModel.PropertyChangedEventArgs(""));
            list.CollectionChanged += (o, e) => ShowResultsListSummary(o, null);

            ////Bind in case updates
            DirStat.PropertyChanged += DirectoriesStatistic_PropertyChanged;
            FileStat.PropertyChanged += FilesStatistic_PropertyChanged;
            ByteStat.PropertyChanged += BytesStatistic_PropertyChanged;
        }

        private void Unbind()
        {
            if (Results != null)
            {
                DirStat.PropertyChanged -= DirectoriesStatistic_PropertyChanged;
                FileStat.PropertyChanged -= FilesStatistic_PropertyChanged;
                ByteStat.PropertyChanged -= BytesStatistic_PropertyChanged;

                DirStat.PropertyChanged -= ShowResultsListSummary;
                FileStat.PropertyChanged -= ShowResultsListSummary;
                ByteStat.PropertyChanged -= ShowResultsListSummary;
            }
            Results = null;
        }

        private void DirectoriesStatistic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            DirStatString= UpdateLabel(DirStat);
            if (!_areResultsBound) ShowSelectedJobSummary();
        }
        private void FilesStatistic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FileStatString= UpdateLabel(FileStat);
            if (!_areResultsBound) ShowSelectedJobSummary();
        }
        private void BytesStatistic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ByteStatString =  UpdateLabel( ByteStat);
        }

        private string UpdateLabel(IStatistic stat) => stat?.ToString(false, true, "\n", false) ?? "";

        /// <summary>
        /// Show the Summary of the Selected job
        /// </summary>
        private void ShowSelectedJobSummary()
        {
            Results.RoboCopyResults result = Results as Results.RoboCopyResults;
            string NL = Environment.NewLine;
            Summary =
                $"Source: {result?.Source ?? ""}" +
                $"{NL}Destination: {result?.Destination ?? ""}" +
                $"{NL}Total Directories: {result?.DirectoriesStatistic?.Total ?? 0}" +
                $"{NL}Total Files: {result?.FilesStatistic?.Total ?? 0}" +
                $"{NL}Total Size (bytes): {result?.BytesStatistic?.Total ?? 0}" +
                $"{NL}Speed (Bytes/Second): {result?.SpeedStatistic?.BytesPerSec ?? 0}" +
                $"{NL}Speed (MB/Min): {result?.SpeedStatistic?.MegaBytesPerMin ?? 0}" +
                $"{NL}Log Lines Count: {result?.LogLines?.Length ?? 0}" +
                $"{NL}{result?.Status.ToString() ?? ""}";
        }

        /// <summary>
        /// Show Summary from a ResultsList object
        /// </summary>
        private void ShowResultsListSummary(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (e == null || (e.PropertyName != "" && e.PropertyName != "Total")) return;
            string NL = Environment.NewLine;
            if (Results is not IRoboCopyResultsList resultsList|| resultsList.Count == 0)
            {
                Summary = "Job History: None";
            }
            else
            {
                Summary =
                    $"Total Directories: {resultsList.DirectoriesStatistic.Total}" +
                    $"{NL}Total Files: {resultsList.FilesStatistic.Total}" +
                    $"{NL}Total Size (bytes): {resultsList.BytesStatistic.Total}" +
                    $"{NL}Speed (Bytes/Second): {resultsList.SpeedStatistic.BytesPerSec}" +
                    $"{NL}Speed (MB/Min): {resultsList.SpeedStatistic.MegaBytesPerMin}" +
                    $"{NL}Any Jobs Cancelled: {(resultsList.Status.WasCancelled ? "YES" : "NO")}" +
                    $"{NL}{resultsList.Status}";
            }
        }

        public override string ToString()
        {
            return Results.ToString();
        }
    }
}
