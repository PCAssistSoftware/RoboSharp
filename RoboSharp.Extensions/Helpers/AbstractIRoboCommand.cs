using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using RoboSharp.EventArgObjects;
using System.ComponentModel;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// Abstract Base class available for consumers to use for custom IRoboCommands
    /// </summary>
    public abstract class AbstractIRoboCommand : IRoboCommand, INotifyPropertyChanged
    {

        #region < Constructors >

        /// <summary>
        /// Instantiate all the robosharp options except JobOptions
        /// </summary>
        protected AbstractIRoboCommand()
        {
            CopyOptions = new CopyOptions();
            LoggingOptions = new LoggingOptions();
            RetryOptions = new RetryOptions();
            SelectionOptions = new SelectionOptions();
            Configuration = new RoboSharpConfiguration();
        }

        /// <summary>
        /// Instantiate all the robosharp options except JobOptions, then apply the provided parameters
        /// </summary>
        /// <inheritdoc cref="RoboCommand(string, string, CopyActionFlags, SelectionFlags, LoggingFlags)"/>
        protected AbstractIRoboCommand(
            string source,
            string destination,
            CopyActionFlags copyActionFlags = CopyActionFlags.Default,
            SelectionFlags selectionFlags = SelectionFlags.Default,
            LoggingFlags loggingFlags = LoggingFlags.RoboSharpDefault)
        {
            CopyOptions = new CopyOptions(source ?? string.Empty, destination ?? string.Empty, copyActionFlags);
            LoggingOptions = new LoggingOptions(loggingFlags);
            RetryOptions = new RetryOptions();
            SelectionOptions = new SelectionOptions(selectionFlags);
            Configuration = new RoboSharpConfiguration();
        }

        /// <summary>
        /// Instantiate all the robosharp options except JobOptions <br/>
        /// If any of the parameters are supplied, this will use that parameter object. If left null, create a new object of that type.
        /// </summary>
        protected AbstractIRoboCommand(CopyOptions copyOptions = null, LoggingOptions loggingOptions = null, RetryOptions retryOptions = null, SelectionOptions selectionOptions = null, RoboSharpConfiguration configuration = null)
        {
            CopyOptions = copyOptions ?? new CopyOptions();
            LoggingOptions = loggingOptions ?? new LoggingOptions();
            RetryOptions = retryOptions ?? new RetryOptions();
            SelectionOptions = selectionOptions ?? new SelectionOptions();
            Configuration = configuration ?? new RoboSharpConfiguration();
        }

        /// <summary>
        /// Create a new RoboCommand with identical options of the supplied IRoboCommand
        /// </summary>
        /// <param name="command">RoboCommand that provides the options to clone</param>
        protected AbstractIRoboCommand(IRoboCommand command)
        {
            Name = command.Name;
            StopIfDisposing = command.StopIfDisposing;
            Configuration = command.Configuration.Clone();
            CopyOptions = command.CopyOptions.Clone();
            //JobOptions = command.JobOptions.Clone();
            LoggingOptions = command.LoggingOptions.Clone();
            RetryOptions = command.RetryOptions.Clone();
            SelectionOptions = command.SelectionOptions.Clone();
        }

        #endregion

        #region < Properties >

        /// <inheritdoc/>
        public string Name
        {
            get { return NameField; }
            set { SetProperty(ref NameField, value, nameof(Name)); }
        }
        private string NameField;


        /// <inheritdoc/>
        public bool IsPaused
        {
            get { return IsPausedField; }
            protected set { SetProperty(ref IsPausedField, value, nameof(IsPaused)); }
        }
        private bool IsPausedField;


        /// <inheritdoc/>
        public bool IsRunning
        {
            get { return IsRunningField; }
            protected set { SetProperty(ref IsRunningField, value, nameof(IsRunning)); }
        }
        private bool IsRunningField;

        /// <inheritdoc/>
        public virtual bool IsScheduled => false;

        /// <inheritdoc/>
        public bool IsCancelled
        {
            get { return IsCancelledField; }
            protected set { SetProperty(ref IsCancelledField, value, nameof(IsCancelled)); }
        }
        private bool IsCancelledField;

        /// <inheritdoc/>

        public bool StopIfDisposing
        {
            get { return StopIfDisposingField; }
            set { SetProperty(ref StopIfDisposingField, value, nameof(StopIfDisposing)); }
        }
        private bool StopIfDisposingField;

        /// <inheritdoc/>
        public IProgressEstimator IProgressEstimator
        {
            get { return IProgressEstimatorField; }
            protected set { SetProperty(ref IProgressEstimatorField, value, nameof(IProgressEstimator)); }
        }
        private IProgressEstimator IProgressEstimatorField;


        /// <inheritdoc/>
        public virtual string CommandOptions { get => GenerateParameters(); }

        /// <inheritdoc/>
        public CopyOptions CopyOptions
        {
            get { return CopyOptionsField; }
            set { SetProperty(ref CopyOptionsField, value, nameof(CopyOptions)); }
        }
        private CopyOptions CopyOptionsField;


        /// <inheritdoc/>
        public SelectionOptions SelectionOptions
        {
            get { return SelectionOptionsField; }
            set { SetProperty(ref SelectionOptionsField, value, nameof(SelectionOptions)); }
        }
        private SelectionOptions SelectionOptionsField;


        /// <inheritdoc/>
        public RetryOptions RetryOptions
        {
            get { return RetryOptionsField; }
            set { SetProperty(ref RetryOptionsField, value, nameof(RetryOptions)); }
        }
        private RetryOptions RetryOptionsField;


        /// <inheritdoc/>
        public LoggingOptions LoggingOptions
        {
            get { return LoggingOptionsField; }
            set { SetProperty(ref LoggingOptionsField, value, nameof(LoggingOptions)); }
        }
        private LoggingOptions LoggingOptionsField;


        /// <inheritdoc/>
        public virtual JobOptions JobOptions => throw new NotImplementedException(string.Format("IRoboCommand of type '{0}' does not implement JobOptions", GetType().ToString()));

        /// <inheritdoc/>
        public RoboSharpConfiguration Configuration
        {
            get => _configuration;
            set
            {
                if (value is null) throw new ArgumentNullException("Configuration property can not be set null!");
                _configuration = value;
            }
        }
        private RoboSharpConfiguration _configuration;

        #endregion

        #region < Events >

        #region < OnFileProcessed >

        /// <inheritdoc/>
        public event RoboCommand.FileProcessedHandler OnFileProcessed;

        /// <summary> Raises the OnFileProcessed event </summary>
        /// <remarks><inheritdoc cref="OnFileProcessed"  path="*"/></remarks>
        protected virtual void RaiseOnFileProcessed(FileProcessedEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            OnFileProcessed?.Invoke(this, e);
        }
        
        /// <summary> Raises the OnFileProcessed event </summary>
        /// <remarks><inheritdoc cref="OnFileProcessed"  path="*"/></remarks>
        protected virtual void RaiseOnFileProcessed(ProcessedFileInfo fileInfo)
        {
            if (fileInfo is null) throw new ArgumentNullException(nameof(fileInfo));
            OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(fileInfo));
        }

        /// <summary> Raises the OnFileProcessed event </summary>
        /// <remarks><inheritdoc cref="OnFileProcessed"  path="*"/></remarks>
        protected void RaiseOnFileProcessed(string systemMessage)
        {
            if (OnFileProcessed != null) RaiseOnFileProcessed(new FileProcessedEventArgs(systemMessage));
        }

        #endregion

        #region < OnCommandError >

        /// <inheritdoc/>
        public event RoboCommand.CommandErrorHandler OnCommandError;

        /// <summary> Raises the OnCommandError event </summary>
        /// <remarks><inheritdoc cref="OnCommandError"  path="*"/></remarks>
        protected virtual void RaiseOnCommandError(CommandErrorEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            OnCommandError?.Invoke(this, e);
        }

        /// <summary> Raises the OnCommandError event </summary>
        /// <remarks><inheritdoc cref="OnCommandError"  path="*"/></remarks>
        protected virtual void RaiseOnCommandError(Exception exception)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));
            OnCommandError?.Invoke(this, new CommandErrorEventArgs(exception));
        }
        /// <summary> Raises the OnCommandError event </summary>
        /// <remarks><inheritdoc cref="OnCommandError"  path="*"/></remarks>
        protected virtual void RaiseOnCommandError(string message, Exception exception)
        {
            OnCommandError?.Invoke(this, new CommandErrorEventArgs(message, exception));
        }

        #endregion

        #region < OnError >

        /// <inheritdoc/>
        public event RoboCommand.ErrorHandler OnError;

        /// <summary> Raises the OnError event </summary>
        /// <remarks><inheritdoc cref="OnError"  path="*"/></remarks>
        protected virtual void RaiseOnError(ErrorEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            OnError?.Invoke(this, e);
        }

        #endregion

        #region < OnCommandCompleted >

        /// <inheritdoc/>
        public event RoboCommand.CommandCompletedHandler OnCommandCompleted;

        /// <summary> Raises the OnCommandCompleted event </summary>
        /// <remarks><inheritdoc cref="OnCommandCompleted"  path="*"/></remarks>
        protected virtual void RaiseOnCommandCompleted(RoboCommandCompletedEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            OnCommandCompleted?.Invoke(this, e);
        }

        /// <summary> Raises the OnCommandCompleted event </summary>
        /// <remarks><inheritdoc cref="OnCommandCompleted"  path="*"/></remarks>
        protected virtual void RaiseOnCommandCompleted(RoboCopyResults results)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));
            OnCommandCompleted?.Invoke(this, new RoboCommandCompletedEventArgs(results));
        }

        #endregion

        #region < OnCopyProgressChanged >

        /// <inheritdoc/>
        public event RoboCommand.CopyProgressHandler OnCopyProgressChanged;

        /// <summary> Raises the OnCopyProgressChanged event </summary>
        /// <remarks><inheritdoc cref="OnCopyProgressChanged"  path="*"/></remarks>
        protected virtual void RaiseOnCopyProgressChanged(CopyProgressEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            OnCopyProgressChanged?.Invoke(this, e);
        }

        /// <summary> Raises the OnCopyProgressChanged event </summary>
        /// <inheritdoc cref="CopyProgressEventArgs(double, ProcessedFileInfo, ProcessedFileInfo)"/>
        /// <remarks><inheritdoc cref="OnCopyProgressChanged"  path="*"/></remarks>
        protected virtual void RaiseOnCopyProgressChanged(double progress, ProcessedFileInfo currentFile, ProcessedFileInfo dirInfo = null)
        {
            OnCopyProgressChanged?.Invoke(this, new CopyProgressEventArgs(progress, currentFile, dirInfo));
        }

        #endregion

        #region < OnProgressEstimatorCreated >

        /// <inheritdoc/>
        public event RoboCommand.ProgressUpdaterCreatedHandler OnProgressEstimatorCreated;

        /// <summary> Raises the OnProgressEstimatorCreated event </summary>
        /// <remarks><inheritdoc cref="OnProgressEstimatorCreated"  path="*"/></remarks>
        protected virtual void RaiseOnProgressEstimatorCreated(ProgressEstimatorCreatedEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            OnProgressEstimatorCreated?.Invoke(this, e);
        }

        /// <summary> Raises the OnProgressEstimatorCreated event </summary>
        /// <remarks><inheritdoc cref="OnProgressEstimatorCreated"  path="*"/></remarks>
        protected virtual void RaiseOnProgressEstimatorCreated(IProgressEstimator estimator)
        {
            if (estimator is null) throw new ArgumentNullException(nameof(estimator));
            OnProgressEstimatorCreated?.Invoke(this, new ProgressEstimatorCreatedEventArgs(estimator));
        }

        #endregion

        #region < TaskFaulted >
        /// <inheritdoc/>
        public event UnhandledExceptionEventHandler TaskFaulted;

        /// <summary> Raises the TaskFaulted event </summary>
        /// <remarks><inheritdoc cref="TaskFaulted"  path="*"/></remarks>
        protected virtual void RaiseOnTaskFaulted(UnhandledExceptionEventArgs e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            TaskFaulted?.Invoke(this, e);
        }

        /// <summary> Raises the TaskFaulted event </summary>
        /// <inheritdoc cref="UnhandledExceptionEventArgs(object, bool)" path="*"/>
        /// <remarks><inheritdoc cref="TaskFaulted"  path="*"/></remarks>
        protected virtual void RaiseOnTaskFaulted(Exception exception, bool isTerminating = false)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));
            TaskFaulted?.Invoke(this, new UnhandledExceptionEventArgs(exception, isTerminating));
        }

        #endregion

        #region < PropertyChanged >

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary> Raises the PropertyChanged event </summary>
        protected virtual void OnPropertyChanged(string propertyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        /// <summary> Raises the PropertyChanged event </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Set the <paramref name="field"/> to the <paramref name="value"/> then raise <see cref="PropertyChanged"/>
        /// </summary>
        /// <returns>TRUE if the property was updated, otherwise false.</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (!field?.Equals(value) ?? true)
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        #endregion

        #endregion

        /// <summary>
        /// The last run's results
        /// </summary>
        protected RoboCopyResults RunResults;

        /// <summary>
        /// The last ListOnly run's results
        /// </summary>
        protected RoboCopyResults ListOnlyResults;

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <inheritdoc/>
        public virtual RoboCopyResults GetResults()
        {
            return RunResults;
        }

        /// <inheritdoc cref="IRoboCommand.GetResults"/>
        public virtual RoboCopyResults GetListOnlyResults()
        {
            return ListOnlyResults;
        }

        /// <summary>
        /// Save the results object to either <see cref="ListOnlyResults"/> or <see cref="RunResults"/> based on <see cref="LoggingOptions.ListOnly"/>
        /// <br/> If <see cref="RunResults"/> is null while saving to <see cref="ListOnlyResults"/>, RunResults will be updated as well.
        /// </summary>
        /// <remarks>
        /// Meant to be called after results are available </remarks>
        /// <param name="results">the results object to save</param>
        protected virtual void SaveResults(RoboCopyResults results)
        {
            if (LoggingOptions.ListOnly)
            {
                ListOnlyResults = results;
                RunResults = results;
            }
            else
                RunResults = results;
        }

        /// <inheritdoc/>
        public virtual void Pause()
        {
            if (IsRunning && !IsCancelled)
                IsPaused = true;
        }

        /// <inheritdoc/>
        public virtual void Resume()
        {
            if (IsRunning && IsPaused)
                IsPaused = false;
        }

        /// <inheritdoc/>
        public abstract Task Start(string domain = "", string username = "", string password = "");

        /// <inheritdoc/>
        public virtual async Task<RoboCopyResults> StartAsync(string domain = "", string username = "", string password = "")
        {
            await Start(domain, username, password);
            return GetResults();
        }

        /// <inheritdoc/>
        public virtual async Task<RoboCopyResults> StartAsync_ListOnly(string domain = "", string username = "", string password = "")
        {
            await Start_ListOnly(domain, username, password);
            return GetListOnlyResults();
        }

        /// <inheritdoc/>
        public virtual async Task Start_ListOnly(string domain = "", string username = "", string password = "")
        {
            bool original = LoggingOptions.ListOnly;
            LoggingOptions.ListOnly = true;
            await Start(domain, username, password);
            LoggingOptions.ListOnly = original;
        }

        /// <inheritdoc/>
        public abstract void Stop();


        /// <summary>
        /// Generate the Parameters and Switches to execute RoboCopy with based on the configured settings
        /// </summary>
        /// <returns>the string of parameters that would normally be passed to a RoboCopy process</returns>
        protected virtual string GenerateParameters()
        {
            string parsedCopyOptions = CopyOptions.ToString();
            string parsedSelectionOptions = SelectionOptions.ToString();
            string parsedRetryOptions = RetryOptions.ToString();
            string parsedLoggingOptions = LoggingOptions.ToString();
            return string.Format("{0}{1}{2}{3} /BYTES", parsedCopyOptions, parsedSelectionOptions, parsedRetryOptions, parsedLoggingOptions);
        }
    }
}
