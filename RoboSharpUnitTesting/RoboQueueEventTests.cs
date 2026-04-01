using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using RoboSharp;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class RoboQueueEventTests
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

        private RoboQueue GenerateRQ(out RoboCommand cmd)
        {
            cmd = Test_Setup.GenerateCommand(Destination, false, false);
            return new RoboQueue(cmd);
        }

        private static async Task RunTestThenAssert(RoboQueue Q, Func<bool> testPassed)
        {
            await Q.StartAll();
            if (Q.RunResults.Count > 0) Test_Setup.WriteLogLines(Q.RunResults[0], true);
            if (!testPassed()) throw new AssertFailedException("Subscribed Event was not Raised!");
        }

        [TestMethod]
        public async Task RoboQueue_OnCommandCompleted()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnCommandCompleted += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_OnCommandError()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            cmd.CopyOptions.Source += "FolderDoesNotExist";
            bool TestPassed = false;
            RQ.OnCommandError += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_OnCopyProgressChanged()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnCopyProgressChanged += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_OnError()
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;

            //Create a file in the destination that would normally be copied, then lock it to force an error being generated.
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnError += (o, e) => TestPassed = true;
            Directory.CreateDirectory(Destination);
            using (var f = File.CreateText(Path.Combine(Destination, "4_Bytes.txt")))
            {
                f.WriteLine("StartTest!");
                Console.WriteLine("Expecting 1 File Failed!\n\n");
                await RunTestThenAssert(RQ, () => TestPassed);
            }
        }

        [TestMethod]
        public async Task RoboQueue_OnFileProcessed()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnFileProcessed += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }


        [TestMethod]
        public async Task RoboQueue_ProgressEstimatorCreated()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnProgressEstimatorCreated += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_OnCommandStarted()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnCommandStarted += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_RunCompleted()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.RunCompleted += (o, e) => TestPassed = true;
            await  RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_RunResultsUpdated()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.RunResultsUpdated += (o, e) => TestPassed = true;
            await RunTestThenAssert(RQ, () => TestPassed);
        }

        [TestMethod]
        public async Task RoboQueue_ListResultsUpdated()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.ListResultsUpdated += (o, e) => TestPassed = true;
            await RQ.StartAll_ListOnly();
            if (!TestPassed) throw new AssertFailedException("ListResultsUpdated Event was not Raised!");
        }

        [TestMethod]
        public void RoboQueue_CommandAdded()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.CollectionChanged += (o, e) => TestPassed = true;
            RQ.AddCommand(new RoboCommand());
            if (!TestPassed) throw new AssertFailedException("CollectionChanged Event was not Raised!");
        }

        [TestMethod]
        public void RoboQueue_CommandRemoved()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.CollectionChanged += (o, e) => TestPassed = true;
            RQ.RemoveCommand(cmd);
            if (!TestPassed) throw new AssertFailedException("CollectionChanged Event was not Raised!");
        }
        
        [TestMethod]
        public void RoboQueue_ReplaceCommand()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.CollectionChanged += (o, e) => TestPassed = true;
            RQ.ReplaceCommand(new RoboCommand(), 0);
            if (!TestPassed) throw new AssertFailedException("CollectionChanged Event was not Raised!");
        }

        [TestMethod]
        public async Task RoboQueue_EventOrder()
        {
            string PEV = "ProgressEstimator.ValuesUpdated";
            List<string> events = new List<string>();
            var RQ = GenerateRQ(out _);
            RQ.OnCommandCompleted += (o,e) => events.Add(nameof(RQ.OnCommandCompleted));
            RQ.OnCommandError += (o, e) => events.Add(nameof(RQ.OnCommandError));
            RQ.OnCommandStarted += (o, e) => events.Add(nameof(RQ.OnCommandStarted));
            RQ.OnCopyProgressChanged += (o, e) => events.Add(nameof(RQ.OnCopyProgressChanged));
            //RQ.OnFileProcessed += (o, e) => events.Add(nameof(RQ.OnFileProcessed));
            RQ.OnProgressEstimatorCreated += (o, e) =>
            {
                events.Add(nameof(RQ.OnProgressEstimatorCreated));
                e.ResultsEstimate.ValuesUpdated += (x, y) => events.Add(PEV);
            };
            RQ.RunCompleted += (o, e) => events.Add(nameof(RQ.RunCompleted));
            RQ.RunResultsUpdated += (o, e) => events.Add(nameof(RQ.RunResultsUpdated));

            await RQ.StartAll();

            Console.WriteLine("---- Events in order ----");
            events.ForEach(Console.WriteLine);
            Console.WriteLine("\n\n");
            Assert.IsTrue(events.IndexOf(nameof(RQ.RunCompleted)) > events.LastIndexOf(PEV), PEV + " occurred after " + nameof(RQ.RunCompleted));
        }

        // Property Change would have to be tested for every time the property is changed, which can get odd to test.
        // ListCount and How many ran for example require running it to trigger the event.
        //[TestMethod]
        //public void RoboQueue_PropertyChanged()
        //{
        //    var RQ = GenerateRQ(out RoboCommand cmd);
        //    bool TestPassed = false;
        //    RQ.PropertyChanged += (o, e) => TestPassed = true;
        //    RQ.RemoveCommand(cmd);
        //    Assert.IsTrue(TestPassed);
        //}

        //[TestMethod] //TODO: Unsure how to force the TaskFaulted Unit test, as it should never actually occurr.....
        //public void RoboQueue_TaskFaulted()
        //{
        //    var RQ = GenerateRQ(out RoboCommand cmd);
        //    bool TestPassed = false;
        //    RQ.TaskFaulted += (o, e) => TestPassed = true;
        //    RunEventTest(RQ, ref TestPassed);
        //    Assert.IsTrue(TestPassed);
        //}

    }
}
