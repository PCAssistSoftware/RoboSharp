using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using RoboSharp.EventArgObjects;
using RoboSharp.Interfaces;
using RoboSharp.Results;

namespace RoboSharp
{
    /// <summary>
    /// Contains a private List{IRoboCommand} object with controlled methods for access to it.  <br/>
    /// Attempting to modify the list while <see cref="IsRunning"/> = true results in <see cref="ListAccessDeniedException"/> being thrown.
    /// <para/>Implements the following: <br/>
    /// <see cref="IRoboQueue"/> <br/>
    /// <see cref="IEnumerable"/> -- Allow enumerating through the collection that is stored in a private list -- Also see <see cref="Commands"/> <br/>
    /// <see cref="INotifyCollectionChanged"/> -- Allow subscription to collection changes against the list <see cref="ObservableList{T}"/> <br/>
    /// <see cref="INotifyPropertyChanged"/> -- Most properties will trigger <see cref="PropertyChanged"/> events when updated.<br/>
    /// <see cref="IDisposable"/> -- Allow disposal of all <see cref="IRoboCommand"/> objects in the list.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/tjscience/RoboSharp/wiki/RoboQueue"/>
    /// </remarks>
    public sealed class RoboQueue : IRoboQueue, IDisposable, INotifyPropertyChanged, IEnumerable<IRoboCommand>, INotifyCollectionChanged
    {
        #region < Constructors >

        /// <summary>
        /// Initialize a new (empty) <see cref="RoboQueue"/> object.
        /// </summary>
        public RoboQueue()
        {
            Init();
            Commands = new ReadOnlyCollection<IRoboCommand>(CommandList);
        }

        /// <summary>
        /// Initialize a new (empty) <see cref="RoboQueue"/> object with a specificed Name.
        /// </summary>
        /// <inheritdoc cref="RoboQueue(IEnumerable{IRoboCommand}, string, int)"/>
        public RoboQueue(string name, int maxConcurrentJobs = 1)
        {
            Init(name, maxConcurrentJobs);
            Commands = new ReadOnlyCollection<IRoboCommand>(CommandList);
        }

        /// <summary>
        /// Initialize a new <see cref="RoboQueue"/> object that contains the supplied <see cref="IRoboCommand"/>.
        /// </summary>
        /// <inheritdoc cref="RoboQueue(IEnumerable{IRoboCommand}, string, int)"/>
        public RoboQueue(IRoboCommand roboCommand, string name = "", int maxConcurrentJobs = 1)
        {
            CommandList.Add(roboCommand);
            Init(name, maxConcurrentJobs);
            Commands = new ReadOnlyCollection<IRoboCommand>(CommandList);
        }

        /// <summary>
        /// Initialize a new <see cref="RoboQueue"/> object that contains the supplied <see cref="IRoboCommand"/> collection.
        /// </summary>
        /// <param name="roboCommand">IRoboCommand(s) to populate the list with.</param>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="maxConcurrentJobs"><inheritdoc cref="MaxConcurrentJobs"/></param>
        public RoboQueue(IEnumerable<IRoboCommand> roboCommand, string name = "", int maxConcurrentJobs = 1)
        {
            CommandList.AddRange(roboCommand);
            Init(name, maxConcurrentJobs);
            Commands = new ReadOnlyCollection<IRoboCommand>(CommandList);
        }

        /// <summary>
        /// Initialize a new <see cref="RoboQueue"/> object that contains the supplied <see cref="IRoboCommand"/> collection.
        /// </summary>
        /// <param name="roboCommands">IRoboCommand(s) to populate the list with.</param>
        public RoboQueue(params IRoboCommand[] roboCommands)
        {
            CommandList.AddRange(roboCommands);
            Init();
            Commands = new ReadOnlyCollection<IRoboCommand>(CommandList);
        }

        private void Init(string name = "", int maxConcurrentJobs = 1)
        {
            NameField = name;
            MaxConcurrentJobsField = maxConcurrentJobs;
        }

        #endregion

        #region < Fields >

        private readonly ObservableList<IRoboCommand> CommandList = new ObservableList<IRoboCommand>();
        private RoboQueueProgressEstimator Estimator;
        private bool disposedValue;
        private CancellationTokenSource TaskCancelSource;
        private string NameField;

        private bool WasCancelledField = false;
        private bool IsPausedField = false;
        private bool IsCopyOperationRunningField = false;
        private bool IsListOperationRunningField = false;
        private bool ListOnlyCompletedField = false;
        private bool CopyOpCompletedField = false;

        private int MaxConcurrentJobsField;
        private int JobsStartedField;
        private int JobsCompleteField;
        private int JobsCompletedSuccessfullyField;

        #endregion

        #region < Properties Dependent on CommandList >

        /// <summary> 
        /// Checks <see cref="IRoboCommand.IsRunning"/> property of all items in the list. 
        /// <br/> INotifyPropertyChanged is not raised when this property changes.
        /// </summary>
        public bool AnyRunning => CommandList.Any(c => c.IsRunning);

        /// <summary> 
        /// Checks <see cref="IRoboCommand.IsPaused"/> property of all items in the list. 
        /// <br/> INotifyPropertyChanged is not raised when this property changes.
        /// </summary>
        public bool AnyPaused => CommandList.Any(c => c.IsPaused);

        /// <summary> 
        /// Checks <see cref="IRoboCommand.IsCancelled"/> property of all items in the list. 
        /// <br/> INotifyPropertyChanged is not raised when this property changes.
        /// </summary>
        public bool AnyCancelled => CommandList.Any(c => c.IsCancelled);

        /// <summary> 
        /// Check the list and get the count of RoboCommands that are either in the 'Run' or 'Paused' state. <br/>
        /// (Paused state is included since these can be resumed at any time) 
        /// </summary>
        public int JobsCurrentlyRunning => CommandList.Where((C) => C.IsRunning | C.IsPaused).Count();

        /// <summary> Number of RoboCommands in the list </summary>
        public int ListCount => CommandList.Count;

        #endregion

        #region < Properties >

        /// <summary>
        /// Name of this collection of RoboCommands
        /// </summary>
        public string Name
        {
            get => NameField;
            private set
            {
                if (value != NameField)
                {
                    NameField = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// Wraps the private <see cref="ObservableList{T}"/> into a ReadOnlyCollection for public consumption and data binding.
        /// </summary>
        public ReadOnlyCollection<IRoboCommand> Commands { get; }

        /// <summary>
        /// <inheritdoc cref="RoboCommand.ProgressEstimator"/> <para/>
        /// This object will produce the sum of all the ProgressEstimator objects generated by the commands within the list.
        /// <para/> After the first request, the values will be updated every 250ms while the Queue is still running.
        /// </summary>
        public IProgressEstimator ProgressEstimator => Estimator;

        /// <summary> 
        /// Indicates if a task is currently running or paused. <br/>
        /// When true, prevents starting new tasks and prevents modication of the list.
        /// </summary>
        public bool IsRunning => IsCopyOperationRunning || IsListOnlyRunning;

        /// <summary>
        /// This is set true when <see cref="PauseAll"/> is called while any of the items in the list were running, and set false when <see cref="ResumeAll"/> or <see cref="StopAll"/> is called.
        /// </summary>
        public bool IsPaused
        {
            get => IsPausedField;
            private set
            {
                if (value != IsPausedField)
                {
                    IsPausedField = value;
                    OnPropertyChanged(nameof(IsPaused));
                }
            }
        }

        /// <summary>
        /// Flag is set to TRUE if the 'Stop' command is issued. Reset to False when starting a new operation.
        /// </summary>
        public bool WasCancelled
        {
            get => WasCancelledField;
            private set
            {
                if (value != WasCancelledField)
                {
                    WasCancelledField = value;
                    OnPropertyChanged(nameof(WasCancelled));
                }
            }
        }

        /// <summary> Indicates if the StartAll task is currently running. </summary>
        public bool IsCopyOperationRunning
        {
            get => IsCopyOperationRunningField;
            private set
            {
                if (value != IsCopyOperationRunningField)
                {
                    bool running = IsRunning;
                    IsCopyOperationRunningField = value;
                    OnPropertyChanged(nameof(IsCopyOperationRunning));
                    if (IsRunning != running) OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        /// <summary> Indicates if the StartAll_ListOnly task is currently running. </summary>
        public bool IsListOnlyRunning
        {
            get => IsListOperationRunningField;
            private set
            {
                if (value != IsListOperationRunningField)
                {
                    bool running = IsRunning;
                    IsListOperationRunningField = value;
                    OnPropertyChanged(nameof(IsListOnlyRunning));
                    if (IsRunning != running) OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        /// <summary> Indicates if the StartAll_ListOnly() operation has been completed. </summary>
        public bool ListOnlyCompleted
        {
            get => ListOnlyCompletedField;
            private set
            {
                if (value != ListOnlyCompletedField)
                {
                    ListOnlyCompletedField = value;
                    OnPropertyChanged(nameof(ListOnlyCompleted));
                }
            }
        }

        /// <summary> Indicates if the StartAll() operation has been completed. </summary>
        public bool CopyOperationCompleted
        {
            get => CopyOpCompletedField;
            private set
            {
                if (value != CopyOpCompletedField)
                {
                    CopyOpCompletedField = value;
                    OnPropertyChanged(nameof(CopyOperationCompleted));
                }
            }
        }

        /// <summary>
        /// Specify the max number of RoboCommands to execute at the same time. <br/>
        /// Set Value to 0 to allow infinite number of jobs (Will issue all start commands at same time) <br/>
        /// Default Value = 1; <br/>
        /// </summary>
        public int MaxConcurrentJobs
        {
            get => MaxConcurrentJobsField;
            set
            {
                int newVal = value > 0 ? value : IsRunning & MaxConcurrentJobsField > 0 ? 1 : 0;  //Allow > 0 at all times //If running, set value to 1
                if (newVal != MaxConcurrentJobsField)
                {
                    MaxConcurrentJobsField = newVal;
                    OnPropertyChanged(nameof(MaxConcurrentJobs));
                }
            }
        }

        /// <summary>
        /// Report how many <see cref="IRoboCommand.Start"/> tasks has completed during the run. <br/>
        /// This value is reset to 0 when a new run starts, and increments as each job exits.
        /// </summary>
        public int JobsComplete
        {
            get => JobsCompleteField;
            private set
            {
                if (value != JobsCompleteField)
                {
                    JobsCompleteField = value;
                    OnPropertyChanged(nameof(JobsComplete));
                }
            }
        }

        /// <summary>
        /// Report how many <see cref="IRoboCommand.Start"/> tasks has completed successfully during the run. <br/>
        /// This value is reset to 0 when a new run starts, and increments as each job exits.
        /// </summary>
        public int JobsCompletedSuccessfully
        {
            get => JobsCompletedSuccessfullyField;
            private set
            {
                if (value != JobsCompletedSuccessfullyField)
                {
                    JobsCompletedSuccessfullyField = value;
                    OnPropertyChanged(nameof(JobsCompletedSuccessfully));
                }
            }
        }

        /// <summary>
        /// Report how many <see cref="IRoboCommand.Start"/> tasks have been started during the run. <br/>
        /// This value is reset to 0 when a new run starts, and increments as each job starts.
        /// </summary>
        public int JobsStarted
        {
            get => JobsStartedField;
            private set
            {
                if (value != JobsStartedField)
                {
                    JobsStartedField = value;
                    OnPropertyChanged(nameof(JobsStarted));
                }
            }
        }

        /// <summary>
        /// Contains the results from the most recent run started via <see cref="StartAll_ListOnly(string, string, string)"/> <para/>
        /// Any time StartALL_ListOnly is called, a new RoboQueueResults object will be created.  <br/>
        /// </summary>
        public IRoboQueueResults ListResults => ListResultsObj;
        private RoboQueueResults ListResultsObj { get; set; }

        /// <summary>
        /// Contains the results from the most recent run started via <see cref="StartAll"/> <para/>
        /// Any time StartALL is called, a new RoboQueueResults object will be created.  <br/>
        /// </summary>
        public IRoboQueueResults RunResults => RunResultsObj;
        private RoboQueueResults RunResultsObj { get; set; }

        /* 
         * Possible To-Do: Code in ConcurrentQueue objects if issues arise with items being added to the ResultsObj lists.
         * private ConcurrentQueue<RoboCopyResults> ListResultsQueue = new ConcurrentQueue<RoboCopyResults>(); 
         * private ConcurrentQueue<RoboCopyResults> RunResultsQueue = new ConcurrentQueue<RoboCopyResults>();
         */

        #endregion

        #region < Events >

        #region < IRoboCommand Events  >

        /// <inheritdoc cref="RoboCommand.OnFileProcessed"/>
        /// <remarks>This bind to every IRoboCommand in the list.</remarks>
        public event RoboCommand.FileProcessedHandler OnFileProcessed;

        /// <inheritdoc cref="RoboCommand.OnCommandError"/>
        /// <remarks>This bind to every RoboCommand in the list.</remarks>
        public event RoboCommand.CommandErrorHandler OnCommandError;

        /// <inheritdoc cref="RoboCommand.OnError"/>
        /// <remarks>This bind to every RoboCommand in the list.</remarks>
        public event RoboCommand.ErrorHandler OnError;

        /// <inheritdoc cref="RoboCommand.OnCommandCompleted"/>
        /// <remarks>This will occur for every RoboCommand in the list.</remarks>
        public event RoboCommand.CommandCompletedHandler OnCommandCompleted;

        /// <inheritdoc cref="RoboCommand.OnCopyProgressChanged"/>
        /// <remarks>This bind to every RoboCommand in the list.</remarks>
        public event RoboCommand.CopyProgressHandler OnCopyProgressChanged;

        #endregion

        #region < ListUpdated Events >

        /// <summary> Occurs when the <see cref="ListResults"/> gets updated </summary>
        public event RoboCopyResultsList.ResultsListUpdated ListResultsUpdated;

        /// <summary> Occurs when the <see cref="RunResults"/> gets updated </summary>
        public event RoboCopyResultsList.ResultsListUpdated RunResultsUpdated;

        #endregion

        #region < ProgressUpdater Event >

        /// <summary>Handles <see cref="OnProgressEstimatorCreated"/></summary>
        public delegate void ProgressUpdaterCreatedHandler(RoboQueue sender, ProgressEstimatorCreatedEventArgs e);
        /// <summary>
        /// Occurs when a <see cref="Results.ProgressEstimator"/> is created when starting a new task, allowing binding to occur within the event subscriber. <br/>
        /// This event will occur once per Start. See notes on <see cref="ProgressEstimator"/> for more details.
        /// </summary>
        public event ProgressUpdaterCreatedHandler OnProgressEstimatorCreated;

        #endregion

        #region < CommandStarted Event >

        /// <summary>Handles <see cref="OnCommandStarted"/></summary>
        public delegate void CommandStartedHandler(RoboQueue sender, RoboQueueCommandStartedEventArgs e);
        /// <summary>
        /// Occurs each time a Command has started succesfully
        /// </summary>
        public event CommandStartedHandler OnCommandStarted;

        #endregion

        #region < RunComplete Event >

        /// <summary>Handles <see cref="OnCommandCompleted"/></summary>
        public delegate void RunCompletedHandler(RoboQueue sender, RoboQueueCompletedEventArgs e);
        /// <summary>
        /// Occurs after when the task started by the StartAll and StartAll_ListOnly methods has finished executing.
        /// </summary>
        public event RunCompletedHandler RunCompleted;

        #endregion

        #region < UnhandledException Fault >

        /// <summary>
        /// Occurs if the RoboQueue task is stopped due to an unhandled exception. Occurs instead of <see cref="RoboQueue.RunCompleted"/>
        /// <br/> Also occurs if any of the RoboCommand objects raise <see cref="RoboCommand.TaskFaulted"/>
        /// </summary>
        public event UnhandledExceptionEventHandler TaskFaulted;

        #endregion

        #endregion

        #region < Methods >

        /// <summary>
        /// Get the current instance of the <see cref="ListResults"/> object
        /// </summary>
        /// <returns>New instance of the <see cref="ListResults"/> list.</returns>
        public RoboQueueResults GetListResults() => ListResultsObj;

        /// <summary>
        /// Get the current of the <see cref="RunResults"/> object
        /// </summary>
        /// <returns>New instance of the <see cref="RunResults"/> list.</returns>
        public RoboQueueResults GetRunResults() => RunResultsObj;

        /// <summary>
        /// Run <see cref="RoboCommand.Stop()"/> against all items in the list.
        /// </summary>
        public void StopAll()
        {
            //If a TaskCancelSource is present, request cancellation. The continuation tasks null the value out then call this method to ensure everything stopped once they complete. 
            if (TaskCancelSource != null && !TaskCancelSource.IsCancellationRequested)
            {
                IsPaused = false;
                TaskCancelSource.Cancel(); // Cancel the IRoboCommand Task
                //IRoboCommand Continuation Task will call StopAllTask() method to ensure all processes are stopped & diposed.
            }
            else if (TaskCancelSource == null)
            {
                //This is supplied to allow stopping all commands if consumer manually looped through the list instead of using the Start* methods.
                CommandList.ForEach((c) => c.Stop());
                IsCopyOperationRunning = false;
                IsListOnlyRunning = false;
                IsPaused = false;
            }
            WasCancelled = true;
        }

        /// <summary>
        /// Loop through the items in the list and issue <see cref="IRoboCommand.Pause"/> on any commands where <see cref="IRoboCommand.IsRunning"/> is true.
        /// </summary>
        public void PauseAll()
        {
            CommandList.ForEach((c) => { if (c.IsRunning) c.Pause(); });
            IsPaused = IsRunning || AnyPaused;
        }

        /// <summary>
        /// Loop through the items in the list and issue <see cref="IRoboCommand.Resume"/> on any commands where <see cref="IRoboCommand.IsPaused"/> is true.
        /// </summary>
        public void ResumeAll()
        {
            CommandList.ForEach((c) => { if (c.IsPaused) c.Resume(); });
            IsPaused = false;
        }

        #endregion

        #region < Run List-Only Mode >

        /// <summary>
        /// Set all IRoboCommand objects to ListOnly mode, run them, then set all RoboCommands back to their previous ListOnly mode setting.
        /// </summary>
        /// <inheritdoc cref="StartJobs"/>
        public async Task<IRoboQueueResults> StartAll_ListOnly(string domain = "", string username = "", string password = "")
        {
            if (IsRunning) throw new InvalidOperationException("Cannot start a new RoboQueue Process while this RoboQueue is already running.");
            IsListOnlyRunning = true;
            ListOnlyCompleted = false;

            ListResultsObj = new RoboQueueResults();
            ListResultsUpdated?.Invoke(this, new ResultListUpdatedEventArgs(ListResults));

            //Run the commands
            Task run = null;
            try
            {
                run = StartJobs(domain, username, password, true);
                await run;
            }
            finally
            {
                //Set Flags
                IsListOnlyRunning = false;
                IsPaused = false;
                ListOnlyCompleted = !WasCancelled && !run.IsFaulted;

                // If some fault occurred while processing, throw the exception to caller
                if (run.IsFaulted)
                {
                    TaskFaulted?.Invoke(this, new UnhandledExceptionEventArgs(run.Exception, true));
                }
                ListResultsObj.EndTime = DateTime.Now;
                RunCompleted?.Invoke(this, new RoboQueueCompletedEventArgs(ListResultsObj, true));
            }
            return ListResultsObj;
        }

        #endregion

        #region < Run User-Set Parameters >

        /// <inheritdoc cref="StartJobs"/>
        public Task<IRoboQueueResults> StartAll(string domain = "", string username = "", string password = "")
        {
            if (IsRunning) throw new InvalidOperationException("Cannot start a new RoboQueue Process while this RoboQueue is already running.");

            IsCopyOperationRunning = true;
            CopyOperationCompleted = false;

            RunResultsObj = new RoboQueueResults();
            RunResultsUpdated?.Invoke(this, new ResultListUpdatedEventArgs(RunResults));

            Task Run = StartJobs(domain, username, password, false);
            Task<IRoboQueueResults> ResultsTask = Run.ContinueWith((continuation) =>
            {
                IsCopyOperationRunning = false;
                IsPaused = false;
                CopyOperationCompleted = !WasCancelled && !continuation.IsFaulted;

                // If some fault occurred while processing, throw the exception to caller
                if (continuation.IsFaulted)
                {
                    TaskFaulted?.Invoke(this, new UnhandledExceptionEventArgs(continuation.Exception, true));
                    throw continuation.Exception;
                }

                RunResultsObj.EndTime = DateTime.Now;
                RunCompleted?.Invoke(this, new RoboQueueCompletedEventArgs(RunResultsObj, false));
                return (IRoboQueueResults)RunResultsObj;
            }, CancellationToken.None
            );
            return ResultsTask;
        }

        #endregion

        #region < StartJobs Method >

        /// <summary>
        /// Create Task that Starts all RoboCommands. 
        /// </summary>
        /// <remarks> <paramref name="domain"/>, <paramref name="password"/>, and <paramref name="username"/> are applied to all IRoboCommand objects during this run. </remarks>
        /// <returns> New Task that finishes after all RoboCommands have stopped executing </returns>
        private Task StartJobs(string domain = "", string username = "", string password = "", bool ListOnlyMode = false)
        {
            Debugger.Instance.DebugMessage("Starting Parallel execution of RoboQueue");

            TaskCancelSource = new CancellationTokenSource();
            CancellationToken cancellationToken = TaskCancelSource.Token;
            var SleepCancelToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken).Token;

            List<Task> TaskList = new List<Task>();
            JobsStarted = 0;
            JobsComplete = 0;
            JobsCompletedSuccessfully = 0;

            WasCancelled = false;
            IsPaused = false;

            //Create a Task to Start all the RoboCommands
            Task StartAll = Task.Factory.StartNew(async () =>
           {
               CommandList.RemoveAll((c) => c is null); // Remove all null references

               //Reset results of all commands in the list
               foreach (RoboCommand cmd in CommandList.Where(cmd => cmd is RoboCommand).Select(cmd => (RoboCommand)cmd))
                   cmd?.ResetResults();


               Estimator = new RoboQueueProgressEstimator();
               OnProgressEstimatorCreated?.Invoke(this, new ProgressEstimatorCreatedEventArgs(Estimator));

               //Start all commands, running as many as allowed
               foreach (IRoboCommand cmd in CommandList)
               {
                   if (cancellationToken.IsCancellationRequested) break;

                   //Assign the events
                   void handler(IRoboCommand o, RoboCommandCompletedEventArgs e) => RaiseCommandCompleted(o, e, ListOnlyMode);
                   cmd.OnCommandCompleted += handler;
                   cmd.OnCommandError += this.OnCommandError;
                   cmd.OnCopyProgressChanged += this.OnCopyProgressChanged;
                   cmd.OnError += this.OnError;
                   cmd.OnFileProcessed += this.OnFileProcessed;
                   cmd.OnProgressEstimatorCreated += Cmd_OnProgressEstimatorCreated;
                   cmd.TaskFaulted += TaskFaulted;

                   //Start the job
                   //Once the job ends, unsubscribe events
                   Task C = !ListOnlyMode ? cmd.Start(domain, username, password) : cmd.Start_ListOnly(domain, username, password);
                   Task T = C.ContinueWith((t) =>
                   {
                       cmd.OnCommandCompleted -= handler;
                       cmd.OnCommandError -= this.OnCommandError;
                       cmd.OnCopyProgressChanged -= this.OnCopyProgressChanged;
                       cmd.OnError -= this.OnError;
                       cmd.OnFileProcessed -= this.OnFileProcessed;
                       if (t.IsFaulted) throw t.Exception; // If some fault occurred while processing, throw the exception to caller
                   }, CancellationToken.None);

                   TaskList.Add(T);                    //Add the continuation task to the list.

                   //Raise Events
                   JobsStarted++; OnPropertyChanged(nameof(JobsStarted));
                   if (cmd.IsRunning) OnCommandStarted?.Invoke(this, new RoboQueueCommandStartedEventArgs(cmd)); //Declare that a new command in the queue has started.
                   OnPropertyChanged(nameof(JobsCurrentlyRunning));  //Notify the Property Changes

                   //Check if more jobs are allowed to run
                   if (IsPaused) cmd.Pause(); //Ensure job that just started gets paused if Pausing was requested
                   while (!cancellationToken.IsCancellationRequested && (IsPaused || (MaxConcurrentJobs > 0 && JobsCurrentlyRunning >= MaxConcurrentJobs && TaskList.Count < CommandList.Count)))
                       await ThreadEx.CancellableSleep(500, SleepCancelToken);

               } //End of ForEachLoop

               //Asynchronous wait for either cancellation is requested OR all jobs to finish.
               //- Task.WaitAll is blocking -> not ideal, and also throws if cancellation is requested -> also not ideal.
               //- Task.WhenAll is awaitable, but does not provide allow cancellation
               //- If Cancelled, the 'WhenAll' task continues to run, but the ContinueWith task here will stop all tasks, thus completing the WhenAll task
               if (!cancellationToken.IsCancellationRequested)
               {
                   var tcs = new TaskCompletionSource<object>();
                   _ = cancellationToken.Register(() => tcs.TrySetResult(null));
                   _ = await Task.WhenAny(Task.WhenAll(TaskList.ToArray()), tcs.Task);
               }

           }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();

            //Continuation Task return results to caller
            Task ContinueWithTask = StartAll.ContinueWith(async (continuation) =>
           {
               await Estimator?.CancelTasks();
               if (cancellationToken.IsCancellationRequested)
               {
                   //If cancellation was requested -> Issue the STOP command to all commands in the list
                   Debugger.Instance.DebugMessage("RoboQueue Task Was Cancelled");
                   await StopAllTask(TaskList);
               }
               else if (continuation.IsFaulted)
               {
                   Debugger.Instance.DebugMessage("RoboQueue Task Faulted");
                   await StopAllTask(TaskList);
                   throw continuation.Exception;
               }
               else
               {
                   Debugger.Instance.DebugMessage("RoboQueue Task Completed");
               }

               TaskCancelSource?.Dispose();
               TaskCancelSource = null;

           }, CancellationToken.None).Unwrap();

            return ContinueWithTask;
        }

        private async Task StopAllTask(IEnumerable<Task> StartedTasks)
        {
            CommandList.ForEach((c) => c.Stop());
            await Task.WhenAll(StartedTasks);

            IsCopyOperationRunning = false;
            IsListOnlyRunning = false;
            IsPaused = false;

            TaskCancelSource.Dispose();
            TaskCancelSource = null;
        }

        private void Cmd_OnProgressEstimatorCreated(IRoboCommand sender, ProgressEstimatorCreatedEventArgs e)
        {
            Estimator?.BindToProgressEstimator(e.ResultsEstimate);
            sender.OnProgressEstimatorCreated -= Cmd_OnProgressEstimatorCreated;
        }

        /// <summary>
        /// Intercept OnCommandCompleted from each IRoboCommand, react, then raise this object's OnCommandCompleted event
        /// </summary>
        private void RaiseCommandCompleted(IRoboCommand sender, RoboCommandCompletedEventArgs e, bool ListOnlyBinding)
        {
            if (ListOnlyBinding)
            {
                ListResultsObj.Add(sender.GetResults());
                ListResultsUpdated?.Invoke(this, new ResultListUpdatedEventArgs(ListResults));
            }
            else
            {
                RunResultsObj.Add(sender.GetResults());
                RunResultsUpdated?.Invoke(this, new ResultListUpdatedEventArgs(RunResults));
            }

            //Notify the Property Changes
            if (!sender.IsCancelled)
            {
                JobsCompletedSuccessfully++;
            }
            JobsComplete++;
            OnPropertyChanged(nameof(JobsCurrentlyRunning));
            OnCommandCompleted?.Invoke(sender, e);
        }

        #endregion

        #region < IDisposable Implementation >

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Estimator?.UnBind();

                    //IRoboCommand objects attach to a process, so must be in the 'unmanaged' section.
                    foreach (IRoboCommand cmd in CommandList)
                        cmd.Dispose();
                    CommandList.Clear();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizer -> Ensures that all IRoboCommand objects get disposed of properly when program exits
        /// </summary>
        ~RoboQueue()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Dispose all IRoboCommand objects contained in the list. - This will kill any Commands that have <see cref="RoboCommand.StopIfDisposing"/> = true (default) <br/>
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region < INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable >

        /// <inheritdoc cref="INotifyPropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <inheritdoc cref="ObservableCollection{T}.CollectionChanged"/>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { CommandList.CollectionChanged += value; }
            remove { CommandList.CollectionChanged -= value; }
        }

        /// <inheritdoc cref="IList.this"/>
        public IRoboCommand this[int i]
        {
            get => this.CommandList[i];
            set => this.CommandList[i] = value;
        }

        /// <summary>
        /// Gets the enumerator for the enumeating through this object's <see cref="IRoboCommand"/> objects
        /// </summary>
        public IEnumerator<IRoboCommand> GetEnumerator()
        {
            return Commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Commands).GetEnumerator();
        }

        #endregion

        #region < List Access Methods >

        /// <summary>
        /// Exception thrown when attempting to run a method accesses the list backing a RoboQueue object while the tasks are in progress.
        /// </summary>
        public class ListAccessDeniedException : Exception
        {
            /// <remarks>This functionality is disabled if <see cref="IsRunning"/> == true.</remarks>
            /// <exception cref="ListAccessDeniedException"/>
            private const string StandardMsg = "Running methods that modify the list of RoboCommands methods while RoboQueue.IsRunning = TRUE is prohibited.";
            internal ListAccessDeniedException() : base(StandardMsg) { }
            internal ListAccessDeniedException(string message) : base($"{StandardMsg}\n{message}") { }
            internal ListAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
        }

        #region < Add >

        /// <inheritdoc cref="List{T}.Add(T)"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void AddCommand(IRoboCommand item)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.Add(item);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));

        }

        /// <inheritdoc cref="List{T}.Insert(int, T)"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void AddCommand(int index, IRoboCommand item)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.Insert(index, item);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <inheritdoc cref="List{T}.AddRange(IEnumerable{T})"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void AddCommand(IEnumerable<IRoboCommand> collection)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.AddRange(collection);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <inheritdoc cref="List{T}.AddRange(IEnumerable{T})"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void AddCommand(params IRoboCommand[] collection)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.AddRange(collection);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        #endregion

        #region < Remove >

        /// <inheritdoc cref="List{T}.Remove(T)"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void RemoveCommand(IRoboCommand item)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.Remove(item);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <inheritdoc cref="List{T}.RemoveAt(int)"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void RemoveCommand(int index)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.RemoveAt(index);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <inheritdoc cref="List{T}.RemoveRange(int, int)"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void RemoveCommand(int index, int count)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.RemoveRange(index, count);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <inheritdoc cref="List{T}.RemoveAll(Predicate{T})"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void RemoveCommand(Predicate<IRoboCommand> match)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.RemoveAll(match);
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <inheritdoc cref="List{T}.Clear"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void ClearCommandList()
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.Clear();
            OnPropertyChanged(nameof(ListCount));
            OnPropertyChanged(nameof(Commands));
        }

        /// <summary>Performs <see cref="RemoveCommand(int)"/> then <see cref="AddCommand(int, IRoboCommand)"/></summary>
        public void ReplaceCommand(IRoboCommand item, int index)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.Replace(index, item);
        }

        #endregion

        #region < Find / Contains / Etc >

        /// <inheritdoc cref="List{T}.Contains(T)"/>
        public bool Contains(IRoboCommand item) => CommandList.Contains(item);

        /// <inheritdoc cref="List{T}.ForEach(Action{T})"/>
        /// <inheritdoc cref="ListAccessDeniedException.StandardMsg"/>
        public void ForEach(Action<IRoboCommand> action)
        {
            if (IsRunning) throw new ListAccessDeniedException();
            CommandList.ForEach(action);
        }

        /// <inheritdoc cref="List{T}.FindAll(Predicate{T})"/>
        public List<IRoboCommand> FindAll(Predicate<IRoboCommand> predicate) => CommandList.FindAll(predicate);

        /// <inheritdoc cref="List{T}.Find(Predicate{T})"/>
        public IRoboCommand Find(Predicate<IRoboCommand> predicate) => CommandList.Find(predicate);

        /// <inheritdoc cref="List{T}.IndexOf(T)"/>
        public int IndexOf(IRoboCommand item) => CommandList.IndexOf(item);

        #endregion

        #endregion

    }
}
