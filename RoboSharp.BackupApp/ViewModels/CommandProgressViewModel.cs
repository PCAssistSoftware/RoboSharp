using CommunityToolkit.Mvvm.ComponentModel;
using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace RoboSharp.BackupApp.ViewModels
{
    internal partial class CommandProgressViewModel : ObservableObject
    {
        [ObservableProperty] private bool isRunning;
        [ObservableProperty] private string pauseResumeButtonContent = "Pause";
        [ObservableProperty] private string progressEstimatorFiles;
        [ObservableProperty] private string progressEstimatorBytes;
        [ObservableProperty] private string progressEstimatorDirs;
        [ObservableProperty] double progressBarPercentage;
        [ObservableProperty] string progressBarText;
        [ObservableProperty] string currentFile;
        [ObservableProperty] string currentOperation;
        [ObservableProperty] string currentSize;

        [ObservableProperty] bool enableProgressGrid;
        [ObservableProperty] bool debugMode;
        [ObservableProperty] string errorsHeader = "Errors";

        private IRoboCommand command;
        private string jobName;
        private List<string> Dirs;
        private List<string> Files;
        private List<string> Dirs2;
        private List<string> Files2;
        private List<string> OrderLog_1;
        private List<string> OrderLog_2;

        public ObservableCollection<string> Errors { get; } = new ObservableCollection<string>();

        public IRoboCommand Command => command;

        public string JobName
        {
            get => jobName;
            set
            {
                if (value != jobName)
                {
                    var tmp = String.IsNullOrWhiteSpace(value) ? "" : value;
                    SetProperty(ref jobName, $"Progress{(tmp == "" ? "" : $" - {tmp}")}");
                }
            }
        }

        public CommandProgressViewModel() { }

        public CommandProgressViewModel(IRoboCommand command) => BindToCommand(command);
        public CommandProgressViewModel(IRoboQueue cmd)
        {
            cmd.OnProgressEstimatorCreated += OnProgressEstimatorCreated;
            cmd.OnError += Cmd_OnError;
            cmd.OnCommandError += Cmd_OnCommandError;
            cmd.OnFileProcessed += OnFileProcessed;
            cmd.OnCommandCompleted += OnCommandCompleted;
        }

        /// <summary>
        /// This command loads in the <see cref="IRoboCommand"/> and starts it
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public void BindToCommand(IRoboCommand cmd)
        {
            IsRunning = cmd?.IsRunning ?? false;

            OnPropertyChanging(nameof(Command));
            command = cmd;
            OnPropertyChanged(nameof(Command));

            JobName = cmd?.Name ?? string.Empty;
            if (cmd is not null)
            {
                if (cmd.IProgressEstimator != null)
                {
                    BindToProgressEstimator(cmd.IProgressEstimator);
                }
                else
                {
                    cmd.OnProgressEstimatorCreated += OnProgressEstimatorCreated;
                }
                if (DebugMode == true)
                {
                    SetupListObjs();
                }

                cmd.OnError += Cmd_OnError;
                cmd.OnCommandError += Cmd_OnCommandError;

                cmd.OnCopyProgressChanged += OnCopyProgressChanged;
                cmd.OnFileProcessed += OnFileProcessed;
                cmd.OnCommandCompleted += OnCommandCompleted;
            }
        }

        public void UnBindFromCommand(IRoboCommand cmd)
        {   
            cmd.OnCommandError -= Cmd_OnCommandError;
            cmd.OnError -= Cmd_OnError;
            cmd.OnCopyProgressChanged -= OnCopyProgressChanged;
            cmd.OnFileProcessed -= OnFileProcessed;
            cmd.OnCommandCompleted -= OnCommandCompleted;
        }

        private void Cmd_OnCommandError(IRoboCommand sender, CommandErrorEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { System.Windows.MessageBox.Show(e.Error); });
        }

        private void Cmd_OnError(IRoboCommand sender, ErrorEventArgs e)
        {
            Errors.Add(e.Error);
            ErrorsHeader = $"Errors ({Errors.Count})";
        }

        private void SetupListObjs()
        {
            Dirs = new List<string> { "Dirs Reported by OnFileProcessed", "---------------------" };
            Files = new List<string> { "Files Reported by OnFileProcessed", "---------------------" };
            Dirs2 = new List<string> { "Unique Dirs Reported by CopyProgressChanged", "---------------------" };
            Files2 = new List<string> { "Unique Files Reported by CopyProgressChanged", "---------------------" };
            OrderLog_1 = new List<string> { "Files and Dirs In Order Reported by OnFileProcessed", "---------------------" };
            OrderLog_2 = new List<string> { "Files and Dirs In Order Reported by CopyProgressChanged", "---------------------" };
        }

        #region < Buttons >

        [RelayCommand(CanExecute = nameof(CanExecuteAgainstCommand))]
        private void CancelButton_Click() => Command?.Stop();

        [RelayCommand(CanExecute = nameof(CanExecuteAgainstCommand))]
        private void PauseResumeButton_Click()
        {
            if (Command != null)
            {
                if (!Command.IsPaused)
                {
                    Command.Pause();
                    PauseResumeButtonContent = "Resume";
                }
                else
                {
                    Command.Resume();
                    PauseResumeButtonContent = "Pause";
                }
            }
        }

        
        private bool CanExecuteAgainstCommand() => Command != null;

        #endregion

        #region < Progress Estimator >

        /// <summary> Bind the ProgressEstimator to the text controls on the PROGRESS tab </summary>
        private void OnProgressEstimatorCreated(object sender, EventArgObjects.ProgressEstimatorCreatedEventArgs e) => BindToProgressEstimator(e.ResultsEstimate);

        private void BindToProgressEstimator(IProgressEstimator e)
        {
            ProgressEstimatorBytes = "Bytes";
            ProgressEstimatorDirs = "Directories";
            ProgressEstimatorFiles = "Files";
            e.ValuesUpdated += IProgressEstimatorValuesUpdated;
        }

        private void IProgressEstimatorValuesUpdated(IProgressEstimator sender, EventArgObjects.IProgressEstimatorUpdateEventArgs e)
        {
            ProgressEstimatorBytes = e.BytesStatistic.ToString(true, true, "\n", true);
            ProgressEstimatorDirs = e.DirectoriesStatistic.ToString(true, true, "\n", true);
            ProgressEstimatorFiles = e.FilesStatistic.ToString(true, true, "\n", true);
        }

        #endregion

        #region < On*Processed >

        private static string DirString(ProcessedFileInfo pf) => pf.FileClass + "(" + pf.Size + ") - " + pf.Name;
        private static string FileString(ProcessedFileInfo pf) => pf.FileClass + "(" + pf.Size + ") - " + pf.Name;


        void OnCopyProgressChanged(object sender, CopyProgressEventArgs e)
        {
            ProgressBarPercentage = e.CurrentFileProgress;
            ProgressBarText = $"{e.CurrentFileProgress:n2}%";// string.Format("{0:d2}%", e.CurrentFileProgress);
            
            if (DebugMode == true)
            {
                if (e.CurrentDirectory != null)
                {
                    var dirString = DirString(e.CurrentDirectory);
                    if (Dirs2.Count == 0 || dirString != Dirs2.Last())
                    {
                        Dirs2.Add(dirString);
                        OrderLog_2.Add(Environment.NewLine + DirString(e.CurrentDirectory));
                    }
                }
                if (e.CurrentFile != null)
                {
                    var fileString = FileString(e.CurrentFile);
                    if (Files2.Count == 0 || fileString != Files2.Last())
                    {
                        Files2.Add(fileString);
                        OrderLog_2.Add(FileString(e.CurrentFile));
                    }
                }
            }
        }

        void OnFileProcessed(object sender, FileProcessedEventArgs e)
        {
            
            IsRunning = true;
            CurrentOperation = e.ProcessedFile.FileClass;
            CurrentFile = e.ProcessedFile.Name;
            CurrentSize = e.ProcessedFile.Size.ToString();

            if (DebugMode == true)
            {
                if (e.ProcessedFile.FileClassType == FileClassType.NewDir)
                {
                    Dirs.Add(DirString(e.ProcessedFile));
                    OrderLog_1.Add(Environment.NewLine + DirString(e.ProcessedFile));
                }
                else if (e.ProcessedFile.FileClassType == FileClassType.File)
                {
                    Files.Add(FileString(e.ProcessedFile));
                    OrderLog_1.Add(FileString(e.ProcessedFile));
                }
            }
        }

        void OnCommandCompleted(object sender, RoboCommandCompletedEventArgs e)
        {
            IsRunning = false;
            EnableProgressGrid = false;
            var results = e.Results;
            Console.WriteLine("Files copied: " + results.FilesStatistic.Copied);
            Console.WriteLine("Directories copied: " + results.DirectoriesStatistic.Copied);

            Command.OnProgressEstimatorCreated -= OnProgressEstimatorCreated;
            Command.OnCopyProgressChanged -= OnCopyProgressChanged;
            Command.OnFileProcessed -= OnFileProcessed;
            Command.OnCommandCompleted -= OnCommandCompleted;

            try
            {
                if (DebugMode == true)
                {
                    var source = new DirectoryInfo(Command.CopyOptions.Source);
                    string path = System.IO.Path.Combine(source.Parent.FullName, "EventLogs") + "\\";
                    var PathDir = Directory.CreateDirectory(path);

                    Dirs.Add(""); Dirs.Add(""); Dirs.AddRange(Files);
                    File.AppendAllLines($"{path}{Command.Name}_OnFileProcessed.txt", Dirs);

                    Dirs2.Add(""); Dirs2.Add(""); Dirs2.AddRange(Files2);
                    File.AppendAllLines($"{path}{Command.Name}_CopyProgressChanged.txt", Dirs2);

                    File.AppendAllLines($"{path}{Command.Name}_OnFileProcessed_InOrder.txt", OrderLog_1);
                    File.AppendAllLines($"{path}{Command.Name}_CopyProgressChanged_InOrder.txt", OrderLog_2);
                }
                Directory.SetCurrentDirectory("C:\\");
            }
            catch { }
        }
        #endregion
    }
}
