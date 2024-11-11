using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoboSharp.EventArgObjects;
using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RoboSharp.BackupApp.ViewModels
{
    /// <summary>
    /// Orchestrates a RoboQueue object
    /// </summary>
    internal partial class RoboQueueViewModel : ObservableObject
    {
        public RoboQueueViewModel() 
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            BindToCommand(new RoboQueue());
            Errors = new ObservableCollection<string>();
            RunningCommands = new ObservableCollection<CommandProgressViewModel>();
            ProgressEstimator = new CommandProgressViewModel(activeCommand);
            System.Windows.Input.CommandManager.RequerySuggested += CommandManager_RequerySuggested;
        }

        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            BtnPauseResumeCommand.NotifyCanExecuteChanged();
            BtnRemoveSelectedItemCommand.NotifyCanExecuteChanged();
            BtnRemoveSelectedItemCommand.NotifyCanExecuteChanged();
            BtnStartStopQueueCommand.NotifyCanExecuteChanged();
        }

        public RoboQueueViewModel(RoboQueue queue) : this()
        {
            BindToCommand(queue);
        }

        private readonly Dispatcher _dispatcher;
        private RoboQueue activeCommand;
        private int _maxConcurrentJobs;
        [ObservableProperty] private IRoboCommand _selectedCommand;
        [ObservableProperty] private int _jobsRunning;
        [ObservableProperty] private int _totalJobsCount;
        [ObservableProperty] private int _totalJobsComplete;
        [ObservableProperty] private string _jobsCompleteText;
        [ObservableProperty] private string _errorsHeader;
        [ObservableProperty] private string _btnStartStopText;
        [ObservableProperty] private string _btnPauseResumeText;
        [ObservableProperty] private bool _btnStartStopEnabled;
        [ObservableProperty] private bool _btnPauseResumeEnabled;
        [ObservableProperty] private bool _runAsListOnly;

        [ObservableProperty] JobHistoryViewModel runResults;
        [ObservableProperty] JobHistoryViewModel listOnlyResults;
        [ObservableProperty] ViewModels.CommandProgressViewModel progressEstimator;

        public ObservableCollection<string> Errors { get; }
        public ObservableCollection<CommandProgressViewModel> RunningCommands { get; }

        public int[] Ints { get; } = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        public RoboQueue Command => activeCommand;

        public int MaxConcurrentJobs
        {
            get => _maxConcurrentJobs;
            set
            {
                OnPropertyChanging();
                if (Command != null) Command.MaxConcurrentJobs = value;
                _maxConcurrentJobs = value;
                OnPropertyChanged();
            }
        }

        public void BindToCommand(RoboQueue roboQ)
        {
            if (activeCommand != null)
            {
                roboQ.RunResultsUpdated -= RoboQueue_RunOperationResultsUpdated;
                roboQ.ListResultsUpdated -= RoboQueue_ListOnlyResultsUpdated;

                roboQ.OnCommandError -= RoboQueue_OnCommandError;
                roboQ.OnError -= RoboQueue_OnError; ;
                roboQ.OnCommandCompleted -= RoboQueue_OnCommandCompleted; ;
                roboQ.OnCommandStarted -= RoboQueue_OnCommandStarted;
                roboQ.CollectionChanged -= RoboQueue_CollectionChanged;
            }

            OnPropertyChanging(nameof(Command));
            activeCommand = roboQ;
            OnPropertyChanged(nameof(Command));
            roboQ.RunResultsUpdated += RoboQueue_RunOperationResultsUpdated;
            roboQ.ListResultsUpdated += RoboQueue_ListOnlyResultsUpdated;

            roboQ.OnCommandError += RoboQueue_OnCommandError;
            roboQ.OnError += RoboQueue_OnError; ;
            roboQ.OnCommandCompleted += RoboQueue_OnCommandCompleted; ;
            //roboQ.OnProgressEstimatorCreated += RoboQueue_OnProgressEstimatorCreated;
            roboQ.OnCommandStarted += RoboQueue_OnCommandStarted;
            roboQ.CollectionChanged += RoboQueue_CollectionChanged;
        }

        private void RoboQueue_OnCommandStarted(RoboQueue sender, RoboQueueCommandStartedEventArgs e)
        {
            //return;
            _dispatcher.Invoke(() =>
            {
                RunningCommands.Add(new CommandProgressViewModel(e.Command));
                JobsRunning = Command.JobsCurrentlyRunning;
            });
        }

        private void RoboQueue_OnCommandCompleted(IRoboCommand sender, RoboCommandCompletedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                JobsCompleteText = $"{Command.JobsComplete} of {Command.ListCount}";
                TotalJobsComplete = Command.JobsComplete;
                JobsRunning = Command.JobsCurrentlyRunning;
            });
        }

        private void RoboQueue_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    // Remove all FirstMultiProgressExpander
                    RunningCommands.Clear();
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    //Remove associated MultiJob_CommandProgressIndicator
                    foreach (var item in RunningCommands.Where(i => e.OldItems.Contains(i.Command)).ToArray())
                        RunningCommands.Remove(item);
                }
            });
        }

        /// <summary>
        /// Log the Error to the Errors expander
        /// </summary>
        private void RoboQueue_OnError(IRoboCommand sender, ErrorEventArgs e)
        {
            Errors.Insert(0, e.Error);
            ErrorsHeader = $"Errors ({Errors.Count})";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Occurs while a command is starting prior to Robocopy starting (for example, due to missing source location), but won't break the entire RoboQueue. 
        /// That single job will just not start, all others will.
        /// </remarks>
        private void RoboQueue_OnCommandError(IRoboCommand sender, CommandErrorEventArgs e)
        {
            _ = BtnStartStopQueue(); // Stop Everything
        }

        /// <summary>
        /// This listener is used to rebind the ListOnly results to the display listbox. 
        /// It had to be done like this due to INotifyCollectionChanged not updating the listbox due to threading issues
        /// </summary>
        private void RoboQueue_ListOnlyResultsUpdated(object sender, EventArgObjects.ResultListUpdatedEventArgs e)
        {
            _dispatcher.Invoke(() => { ListOnlyResults = new JobHistoryViewModel(e.ResultsList); });
        }

        /// <summary>
        /// This listener is used to rebind the RunOperation results to the display listbox. 
        /// It had to be done like this due to INotifyCollectionChanged not updating the listbox due to threading issues
        /// </summary>
        private void RoboQueue_RunOperationResultsUpdated(object sender, EventArgObjects.ResultListUpdatedEventArgs e)
        {
            _dispatcher.Invoke(() => { RunResults = new JobHistoryViewModel(e.ResultsList); });
        }

        [RelayCommand(CanExecute = nameof(CanStartRoboQueue))]
        private async Task BtnStartStopQueue()
        {
            if (!Command.IsRunning)
            {
                if (Command.ListCount > 0)
                {
                    TotalJobsComplete = 0;
                    TotalJobsCount = Command.ListCount;

                    RunningCommands.Clear();

                    if (RunAsListOnly == true)
                    {
                        await Command.StartAll_ListOnly();
                        ListOnlyResults = new JobHistoryViewModel(Command.ListResults);
                    }
                    else
                    {
                        await Command.StartAll();
                        RunResults = new JobHistoryViewModel(Command.RunResults);
                    }
                }
                else
                {
                    MessageBox.Show("Job Queue is Empty");
                }
            }

            else
                Command.StopAll();
        }

        [RelayCommand(CanExecute =nameof(CanPause))]
        private void BtnPauseResume()
        {
            if (Command is null) return;
            if (!Command.IsPaused)
            {
                Command.PauseAll();
                BtnPauseResumeText = "Resume Job Queue";
            }
            else
            {
                Command.ResumeAll();
                BtnPauseResumeText = "Pause Job Queue";
            }
        }

        [RelayCommand(CanExecute = nameof(IsCommandSelected))]
        private void BtnRemoveSelectedItem() => Command.RemoveCommand(SelectedCommand);
        private bool CanPause() => IsCommandBound() && Command.IsRunning | Command.IsPaused;
        private bool IsCommandSelected() => SelectedCommand != null;
        private bool IsCommandBound() => Command != null;
        private bool IsQueueRunning() => Command?.IsRunning ?? false;
        private bool IsQueueStopped() => !IsQueueRunning();
        private bool CanStartRoboQueue() => IsQueueStopped() && Command.ListCount > 0;
    }
}
