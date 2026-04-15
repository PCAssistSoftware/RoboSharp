using RoboSharp.Interfaces;


namespace RoboSharp.Extensions.Mocks
{
    /// <summary>
    /// An <see cref="RoboSharp.Interfaces.IRoboCommandFactory"/> that generates <see cref="MockRoboCommand"/>s
    /// </summary>
    public class MockRoboCommandFactory : RoboCommandFactory
    {
        /// <inheritdoc/>
        public override IRoboCommand GetRoboCommand()
        {
            return new MockRoboCommand();
        }
    }
}
