using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Interface for authenticating an <see cref="IRoboCommand"/> is valid to continue
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Validate if the robocommand can continue.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="domain"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        AuthenticationResult Authenticate(IRoboCommand command, string domain, string username, string password);
    }

    /// <summary>
    /// The Default <see cref="IAuthenticator"/>  provider. 
    /// <para/>Uses <see cref="Authentication.AuthenticateSourceAndDestination(IRoboCommand, string, string, string)"/>
    /// </summary>
    public sealed class DefaultAuthenticator : IAuthenticator
    {
        /// <summary>
        /// A thread-safe singleton that can be used
        /// </summary>
        public static IAuthenticator Instance => instance ??= new();
        private static DefaultAuthenticator instance = null;

        /// <inheritdoc cref="Authentication.AuthenticateSourceAndDestination(IRoboCommand, string, string, string)" />
        public AuthenticationResult Authenticate(IRoboCommand command, string domain, string username, string password)
        {
            return Authentication.AuthenticateSourceAndDestination(command, domain, username, password);
        }
    }

    /// <summary>
    /// An <see cref="IAuthenticator"/> that only checks that the source and destination directories are accessible
    /// </summary>
    public sealed class SourceAndDestinationAuthenticator : IAuthenticator
    {
        /// <summary>
        /// A thread-safe singleton that can be used
        /// </summary>
        public static IAuthenticator Instance => instance ??= new();
        private static SourceAndDestinationAuthenticator instance = null;
        
        /// <inheritdoc cref="Authentication.CheckSourceAndDestinationDirectories(IRoboCommand)" />
        public AuthenticationResult Authenticate(IRoboCommand command, string domain, string username, string password)
        {
            return Authentication.CheckSourceAndDestinationDirectories(command);
        }
    }
}
