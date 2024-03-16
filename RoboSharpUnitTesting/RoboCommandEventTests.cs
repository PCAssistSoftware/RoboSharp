﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class RoboCommandEventTests
    {
        private static void RunTestThenAssert(IRoboCommand cmd, ref bool EventBool)
        {
            var results = cmd.StartAsync().GetAwaiter().GetResult();
            Test_Setup.WriteLogLines(results);
            if (!EventBool) throw new AssertFailedException("Subscribed Event was not Raised!");
        }

        /// <inheritdoc cref="UnitTests.Test_Setup.GenerateCommand(bool, bool)"/>
        protected virtual IRoboCommand GenerateCommand(bool UseLargerFileSet, bool ListOnlyMode) => UnitTests.Test_Setup.GenerateCommand(UseLargerFileSet, ListOnlyMode);

        [TestMethod]
        public void RoboCommand_OnCommandCompleted()
        {
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.OnCommandCompleted += (o, e) => TestPassed = true;
            RunTestThenAssert(cmd, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_OnCommandError()
        {
            var cmd = GenerateCommand(false, true);
            cmd.CopyOptions.Source += "FolderDoesNotExist";
            bool TestPassed = false;
            cmd.OnCommandError += (o, e) => TestPassed = true;
            RunTestThenAssert(cmd, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_OnCopyProgressChanged()
        {
            Test_Setup.ClearOutTestDestination();
            var cmd = GenerateCommand(false, false);
            bool TestPassed = false;
            cmd.OnCopyProgressChanged += (o, e) => TestPassed = true;
            RunTestThenAssert(cmd, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_OnError()
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;

            //Create a file in the destination that would normally be copied, then lock it to force an error being generated.
            var cmd = GenerateCommand(false, false);
            bool TestPassed = false;
            cmd.OnError += (o, e) => TestPassed = true;
            Test_Setup.ClearOutTestDestination();
            Directory.CreateDirectory(Test_Setup.TestDestination);
            using (var f = File.CreateText(Path.Combine(Test_Setup.TestDestination, "4_Bytes.txt")))
            {
                f.WriteLine("StartTest!");
                Console.WriteLine("Expecting 1 File Failed!\n\n");
                RunTestThenAssert(cmd, ref TestPassed);
            }
        }

        [TestMethod]
        public void RoboCommand_OnFileProcessed()
        {
            Test_Setup.ClearOutTestDestination();
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.OnFileProcessed += (o, e) => TestPassed = true;
            RunTestThenAssert(cmd, ref TestPassed);
        }

        [TestMethod]
        public void RoboCommand_ProgressEstimatorCreated()
        {
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.OnProgressEstimatorCreated += (o, e) => TestPassed = true;
            RunTestThenAssert(cmd, ref TestPassed);
        }

        //[TestMethod] //TODO: Unsure how to force the TaskFaulted Unit test, as it should never actually occurr.....
        public void RoboCommand_TaskFaulted()
        {
            var cmd = GenerateCommand(false, true);
            bool TestPassed = false;
            cmd.TaskFaulted += (o, e) => TestPassed = true;
            RunTestThenAssert(cmd, ref TestPassed);
        }

    }
}
