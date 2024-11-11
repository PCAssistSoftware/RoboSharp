using RoboSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Security.Principal;
using System.Diagnostics;
using System.IO;
using RoboSharp.Interfaces;
using RoboSharp.Extensions.Windows;

namespace RoboSharp.BackupApp.ViewModels
{
    enum IFileCopiersEnum
    {
        StreamedCopier,
        CopyFileEx
    }

    internal partial class BatchCommandViewModel : ObservableObject
    {
        public BatchCommandViewModel()
        {
            System.Windows.Input.CommandManager.RequerySuggested += CommandManager_RequerySuggested;
            this.PropertyChanged += BatchCommandViewModel_PropertyChanged;
        }

        private void BatchCommandViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedFilePair):
                    RemoveSelectedFilePairCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(SourcePath):
                case nameof(DestinationDirectoryPath):
                case nameof(DestinationFileName):
                    AddFilePairCommand.NotifyCanExecuteChanged();
                    break;
            }
        }

        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            RunCommandCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty] private IFilePair selectedFilePair;
        [ObservableProperty] private string sourcePath;
        [ObservableProperty] private string destinationFileName;
        [ObservableProperty] private string destinationDirectoryPath;
        [ObservableProperty] private int option_RetryPeriod = 30;
        [ObservableProperty] private int option_RetryCount = 1;
        [ObservableProperty] private string option_LogPath;
        [ObservableProperty] private bool option_RunAsFileMover;
        [ObservableProperty] private bool option_ExcludeNewer;
        [ObservableProperty] private bool option_ExcludeOlder;
        [ObservableProperty] private bool option_ListOnly;
        [ObservableProperty] private bool option_FullPathNames;
        [ObservableProperty] private bool option_NoSummary;
        [ObservableProperty] private bool option_NoHeader;
        [ObservableProperty] private bool option_NoFileList;
        [ObservableProperty] private int streamedCopierBufferSize = StreamedCopier.DefaultBufferSize;

        public RadioButtonHelper[] IFileCopiers { get; } = new RadioButtonHelper[]
        {
            new RadioButtonHelper(IFileCopiersEnum.StreamedCopier) {IsChecked = true},
            new RadioButtonHelper(IFileCopiersEnum.CopyFileEx)
        };

        public ObservableCollection<IFilePair> FilePairs { get; } = new ObservableCollection<IFilePair>();
        public CommandProgressViewModel CommandProgress { get; } = new CommandProgressViewModel();
        public JobResultsViewModel LastJobResults { get; } = new JobResultsViewModel();

        /// <summary>
        /// This command generates the BatchCommand, and passes it over to the Progress View Model to run it
        /// </summary>
        /// <returns></returns>
        [RelayCommand(CanExecute = nameof(CanRunCommand))]
        private async Task RunCommand()
        {
            IFileCopierFactory factory = IFileCopiers.First(rb => rb.IsChecked).Name switch
            {
                nameof(IFileCopiersEnum.StreamedCopier) => new StreamedCopierFactory() { BufferSize = StreamedCopierBufferSize },
                nameof(IFileCopiersEnum.CopyFileEx) => new RoboSharp.Extensions.Windows.CopyFileExFactory(),
                _ => throw new NotImplementedException("Unkown Factory Type")
            };

            var cmd = new BatchCommand(factory);
            cmd.Configuration.EnableFileLogging = true;
            cmd.AddCopiers(FilePairs);
            //ClearFilePairs();
            CommandProgress.BindToCommand(cmd);
            cmd.CopyOptions.MoveFiles = Option_RunAsFileMover;
            
            cmd.RetryOptions.RetryCount = Option_RetryCount;
            cmd.RetryOptions.RetryWaitTime = Option_RetryPeriod;

            if (string.IsNullOrWhiteSpace(Option_LogPath) || RoboCommandParser.IsPathFullyQualified(Option_LogPath))
                cmd.LoggingOptions.LogPath = Option_LogPath ?? string.Empty;
            else
                cmd.LoggingOptions.LogPath = string.Empty;

            cmd.LoggingOptions.ListOnly = Option_ListOnly;
            cmd.LoggingOptions.IncludeFullPathNames = Option_FullPathNames;
            cmd.LoggingOptions.NoJobHeader = Option_NoHeader;
            cmd.LoggingOptions.NoJobSummary = Option_NoSummary;
            cmd.LoggingOptions.NoFileList = Option_NoFileList;
            try
            {
                await cmd.Start();
            }
            finally
            {
                LastJobResults.BindToResults(cmd.GetResults());
            }
            RunCommandCommand.NotifyCanExecuteChanged();
        }
        private bool CanRunCommand() => !CommandProgress.IsRunning && FilePairs.Count > 0;

        [RelayCommand]
        private void ClearFilePairs() { FilePairs.Clear(); }

        [RelayCommand(CanExecute = nameof(CanAddFilePair))]
        private void AddFilePair()
        {
            FilePairs.Add(new FilePair(SourcePath, Path.Combine(DestinationDirectoryPath, DestinationFileName)));
            SourcePath = string.Empty;
            DestinationFileName = string.Empty;
        }
        private bool CanAddFilePair() 
            => RoboCommandParser.IsPathFullyQualified(SourcePath)
            && !String.IsNullOrWhiteSpace(Path.GetFileName(SourcePath))
            && RoboCommandParser.IsPathFullyQualified(DestinationDirectoryPath) 
            && !String.IsNullOrWhiteSpace(DestinationFileName);

        [RelayCommand(CanExecute =nameof(IsItemSelected))]
        private void RemoveSelectedFilePair() => FilePairs.Remove(SelectedFilePair);
        private bool IsItemSelected() => !(SelectedFilePair is null);

        [RelayCommand]
        private void SelectSource()
        {
            var result = SelectFile();
            if (result.Item1)
            {
                SourcePath = result.Item2;
                DestinationFileName = Path.GetFileName(result.Item2);
            }
        }

        [RelayCommand]
        private void SelectDestinationDirectory()
        {
            var result = SelectDirectory();
            if (result.Item1)
            {
                DestinationDirectoryPath = result.Item2;
            }
        }

        private (bool,string) SelectFile()
        {
            var diag = new System.Windows.Forms.OpenFileDialog()
            {
                Multiselect = false
            };
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return (true, diag.FileName);
            else
                return (false, diag.FileName);
        }

        private (bool, string) SelectDirectory()
        {
            return ViewModels.CommandGeneratorViewModel.SelectDirectory();
        }
    }
}
