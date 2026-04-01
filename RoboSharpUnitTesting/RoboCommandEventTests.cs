using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class RoboCommandEventTests
    {
        public TestContext TestContext { get; set; }
        private string Destination { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            Destination = Test_Setup.GetNewTempPath();
        }

        [TestCleanup]
        public void Cleanup() => Test_Setup.ClearOutTestDestination(Destination);

        private static async Task RunTestThenAssert(IRoboCommand cmd, Func<bool> wasRaised)
        {
            Console.WriteLine($"Type of command : {cmd.GetType()}");
            var results = await cmd.StartAsync();
            Test_Setup.WriteLogLines(results);
            if (!wasRaised()) throw new AssertFailedException("Subscribed Event was not Raised!");
        }

        /// <inheritdoc cref="UnitTests.Test_Setup.GenerateCommand(bool, bool)"/>
        protected virtual IRoboCommand GenerateCommand(bool UseLargerFileSet, bool ListOnlyMode) => UnitTests.Test_Setup.GenerateCommand(Destination, UseLargerFileSet, ListOnlyMode);

        [TestMethod]
        public virtual async Task RoboCommand_OnCommandCompleted()
        {
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.OnCommandCompleted += (o, e) => TestPassed = true;
            await RunTestThenAssert(cmd, () => TestPassed);
        }

        [TestMethod]
        public virtual async Task RoboCommand_OnCommandError()
        {
            var cmd = GenerateCommand(false, true);
            cmd.CopyOptions.Source += "FolderDoesNotExist";
            bool TestPassed = false;
            cmd.OnCommandError += (o, e) => TestPassed = true;
            await RunTestThenAssert(cmd, () => TestPassed);
        }

        [TestMethod]
        public virtual async Task RoboCommand_OnCopyProgressChanged()
        {
            var cmd = GenerateCommand(false, false);
            bool TestPassed = false;
            cmd.OnCopyProgressChanged += (o, e) => TestPassed = true;
            await RunTestThenAssert(cmd, () => TestPassed);
        }

        [TestMethod]
        public virtual async Task RoboCommand_OnError()
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;

            //Create a file in the destination that would normally be copied, then lock it to force an error being generated.
            var cmd = GenerateCommand(false, false);
            bool TestPassed = false;
            cmd.OnError += (o, e) => TestPassed = true;
            Directory.CreateDirectory(Destination);
            using (var f = File.CreateText(Path.Combine(Destination, "4_Bytes.txt")))
            {
                f.WriteLine("StartTest!");
                Console.WriteLine("Expecting 1 File Failed!\n\n");
                await RunTestThenAssert(cmd, () => TestPassed);
            }
        }

        [TestMethod]
        public virtual async Task RoboCommand_OnFileProcessed()
        {
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.OnFileProcessed += (o, e) => TestPassed = true;
            await RunTestThenAssert(cmd, () => TestPassed);
        }

        [TestMethod]
        public virtual async Task RoboCommand_ProgressEstimatorCreated()
        {
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.OnProgressEstimatorCreated += (o, e) => TestPassed = true;
            await RunTestThenAssert(cmd, () => TestPassed);
        }

        ////[TestMethod] //TODO: Unsure how to force the TaskFaulted Unit test, as it should never actually occurr.....
        //public virtual async Task RoboCommand_TaskFaulted()
        //{
        //    var cmd = GenerateCommand(false, true);
        //    bool TestPassed = false;
        //    cmd.TaskFaulted += (o, e) => TestPassed = true;
        //    await RunEventTest(cmd, () => TestPassed);
        //}

    }
}
