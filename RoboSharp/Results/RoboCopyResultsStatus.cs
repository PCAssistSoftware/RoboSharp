using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RoboSharp.Interfaces;

namespace RoboSharp.Results
{
    /// <summary>
    /// Object that evaluates the ExitCode reported after RoboCopy finishes executing.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/PCAssistSoftware/RoboSharp/wiki/RoboCopyExitStatus"/>
    /// </remarks>
    public class RoboCopyExitStatus
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public RoboCopyExitStatus(int exitCodeValue)
        {
            ExitCodeValue = exitCodeValue;
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public RoboCopyExitStatus(RoboCopyExitCodes exitCodeValue)
        {
            ExitCodeValue = (int)exitCodeValue;
        }

        /// <summary>ExitCode as reported by RoboCopy</summary>
        public int ExitCodeValue { get; protected set; }

        /// <summary>ExitCode reported by RoboCopy converted into the Enum</summary>
        public RoboCopyExitCodes ExitCode => (RoboCopyExitCodes)ExitCodeValue;

        /// <inheritdoc cref="RoboCopyExitCodes.FilesCopiedSuccessful"/>
        public bool Successful => !WasCancelled && ExitCodeValue < 0x10;

        /// <inheritdoc cref="RoboCopyExitCodes.MismatchedDirectoriesDetected"/>
        public bool HasWarnings => ExitCodeValue >= 0x4;

        /// <inheritdoc cref="RoboCopyExitCodes.SeriousErrorOccurred"/>
        public bool HasErrors => ExitCodeValue >= 0x10;

        /// <inheritdoc cref="RoboCopyExitCodes.Cancelled"/>
        public virtual bool WasCancelled => ExitCodeValue < 0x0;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"ExitCode: {ExitCodeValue} ({ExitCode})";
        }

    }
    
    /// <summary>
    /// Represents the combination of multiple Exit Statuses
    /// </summary>
    public sealed class RoboCopyCombinedExitStatus : RoboCopyExitStatus, IRoboCopyCombinedExitStatus
    {
        #region < Constructor >

        /// <summary>
        /// Initializes a new instance of the <see cref="RoboCopyCombinedExitStatus"/> class.
        /// </summary>
        public RoboCopyCombinedExitStatus() : base(0) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoboCopyCombinedExitStatus"/> class.
        /// </summary>
        public RoboCopyCombinedExitStatus(int exitCodeValue) : base(exitCodeValue) { }

        /// <summary>
        /// Clone this into a new instance
        /// </summary>
        /// <param name="obj"></param>
        public RoboCopyCombinedExitStatus(RoboCopyCombinedExitStatus obj) :base((int)obj.ExitCode)
        {
            wascancelled = obj.wascancelled;
            noCopyNoError = obj.noCopyNoError;
        }

        #endregion

        #region < Fields and Event >

        //Private bools for the Combine methods
        private bool wascancelled;
        private bool noCopyNoError;
        private bool EnablePropertyChangeEvent = true;

        /// <summary>This event when the ExitStatus summary has changed </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region < Public Properties >

        /// <summary>Overides <see cref="RoboCopyExitStatus.WasCancelled"/></summary>
        /// <returns> <see cref="AnyWasCancelled"/></returns>
        public override bool WasCancelled => AnyWasCancelled;

        /// <summary>
        /// Atleast one <see cref="RoboCopyExitStatus"/> objects combined into this result resulted in no errors and no files/directories copied.
        /// </summary>
        public bool AnyNoCopyNoError => noCopyNoError || ExitCodeValue == 0x0;

        /// <summary>
        /// Atleast one <see cref="RoboCopyExitStatus"/> object combined into this result had been cancelled / exited prior to completion.
        /// </summary>
        public bool AnyWasCancelled => wascancelled || ExitCodeValue < 0x0;

        /// <summary>
        /// All jobs completed without errors or warnings.
        /// </summary>
        public bool AllSuccessful => !WasCancelled && (ExitCodeValue == 0x0 || ExitCodeValue == 0x1);

        /// <summary>
        /// All jobs completed without errors or warnings, but Extra Files/Folders were detected.
        /// </summary>
        public bool AllSuccessful_WithWarnings => !WasCancelled && Successful && ExitCodeValue > 0x1;

        #endregion

        #region < RaiseEvent Methods >

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private object[] StoreCurrentValues()
        {
            return new object[10] 
            { 
                WasCancelled, AnyNoCopyNoError, AnyWasCancelled, AllSuccessful, AllSuccessful_WithWarnings, HasErrors, HasWarnings, Successful, ExitCode, ExitCodeValue
            };
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private void CompareAndRaiseEvents(object[] OriginalValues)
        {
            object[] CurrentValues = StoreCurrentValues();
            if (CurrentValues[0] != OriginalValues[0]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WasCancelled)));
            if (CurrentValues[1] != OriginalValues[1]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnyNoCopyNoError)));
            if (CurrentValues[2] != OriginalValues[2]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnyWasCancelled)));
            if (CurrentValues[3] != OriginalValues[3]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllSuccessful)));
            if (CurrentValues[4] != OriginalValues[4]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllSuccessful_WithWarnings)));
            if (CurrentValues[5] != OriginalValues[5]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
            if (CurrentValues[6] != OriginalValues[6]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasWarnings)));
            if (CurrentValues[7] != OriginalValues[7]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Successful)));
            if (CurrentValues[8] != OriginalValues[8]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExitCode)));
            if (CurrentValues[9] != OriginalValues[9]) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExitCodeValue)));
        }

        #endregion

        /// <summary>
        /// Combine the RoboCopyExitCodes of the supplied ExitStatus with this ExitStatus.
        /// </summary>
        /// <remarks>If any were Cancelled, set the WasCancelled property to TRUE. Otherwise combine the exit codes.</remarks>
        /// <param name="status">ExitStatus to combine with</param>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void CombineStatus(RoboCopyExitStatus status)
        {
            if (status == null) return;
            object[] OriginalValues = EnablePropertyChangeEvent ? StoreCurrentValues() : null;
            //Combine the status
            if (status.WasCancelled)
            {
                wascancelled = true;
            }
            else
            {
                if (status.ExitCode == 0x0) this.noCopyNoError = true; //0x0 is lost if any other values have been added, so this logs the state
                RoboCopyExitCodes code = this.ExitCode | status.ExitCode;
                this.ExitCodeValue = (int)code;
            }
            //Raise Property Change Events
            if (EnablePropertyChangeEvent) CompareAndRaiseEvents(OriginalValues);
        }

        internal void CombineStatus(RoboCopyExitStatus status, bool enablePropertyChangeEvent)
        {
            EnablePropertyChangeEvent = enablePropertyChangeEvent;
            CombineStatus(status);
            EnablePropertyChangeEvent = enablePropertyChangeEvent;
        }

        /// <summary>
        /// Combine all the RoboCopyExitStatuses together.
        /// </summary>
        /// <param name="status">Array or List of ExitStatuses to combine.</param>
        public void CombineStatus(IEnumerable<RoboCopyExitStatus> status)
        {
            foreach (RoboCopyExitStatus s in status)
            {
                EnablePropertyChangeEvent = s == status.Last();
                CombineStatus(s);
            }
        }

        /// <summary>
        /// Combine all the RoboCopyExitStatuses together.
        /// </summary>
        /// <param name="statuses">Array or List of ExitStatuses to combine.</param>
        /// <returns> new RoboCopyExitStatus object </returns>
        public static RoboCopyCombinedExitStatus CombineStatuses(IEnumerable<RoboCopyExitStatus> statuses)
        {
            RoboCopyCombinedExitStatus ret = new RoboCopyCombinedExitStatus(0);
            ret.CombineStatus(statuses);
            return ret;
        }

        /// <summary>
        /// Reset the value of the object
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            object[] OriginalValues = EnablePropertyChangeEvent ? StoreCurrentValues() : null;
            this.wascancelled = false;
            this.noCopyNoError = false;
            this.ExitCodeValue = 0;
            if (EnablePropertyChangeEvent) CompareAndRaiseEvents(OriginalValues);
        }

        /// <summary>
        /// Reset the value of the object
        /// </summary>
        internal void Reset(bool enablePropertyChangeEvent)
        {
            EnablePropertyChangeEvent = enablePropertyChangeEvent;
            Reset();
            EnablePropertyChangeEvent = true;
        }

        /// <inheritdoc cref="ICloneable.Clone"/>
        public RoboCopyCombinedExitStatus Clone() => new RoboCopyCombinedExitStatus(this);

        object ICloneable.Clone() => Clone();
    }
}
