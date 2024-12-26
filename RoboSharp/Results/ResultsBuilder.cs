using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboSharp.Results
{
    /// <summary>
    /// Helper class to build a <see cref="RoboCopyResults"/> object.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/ResultsBuilder"/>
    /// </remarks>
    internal partial class ResultsBuilder
    {
        internal ResultsBuilder(RoboCommand roboCommand, ProgressEstimator estimator) {
            RoboCommand = roboCommand;
            Estimator = estimator;
            _isLoggingHeader = !roboCommand.LoggingOptions.NoJobHeader;
            _enableFileLogging = roboCommand.Configuration.EnableFileLogging;
            _noJobSummary = roboCommand.LoggingOptions.NoJobSummary;
            if (!_noJobSummary)
            {
                _lastLines = new Queue<string>();
                _maxLinesQueued = roboCommand.LoggingOptions.ListOnly ? 9 : 13;
            }
        }

        #region < Private Members >

        ///<summary>Reference back to the RoboCommand that spawned this object</summary>
        private readonly RoboCommand RoboCommand; 
        private readonly List<string> _outputLines = new List<string>();
        private readonly Queue<string> _lastLines;
        private readonly int _maxLinesQueued;
        private readonly bool _enableFileLogging;
        private readonly bool _noJobSummary;
        private bool _isLoggingHeader;
        private int _headerSepCount;
        private string _lastLine;

        /// <summary>This is the last line that was logged.</summary>
        internal string LastLine => _lastLine ?? "";

        #endregion

        #region < Command Options Properties >

        /// <see cref="RoboCommand.CommandOptions"/>
        internal string CommandOptions { get; set; }

        /// <inheritdoc cref="CopyOptions.Source"/>
        internal string Source { get; set; }
        
        /// <inheritdoc cref="CopyOptions.Destination"/>
        internal string Destination { get; set; }

        internal List<ErrorEventArgs> RoboCopyErrors { get; } = new List<ErrorEventArgs>();

        #endregion

        #region < Counters in case cancellation >

        /// <inheritdoc cref="ProgressEstimator"/>
        internal ProgressEstimator Estimator { get; }

        #endregion

        /// <summary>
        /// Add a LogLine reported by RoboCopy to the LogLines list.
        /// </summary>
        internal void AddOutput(string output)
        {
            if (_isLoggingHeader && output.Contains("--------------------------------"))
            {
                _headerSepCount += 1;
                _isLoggingHeader = _headerSepCount < 3;
                _outputLines.Add(output);
                _lastLine = output;
                return;
            }

            if (_isLoggingHeader || _enableFileLogging) _outputLines.Add(output); // Bypass logging the file names if EnableLogging is set to false
            if (!_noJobSummary)
            {
                if (_lastLines.Count >= _maxLinesQueued) _ = _lastLines.Dequeue();
                _lastLines.Enqueue(output);
            }
            _lastLine = output;
        }

        /// <summary>
        /// Ensures the Error Data gets added to the log lines
        /// </summary>
        internal void AddErrorOutput(string line1, string line2)
        {
            if (_isLoggingHeader | _enableFileLogging) return; // should already be logged
            _outputLines.Add(line1);
            _outputLines.Add(line2);
            _lastLine = line2;
        }

        /// <summary>
        /// Builds the results from parsing the logLines.
        /// </summary>
        /// <param name="exitCode"></param>
        /// <param name="IsProgressUpdateEvent">This is used by the ProgressUpdateEventArgs to ignore the loglines when generating the estimate </param>
        /// <returns></returns>
        internal RoboCopyResults BuildResults(int exitCode, bool IsProgressUpdateEvent = false)
        {
            var res = Estimator.GetResults(); //Start off with the estimated results, and replace if able

            res.JobName = RoboCommand.Name;
            res.Status = new RoboCopyExitStatus(exitCode);

            var statisticLines = IsProgressUpdateEvent ? new List<string>() : GetStatisticLines();

            //Dir Stats
            if (exitCode >= 0 && statisticLines.Count >= 1)
                res.DirectoriesStatistic = Statistic.Parse(Statistic.StatType.Directories, statisticLines[0], res.DirectoriesStatistic);

            //File Stats
            if (exitCode >= 0 && statisticLines.Count >= 2)
                res.FilesStatistic = Statistic.Parse(Statistic.StatType.Files, statisticLines[1], res.FilesStatistic);

            //Bytes
            if (exitCode >= 0 && statisticLines.Count >= 3)
                res.BytesStatistic = Statistic.Parse(Statistic.StatType.Bytes, statisticLines[2], res.BytesStatistic);

            //Speed Stats
            if (exitCode >= 0 && statisticLines.Count >= 6)
                res.SpeedStatistic = SpeedStatistic.Parse(statisticLines[4], statisticLines[5]);

            res.LogLines = _outputLines.ToArray();
            res.RoboCopyErrors = this.RoboCopyErrors.ToArray();
            res.Source = this.Source;
            res.Destination = this.Destination;
            res.CommandOptions = this.CommandOptions;
            return res;
        }

        private IList<string> GetStatisticLines()
        {
            if (_noJobSummary)
            {
#if NET452
                return new string[] { };
#else
                return System.Array.Empty<string>();
#endif
            }

            var res = new List<string>();
            while (_lastLines.TryDequeue(out string line))
            {
                if (!_enableFileLogging) _outputLines.Add(line); // Add the summary lines to the output lines if they were not already recorded
                if (line.Contains(':') && !line.Contains('\\'))
                    res.Add(line);
            }
            return res;
        }
    }
}
