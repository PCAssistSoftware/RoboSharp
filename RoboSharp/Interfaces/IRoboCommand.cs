using System.Threading.Tasks;

namespace RoboSharp.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/IRoboCommand"/>
    /// </remarks>
    public interface IRoboCommand
    {
        #region Properties

        /// <inheritdoc cref="RoboCommand.Name"/>
        string Name { get; }

        /// <inheritdoc cref="RoboCommand.IsPaused"/>
        bool IsPaused { get; }

        /// <inheritdoc cref="RoboCommand.IsRunning"/>
        bool IsRunning { get; }

        /// <inheritdoc cref="RoboCommand.IsScheduled"/>
        bool IsScheduled{ get; }

        /// <inheritdoc cref="RoboCommand.IsCancelled"/>
        bool IsCancelled { get; }

        /// <inheritdoc cref="RoboCommand.StopIfDisposing"/>
        bool StopIfDisposing { get; }

        /// <inheritdoc cref="RoboCommand.ProgressEstimator"/>
        IProgressEstimator IProgressEstimator { get; }

        /// <inheritdoc cref="RoboCommand.CommandOptions"/>
        string CommandOptions { get; }
        
        /// <inheritdoc cref="RoboCommand.CopyOptions"/>
        CopyOptions CopyOptions { get; set; }
        
        /// <inheritdoc cref="RoboCommand.SelectionOptions"/>
        SelectionOptions SelectionOptions { get; set; }
        
        /// <inheritdoc cref="RoboCommand.RetryOptions"/>
        RetryOptions RetryOptions { get; set; }
        
        /// <inheritdoc cref="RoboCommand.LoggingOptions"/>
        LoggingOptions LoggingOptions { get; set; }

        /// <inheritdoc cref="RoboCommand.JobOptions"/>
        JobOptions JobOptions{ get; }

        /// <inheritdoc cref="RoboCommand.Configuration"/>
        RoboSharpConfiguration Configuration { get; }

        #endregion Properties

        #region Events

        /// <inheritdoc cref="RoboCommand.OnFileProcessed"/>
        event RoboCommand.FileProcessedHandler OnFileProcessed;
        
        /// <inheritdoc cref="RoboCommand.OnCommandError"/>
        event RoboCommand.CommandErrorHandler OnCommandError;
        
        /// <inheritdoc cref="RoboCommand.OnError"/>
        event RoboCommand.ErrorHandler OnError;
        
        /// <inheritdoc cref="RoboCommand.OnCommandCompleted"/>
        event RoboCommand.CommandCompletedHandler OnCommandCompleted;
        
        /// <inheritdoc cref="RoboCommand.OnCopyProgressChanged"/>
        event RoboCommand.CopyProgressHandler OnCopyProgressChanged;

        /// <inheritdoc cref="RoboCommand.OnProgressEstimatorCreated"/>
        event RoboCommand.ProgressUpdaterCreatedHandler OnProgressEstimatorCreated;

        /// <inheritdoc cref="RoboCommand.TaskFaulted"/>
        event System.UnhandledExceptionEventHandler TaskFaulted;

        #endregion Events

        #region Methods

        /// <inheritdoc cref="RoboCommand.Pause"/>
        void Pause();
        
        /// <inheritdoc cref="RoboCommand.Resume"/>
        void Resume();
        
        /// <inheritdoc cref="RoboCommand.Start(string, string, string)"/>
        Task Start(string domain = "", string username = "", string password = "");

        /// <inheritdoc cref="RoboCommand.Start_ListOnly(string, string, string)"/>
        Task Start_ListOnly(string domain = "", string username = "", string password = "");


        /// <inheritdoc cref="RoboCommand.GetResults"/>
        Results.RoboCopyResults GetResults();

        /// <inheritdoc cref="RoboCommand.Stop()"/>
        void Stop();
        
        /// <inheritdoc cref="RoboCommand.Dispose()"/>
        void Dispose();

        /// <inheritdoc cref="RoboCommand.StartAsync_ListOnly(string, string, string)"/>
        Task<Results.RoboCopyResults> StartAsync_ListOnly(string domain = "", string username = "", string password = "");

        /// <inheritdoc cref="RoboCommand.StartAsync(string, string, string)"/>
        Task<Results.RoboCopyResults> StartAsync(string domain = "", string username = "", string password = "");

        #endregion Methods
    }
}