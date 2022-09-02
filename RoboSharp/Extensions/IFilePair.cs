﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Interface used for extension methods for RoboSharp custom implementations that has File Source/Destination info 
    /// </summary>
    /// <remarks>
    /// This interface should only include the Source and Destination information, and not any methods to implement copying/moving
    /// </remarks>
    public interface IFilePair
    {
        /// <summary>
        /// Source File Information
        /// </summary>
        public FileInfo Source { get; }

        /// <summary>
        /// Destination FIle Information
        /// </summary>
        public FileInfo Destination { get; }
    }
}
