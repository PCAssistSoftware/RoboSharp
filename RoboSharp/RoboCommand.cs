using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RoboSharp.Interfaces;
using RoboSharp.EventArgObjects;
using System.Text;

namespace RoboSharp
{
    /// <summary>
    /// Wrapper for the RoboCopy process
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/tjscience/RoboSharp/wiki/RoboCommand"/>
    /// </remarks>
    public partial class RoboCommand : IDisposable, IRoboCommand, ICloneable
    {
        /// <summary>
        /// The base <see cref="RoboCommandFactory"/> object provided by the RoboSharp library.
        /// </summary>
        public static RoboCommandFactory Factory { get; } = new RoboCommandFactory();

        #region < Constructors >

        /// <summary>Create a new RoboCommand object</summary>
        public RoboCommand()
        {
            InitClassProperties();
            Init();
        }

        /// <inheritdoc cref="RoboCommand.RoboCommand(string, string, CopyActionFlags, SelectionFlags, LoggingFlags)"/>
        public RoboCommand(string source, string destination, string name, CopyActionFlags copyActionFlags, SelectionFlags selectionFlags = SelectionFlags.Default, LoggingFlags loggingFlags = LoggingFlags.RoboSharpDefault)
            :this(source, destination, copyActionFlags, selectionFlags, loggingFlags)
        {
            Name = name;
        }

        /// <summary>
        /// Create a new RoboCommand object with the provided settings.
        /// </summary>
        /// <inheritdoc cref="Init"/>
        /// <inheritdoc cref="CopyOptions.ApplyActionFlags(CopyActionFlags)"/>
        /// <inheritdoc cref="SelectionOptions.ApplySelectionFlags(SelectionFlags)"/>
        public RoboCommand(string source, string destination, CopyActionFlags copyActionFlags, SelectionFlags selectionFlags = SelectionFlags.Default, LoggingFlags loggingFlags = LoggingFlags.RoboSharpDefault)
        {
            InitClassProperties();
            Init("", true, source, destination);
            this.copyOptions.ApplyActionFlags(copyActionFlags);
            this.selectionOptions.ApplySelectionFlags(selectionFlags);
            this.LoggingOptions.ApplyLoggingFlags(loggingFlags);
        }

        /// <inheritdoc cref="Init"/>
        public RoboCommand(string name) : this(string.Empty, string.Empty, name, stopIfDisposing: true) { }

        /// <inheritdoc cref="Init"/>
        public RoboCommand(string name, bool stopIfDisposing) : this(string.Empty, string.Empty, name, stopIfDisposing) { }

        /// <inheritdoc cref="Init"/>
        public RoboCommand(string source, string destination) : this(source, destination, string.Empty, stopIfDisposing: true) { }

        /// <inheritdoc cref="Init"/>
        public RoboCommand(string source, string destination, bool stopIfDisposing) : this(source, destination, string.Empty, stopIfDisposing) { }

        /// <inheritdoc cref="Init"/>
        public RoboCommand(string source, string destination, string name) : this(source, destination, name, StopIfDisposing: true) { }

        /// <inheritdoc cref="Init"/>
        public RoboCommand(string source, string destination, string name, bool stopIfDisposing)
        {
            InitClassProperties();
            Init(name, stopIfDisposing, source, destination);
        }

        /// <remarks> Each of the Options objects can be specified within this constructor. If left = null, a new object will be generated using the default options for that object. </remarks>
        /// <inheritdoc cref="Init"/>
        public RoboCommand(string name, string source = null, string destination = null, bool StopIfDisposing = true, RoboSharpConfiguration configuration = null, CopyOptions copyOptions = null, SelectionOptions selectionOptions = null, RetryOptions retryOptions = null, LoggingOptions loggingOptions = null, JobOptions jobOptions = null)
        {
            this.configuration = configuration ?? new RoboSharpConfiguration();
            this.copyOptions = copyOptions ?? new CopyOptions();
            this.selectionOptions = selectionOptions ?? new SelectionOptions();
            this.retryOptions = retryOptions ?? new RetryOptions();
            this.loggingOptions = loggingOptions ?? new LoggingOptions();
            this.jobOptions = jobOptions ?? new JobOptions();
            Init(name, StopIfDisposing, source ?? CopyOptions.Source, destination ?? CopyOptions.Destination);
        }

        /// <summary>
        /// Create a new RoboCommand with identical options at this RoboCommand
        /// </summary>
        /// <remarks>
        /// If Desired, the new RoboCommand object will share some of the same Property objects as the input <paramref name="command"/>. 
        /// For Example, that means that if a SelectionOption property changes, it will affect both RoboCommand objects since the  <see cref="SelectionOptions"/> property is shared between them. <br/>
        /// If the Link* options are set to FALSE (default), then it will create new property objects whose settings match the current settings of <paramref name="command"/>.
        /// <para/> Properties that can be linked: <br/>
        /// <see cref="Configuration"/> ( Linked by default ) <br/>
        /// <see cref="RetryOptions"/> ( Linked by default )<br/>
        /// <see cref="SelectionOptions"/><br/>
        /// <see cref="LoggingOptions"/><br/>
        /// <see cref="JobOptions"/><br/>
        /// </remarks>
        /// <param name="command">RoboCommand to Clone</param>
        /// <param name="NewSource">Specify a new source if desired. If left as null, will use Source from <paramref name="command"/></param>
        /// <param name="NewDestination">Specify a new source if desired. If left as null, will use Destination from <paramref name="command"/></param>
        /// <param name="LinkConfiguration">Link the <see cref="Configuration"/> of the two commands ( True Default )</param>
        /// <param name="LinkLoggingOptions">Link the <see cref="LoggingOptions"/> of the two commands</param>
        /// <param name="LinkRetryOptions">Link the <see cref="RetryOptions"/> of the two commands ( True Default )</param>
        /// <param name="LinkSelectionOptions">Link the <see cref="SelectionOptions"/> of the two commands</param>
        /// <param name="LinkJobOptions">Link the <see cref="SelectionOptions"/> of the two commands</param>
        public RoboCommand(IRoboCommand command, string NewSource = null, string NewDestination = null, bool LinkConfiguration = true, bool LinkRetryOptions = true, bool LinkSelectionOptions = false, bool LinkLoggingOptions = false, bool LinkJobOptions = false)
        {
            Name = command.Name;
            StopIfDisposing = command.StopIfDisposing;

            configuration = LinkConfiguration ? command.Configuration : command.Configuration.Clone();
            CopyOptions = new CopyOptions(command.CopyOptions, NewSource, NewDestination);
            LoggingOptions = LinkLoggingOptions ? command.LoggingOptions : command.LoggingOptions.Clone();
            RetryOptions = LinkRetryOptions ? command.RetryOptions : command.RetryOptions.Clone();
            SelectionOptions = LinkSelectionOptions ? command.SelectionOptions : command.SelectionOptions.Clone();
            try { JobOptions = LinkJobOptions ? command.JobOptions : command.JobOptions.Clone(); } catch(NotImplementedException) { JobOptions = new JobOptions(); }
        }

        /// <summary>Create a new RoboCommand object</summary>
        /// <param name="name"><inheritdoc cref="Name" path="*"/></param>
        /// <param name="stopIfDisposing"><inheritdoc cref="StopIfDisposing" path="*"/></param>
        /// <param name="source"><inheritdoc cref="RoboSharp.CopyOptions.Source" path="*"/></param>
        /// <param name="destination"><inheritdoc cref="RoboSharp.CopyOptions.Destination" path="*"/></param>
        private void Init(string name = "", bool stopIfDisposing = true, string source = "", string destination = "")
        {
            Name = name;
            StopIfDisposing = stopIfDisposing;
            CopyOptions.Source = source;
            CopyOptions.Destination = destination;
        }

        private void InitClassProperties()
        {
            copyOptions = new CopyOptions();
            selectionOptions = new SelectionOptions();
            retryOptions = new RetryOptions();
            loggingOptions = new LoggingOptions();
            configuration = new RoboSharpConfiguration();
            jobOptions = new JobOptions();
        }

        /// <inheritdoc cref="RoboCommand.RoboCommand(IRoboCommand, string, string, bool, bool, bool, bool, bool)"/>
        public RoboCommand Clone(string NewSource = null, string NewDestination = null, bool LinkConfiguration = true, bool LinkRetryOptions = true, bool LinkSelectionOptions = false, bool LinkLoggingOptions = false, bool LinkJobOptions = false)
            => new RoboCommand(this, NewSource, NewDestination, LinkConfiguration, LinkRetryOptions, LinkSelectionOptions, LinkLoggingOptions, LinkJobOptions);

        object ICloneable.Clone() => new RoboCommand(this, null, null, false, false, false, false, false);

        #endregion

        #region < Private Vars >

        private static char[] _OutputDataSplitter; // Static array storage used for processing output data from robocopy - created on first run

        // set up in Constructor
        private CopyOptions copyOptions;
        private SelectionOptions selectionOptions;
        private RetryOptions retryOptions;
        private LoggingOptions loggingOptions;
        private RoboSharpConfiguration configuration;
        private JobOptions jobOptions;

        // Modified while running
        private Process _process;
        private Task backupTask;
        private bool hasError;
        //private bool hasExited; //No longer evaluated
        private bool isPaused;
        private bool isRunning;
        private bool isCancelled;
        private bool isScheduled;
        private Results.ResultsBuilder _resultsBuilder;

        /// <summary>
        /// The last set of results that were created
        /// </summary>
        protected Results.RoboCopyResults results;

        /// <summary> 
        /// Stores the LastData processed by <see cref="Process_OutputDataReceived(object, DataReceivedEventArgs)"/> 
        /// </summary>
        private string _lastDataReceived;
        private Match _lastErrorRegexMatch;

        #endregion Private Vars

        #region < Public Vars >

        /// <summary> ID Tag for the job - Allows consumers to find/sort/remove/etc commands within a list via string comparison</summary>
        public string Name { get; set; }
        /// <summary> Value indicating if process is currently paused </summary>
        public bool IsPaused { get { return isPaused; } }
        /// <summary> Value indicating if process is currently running </summary>
        public bool IsRunning { get { return isRunning; } }
        /// <summary> Value indicating if process was Cancelled </summary>
        public bool IsCancelled { get { return isCancelled; } }
        /// <summary> TRUE if <see cref="CopyOptions.RunHours"/> is set up (Copy Operation is scheduled to only operate within specified timeframe). Otherwise False. </summary>
        public bool IsScheduled { get => isScheduled; }
        /// <summary> Get the parameters string passed to RoboCopy based on the current setup </summary>
        public string CommandOptions { get { return GenerateParameters(); } }
        /// <inheritdoc cref="RoboSharp.CopyOptions"/>
        public CopyOptions CopyOptions
        {
            get { return copyOptions; }
            set { copyOptions = value ?? copyOptions; }
        }
        /// <inheritdoc cref="RoboSharp.SelectionOptions"/>
        public SelectionOptions SelectionOptions
        {
            get { return selectionOptions; }
            set { selectionOptions = value ?? selectionOptions; }
        }
        /// <inheritdoc cref="RoboSharp.RetryOptions"/>
        public RetryOptions RetryOptions
        {
            get { return retryOptions; }
            set { retryOptions = value ?? retryOptions; }
        }
        /// <inheritdoc cref="RoboSharp.LoggingOptions"/>
        public LoggingOptions LoggingOptions
        {
            get { return loggingOptions; }
            set { loggingOptions = value ?? loggingOptions; }
        }
        /// <inheritdoc cref="RoboSharp.JobOptions"/>
        public JobOptions JobOptions
        {
            get { return jobOptions; }
            set { jobOptions = value ?? jobOptions; }
        }
        /// <inheritdoc cref="RoboSharp.RoboSharpConfiguration"/>
        public RoboSharpConfiguration Configuration
        {
            get { return configuration; }
            set
            {
                if (value is null) throw new ArgumentNullException("Can not set Configuration to null!");
                configuration = value;
            }
        }
        /// <inheritdoc cref="Results.ProgressEstimator"/>
        /// <remarks>
        /// A new <see cref="Results.ProgressEstimator"/> object is created every time the <see cref="Start"/> method is called, but will not be created until called for the first time. 
        /// </remarks>
        protected internal Results.ProgressEstimator ProgressEstimator { get; protected set; }

        /// <inheritdoc cref="RoboCommand.ProgressEstimator"/>
        public IProgressEstimator IProgressEstimator => this.ProgressEstimator;

        /// <summary>
        /// Value indicating if the process should be killed when the <see cref="Dispose()"/> method is called.<br/>
        /// For example, if the RoboCopy process should exit when the program exits, this should be set to TRUE (default).
        /// </summary>
        public bool StopIfDisposing { get; set; } = true;

        #endregion Public Vars

        #region < Events >

        /// <summary>Handles <see cref="OnFileProcessed"/></summary>
        public delegate void FileProcessedHandler(IRoboCommand sender, FileProcessedEventArgs e);
        /// <summary>Occurs each time a new item has started processing</summary>
        public event FileProcessedHandler OnFileProcessed;

        /// <summary> Raises the OnFileProcessed event </summary>
        protected virtual void RaiseOnFileProcessed(FileProcessedEventArgs e)
        {
            OnFileProcessed?.Invoke(this, e);
        }

        /// <summary>Handles <see cref="OnCommandError"/></summary>
        public delegate void CommandErrorHandler(IRoboCommand sender, CommandErrorEventArgs e);
        /// <summary>Occurs when an error occurs while generating the command that prevents the RoboCopy process from starting.</summary>
        public event CommandErrorHandler OnCommandError;

        /// <summary> Raises the OnError event </summary>
        protected virtual void RaiseOnCommandError(CommandErrorEventArgs e)
        {
            OnCommandError?.Invoke(this, e);
        }

        /// <summary>Handles <see cref="OnError"/></summary>
        public delegate void ErrorHandler(IRoboCommand sender, ErrorEventArgs e);
        /// <summary>Occurs an error is detected by RoboCopy </summary>
        public event ErrorHandler OnError;

        /// <summary> Raises the OnError event </summary>
        protected virtual void RaiseOnError(ErrorEventArgs e)
        {
            OnError?.Invoke(this, e);
        }

        /// <summary>Handles <see cref="OnCommandCompleted"/></summary>
        public delegate void CommandCompletedHandler(IRoboCommand sender, RoboCommandCompletedEventArgs e);
        /// <summary>Occurs when the RoboCopy process has finished executing and results are available.</summary>
        public event CommandCompletedHandler OnCommandCompleted;

        /// <summary> Raises the OnCommandCompleted event </summary>
        protected virtual void RaiseCommandCompleted(RoboCommandCompletedEventArgs e)
        {
            OnCommandCompleted?.Invoke(this, e);
        }

        /// <summary>Handles <see cref="OnCopyProgressChanged"/></summary>
        public delegate void CopyProgressHandler(IRoboCommand sender, CopyProgressEventArgs e);
        /// <summary>Occurs each time the current item's progress is updated</summary>
        public event CopyProgressHandler OnCopyProgressChanged;


        /// <summary> Raises the OnCopyProgressChanged event </summary>
        protected virtual void RaiseCopyProgressChanged(CopyProgressEventArgs e)
        {
            OnCopyProgressChanged?.Invoke(this, e);
        }


        /// <summary>Handles <see cref="OnProgressEstimatorCreated"/></summary>
        public delegate void ProgressUpdaterCreatedHandler(IRoboCommand sender, ProgressEstimatorCreatedEventArgs e);
        /// <summary>
        /// Occurs when a <see cref="Results.ProgressEstimator"/> is created during <see cref="Start"/>, allowing binding to occur within the event subscriber. <br/>
        /// This event will occur once per Start.
        /// </summary>
        public event ProgressUpdaterCreatedHandler OnProgressEstimatorCreated;

        /// <summary> Raises the OnProgressEstimatorCreated event </summary>
        protected virtual void RaiseProgressEstimatorCreated(ProgressEstimatorCreatedEventArgs e)
        {
            OnProgressEstimatorCreated?.Invoke(this, e);
        }


        /// <summary>
        /// Occurs if the RoboCommand task is stopped due to an unhandled exception. Occurs instead of <see cref="OnCommandCompleted"/>
        /// </summary>
        public event UnhandledExceptionEventHandler TaskFaulted;

        /// <summary> Raises the OnProgressEstimatorCreated event </summary>
        protected virtual void RaiseTaskFaulted(UnhandledExceptionEventArgs e)
        {
            TaskFaulted?.Invoke(this, e);
        }

        #endregion

        #region < Pause / Stop / Resume >

        /// <summary>Pause execution of the RoboCopy process when <see cref="IsPaused"/> == false</summary>
        public virtual void Pause()
        {
            if (_process != null && !_process.HasExited && isPaused == false)
            {
                Debugger.Instance.DebugMessage("RoboCommand execution paused.");
                isPaused = _process.Suspend();
            }
        }

        /// <summary>Resume execution of the RoboCopy process when <see cref="IsPaused"/> == true</summary>
        public virtual void Resume()
        {
            if (_process != null && !_process.HasExited && isPaused == true)
            {
                Debugger.Instance.DebugMessage("RoboCommand execution resumed.");
                _process.Resume();
                isPaused = false;
            }
        }

        /// <summary> Immediately Kill the RoboCopy process</summary>
        public virtual void Stop() => Stop(false);

        private void Stop(bool DisposeProcess)
        {
            //Note: This previously checked for CopyOptions.RunHours.IsNullOrWhiteSpace() == TRUE prior to issuing the stop command
            //If the removal of that check broke your application, please create a new issue thread on the repo.
            if (_process != null)
            {
                if (!isCancelled && (!_process?.HasExited ?? true))
                {
                    _process?.Kill();
                    isCancelled = true;
                }
                if (DisposeProcess)
                {
                    _process?.Dispose();
                    _process = null;
                }
            }
            isPaused = false;
        }

        #endregion

        #region < Start Methods >

        /// <summary>
        /// awaits <see cref="Start(string, string, string)"/> then returns the results.
        /// </summary>
        /// <returns>Returns the RoboCopy results once RoboCopy has finished executing.</returns>        
        /// <inheritdoc cref="Start(string, string, string)"/>
        public virtual async Task<Results.RoboCopyResults> StartAsync(string domain = "", string username = "", string password = "")
        {
            await Start(domain, username, password);
            return GetResults();
        }

        /// <summary>awaits <see cref="Start_ListOnly(string, string, string)"/> then returns the results.</summary>
        /// <returns>Returns the List-Only results once RoboCopy has finished executing.</returns>
        /// <inheritdoc cref="Start_ListOnly(string, string, string)"/>
        public virtual async Task<Results.RoboCopyResults> StartAsync_ListOnly(string domain = "", string username = "", string password = "")
        {
            await Start_ListOnly(domain, username, password);
            return GetResults();
        }

        /// <summary>
        /// Run the currently selected options in ListOnly mode by setting <see cref="LoggingOptions.ListOnly"/> = TRUE
        /// </summary>
        /// <returns>Task that awaits <see cref="Start(string, string, string)"/>, then resets the ListOnly option to original value.</returns>
        /// <inheritdoc cref="Start(string, string, string)"/>
        public virtual async Task Start_ListOnly(string domain = "", string username = "", string password = "")
        {
            bool _listOnly = LoggingOptions.ListOnly;
            LoggingOptions.ListOnly = true;
            await Start(domain, username, password);
            LoggingOptions.ListOnly = _listOnly;
            return;
        }

        /// <summary>
        /// Start the RoboCopy Process. 
        /// </summary>
        /// <remarks>
        /// If overridden by a derived class, the override affects all Start* methods within RoboCommand. Base.Start() must be called to start the robocopy process.
        /// </remarks>
        /// <returns>Returns a task that reports when the RoboCopy process has finished executing.</returns>
        /// <exception cref="InvalidOperationException"/>
        /// <inheritdoc cref="Authentication.AuthenticateSourceAndDestination"/>
        public virtual Task Start(string domain = "", string username = "", string password = "")
        {
            VersionManager.ThrowIfNotWindowsPlatform("RoboCommand.Start(), which invokes robocopy, is only available in a windows environment. Use a custom implementation instead.");
            if (disposedValue) throw GetDisposedException();
            if (_process != null | IsRunning) throw new InvalidOperationException("RoboCommand.Start() method cannot be called while process is already running / IsRunning = true.");

#if !NETFRAMEWORK && !NET6_0_OR_GREATER
            if (!username.IsNullOrWhiteSpace()) throw new PlatformNotSupportedException("Authentication is only supported in .Net Framework >= 4.0 and .Net >= 6.0.");
#endif

            Debugger.Instance.DebugMessage("RoboCommand started execution.");
            hasError = false;
            isCancelled = false;
            isPaused = false;
            isRunning = true;
            isScheduled = !String.IsNullOrWhiteSpace(CopyOptions?.RunHours) && CopyOptions.IsRunHoursStringValid(CopyOptions.RunHours);
            results = null;

            //Check Source and Destination
            var authResult = Authentication.AuthenticateSourceAndDestination(this, domain, username, password);

            if (!authResult.Success)
            {
                hasError = true;
                OnCommandError?.Invoke(this, authResult.CommandErrorArgs);
                isRunning = false;
                Debugger.Instance.DebugMessage("RoboCommand execution stopped due to error.");
                return Task.Delay(5);
            }
            else
            {
                //Raise EstimatorCreatedEvent to alert consumers that the Estimator can now be bound to
                ProgressEstimator = new Results.ProgressEstimator(this);
                _resultsBuilder = new Results.ResultsBuilder(this, ProgressEstimator);
                OnProgressEstimatorCreated?.Invoke(this, new ProgressEstimatorCreatedEventArgs(ProgressEstimator));
                return GetRoboCopyTask(_resultsBuilder, domain, username, password);
            }
        }

        /// <summary>
        /// Start the RoboCopy process and the watcher task
        /// </summary>
        /// <returns>The continuation task that cleans up after the task that watches RoboCopy has finished executing.</returns>
        /// <exception cref="InvalidOperationException"/>
        private Task GetRoboCopyTask(Results.ResultsBuilder resultsBuilder, string domain = "", string username = "", string password = "")
        {
            if (disposedValue) throw GetDisposedException();
            if (_process != null) throw new InvalidOperationException("Cannot start a new RoboCopy Process while this RoboCommand is already running.");

            isRunning = true;
            DateTime StartTime = DateTime.Now;


            Process process = new Process();
            _process = process;
            var processExitedSource = new TaskCompletionSource<object>();

            backupTask = Task.Run(async () =>
          {
              //Declare Encoding
              process.StartInfo.StandardOutputEncoding = Configuration.StandardOutputEncoding;
              process.StartInfo.StandardErrorEncoding = Configuration.StandardErrorEncoding;

              if (!string.IsNullOrEmpty(domain))
              {
                  Debugger.Instance.DebugMessage(string.Format("RoboCommand running under domain - {0}", domain));
                  process.StartInfo.Domain = domain;
              }

              if (!string.IsNullOrEmpty(username))
              {
                  Debugger.Instance.DebugMessage(string.Format("RoboCommand running under username - {0}", username));
                  process.StartInfo.UserName = username;
              }

              if (!string.IsNullOrEmpty(password))
              {
                  Debugger.Instance.DebugMessage("RoboCommand password entered.");
                  var ssPwd = new System.Security.SecureString();

                  for (int x = 0; x < password.Length; x++)
                  {
                      ssPwd.AppendChar(password[x]);
                  }

                  process.StartInfo.Password = ssPwd;
              }

              Debugger.Instance.DebugMessage("Setting RoboCopy process up...");
              process.StartInfo.UseShellExecute = false;
              process.StartInfo.RedirectStandardOutput = true;
              process.StartInfo.RedirectStandardError = true;
              process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
              process.StartInfo.CreateNoWindow = true;
              process.StartInfo.FileName = Configuration.RoboCopyExe;
              if (resultsBuilder != null)
              {
                  resultsBuilder.Source = CopyOptions.Source;
                  resultsBuilder.Destination = CopyOptions.Destination;
                  resultsBuilder.CommandOptions = GenerateParameters();
              }
              process.StartInfo.Arguments = resultsBuilder?.CommandOptions ?? GenerateParameters();
              process.EnableRaisingEvents = true;
              process.OutputDataReceived += Process_OutputDataReceived;
              process.ErrorDataReceived += Process_ErrorDataReceived;
              process.Exited += ProcessExitedHandler;

              //Start the Task
              Debugger.Instance.DebugMessage("RoboCopy process started.");
              process.Start();
              process.BeginOutputReadLine();
              process.BeginErrorReadLine();
              _ = await processExitedSource.Task;   //This allows task to release the thread to perform other work
              if (resultsBuilder != null)      // Only replace results if a ResultsBuilder was supplied (Not supplied when saving as a JobFile)
              {
                  results = resultsBuilder.BuildResults(process?.ExitCode ?? -1);
              }
              Debugger.Instance.DebugMessage("RoboCopy process exited.");
          }, CancellationToken.None);

            Task continueWithTask = backupTask.ContinueWith((continuation) => // this task always runs
            {
                // unsubscribe from the process
                process.Exited -= ProcessExitedHandler;
                process.OutputDataReceived -= Process_OutputDataReceived;
                process.ErrorDataReceived -= Process_ErrorDataReceived;
                processExitedSource = null;
                
                bool WasCancelled = process.ExitCode == -1;
                Stop(true); //Ensure process is disposed of - Sets IsRunning flags to false

                //Run Post-Processing of the Generated JobFile if one was created.
                JobOptions.RunPostProcessing(this);

                _resultsBuilder = null; // Clear the object reference to the ResultsBuilder as its no longer needed
                isScheduled = false;
                isRunning = false; //Now that all processing is complete, IsRunning should be reported as false.

                if (continuation.IsFaulted && !WasCancelled) // If some fault occurred while processing, throw the exception to caller
                {
                    TaskFaulted?.Invoke(this, new UnhandledExceptionEventArgs(continuation.Exception, true));
                    throw continuation.Exception;
                }
                //Raise event announcing results are available
                if (!hasError && resultsBuilder != null)
                {
                    results.StartTime = StartTime;
                    results.EndTime = DateTime.Now;
                    results.TimeSpan = results.EndTime.Subtract(results.StartTime);
                    OnCommandCompleted?.Invoke(this, new RoboCommandCompletedEventArgs(results));
                }
            }, CancellationToken.None);


            void ProcessExitedHandler(object sender, EventArgs e)
            {
                try
                {
                    // WaitForExit ensures that all output lines have been sent by the process, whereas the Exited event occurs prior to lines finishing being printed
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Debugger.Instance.DebugMessage(ex.Message);
                }
                finally
                {
                    processExitedSource.TrySetResult(null);
                }
            }
            return continueWithTask;
        }

        /// <summary>
        /// Save this RoboCommand's options to a new RoboCopyJob ( *.RCJ ) file. <br/>
        /// Note: This will not save the path submitted into <see cref="JobOptions.FilePath"/>.
        /// </summary>
        /// <remarks>
        /// Job Files don't care if the Source/Destination are invalid, since they just save the command values to a file.
        /// </remarks>
        /// <param name="path"><inheritdoc cref="JobOptions.FilePath"/></param>
        /// <param name="IncludeSource">Save <see cref="CopyOptions.Source"/> into the RCJ file.</param>
        /// <param name="IncludeDestination">Save <see cref="CopyOptions.Destination"/> into the RCJ file.</param>
        /// <inheritdoc cref = "Start(string, string, string)" />
        /// <param name="domain"/><param name="password"/><param name="username"/>
        public async Task SaveAsJobFile(string path, bool IncludeSource = false, bool IncludeDestination = false, string domain = "", string username = "", string password = "")
        {
            //If currently running and this is called, clone the command, then run the save method against the clone.
            if (disposedValue | _process != null)
            {
                var cmd = this.Clone();
                cmd.StopIfDisposing = true;
                try
                {
                    await cmd.SaveAsJobFile(path, IncludeSource, IncludeDestination, domain, username, password);
                }
                catch (Exception Fault)
                {
                    cmd.Dispose();
                    throw new Exception("Failed to save the job file, see inner exception", Fault);
                }
                cmd.Dispose();
                return;
            }

            if (disposedValue) throw GetDisposedException();
            bool _QUIT = JobOptions.PreventCopyOperation;
            string _PATH = JobOptions.FilePath;
            string _SOURCE = CopyOptions.Source;
            string _DEST = CopyOptions.Destination;

            JobOptions.FilePath = path;
            JobOptions.PreventCopyOperation = true;
            CopyOptions.Source = IncludeSource ? _SOURCE : string.Empty;
            CopyOptions.Destination = IncludeDestination ? _DEST : string.Empty;
            try
            {
                var AuthResult = Authentication.AuthenticateJobFileSavePath(this, domain, username, password);
                if (!AuthResult.Success)
                {
                    RaiseOnCommandError(AuthResult.CommandErrorArgs);
                    return;
                }
                await GetRoboCopyTask(null, domain, username, password); //This should take approximately 1-2 seconds at most
            }
            finally
            {
                //Restore Original Settings
                JobOptions.FilePath = _PATH;
                JobOptions.PreventCopyOperation = _QUIT;
                CopyOptions.Source = _SOURCE;
                CopyOptions.Destination = _DEST;
            }
        }

        /// <inheritdoc cref="JobFileBuilder.Parse(string)"/>
        public static RoboCommand LoadFromJobFile(string filePath)
        {
            return JobFileBuilder.Parse(filePath);
        }

        /// <inheritdoc cref="JobFileBuilder.Parse(string)"/>
        public static bool TryLoadFromJobFile(string filePath, out RoboCommand cmd)
        {
            try
            {
                cmd = JobFileBuilder.Parse(filePath);
                return true;
            }
            catch
            {
                cmd = null;
                return false;

            }
            
        }

        #endregion

        #region < Process Event Handlers >

        /// <summary> Occurs when the Process reports an error prior to starting the robocopy process, not an 'error' from Robocopy </summary>
        void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (OnCommandError != null && !e.Data.IsNullOrWhiteSpace())
            {
                hasError = true;
                OnCommandError(this, new CommandErrorEventArgs(e.Data, null));
            }
        }

        //lang=regex
        private const string _outputProgressDataPattern = "^[0-9]+[.,]?[0-9]*%";
        //lang=regex -- Regex to parse the string for FileCount, Path, and Type (Description)
        private const string _outputDirectoryDataPattern = "^(?<Type>\\*?[a-zA-Z]{0,10}\\s?[a-zA-Z]{0,3})\\s*(?<FileCount>[-]{0,1}[0-9]{1,100})\\t(?<Path>.+)";

#if NET7_0_OR_GREATER
        [GeneratedRegex(_outputProgressDataPattern, RegexOptions.None, 1000)]
        internal static partial Regex Process_OutputProgressDataRegex();

        [GeneratedRegex(_outputDirectoryDataPattern, RegexOptions.ExplicitCapture, 1000)]
        internal static partial Regex Process_OutputDirectoryDataRegex();
#else
        private static Regex _OutputDataRegex_Progress;
        private static Regex _OutputDataRegex_Directory;
        internal static Regex Process_OutputProgressDataRegex() => _OutputDataRegex_Progress ??= new Regex(_outputProgressDataPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
        internal static Regex Process_OutputDirectoryDataRegex() => _OutputDataRegex_Directory ??= new Regex(_outputDirectoryDataPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));
#endif

        /// <summary> React to Process.StandardOutput </summary>
        void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return; // Nothing to do
            string data = e.Data.Replace("\0", string.Empty).Trim(); // remove null characters (is this needed?), then trim (required)

            if (string.IsNullOrWhiteSpace(data))
            {
                //adds the whitespace to the results builder. and report it as a system message
                _resultsBuilder?.AddOutput(e.Data);
                OnFileProcessed?.Invoke(this, FileProcessedEventArgs.WhiteSpaceLogLine);
                return;  
            }

            if (data == _lastDataReceived) return;   // Sometimes RoboCopy reports same item multiple times - Typically for Progress indicators
            _lastDataReceived = data;

            if (Process_OutputProgressDataRegex().IsMatch(data))
            {
                var currentFile = ProgressEstimator.CurrentFile;
                var currentDir = ProgressEstimator?.CurrentDir;

                //Increment ProgressEstimator
                if (data.Equals("100%"))
                    ProgressEstimator?.AddFileCopied(currentFile);
                else
                    ProgressEstimator?.SetCopyOpStarted();

                // copy progress data -> Use the CurrentFile and CurrentDir from the ResultsBuilder
                OnCopyProgressChanged?.Invoke(this,
                    new CopyProgressEventArgs(
                        Convert.ToDouble(data.Substring(0, data.IndexOf('%')), CultureInfo.InvariantCulture),
                        currentFile, currentDir
                    ));

            }
            else
            {
                // add the untrimmed data to the results builder
                string lastData = _resultsBuilder?.LastLine ?? "";
                _resultsBuilder?.AddOutput(e.Data); 

                //Parse the string to determine which event to raise
                var splitData = data.Split(_OutputDataSplitter ??= new char[]{ '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (splitData.Length == 2) // Directory
                {
                    var file = new ProcessedFileInfo
                    {
                        FileClassType = FileClassType.NewDir
                    };

                    Match dirMatch = Process_OutputDirectoryDataRegex().Match(data);
                    if (dirMatch.Success)
                    {
                        //New Method - Parsed using Regex
                        GroupCollection MatchData = dirMatch.Groups;
                        file.FileClass = MatchData["Type"].Value.Trim();
                        if (file.FileClass == "") file.FileClass = configuration.LogParsing_ExistingDir;
                        file.Size = long.TryParse(MatchData["FileCount"].Value, out long size) ? size : 0;
                        file.Name = MatchData["Path"].Value.Trim();
                    }
                    else
                    {
                        //Old Method -> Left Intact for other language compatibilty / unforseen cases
                        file.FileClass = "New Dir";
                        file.Size = long.TryParse(splitData[0].Replace("New Dir", "").Trim(), out long size) ? size : 0;
                        file.Name = splitData[1];
                    }

                    ProgressEstimator?.AddDir(file);
                    OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(file));
                }
                else if (splitData.Length == 3) // File
                {
                    var file = new ProcessedFileInfo
                    {
                        FileClass = splitData[0].Trim(),
                        FileClassType = FileClassType.File,
                        Size = long.TryParse(splitData[1], out long size) ? size : 0,
                        Name = splitData[2]
                    };
                    ProgressEstimator?.AddFile(file);
                    OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(file));
                }
                else if (_lastErrorRegexMatch?.Success ?? false) // Error Message - Uses previous data instead since RoboCopy reports errors onto line 1, then description onto line 2.
                {
                    ErrorEventArgs args = new ErrorEventArgs(lastData, data, _lastErrorRegexMatch);
                    _lastErrorRegexMatch = null;
                    _resultsBuilder.AddErrorOutput(lastData, data);
                    _resultsBuilder.RoboCopyErrors.Add(args);
                    OnError?.Invoke(this, args);
                }
                else if ((_lastErrorRegexMatch = Configuration.ErrorTokenRegex.Match(data)).Success) // Error Message - Mark the current file as FAILED immediately - Don't raise OnError event until error description comes in though
                {
                    /* 
                     * Mark the current file as Failed
                     * TODO: This data may have to be parsed to determine if it involved the current file's filename, or some other error. At time of writing, it appears that it doesn't require this check.
                     * */
                    ProgressEstimator.FileFailed = true;
                    return;
                }
                else if (!data.StartsWith("----------")) // System Message
                {
                    // Do not log errors that have already been logged
                    var errorCode = ApplicationConstants.ErrorCodes.FirstOrDefault(x => data == x.Value);
                    if (errorCode.Key == null)
                    {
                        OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(e.Data));
                    }
                }
                else
                {
                    OnFileProcessed?.Invoke(this, new FileProcessedEventArgs(e.Data)); // report all misc. lines as system messages
                }
            }
        }

#endregion

        #region < Other Public Methods >

        /// <inheritdoc cref="Results.RoboCopyResults"/>
        /// <returns>The RoboCopyResults object from the last run</returns>
        public Results.RoboCopyResults GetResults()
        {
            return results;
        }

        /// <summary>
        /// Set the results to null - This is to prevent adding results from a previous run being added to the results list by RoboQueue
        /// </summary>
        internal void ResetResults()
        {
            results = null;
        }

        /// <summary>
        /// Generate the Parameters and Switches to execute RoboCopy with based on the configured settings
        /// </summary>
        /// <returns></returns>
        private string GenerateParameters()
        {
            Debugger.Instance.DebugMessage("Generating parameters...");
            Debugger.Instance.DebugMessage(CopyOptions);
            var parsedCopyOptions = CopyOptions.Parse();
            Debugger.Instance.DebugMessage("CopyOptions parsed.");
            var parsedSelectionOptions = SelectionOptions.Parse();
            Debugger.Instance.DebugMessage("SelectionOptions parsed.");
            var parsedRetryOptions = RetryOptions.Parse();
            Debugger.Instance.DebugMessage("RetryOptions parsed.");
            var parsedLoggingOptions = LoggingOptions.Parse();
            Debugger.Instance.DebugMessage("LoggingOptions parsed.");
            var parsedJobOptions = JobOptions.Parse();
            Debugger.Instance.DebugMessage("JobOptions parsed.");
            //var systemOptions = " /V /R:0 /FP /BYTES /W:0 /NJH /NJS";

            return string.Format("{0}{1}{2}{3}{4}", parsedCopyOptions, parsedSelectionOptions,
                parsedRetryOptions, parsedLoggingOptions, parsedJobOptions);
        }

        /// <inheritdoc cref="GenerateParameters"/>
        public override string ToString()
        {
            return GenerateParameters();
        }

        /// <summary>
        /// Combine this object's options with that of some JobFile
        /// </summary>
        /// <param name="jobFile"></param>
        public void MergeJobFile(JobFile jobFile)
        {
            Name = Name.ReplaceIfEmpty(jobFile.Name);
            copyOptions.Merge(jobFile.CopyOptions);
            LoggingOptions.Merge(jobFile.LoggingOptions);
            RetryOptions.Merge(jobFile.RetryOptions);
            SelectionOptions.Merge(jobFile.SelectionOptions);
            JobOptions.Merge(((IRoboCommand)jobFile).JobOptions);
            //this.StopIfDisposing |= ((IRoboCommand)jobFile).StopIfDisposing;
        }

        #endregion

        #region < IDisposable Implementation >

        private bool disposedValue;

        /// <summary>Dispose of this object. Kills RoboCopy process if <see cref="StopIfDisposing"/> == true &amp;&amp; <see cref="IsScheduled"/> == false. </summary>
        /// <remarks><inheritdoc cref="IDisposable.Dispose" path="/summary"/></remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>  Finalizer -> Cleans up the process hooks during finalization </summary>
        ~RoboCommand()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>IDisposable Implementation</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects) - None
                }

                // Check if a running process should be killed
                if (StopIfDisposing && !IsScheduled)
                {
                    Stop(true);
                }

                //Release any hooks to the process, but allow it to continue running
                _process?.Dispose();
                _process = null;
                disposedValue = true;
            }
        }

        private ObjectDisposedException GetDisposedException() => new ObjectDisposedException(Name, "Cannot Start a disposed RoboCommand");

        #endregion IDisposable Implementation
    }
}

