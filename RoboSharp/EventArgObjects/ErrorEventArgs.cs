using System;
using System.Linq;
using System.Text.RegularExpressions;

// Do Not change NameSpace here! -> Must be RoboSharp due to prior releases
namespace RoboSharp
{
    /// <summary>
    /// Information about an Error reported by the RoboCopy process
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <inheritdoc/>
        public ErrorEventArgs() { }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="errorPath"></param>
        /// <param name="dateTime"></param>
        public ErrorEventArgs(Exception ex, string errorPath, DateTime dateTime)
        {
            Error = ex?.Message;
            ErrorDescription = ex?.StackTrace;
            ErrorCode = ex?.HResult ?? 0;
            SignedErrorCode = ErrorCode.ToString();
            ErrorPath = errorPath;
            DateTime = dateTime;
        }

        /// <summary> </summary>
        /// <param name="error"><inheritdoc cref="Error" path="*"/></param>
        /// <param name="errorDescription"><inheritdoc cref="ErrorDescription" path="*"/></param>
        /// <param name="errorCode"><inheritdoc cref="ErrorCode" path="*"/></param>
        /// <param name="signedErrorCode"><inheritdoc cref="SignedErrorCode" path="*"/></param>
        /// <param name="errorPath"><inheritdoc cref="ErrorPath" path="*"/></param>
        /// <param name="dateTime"><inheritdoc cref="DateTime" path="*"/></param>
        public ErrorEventArgs(int errorCode, string error, string errorDescription, string errorPath, DateTime? dateTime = null, string signedErrorCode = null)
        {
            Error = error;
            ErrorDescription = errorDescription;
            ErrorCode = errorCode;
            ErrorPath = errorPath;
            DateTime = dateTime ?? DateTime.Now;
            SignedErrorCode = signedErrorCode ?? errorCode.ToString();
        }

        /// <summary>
        /// The error message
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Error Description or stack trace
        /// </summary>
        public string ErrorDescription { get; }
        
        /// <summary>
        /// The error code
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Signed Error Code
        /// </summary>
        public string SignedErrorCode { get; }

        /// <summary>
        /// The File or Directory Path the Error refers to
        /// </summary>
        public string ErrorPath { get; }

        /// <summary>
        /// DateTime the error occurred
        /// </summary>
        public DateTime DateTime { get; }

        /// <summary>
        /// Concatenate the <see cref="Error"/> and <see cref="ErrorDescription"/> into a string seperated by an <see cref="Environment.NewLine"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (ErrorDescription.IsNullOrWhiteSpace())
                return Error;
            else
                return $"{Error}{Environment.NewLine}{ErrorDescription}";
        }

        /// <summary>
        /// <inheritdoc cref="ErrorEventArgs"/>
        /// </summary>
        /// <param name="errorData"><inheritdoc cref="Error"/></param>
        /// <param name="descripData"><inheritdoc cref="ErrorCode"/></param>
        /// <param name="errTokenRegex">
        /// Regex used to split the Error Code into its various parts. <br/>
        /// Must have the following groups: Date, ErrCode, SignedErrCode, Descrip, Path
        /// </param>
        internal ErrorEventArgs(string errorData, string descripData, Match errTokenRegex)
        {
            var groups = errTokenRegex.Groups;

            //Date
            string dateStr = groups["Date"].Value;
            if (DateTime.TryParse(dateStr, out var DT))
                this.DateTime = DT;
            else
                this.DateTime = DateTime.Now;

            //Path
            ErrorPath = groups["Path"].Value;

            //Error Code
            ErrorCode = Convert.ToInt32(groups["ErrCode"].Value);
            SignedErrorCode = groups["SignedErrCode"].Value;

            //Error String
            Error = errorData;
            ErrorDescription = descripData;
            
        }
    }
}
