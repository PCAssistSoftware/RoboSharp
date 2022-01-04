﻿using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace RoboSharp.BackupApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region < SingleJob Fields >

        RoboCommand copy;
        public ObservableCollection<FileError> SingleJobErrors = new ObservableCollection<FileError>();
        private Results.RoboCopyResultsList SingleJobResults = new Results.RoboCopyResultsList();

        #endregion

        #region < RoboQueue Fields >

        /// <summary> List of RoboCommand objects to start at same time </summary>
        private RoboSharp.RoboQueue RoboQueue = new RoboSharp.RoboQueue();
        public ObservableCollection<FileError> MultiJobErrors = new ObservableCollection<FileError>();
        private int[] AllowedJobCounts = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        #endregion

        #region < Init >

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;

            VersionManager.VersionCheck = VersionManager.VersionCheckType.UseWMI;
            var v = VersionManager.Version;
            //Button Setup
            btnAddToQueue.IsEnabled = true;
            btnStartJobQueue.IsEnabled = false;
            btnPauseQueue.IsEnabled = false;
            
            //Setup SingleJob Tab
            SingleJobExpander_JobHistory.BindToList(SingleJobResults);
            SingleJobErrorGrid.ItemsSource = SingleJobErrors;

            //RoboQueue Setup
            ListBox_RoboQueueJobs_MultiJobPage.ItemsSource = RoboQueue;
            ListBox_RoboQueueJobs_OptionsPage.ItemsSource = RoboQueue;
            MultiJobErrorGrid.ItemsSource = MultiJobErrors;
            MultiJob_ListOnlyResults.BindToList(RoboQueue.ListOnlyResults);
            MultiJob_RunResults.BindToList(RoboQueue.ListOnlyResults);
            cmbConcurrentJobs_OptionsPage.ItemsSource = AllowedJobCounts;
            cmbConcurrentJobs_MultiJobPage.ItemsSource = AllowedJobCounts;
            cmbConcurrentJobs_MultiJobPage.SelectedItem = RoboQueue.MaxConcurrentJobs;

            RoboQueue.OnCommandError += RoboQueue_OnCommandError;
            RoboQueue.OnError += RoboQueue_OnError; ;
            RoboQueue.OnCommandCompleted += RoboQueue_OnCommandCompleted; ;
            RoboQueue.OnProgressEstimatorCreated += RoboQueue_OnProgressEstimatorCreated;
            RoboQueue.OnCommandStarted += RoboQueue_OnCommandStarted;
            RoboQueue.CollectionChanged += RoboQueue_CollectionChanged;
            //RoboQueue.OnCopyProgressChanged += copy_OnCopyProgressChanged; // Not In Use for this project -> See MultiJob_CommandProgressIndicator
            //RoboQueue.OnFileProcessed += RoboQueue_OnFileProcessed; // Not In Use for this project -> See MultiJob_CommandProgressIndicator
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (copy != null)
            {
                copy.Stop();
                copy.Dispose();
            }
        }

        #endregion

        #region < Shared Methods >

        private RoboCommand GetCommand(bool BindEvents)
        {
            Debugger.Instance.DebugMessageEvent += DebugMessage;
            RoboCommand copy = new RoboCommand();
            if (BindEvents)
            {
                SingleJobExpander_Progress.BindToCommand(copy);
                copy.OnCommandError += copy_OnCommandError;
                copy.OnError += copy_OnError;
                copy.OnCommandCompleted += copy_OnCommandCompleted;
            }
            // copy options
            copy.CopyOptions.Source = Source.Text;
            copy.CopyOptions.Destination = Destination.Text;

            // split user input by whitespace, mantaining those enclosed by quotes
            var fileFilterItems = Regex.Matches(FileFilter.Text, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(m => m.Value);

            copy.CopyOptions.FileFilter = fileFilterItems;
            copy.CopyOptions.CopySubdirectories = CopySubDirectories.IsChecked ?? false;
            copy.CopyOptions.CopySubdirectoriesIncludingEmpty = CopySubdirectoriesIncludingEmpty.IsChecked ?? false;
            if (!string.IsNullOrWhiteSpace(Depth.Text))
                copy.CopyOptions.Depth = Convert.ToInt32(Depth.Text);
            copy.CopyOptions.EnableRestartMode = EnableRestartMode.IsChecked ?? false;
            copy.CopyOptions.EnableBackupMode = EnableBackupMode.IsChecked ?? false;
            copy.CopyOptions.EnableRestartModeWithBackupFallback = EnableRestartModeWithBackupFallback.IsChecked ?? false;
            copy.CopyOptions.UseUnbufferedIo = UseUnbufferedIo.IsChecked ?? false;
            copy.CopyOptions.EnableEfsRawMode = EnableEfsRawMode.IsChecked ?? false;
            copy.CopyOptions.CopyFlags = CopyFlags.Text;
            copy.CopyOptions.CopyFilesWithSecurity = CopyFilesWithSecurity.IsChecked ?? false;
            copy.CopyOptions.CopyAll = CopyAll.IsChecked ?? false;
            copy.CopyOptions.RemoveFileInformation = RemoveFileInformation.IsChecked ?? false;
            copy.CopyOptions.FixFileSecurityOnAllFiles = FixFileSecurityOnAllFiles.IsChecked ?? false;
            copy.CopyOptions.FixFileTimesOnAllFiles = FixFileTimesOnAllFiles.IsChecked ?? false;
            copy.CopyOptions.Purge = Purge.IsChecked ?? false;
            copy.CopyOptions.Mirror = Mirror.IsChecked ?? false;
            copy.CopyOptions.MoveFiles = MoveFiles.IsChecked ?? false;
            copy.CopyOptions.MoveFilesAndDirectories = MoveFilesAndDirectories.IsChecked ?? false;
            copy.CopyOptions.AddAttributes = AddAttributes.Text;
            copy.CopyOptions.RemoveAttributes = RemoveAttributes.Text;
            copy.CopyOptions.CreateDirectoryAndFileTree = CreateDirectoryAndFileTree.IsChecked ?? false;
            copy.CopyOptions.FatFiles = FatFiles.IsChecked ?? false;
            copy.CopyOptions.TurnLongPathSupportOff = TurnLongPathSupportOff.IsChecked ?? false;
            if (!string.IsNullOrWhiteSpace(MonitorSourceChangesLimit.Text))
                copy.CopyOptions.MonitorSourceChangesLimit = Convert.ToInt32(MonitorSourceChangesLimit.Text);
            if (!string.IsNullOrWhiteSpace(MonitorSourceTimeLimit.Text))
                copy.CopyOptions.MonitorSourceTimeLimit = Convert.ToInt32(MonitorSourceTimeLimit.Text);

            // select options
            copy.SelectionOptions.OnlyCopyArchiveFiles = OnlyCopyArchiveFiles.IsChecked ?? false;
            copy.SelectionOptions.OnlyCopyArchiveFilesAndResetArchiveFlag = OnlyCopyArchiveFilesAndResetArchiveFlag.IsChecked ?? false;
            copy.SelectionOptions.IncludeAttributes = IncludeAttributes.Text;
            copy.SelectionOptions.ExcludeAttributes = ExcludeAttributes.Text;
            copy.SelectionOptions.ExcludeFiles = ExcludeFiles.Text;
            copy.SelectionOptions.ExcludeDirectories = ExcludeDirectories.Text;
            copy.SelectionOptions.ExcludeOlder = ExcludeOlder.IsChecked ?? false;
            copy.SelectionOptions.ExcludeJunctionPoints = ExcludeJunctionPoints.IsChecked ?? false;

            // retry options
            if (!string.IsNullOrWhiteSpace(RetryCount.Text))
                copy.RetryOptions.RetryCount = Convert.ToInt32(RetryCount.Text);
            if (!string.IsNullOrWhiteSpace(RetryWaitTime.Text))
                copy.RetryOptions.RetryWaitTime = Convert.ToInt32(RetryWaitTime.Text);

            // logging options
            copy.LoggingOptions.VerboseOutput = VerboseOutput.IsChecked ?? false;
            copy.LoggingOptions.NoFileSizes = NoFileSizes.IsChecked ?? false;
            copy.LoggingOptions.NoProgress = NoProgress.IsChecked ?? false;
            return copy;
        }

        private void LoadCommand(RoboCommand copy)
        {
            if (copy == null) return;
            // copy options
            Source.Text = copy.CopyOptions.Source;
            Destination.Text = copy.CopyOptions.Destination;

            // 
            string fileFilterItems = "";
            foreach (string s in copy.CopyOptions.FileFilter)
                fileFilterItems += s;
            FileFilter.Text = fileFilterItems;

            CopySubDirectories.IsChecked = copy.CopyOptions.CopySubdirectories;
            CopySubdirectoriesIncludingEmpty.IsChecked  = copy.CopyOptions.CopySubdirectoriesIncludingEmpty;
            Depth.Text = copy.CopyOptions.Depth.ToString();
            EnableRestartMode.IsChecked = copy.CopyOptions.EnableRestartMode;
            EnableBackupMode.IsChecked  = copy.CopyOptions.EnableBackupMode;
            EnableRestartModeWithBackupFallback.IsChecked = copy.CopyOptions.EnableRestartModeWithBackupFallback;
            UseUnbufferedIo.IsChecked = copy.CopyOptions.UseUnbufferedIo;
            EnableEfsRawMode.IsChecked = copy.CopyOptions.EnableEfsRawMode;
            CopyFlags.Text = copy.CopyOptions.CopyFlags;
            CopyFilesWithSecurity.IsChecked = copy.CopyOptions.CopyFilesWithSecurity;
            CopyAll.IsChecked = copy.CopyOptions.CopyAll;
            RemoveFileInformation.IsChecked = copy.CopyOptions.RemoveFileInformation;
            FixFileSecurityOnAllFiles.IsChecked  = copy.CopyOptions.FixFileSecurityOnAllFiles;
            FixFileTimesOnAllFiles.IsChecked = copy.CopyOptions.FixFileTimesOnAllFiles;
            Purge.IsChecked = copy.CopyOptions.Purge;
            Mirror.IsChecked = copy.CopyOptions.Mirror;
            MoveFiles.IsChecked = copy.CopyOptions.MoveFiles;
            MoveFilesAndDirectories.IsChecked  = copy.CopyOptions.MoveFilesAndDirectories;
            AddAttributes.Text = copy.CopyOptions.AddAttributes;
            RemoveAttributes.Text = copy.CopyOptions.RemoveAttributes;
            CreateDirectoryAndFileTree.IsChecked  = copy.CopyOptions.CreateDirectoryAndFileTree;
            FatFiles.IsChecked = copy.CopyOptions.FatFiles;
            TurnLongPathSupportOff.IsChecked  = copy.CopyOptions.TurnLongPathSupportOff;
            
            MonitorSourceChangesLimit.Text = copy.CopyOptions.MonitorSourceChangesLimit.ToString();
            MonitorSourceTimeLimit.Text = copy.CopyOptions.MonitorSourceTimeLimit.ToString();

            // select options
            OnlyCopyArchiveFiles.IsChecked = copy.SelectionOptions.OnlyCopyArchiveFiles;
            OnlyCopyArchiveFilesAndResetArchiveFlag.IsChecked = copy.SelectionOptions.OnlyCopyArchiveFilesAndResetArchiveFlag;
            IncludeAttributes.Text = copy.SelectionOptions.IncludeAttributes;
            ExcludeAttributes.Text = copy.SelectionOptions.ExcludeAttributes;
            ExcludeFiles.Text = copy.SelectionOptions.ExcludeFiles;
            ExcludeDirectories.Text = copy.SelectionOptions.ExcludeDirectories;
            ExcludeOlder.IsChecked = copy.SelectionOptions.ExcludeOlder;
            ExcludeJunctionPoints.IsChecked = copy.SelectionOptions.ExcludeJunctionPoints;

            // retry options
            RetryCount.Text = copy.RetryOptions.RetryCount.ToString();
            RetryWaitTime.Text = copy.RetryOptions.RetryWaitTime.ToString();

            // logging options
            VerboseOutput.IsChecked = copy.LoggingOptions.VerboseOutput;
            NoFileSizes.IsChecked  = copy.LoggingOptions.NoFileSizes;
            NoProgress.IsChecked  = copy.LoggingOptions.NoProgress;
        }

        void DebugMessage(object sender, Debugger.DebugMessageArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public static bool IsInt(string text)
        {
            Regex regex = new Regex("[^0-9]+$", RegexOptions.Compiled);
            return !regex.IsMatch(text);
        }

        #endregion

        #region < Single Job Methods >

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            copy = GetCommand(true);
            copy.Start();
            OptionsGrid.IsEnabled = false;
            SingleJobExpander_Progress.IsExpanded = true;
            SingleJobExpander_JobHistory.IsExpanded = false;
            SingleJobExpander_Errors.IsExpanded = false;
            SingleJobTab.IsSelected = true;
            SingleJobExpander_Progress.ProgressGrid.IsEnabled = true;
        }

        void copy_OnCommandError(object sender, CommandErrorEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                MessageBox.Show(e.Error);
                OptionsGrid.IsEnabled = true;
                SingleJobExpander_Progress.ProgressGrid.IsEnabled = false;
            }));
        }

        void copy_OnError(object sender, ErrorEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                SingleJobErrors.Insert(0, new FileError { Error = e.Error });
                SingleJobExpander_Errors.Header = string.Format("Errors ({0})", SingleJobErrors.Count);
            }));
        }

        void copy_OnCommandCompleted(object sender, RoboCommandCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                OptionsGrid.IsEnabled = true;
                SingleJobExpander_Progress.ProgressGrid.IsEnabled = false;

                var results = e.Results;
                Console.WriteLine("Files copied: " + results.FilesStatistic.Copied);
                Console.WriteLine("Directories copied: " + results.DirectoriesStatistic.Copied);
                SingleJobResults.Add(e.Results);
            }));
        }



        #endregion

        #region < Multi-Job >

        #region < Button Events >

        private void btn_AddToQueue(object sender, RoutedEventArgs e)
        {
            if (!RoboQueue.IsRunning)
            {
                RoboQueue.AddCommand(GetCommand(false));
                btnStartJobQueue.IsEnabled = true;
                btnStartJobQueue_Copy.IsEnabled = true;
            }
        }

        private async void btn_StartQueue(object sender, RoutedEventArgs e)
        {
            if (!RoboQueue.IsRunning)
            {
                btnAddToQueue.IsEnabled = false;
                btnRemoveSelectedJob.IsEnabled = false;
                btnRemoveSelectedJob_Copy.IsEnabled = false;
                btnReplaceSelected.IsEnabled = false;
                btnUPdateSelectedJob_Copy.IsEnabled = false;

                btnStartJobQueue.IsEnabled = false;
                btnStartJobQueue.Content = "Stop Queued Jobs";

                btnStartJobQueue_Copy.IsEnabled = false;
                btnStartJobQueue_Copy.Content = "Stop Queued Jobs";

                btnPauseQueue.IsEnabled = true;

                RoboQueueProgressStackPanel.Children.Clear();

                if (chkListOnly.IsChecked == true)
                    await RoboQueue.StartAll_ListOnly();
                else
                    await RoboQueue.StartAll();

                btnPauseQueue.IsEnabled = false;
                btnAddToQueue.Content = "Add to Queue";
                btnStartJobQueue.Content = "Start Queued Jobs";
                btnStartJobQueue_Copy.Content = "Start Queued Jobs";

                btnAddToQueue.IsEnabled = true;
                btnRemoveSelectedJob.IsEnabled = true;
                btnRemoveSelectedJob_Copy.IsEnabled = true;
                btnReplaceSelected.IsEnabled = true;
                btnUPdateSelectedJob_Copy.IsEnabled = true;
            }
            else
                RoboQueue.StopAll();
        }

        private void btn_PauseResumeQueue(object sender, RoutedEventArgs e)
        {
            if (RoboQueue.IsRunning)
                RoboQueue.PauseAll();
            else
                RoboQueue.ResumeAll();
        }

        /// <summary>
        /// Remove RoboCommand from the list
        /// </summary>
        private void btn_RemoveSelected(object sender, RoutedEventArgs e)
        {
            var cmd = (RoboCommand)ListBox_RoboQueueJobs_MultiJobPage.SelectedItem;
            RoboQueue.RemoveCommand(cmd);
        }

        /// <summary>
        /// Load into Options Page
        /// </summary>
        private void btn_LoadSelected(object sender, RoutedEventArgs e)
        {
            LoadCommand((RoboCommand)ListBox_RoboQueueJobs_MultiJobPage.SelectedItem);
        }

        /// <summary>
        /// Replace item in list with new RoboCommand
        /// </summary>
        private void btn_ReplaceSelected(object sender, RoutedEventArgs e)
        {
            int i = ListBox_RoboQueueJobs_MultiJobPage.SelectedIndex;
            if (i >= 0)
                RoboQueue.ReplaceCommand(GetCommand(false), i);
            else
                RoboQueue.AddCommand(GetCommand(false));
        }

        #endregion

        #region < Progress Estimator >

        private void RoboQueue_OnProgressEstimatorCreated(RoboQueue sender, Results.ProgressEstimatorCreatedEventArgs e)
        {
            e.ResultsEstimate.DirStats.OnTotalChanged += DirectoriesStatistic_PropertyChanged;
            e.ResultsEstimate.FileStats.OnTotalChanged += FilesStatistic_PropertyChanged;
            e.ResultsEstimate.ByteStats.OnTotalChanged += BytesStatistic_PropertyChanged;
        }

        private void DirectoriesStatistic_PropertyChanged(Results.Statistic sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateLabel(ProgressEstimator_Directories, sender);
        private void FilesStatistic_PropertyChanged(Results.Statistic sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateLabel(ProgressEstimator_Files, sender);
        private void BytesStatistic_PropertyChanged(Results.Statistic sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateLabel(ProgressEstimator_Bytes, sender);

        private void UpdateLabel(TextBlock lbl, RoboSharp.Results.Statistic stat)
        {
            Dispatcher.Invoke(() => {
                    lbl.Text = stat?.ToString(true, true, "\n", true) ?? "";
                });
        }

        #endregion

        #region < RoboQueue & Command Events >

        private void UpdateCommandsRunningBox()
        {
            Dispatcher.Invoke(() => MultiJob_JobRunningCount.Text = $"{RoboQueue.JobsCurrentlyRunning}");
        }

        /// <summary>
        /// Add MultiJob_CommandProgressIndicator to window
        /// </summary>
        private void RoboQueue_OnCommandStarted(RoboQueue sender, RoboQueue.CommandStartedEventArgs e)
        {
            MultiJob_CommandProgressIndicator ProgressIndicator = new MultiJob_CommandProgressIndicator(e.Command);
            Dispatcher.Invoke(() => RoboQueueProgressStackPanel.Children.Add(ProgressIndicator));
            UpdateCommandsRunningBox();
        }

        /// <summary>
        /// Disable associated MultiJob_CommandProgressIndicator to window
        /// </summary>
        private void RoboQueue_OnCommandCompleted(RoboCommand sender, RoboCommandCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
           {
               foreach (UIElement element in RoboQueueProgressStackPanel.Children)
               {
                   if (element.GetType() == typeof(MultiJob_CommandProgressIndicator))
                   {
                       var el = (MultiJob_CommandProgressIndicator)element;
                       if (el.Command == sender)
                           el.IsEnabled = false;
                   }
               }
           });
            UpdateCommandsRunningBox();
            Dispatcher.Invoke(() => MultiJob_JobsCompleteXofY.Text = $"{RoboQueue.JobsComplete} of {RoboQueue.ListCount}");
        }

        /// <summary>
        /// Removes unneeded FirstMultiProgressExpander controls
        /// </summary>
        private void RoboQueue_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    // Remove all FirstMultiProgressExpander
                    RoboQueueProgressStackPanel.Children.Clear();
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    //Remove associated MultiJob_CommandProgressIndicator
                    foreach (UIElement element in RoboQueueProgressStackPanel.Children)
                    {
                        if (element.GetType() == typeof(MultiJob_CommandProgressIndicator))
                        {
                            var el = (MultiJob_CommandProgressIndicator)element;
                            if (e.OldItems.Contains(el.Command))
                                RoboQueueProgressStackPanel.Children.Remove(element);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Log the Error to the Errors expander
        /// </summary>
        private void RoboQueue_OnError(RoboCommand sender, ErrorEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                MultiJobErrors.Insert(0, new FileError { Error = e.Error });
                MultiJobExpander_Errors.Header = string.Format("Errors ({0})", MultiJobErrors.Count);
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Occurs while a command is starting prior to Robocopy starting (for example, due to missing source location), but won't break the entire RoboQueue. 
        /// That single job will just not start, all others will.
        /// </remarks>
        private void RoboQueue_OnCommandError(RoboCommand sender, CommandErrorEventArgs e)
        {
            // TO DO: FIgure Out what to do
        }

        #endregion

        #endregion

        #region < Options Page >

        private void SourceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            Source.Text = dialog.SelectedPath;
        }

        private void DestinationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            Destination.Text = dialog.SelectedPath;
        }

        #endregion

        #region < Form Stuff >

        private void chkListOnly_Checked(object sender, RoutedEventArgs e)
        {
            chkListOnly.IsChecked = true;
            chkListOnly_Copy.IsChecked = true;
        }

        private void chkListOnly_UnChecked(object sender, RoutedEventArgs e)
        {
            chkListOnly.IsChecked = false;
            chkListOnly_Copy.IsChecked = false;
        }

        private void MultiJob_ConcurrentAmountChanged(object sender, RoutedEventArgs e)
        {
            RoboQueue.MaxConcurrentJobs = (int)((ComboBox)sender).SelectedItem;
            cmbConcurrentJobs_OptionsPage.SelectedItem = RoboQueue.MaxConcurrentJobs;
            cmbConcurrentJobs_MultiJobPage.SelectedItem = RoboQueue.MaxConcurrentJobs;
        }

        private void RoboQueueListBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            var LB = (ListBox)sender;
            ListBox_RoboQueueJobs_OptionsPage.SelectedIndex = LB.SelectedIndex;
            ListBox_RoboQueueJobs_MultiJobPage.SelectedIndex = LB.SelectedIndex;
        }

        private void IsNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsInt(e.Text);
        }

        private void IsAttribute_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[a-zA-Z]+$", RegexOptions.Compiled))
                e.Handled = true;
            if ("bcefghijklmnpqrvwxyzBCEFGHIJKLMNPQRVWXYZ".Contains(e.Text))
                e.Handled = true;
            if (((TextBox)sender).Text.Contains(e.Text))
                e.Handled = true;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        #endregion
        
    }

    public class FileError
    {
        public string Error { get; set; }
    }
}
