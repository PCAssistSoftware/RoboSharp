using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// <see cref="IFileCopierFactory"/> that creates <see cref="CopyFileEx"/> objects with a specified set of <see cref="Windows.CopyFileExOptions"/>
    /// <br/>This class is only usable on a Windows platform!
    /// </summary>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public sealed class CopyFileExFactory : AbstractFileCopierFactory<CopyFileEx>, IFileCopierFactory
    {
        /// <summary>
        /// The options to apply to generated copiers
        /// </summary>
        public CopyFileExOptions Options { get; set; }

        /// <inheritdoc/>
        public override CopyFileEx Create(FileInfo source, FileInfo destination, IDirectoryPair parent)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            return new CopyFileEx(source, destination, parent)
            {
                CopyOptions = Options
            };
        }
    }
}
