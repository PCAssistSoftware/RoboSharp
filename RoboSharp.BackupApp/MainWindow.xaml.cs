using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using Microsoft.Win32;
using RoboSharp.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Collections;
using RoboSharp.BackupApp.Views;

namespace RoboSharp.BackupApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            
            this.Closing += MainWindow_Closing;
            var v = VersionManager.Version;

            //UnitTests
            new ObservableListTester().RunTest(); // Test ObservableList works properly

            this.DataContext = new ViewModels.MainWindowViewModel();

            CommandManager.RequerySuggested += CommandManager_RequerySuggested;
            //MultiJob_ListOnlyResults.UpdateDescriptionLblText("List of results from this List-Only Operation.\nThis list is reset every time the queue is restarted.");
            //MultiJob_RunResults.UpdateDescriptionLblText("List of results from this Copy/Move Operation.\nThis list is reset every time the queue is restarted.");
        }

        private void CommandManager_RequerySuggested(object sender, EventArgs e)
        {
            
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }

        [DebuggerHidden()]
        void DebugMessage(object sender, Debugger.DebugMessageArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public static bool IsInt(string text)
        {
            Regex regex = new Regex("[^0-9]+$", RegexOptions.Compiled);
            return !regex.IsMatch(text);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Moves to other window
            SingleJobExpander_Progress.IsExpanded = true;
            SingleJobExpander_Progress.IsEnabled = true;
            SingleJobExpander_JobHistory.IsExpanded = false;
            SingleJobExpander_Errors.IsExpanded = false;
            SingleJobTab.IsSelected = true;
        }
        private void RoboQueueStart_Click(object sender, RoutedEventArgs e)
        {
            MultiJobProgressTab.IsSelected = true;
            MultiJobExpander_Progress.IsExpanded = true;
        }

        private void RefreshQueueListBoxes()
        {
            if (RoboQueueListBox1.HasItems)
            {
                RoboQueueListBox1.Items.Refresh();
                RoboQueueListBox2.Items.Refresh();
            }
        }
        private void RefreshQueueListbox(object sender, RoutedEventArgs e) => RefreshQueueListBoxes();
        private void RefreshRoboQueueListBox(object sender, TextChangedEventArgs e) => RefreshQueueListBoxes();

        private void IsNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsInt(e.Text);
        }

        private void IsAttribute_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[a-zA-Z]+$", RegexOptions.Compiled))
                e.Handled = true;
            if ("bdfgijklmopquvwxyzBDFGIJKLMOPQUVWXYZ".Contains(e.Text))
                e.Handled = true;
            if (((TextBox)sender).Text.Contains(e.Text))
                e.Handled = true;
        }

        private void IsCopyFlag_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[a-zA-Z]+$", RegexOptions.Compiled))
                e.Handled = true;
            if ("bcefghijklmnpqrvwxyzBCEFGHIJKLMNPQRVWXYZ".Contains(e.Text))
                e.Handled = true;
            if (((TextBox)sender).Text.Contains(e.Text))
                e.Handled = true;
        }
    }
}
