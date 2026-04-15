using RoboSharp.Interfaces;
using System;

#nullable enable

namespace RoboSharp.Extensions.Mocks
{
    /// <summary>
    /// An <see cref="RoboSharp.Interfaces.IRoboCommandFactory"/> that generates <see cref="MockRoboCommand"/>s
    /// </summary>
    public class MockRoboCommandFactory : RoboSharp.Interfaces.IRoboCommandFactory
    {
        private Func<MockRoboCommand>? factory;

        /// <summary>
        /// A function to provide customizations to the <see cref="MockRoboCommand"/> before returning from this factory, before applying any settings from the overloads for <see cref="GetRoboCommand()"/>
        /// </summary>
        public Func<MockRoboCommand> Factory { get => factory ??= () => new MockRoboCommand(); set => factory = value; }

        /// <summary>
        /// An action to run against the generated <see cref="MockRoboCommand"/> before returning from the factory
        /// <br/> Not Required.
        /// </summary>
        public Action<MockRoboCommand>? Customize { get; set; }

        /// <returns>new <see cref="MockRoboCommand"/></returns>
        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand()
            => GetRoboCommand("", "", CopyActionFlags.Default, SelectionFlags.Default);

        /// <returns>new <see cref="MockRoboCommand"/></returns>
        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand(string source, string destination)
            => GetRoboCommand(source, destination, CopyActionFlags.Default, SelectionFlags.Default);

        /// <returns>new <see cref="MockRoboCommand"/></returns>
        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand(string source, string destination, CopyActionFlags copyActionFlags)
            => GetRoboCommand(source, destination, copyActionFlags, SelectionFlags.Default);

        /// <returns>new <see cref="MockRoboCommand"/></returns>
        /// <inheritdoc/>
        public IRoboCommand GetRoboCommand(string source, string destination, CopyActionFlags copyActionFlags, SelectionFlags selectionFlags)
        {
            var cmd = Factory();
            cmd.CopyOptions.Source = source;
            cmd.CopyOptions.Destination = destination;
            cmd.CopyOptions.ApplyActionFlags(copyActionFlags);
            cmd.SelectionOptions.ApplySelectionFlags(selectionFlags);
            Customize?.Invoke(cmd);
            return cmd;
        }
    }
}
