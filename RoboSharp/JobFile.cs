using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RoboSharp.Interfaces;
using System.Threading.Tasks;
using RoboSharp.Results;

namespace RoboSharp
{
    /// <summary>
    /// Represents a single RoboCopy Job File
    /// <para/>Implements: <br/>
    /// <see cref="IRoboCommand"/> <br/>
    /// <see cref="ICloneable"/> <br/>
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/JobFile"/>
    /// </remarks>
    public class JobFile : ICloneable, IRoboCommand
    {

        #region < Constructor >

        /// <summary>
        /// Create a JobFile with Default Options
        /// </summary>
        public JobFile() { roboCommand = new RoboCommand(); }

        /// <summary>
        /// Constructor for ICloneable Interface
        /// </summary>
        /// <param name="jobFile"></param>
        /// <exception cref="ArgumentNullException"/>
        public JobFile(JobFile jobFile)
        {
            this.roboCommand = jobFile?.roboCommand?.Clone() ?? throw new ArgumentNullException(nameof(jobFile));
        }

        /// <summary>
        /// Create a JobFile object that shares its options objects with the specified <paramref name="cmd"/>
        /// </summary>
        /// <param name="cmd">IRoboCommand whose options shall be saved.</param>
        /// <param name="filePath">Optional FilePath to specify for future call to <see cref="Save()"/></param>
        /// <exception cref="ArgumentNullException"/>
        public JobFile(IRoboCommand cmd, string filePath = "")
        {
            if (cmd is null) throw new ArgumentNullException(nameof(cmd));
            FilePath = filePath ?? "";
            roboCommand = new RoboCommand(cmd.Name, cmd.CopyOptions.Source, cmd.CopyOptions.Destination, true, cmd.Configuration, cmd.CopyOptions, cmd.SelectionOptions, cmd.RetryOptions, cmd.LoggingOptions);
            try { roboCommand.JobOptions = cmd.JobOptions; } catch { }
        }

        /// <summary>
        /// Constructor for Factory Methods
        /// </summary>
        /// <param name="filePath">the filepath to save the job file into</param>
        /// <param name="commandToUseWhenSaving">the RoboCommand to use when saving</param>
        /// <exception cref="ArgumentNullException"/>
        private JobFile(string filePath, RoboCommand commandToUseWhenSaving)
        {
            FilePath = filePath ?? "";
            roboCommand = commandToUseWhenSaving ?? throw new ArgumentNullException(nameof(commandToUseWhenSaving));
        }

        #endregion

        #region < Factory Methods >

        /// <inheritdoc cref="JobFileBuilder.Parse(string)"/>
        public static JobFile ParseJobFile(string path)
        {
            return new JobFile(path, JobFileBuilder.Parse(path));
        }

        /// <inheritdoc cref="JobFileBuilder.Parse(StreamReader)"/>
        public static JobFile ParseJobFile(StreamReader streamReader)
        {
            return new JobFile("", JobFileBuilder.Parse(streamReader));
        }

        /// <inheritdoc cref="JobFileBuilder.Parse(FileInfo)"/>
        public static JobFile ParseJobFile(FileInfo file)
        {
            return new JobFile(file.FullName, JobFileBuilder.Parse(file));
        }

        /// <inheritdoc cref="JobFileBuilder.Parse(IEnumerable{String})"/>
        public static JobFile ParseJobFile(IEnumerable<string> FileText)
        {
            return new JobFile("", JobFileBuilder.Parse(FileText));
        }

        /// <summary>Try to parse a file at the specified path.</summary>
        /// <returns>True if the the <paramref name="jobFile"/> was created successfully, otherwise false.</returns>
        /// <inheritdoc cref="ParseJobFile(string)"/>
        public static bool TryParseJobFile(string path, out JobFile jobFile)
        {
            jobFile = null;
            try
            {
                jobFile = ParseJobFile(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Try to parse a file at the specified path.</summary>
        /// <returns>True if the the <paramref name="jobFile"/> was created successfully, otherwise false.</returns>
        /// <inheritdoc cref="ParseJobFile(FileInfo)"/>
        public static bool TryParseJobFile(FileInfo file, out JobFile jobFile)
        {
            jobFile = null;
            try
            {
                jobFile = ParseJobFile(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Try to parse a file at the specified path.</summary>
        /// <returns>True if the the <paramref name="jobFile"/> was created successfully, otherwise false.</returns>
        /// <inheritdoc cref="ParseJobFile(StreamReader)"/>
        public static bool TryParseJobFile(StreamReader streamReader, out JobFile jobFile)
        {
            jobFile = null;
            try
            {
                jobFile = ParseJobFile(streamReader);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region < ICLONEABLE >

        /// <summary>
        /// Create a clone of this JobFile
        /// </summary>
        public JobFile Clone() => new JobFile(this);

        object ICloneable.Clone() => Clone();

        #endregion

        #region < Constants >

        /// <summary>
        /// Expected File Extension for Job Files exported from RoboCopy.
        /// </summary>
        public const string JOBFILE_Extension = ".RCJ";

        /// <summary>
        /// FileFilter to use in an to search for this extension, such as with <see cref="DirectoryInfo.GetFiles(string)"/>
        /// </summary>
        public const string JOBFILE_SearchPattern = "*.RCJ";

        /// <summary>
        /// FileFilter to use in a dialog window, such as the OpenFileDialog window.
        /// </summary>

        public const string JOBFILE_DialogFilter = "RoboCopy Job|*.RCJ";
        #endregion

        #region < Fields >

        /// <summary>
        /// The underlying RoboCommand object
        /// </summary>
        private readonly RoboCommand roboCommand;

        #endregion

        #region < Properties >

        /// <summary>FilePath of the Job File </summary>
        public virtual string FilePath { get; set; }

        /// <inheritdoc cref="RoboCommand.Name"/>
        public string Name
        {
            get => roboCommand.Name;
            set => roboCommand.Name = value;
        }

        /// <inheritdoc cref="RoboCommand.LoggingOptions"/>
        public CopyOptions CopyOptions => roboCommand?.CopyOptions;

        /// <inheritdoc cref="RoboCommand.LoggingOptions"/>
        public LoggingOptions LoggingOptions => roboCommand?.LoggingOptions;

        /// <inheritdoc cref="RoboCommand.LoggingOptions"/>
        public RetryOptions RetryOptions => roboCommand?.RetryOptions;

        /// <inheritdoc cref="RoboCommand.LoggingOptions"/>
        public SelectionOptions SelectionOptions => roboCommand?.SelectionOptions;

        /// <summary>
        /// Check if the <see cref="CopyOptions.Source"/> is empty
        /// </summary>
        public bool NoSourceDirectory => string.IsNullOrWhiteSpace(CopyOptions.Source);

        /// <summary>
        /// Check if the <see cref="CopyOptions.Destination"/> is empty
        /// </summary>
        public bool NoDestinationDirectory => string.IsNullOrWhiteSpace(CopyOptions.Destination);

        #endregion

        #region < Methods >
#pragma warning disable CS1573

        /// <summary>
        /// Update the <see cref="FilePath"/> property and save the JobFile to the <paramref name="path"/>
        /// </summary>
        /// <param name="path">Update the <see cref="FilePath"/> property, then save the JobFile to this path.</param>
        /// <inheritdoc cref="Save()"/>
        /// <inheritdoc cref="RoboCommand.SaveAsJobFile(string, bool, bool, string, string, string)"/>
        public async Task Save(string path, bool IncludeSource = false, bool IncludeDestination = false)

        {
            if (path.IsNullOrWhiteSpace()) throw new ArgumentException("path Property is Empty");
            FilePath = path;
            await roboCommand.SaveAsJobFile(FilePath, IncludeSource, IncludeDestination);
        }

        /// <summary>
        /// Save the JobFile to <see cref="FilePath"/>. <br/>
        /// Source and Destination will be included by default.
        /// </summary>
        /// <remarks>If path is null/empty, will throw <see cref="ArgumentException"/></remarks>
        /// <returns>Task that completes when the JobFile has been saved.</returns>
        /// <exception cref="ArgumentException"/>
        public async Task Save()
        {
            if (FilePath.IsNullOrWhiteSpace()) throw new ArgumentException("FilePath Property is Empty");
            await roboCommand.SaveAsJobFile(FilePath, true, true);
        }

        /// <inheritdoc cref="RoboCommand.ToString"/>
        public override string ToString()
        {
            return roboCommand.ToString();
        }

#pragma warning restore CS1573
        #endregion

        #region < IRoboCommand Interface >

        #region < Events >

        event RoboCommand.FileProcessedHandler IRoboCommand.OnFileProcessed
        {
            add
            {
                ((IRoboCommand)roboCommand).OnFileProcessed += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).OnFileProcessed -= value;
            }
        }

        event RoboCommand.CommandErrorHandler IRoboCommand.OnCommandError
        {
            add
            {
                ((IRoboCommand)roboCommand).OnCommandError += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).OnCommandError -= value;
            }
        }

        event RoboCommand.ErrorHandler IRoboCommand.OnError
        {
            add
            {
                ((IRoboCommand)roboCommand).OnError += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).OnError -= value;
            }
        }

        event RoboCommand.CommandCompletedHandler IRoboCommand.OnCommandCompleted
        {
            add
            {
                ((IRoboCommand)roboCommand).OnCommandCompleted += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).OnCommandCompleted -= value;
            }
        }

        event RoboCommand.CopyProgressHandler IRoboCommand.OnCopyProgressChanged
        {
            add
            {
                ((IRoboCommand)roboCommand).OnCopyProgressChanged += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).OnCopyProgressChanged -= value;
            }
        }

        event RoboCommand.ProgressUpdaterCreatedHandler IRoboCommand.OnProgressEstimatorCreated
        {
            add
            {
                ((IRoboCommand)roboCommand).OnProgressEstimatorCreated += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).OnProgressEstimatorCreated -= value;
            }
        }

        event UnhandledExceptionEventHandler IRoboCommand.TaskFaulted
        {
            add
            {
                ((IRoboCommand)roboCommand).TaskFaulted += value;
            }

            remove
            {
                ((IRoboCommand)roboCommand).TaskFaulted -= value;
            }
        }

        #endregion

        #region < Properties >

        string IRoboCommand.Name => roboCommand.Name;
        bool IRoboCommand.IsPaused => roboCommand.IsPaused;
        bool IRoboCommand.IsRunning => roboCommand.IsRunning;
        bool IRoboCommand.IsScheduled => roboCommand.IsScheduled;
        bool IRoboCommand.IsCancelled => roboCommand.IsCancelled;
        bool IRoboCommand.StopIfDisposing => roboCommand.StopIfDisposing;
        IProgressEstimator IRoboCommand.IProgressEstimator => roboCommand.IProgressEstimator;
        SelectionOptions IRoboCommand.SelectionOptions { get => ((IRoboCommand)roboCommand).SelectionOptions; set => ((IRoboCommand)roboCommand).SelectionOptions = value; }
        RetryOptions IRoboCommand.RetryOptions { get => ((IRoboCommand)roboCommand).RetryOptions; set => ((IRoboCommand)roboCommand).RetryOptions = value; }
        LoggingOptions IRoboCommand.LoggingOptions { get => roboCommand.LoggingOptions; set => roboCommand.LoggingOptions = value; }
        CopyOptions IRoboCommand.CopyOptions { get => ((IRoboCommand)roboCommand).CopyOptions; set => ((IRoboCommand)roboCommand).CopyOptions = value; }
        JobOptions IRoboCommand.JobOptions { get => ((IRoboCommand)roboCommand).JobOptions; }
        RoboSharpConfiguration IRoboCommand.Configuration => roboCommand.Configuration;
        string IRoboCommand.CommandOptions => roboCommand.CommandOptions;

        #endregion

        #region < Methods >

        void IRoboCommand.Pause()
        {
            ((IRoboCommand)roboCommand).Pause();
        }

        void IRoboCommand.Resume()
        {
            ((IRoboCommand)roboCommand).Resume();
        }

        Task IRoboCommand.Start(string domain, string username, string password)
        {
            return ((IRoboCommand)roboCommand).Start(domain, username, password);
        }

        void IRoboCommand.Stop()
        {
            ((IRoboCommand)roboCommand).Stop();
        }

        void IRoboCommand.Dispose()
        {
            roboCommand.Stop();
        }

        Task IRoboCommand.Start_ListOnly(string domain, string username, string password)
        {
            return roboCommand.Start_ListOnly();
        }

        Task<RoboCopyResults> IRoboCommand.StartAsync_ListOnly(string domain, string username, string password)
        {
            return roboCommand.StartAsync_ListOnly();
        }

        Task<RoboCopyResults> IRoboCommand.StartAsync(string domain, string username, string password)
        {
            return roboCommand.StartAsync_ListOnly();
        }

        RoboCopyResults IRoboCommand.GetResults()
        {
            return ((IRoboCommand)roboCommand).GetResults();
        }
        #endregion

        #endregion
    }
}
