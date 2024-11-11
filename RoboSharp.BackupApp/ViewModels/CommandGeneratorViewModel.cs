using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoboSharp.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoboSharp.BackupApp.ViewModels
{
    internal partial class CommandGeneratorViewModel : ObservableObject
    {
        public CommandGeneratorViewModel() 
        {
            ResetOptions();
            this.PropertyChanged += PropertyChangedHandler;
            System.Windows.Input.CommandManager.RequerySuggested += CommandManager_RequerySuggested;

        }
        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            this.BtnSelectDestinationCommand.NotifyCanExecuteChanged();
            this.BtnSelectSourceCommand.NotifyCanExecuteChanged();
            this.BtnParseCommandCommand.NotifyCanExecuteChanged();
            this.BtnParseCommandOptionsCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty] private IRoboCommand _command;
        [ObservableProperty] private string runHoursStart;
        [ObservableProperty] private string runHoursEnd;
        [ObservableProperty] private string _jobNameTextbox;
        [ObservableProperty] private bool _configurationEnableFileLogging;

        [RelayCommand]
        private void ResetOptions()
        {
            Command = new RoboCommand();
            Command.LoggingOptions.VerboseOutput = true;
            JobNameTextbox = string.Empty;
        }

        public IRoboCommand GetCommand()
        {
            UpdateCommandName();
            return new RoboCommand(Command);
        }

        private void UpdateCommandName()
        {
            if (Command is RoboCommand rc)
            {
                rc.Name = JobNameTextbox;
            }
            else if (Command is JobFile jf)
            {
                jf.Name = JobNameTextbox;
            }
        }

        public void LoadCommand(IRoboCommand cmd)
        {
            Command = cmd;
            JobNameTextbox = cmd.Name;
        }

        [RelayCommand]
        private void BtnSelectSource()
        {
            var result = SelectDirectory();
            if (result.Item1)
                Command.CopyOptions.Source = result.Item2;
            OnPropertyChanged(nameof(Command));
        }

        [RelayCommand]
        private void BtnSelectDestination()
        {
            var result = SelectDirectory();
            if (result.Item1)
                Command.CopyOptions.Destination = result.Item2;
            OnPropertyChanged(nameof(Command));
        }

        public static (bool, string) SelectDirectory()
        {
            var diag = new System.Windows.Forms.FolderBrowserDialog()
            {
                ShowNewFolderButton = true,
            };
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return (true, diag.SelectedPath);
            else
                return (false, diag.SelectedPath);
        }

        private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (Command is null) return;
            switch (e.PropertyName)
                {
                case nameof(JobNameTextbox):
                    UpdateCommandName();
                    break;
                case nameof(Command):
                    if (string.IsNullOrEmpty(Command?.CopyOptions?.RunHours))
                    {
                        RunHoursStart = string.Empty;
                        RunHoursEnd = string.Empty;
                    }
                    else
                    {
                        var rh = Command.CopyOptions?.RunHours.Split('-');
                        if (rh.Length >= 1)
                            RunHoursStart = rh[0];
                        else
                            RunHoursStart = string.Empty;

                        if (rh.Length >= 2)
                            RunHoursEnd = rh[1];
                        else
                            RunHoursEnd = string.Empty;
                    }
                    break;

                case nameof(RunHoursStart):
                case nameof(RunHoursEnd):
                    string runHours = $"{RunHoursStart}-{RunHoursEnd}";
                    if (CopyOptions.IsRunHoursStringValid(runHours))
                        Command.CopyOptions.RunHours = runHours;
                    break;
            }
        }

        #region < Parse Buttons >

        [ObservableProperty] private string _parseCommandText;
        [ObservableProperty] private string _parseCommandOptionsText;

        [RelayCommand(CanExecute = nameof(CanParseCommand))]
        private void BtnParseCommand()
        {
            try
            {
                LoadCommand(RoboCommandParser.Parse(ParseCommandText));
            }
            catch (Exception ex)
            {
                StringBuilder text = new StringBuilder();
                text.Append(ex.Message);
                foreach (DictionaryEntry item in ex.Data)
                    text.Append(string.Format("\n{0} : {1}", item.Key, item.Value));
                MessageBox.Show(text.ToString(), "RoboCommandParser Error!");
            }
        }
        private bool CanParseCommand() => !string.IsNullOrWhiteSpace(ParseCommandText);

        [RelayCommand(CanExecute = nameof(CanParseCommandOptions))]
        private void BtnParseCommandOptions()
        {
            try
            {
                LoadCommand(RoboCommandParser.ParseOptions(ParseCommandOptionsText));
            }
            catch (Exception ex)
            {
                StringBuilder text = new StringBuilder();
                text.Append(ex.Message);
                foreach (DictionaryEntry item in ex.Data)
                    text.Append(string.Format("\n{0} : {1}", item.Key, item.Value));
                MessageBox.Show(text.ToString(), "RoboCommandParser Error!");
            }
        }
        private bool CanParseCommandOptions() => !string.IsNullOrWhiteSpace(ParseCommandOptionsText);

        #endregion
    }
}
