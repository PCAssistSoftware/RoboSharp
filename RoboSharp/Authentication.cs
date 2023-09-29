﻿using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp
{
    /// <summary>
    /// The return value of an Authentication attempt
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// The constructor used to create a new authentication result
        /// </summary>
        internal protected AuthenticationResult(bool success, CommandErrorEventArgs args)
        {
            Success = success;
            CommandErrorArgs = args;
        }

        /// <summary><see langword="true"/> if both the source and destination are accessible, otherwise <see langword="false"/></summary>
        public bool Success { get; }

        /// <summary>The Command Error args that result from the authentication attempt </summary>
        /// <remarks>This is only expected to have a value when <see cref="Success"/> == <see langword="false"/></remarks>
        public CommandErrorEventArgs CommandErrorArgs { get; }
    }

    /// <summary>
    /// Provider to allow IRoboCommands to authenticate against remote servers and check existence of source/destination directories
    /// </summary>
    public static class Authentication
    {
        private delegate AuthenticationResult AuthenticationDelegate(IRoboCommand command);
        
        /// <inheritdoc cref="Authenticate"/>
        /// <inheritdoc cref="CheckSourceAndDestinationDirectories"/>
        public static AuthenticationResult AuthenticateSourceAndDestination(IRoboCommand command, string domain = "", string username = "", string password = "")
        {
            return Authenticate(domain, username, password, command, CheckSourceAndDestinationDirectories);
        }

        /// <inheritdoc cref="Authenticate"/>
        /// <inheritdoc cref="CheckSourceDirectory"/>
        public static AuthenticationResult AuthenticateSource(IRoboCommand command, string domain = "", string username = "", string password = "")
        {
            return Authenticate(domain, username, password, command, CheckSourceDirectory);
        }

        /// <inheritdoc cref="Authenticate"/>
        /// <inheritdoc cref="CheckDestinationDirectory"/>
        public static AuthenticationResult AuthenticateDestination(IRoboCommand command, string domain = "", string username = "", string password = "")
        {
            return Authenticate(domain, username, password, command, CheckDestinationDirectory);
        }

        /// <summary>
        /// Authenticate against a remote server using the supplied strings. 
        /// </summary>
        /// <param name="domain">The authentication domain, if any</param>
        /// <param name="username">The username to use for authentication - without this value no authentication will occur</param>
        /// <param name="password">The password if one is required</param>
        /// <param name="command">The IRobocommand that will provide the source/destination folder paths to evaluate during its authentication</param>
        /// <param name="auth">The action to perform during authentication</param>
        /// <exception cref="ArgumentNullException"/>
        private static AuthenticationResult Authenticate(
            string domain,
            string username,
            string password,
            IRoboCommand command,
            AuthenticationDelegate auth)
        {
            AuthenticationResult result;

#if NET40_OR_GREATER
            // Authenticate on Target Server -- Create user if username is provided, else null
            ImpersonatedUser impersonation = username.IsNullOrWhiteSpace() ? null : impersonation = new ImpersonatedUser(username, domain, password);
#endif

#if NET6_0_OR_GREATER
            if (username.IsNullOrWhiteSpace())
            {
                result = auth(command);
            }
            else
            {
                result = null;
                void Eval() { result = auth(command); }
                ImpersonatedRun.Execute(domain, username, password, Eval);
            }
#else
            result = auth(command);
#endif

#if NET40_OR_GREATER
            //Dispose Authentification
            impersonation?.Dispose();
#endif
            return result ?? throw new NullReferenceException("Authentication Method Failed - Please report to the github RoboSharp project page.");
        }

        /// <remarks>
        /// <inheritdoc cref="CheckSourceDirectory" path="/remarks"/> <br/>
        /// <inheritdoc cref="CheckDestinationDirectory" path="/remarks"/> <br/>
        /// </remarks>
        /// <returns><see langword="true"/> if both the source and destination are accessible, otherwise <see langword="false"/></returns>
        private static AuthenticationResult CheckSourceAndDestinationDirectories(IRoboCommand command)
        {
            var source = CheckSourceDirectory(command);
            if (source.Success)
                return CheckDestinationDirectory(command);
            else
                return source;
        }

        /// <remarks>
        /// Check that the source directory exists.
        /// </remarks>
        /// <returns><see langword="true"/> if the source is accessible, otherwise <see langword="false"/></returns>
        private static AuthenticationResult CheckSourceDirectory(IRoboCommand command)
        {
            const string SourceMissing = "The Source directory does not exist.";
            
            // make sure source path is valid
            if (!Directory.Exists(command.CopyOptions.Source))
            {
                Debugger.Instance.DebugMessage(SourceMissing);
                return new AuthenticationResult(false, new CommandErrorEventArgs(new DirectoryNotFoundException(SourceMissing)));
            }
            return new AuthenticationResult(true, null);
        }

        /// <remarks>
        /// Check that the destination root directory exists. <br/>
        /// If not list only, verify that the destination drive has write access.
        /// </remarks>
        /// <returns><see langword="true"/> if the destination is accessible, otherwise <see langword="false"/></returns>
        private static AuthenticationResult CheckDestinationDirectory(IRoboCommand command)
        {
            const string DestDriveInvalid = "The destination drive does not exist.";
            const string CheckWriteAccess = "Unable to create Destination Folder. Check Write Access.";
            const string CatchMessage = "The Destination directory is invalid.";

            //Check that the Destination Drive is accessible instead [fixes #106]
            try
            {
                //Check if the destination drive is accessible -> should not cause exception [Fix for #106]
                DirectoryInfo dInfo = new DirectoryInfo(command.CopyOptions.Destination).Root;
                if (!dInfo.Exists)
                {
                    Debugger.Instance.DebugMessage(DestDriveInvalid);
                    return new AuthenticationResult(false, new CommandErrorEventArgs(new DirectoryNotFoundException(DestDriveInvalid)));
                }

                //If not list only, verify that drive has write access -> should cause exception if no write access [Fix #101]
                if (!command.LoggingOptions.ListOnly)
                {
                    dInfo = Directory.CreateDirectory(command.CopyOptions.Destination);
                    if (!dInfo.Exists)
                    {
                        Debugger.Instance.DebugMessage(CheckWriteAccess);
                        return new AuthenticationResult(false, new CommandErrorEventArgs(new DirectoryNotFoundException(CheckWriteAccess)));
                    }
                }
            }
            catch (Exception ex)
            {
                Debugger.Instance.DebugMessage(ex.Message);
                return new AuthenticationResult(false, new CommandErrorEventArgs(CatchMessage, ex));
            }
            return new AuthenticationResult(true, null);
        }
    }
}
