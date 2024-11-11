using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using RoboSharp.Interfaces;

namespace RoboSharp.BackupApp.ViewModels
{
    internal partial class JobHistoryViewModel : ObservableObject
    {
        public JobHistoryViewModel()
        {
            UpdateDescriptionLblText();
            OverallResults.BindToResultsList(_collection);
            System.Windows.Input.CommandManager.RequerySuggested += CommandManager_RequerySuggested;
        }

        public JobHistoryViewModel(IRoboCopyResultsList resultsList) : this()
        {
            OverallResults.BindToResultsList(resultsList);
            foreach (var result in resultsList)
            {
                ResultsCollection.Add(new JobResultsViewModel(result));
            }
        }

        private readonly Results.RoboCopyResultsList _collection = new Results.RoboCopyResultsList();
        private JobResultsViewModel _selectedItem;
        [ObservableProperty] string _expanderHeaderText;
        [ObservableProperty] string _selectedJobHeader;

        public ObservableCollection<JobResultsViewModel> ResultsCollection { get; } = new ObservableCollection<JobResultsViewModel>();
        public JobResultsViewModel OverallResults { get; } = new JobResultsViewModel();

        public JobResultsViewModel SelectedItem { 
            get => _selectedItem;
            set
            {
                OnPropertyChanging(nameof(SelectedItem));
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                SelectedJobHeader = value is null ? $"Selected Job Results" : $"Selected Job Results: {value.JobName}";
            }
        }

        public void AddResults(IResults results)
        {
            _collection.Add(results as Results.RoboCopyResults);
            ResultsCollection.Add(new JobResultsViewModel(results));
        }

        [RelayCommand(CanExecute = nameof(IsItemSelected))]
        void RemoveSelectedItem()
        {
            _collection.Remove(SelectedItem.Results as Results.RoboCopyResults);
            ResultsCollection.Remove(SelectedItem);
        }
        bool IsItemSelected() => SelectedItem != null;

        public void UpdateDescriptionLblText(string text = "This contains a list of the results from all previous runs during this session.")
        {
            ExpanderHeaderText = text;
        }

        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            RemoveSelectedItemCommand.NotifyCanExecuteChanged();
        }
    }
}
