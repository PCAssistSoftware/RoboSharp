﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using RoboSharp;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharpUnitTesting
{
    [TestClass]
    public class RoboQueueEventTests
    {
        private static RoboQueue GenerateRQ(out RoboCommand cmd)
        {
            cmd = Test_Setup.GenerateCommand(false, false);
            return new RoboQueue(cmd);
        }

        private static void RunTestThenAssert(RoboQueue Q, ref bool testPassed)
        {
            Q.StartAll().Wait();
            Assert.IsTrue(testPassed);
        }

        [TestMethod]
        public void RoboCommand_OnCommandCompleted()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnCommandCompleted += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_OnCommandError()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            cmd.CopyOptions.Source += "FolderDoesNotExist";
            bool TestPassed = false;
            RQ.OnCommandError += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_OnCopyProgressChanged()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            Test_Setup.ClearOutTestDestination();
            bool TestPassed = false;
            RQ.OnCopyProgressChanged += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_OnError()
        {
            //Create a file in the destination that would normally be copied, then lock it to force an error being generated.
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnError += (o, e) => TestPassed = true;
            Test_Setup.ClearOutTestDestination();
            Directory.CreateDirectory(Test_Setup.TestDestination);
            using (var f = File.CreateText(Path.Combine(Test_Setup.TestDestination, "4_Bytes.txt")))
            {
                f.WriteLine("StartTest!");
                RunTestThenAssert(RQ, ref TestPassed);
            }
        }

        [TestMethod]
        public void RoboCommand_OnFileProcessed()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            Test_Setup.ClearOutTestDestination();
            bool TestPassed = false;
            RQ.OnFileProcessed += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        

        [TestMethod]
        public void RoboQueue_ProgressEstimatorCreated()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnProgressEstimatorCreated += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboQueue_OnCommandStarted()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.OnCommandStarted += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboQueue_RunCompleted()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.RunCompleted += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboQueue_RunResultsUpdated()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.RunResultsUpdated += (o, e) => TestPassed = true;
            RunTestThenAssert(RQ, ref TestPassed);
        }

        [TestMethod]
        public void RoboQueue_ListResultsUpdated()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.ListResultsUpdated += (o, e) => TestPassed = true;
            RQ.StartAll_ListOnly().Wait();
            Assert.IsTrue(TestPassed);
        }

        [TestMethod]
        public void RoboQueue_CommandAdded()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.CollectionChanged += (o, e) => TestPassed = true;
            RQ.AddCommand(new RoboCommand());
            Assert.IsTrue(TestPassed);
        }

        [TestMethod]
        public void RoboQueue_CommandRemoved()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.CollectionChanged += (o, e) => TestPassed = true;
            RQ.RemoveCommand(cmd);
            Assert.IsTrue(TestPassed);
        }
        
        [TestMethod]
        public void RoboQueue_ReplaceCommand()
        {
            var RQ = GenerateRQ(out RoboCommand cmd);
            bool TestPassed = false;
            RQ.CollectionChanged += (o, e) => TestPassed = true;
            RQ.ReplaceCommand(new RoboCommand(), 0);
            Assert.IsTrue(TestPassed);
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
        //public void RoboCommand_TaskFaulted()
        //{
        //    var RQ = GenerateRQ(out RoboCommand cmd);
        //    bool TestPassed = false;
        //    RQ.TaskFaulted += (o, e) => TestPassed = true;
        //    RunTestThenAssert(RQ, ref TestPassed);
        //    Assert.IsTrue(TestPassed);
        //}

    }
}
