using RoboSharp.Interfaces;
using System;

#nullable enable

#if NET6_0_OR_GREATER

namespace RoboSharp.Extensions
{
    /// <summary>
    /// An <see cref="IRoboCommandFactory"/> that creates <see cref="RoboCommandPortable"/> objects that will use some <see cref="IFileCopierFactory"/> to determine how the copy operation is actually performed.
    /// <br/>This class should allow use of this library in non-windows environments.
    /// </summary>
    public class RoboCommandPortableFactory : IRoboCommandFactory
    {
        /// <summary>
        /// Gets a <see cref="RoboCommandFactory"/> that uses the <see cref="StreamedCopierFactory"/>
        /// </summary>
        /// <param name="authenticator"></param>
        /// <returns></returns>
        public  static IRoboCommandFactory GetStreamedCopierFactory(IAuthenticator? authenticator = null) => new RoboCommandPortableFactory(StreamedCopierFactory.DefaultFactory, authenticator);

        /// <summary>
        /// Create a new <see cref="RoboCommandPortableFactory"/> to produce <see cref="RoboCommandPortable"/> objects
        /// </summary>
        /// <param name="fileCopierFactory">The factory to use when a copy or move operation is required</param>
        /// <param name="authenticator">
        /// The <see cref="IAuthenticator"/> used to validate the robocommand prior to running. 
        /// <br/>Default uses <see cref="SourceAndDestinationAuthenticator"/>
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException">Applies to .NetFramework and .NetStandard2.0</exception>
        public RoboCommandPortableFactory(IFileCopierFactory fileCopierFactory, IAuthenticator? authenticator = null)
        {
            RoboCommandPortable.ThrowUnsupportedFrameworkException();
            _fileCopierFactory  = fileCopierFactory ?? throw new ArgumentNullException(nameof(fileCopierFactory));
            _authenticator = authenticator ?? SourceAndDestinationAuthenticator.Instance;
        }

        private readonly IFileCopierFactory _fileCopierFactory;
        private readonly IAuthenticator _authenticator;

        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand()
        {
            return new RoboCommandPortable(_fileCopierFactory, _authenticator);
        }

        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand(string source, string destination)
        {
            var cmd = new RoboCommandPortable(_fileCopierFactory, _authenticator);
            cmd.CopyOptions.Source = source;
            cmd.CopyOptions.Destination = destination;
            return cmd;
        }

        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand(string source, string destination, CopyActionFlags copyActionFlags)
        {
            var cmd = new RoboCommandPortable(_fileCopierFactory, _authenticator);
            cmd.CopyOptions.Source = source;
            cmd.CopyOptions.Destination = destination;
            cmd.CopyOptions.ApplyActionFlags(copyActionFlags);
            return cmd;
        }

        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand(string source, string destination, CopyActionFlags copyActionFlags, SelectionFlags selectionFlags)
        {
            var cmd = new RoboCommandPortable(_fileCopierFactory, _authenticator);
            cmd.CopyOptions.Source = source;
            cmd.CopyOptions.Destination = destination;
            cmd.CopyOptions.ApplyActionFlags(copyActionFlags);
            cmd.SelectionOptions.ApplySelectionFlags(selectionFlags);
            return cmd;
        }
    }
}

#endif