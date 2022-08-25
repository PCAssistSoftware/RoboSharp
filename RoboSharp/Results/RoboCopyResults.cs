﻿using System;
using System.Collections.Generic;
using RoboSharp.Interfaces;

namespace RoboSharp.Results
{
    /// <summary>
    /// Results provided by the RoboCopy command. Includes the Log, Exit Code, and statistics parsed from the log.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/tjscience/RoboSharp/wiki/RoboCopyResults"/>
    /// </remarks>
    public class RoboCopyResults : IResults, ITimeSpan
    {
        internal RoboCopyResults() { }

        /// <summary>
        /// Constructor available for consumers to use in custom IRoboCommand implementations
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="bytes"></param>
        /// <param name="files"></param>
        /// <param name="directories"></param>
        /// <param name="speed"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="status"></param>
        /// <param name="logLines"></param>
        public RoboCopyResults(string source, string destination, IStatistic bytes, IStatistic files, IStatistic directories, ISpeedStatistic speed, DateTime startTime, DateTime endTime = default,  RoboCopyExitStatus status = null, params string[] logLines)
        {
            DirectoriesStatistic = new Statistic(directories);
            FilesStatistic = new Statistic(files);
            BytesStatistic = new Statistic(bytes);
            SpeedStatistic = new SpeedStatistic(speed);
            Source = source;
            Destination = destination;
            StartTime = startTime;
            EndTime = endTime ==  default ?  DateTime.Now : endTime;
            TimeSpan = EndTime - StartTime;
            Status = status ?? new RoboCopyExitStatus(ProgressEstimator.GetExitCode(files, directories));
            LogLines = logLines;
        }

        #region < Properties >

        /// <inheritdoc cref="CopyOptions.Source"/>
        public string Source { get; internal set; }

        /// <inheritdoc cref="CopyOptions.Destination"/>
        public string Destination { get; internal set; }

        /// <inheritdoc cref="RoboCommand.CommandOptions"/>
        public string CommandOptions { get; internal set; }

        /// <inheritdoc cref="RoboCommand.Name"/>
        public string JobName { get; internal set; }

        /// <summary>
        /// All Errors that were generated by RoboCopy during the run.
        /// </summary>
        public ErrorEventArgs[] RoboCopyErrors{ get; internal set; }

        /// <inheritdoc cref="RoboCopyExitStatus"/>
        public RoboCopyExitStatus Status { get; internal set; }

        /// <summary> Information about number of Directories Copied, Skipped, Failed, etc.</summary>
        /// <remarks> 
        /// If the job was cancelled, or run without a Job Summary, this will attempt to provide approximate results based on the Process.StandardOutput from Robocopy. <br/>
        /// Results should only be treated as accurate if <see cref="Status"/>.ExitCodeValue >= 0 and the job was run with <see cref="LoggingOptions.NoJobSummary"/> = FALSE
        /// </remarks>
        public Statistic DirectoriesStatistic { get; internal set; }

        /// <summary> Information about number of Files Copied, Skipped, Failed, etc.</summary>
        /// <remarks> 
        /// If the job was cancelled, or run without a Job Summary, this will attempt to provide approximate results based on the Process.StandardOutput from Robocopy. <br/>
        /// Results should only be treated as accurate if <see cref="Status"/>.ExitCodeValue >= 0 and the job was run with <see cref="LoggingOptions.NoJobSummary"/> = FALSE
        /// </remarks>
        public Statistic FilesStatistic { get; internal set; }

        /// <summary> Information about number of Bytes processed.</summary>
        /// <remarks> 
        /// If the job was cancelled, or run without a Job Summary, this will attempt to provide approximate results based on the Process.StandardOutput from Robocopy. <br/>
        /// Results should only be treated as accurate if <see cref="Status"/>.ExitCodeValue >= 0 and the job was run with <see cref="LoggingOptions.NoJobSummary"/> = FALSE
        /// </remarks>
        public Statistic BytesStatistic { get; internal set; }

        /// <inheritdoc cref="RoboSharp.Results.SpeedStatistic"/>
        public SpeedStatistic SpeedStatistic { get; internal set; }

        /// <summary> Output Text reported by RoboCopy </summary>
        public string[] LogLines { get; internal set; }

        /// <summary> Time the RoboCopy process was started </summary>
        public DateTime StartTime { get; internal set; }

        /// <summary> Time the RoboCopy process was completed / cancelled. </summary>
        public DateTime EndTime { get; internal set; }

        /// <summary> Length of Time the RoboCopy Process ran </summary>
        public TimeSpan TimeSpan { get; internal set; }

        #endregion

        #region < IResults >

        IStatistic IResults.BytesStatistic => BytesStatistic;
        IStatistic IResults.DirectoriesStatistic => DirectoriesStatistic;
        IStatistic IResults.FilesStatistic => FilesStatistic;

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            string str = $"ExitCode: {Status.ExitCode}, Directories: {DirectoriesStatistic?.Total.ToString() ?? "Unknown"}, Files: {FilesStatistic?.Total.ToString() ?? "Unknown"}, Bytes: {BytesStatistic?.Total.ToString() ?? "Unknown"}";

            if (SpeedStatistic != null)
            {
                str += $", Speed: {SpeedStatistic.BytesPerSec} Bytes/sec";
            }

            return str;
        }
    }
}