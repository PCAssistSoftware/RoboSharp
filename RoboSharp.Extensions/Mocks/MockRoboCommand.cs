using RoboSharp.Extensions.Helpers;
using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace RoboSharp.Extensions.Mocks
{
    /// <summary>
    /// A mock <see cref="RoboSharp.Interfaces.IRoboCommand"/>
    /// <br/>Starting only returns <see cref="Task.CompletedTask"/>
    /// </summary>
    public class MockRoboCommand : AbstractIRoboCommand
    {
        public override void Dispose()
        {
            
        }

        public override Task Start(string domain = "", string username = "", string password = "")
        {
            var resultsbuilder = new ResultsBuilder(this);
            base.ListOnlyResults = resultsbuilder.GetResults();
            base.RunResults = ListOnlyResults;
            return Task.CompletedTask;
        }

        public override void Stop()
        {
            
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member