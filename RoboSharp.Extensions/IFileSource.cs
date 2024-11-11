using System;
using System.Collections.Generic;
using System.Text;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// An object that provides the path to a file.
    /// </summary>
    public interface IFileSource
    {
        /// <summary>
        /// The fully qualified path to a file
        /// </summary>
        string FilePath { get; }
    }

}
