using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Extensions.Tests;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER

namespace RoboSharp.Extensions.Tests
{
    /// <summary>
    /// Test the <see cref="RoboCommandPortable"/> object
    /// </summary>
    [TestClass]
    public class RoboCommandPortable_EventTests : RoboSharp.UnitTests.RoboCommandEventTests
    {
        protected override IRoboCommand GenerateCommand(bool UseLargerFileSet, bool ListOnlyMode)
        {
            var rc = RoboSharp.UnitTests.Test_Setup.GenerateCommand(false, true);
            var command = new RoboCommandPortable(RoboSharp.Extensions.StreamedCopierFactory.DefaultFactory)
            {
                CopyOptions = rc.CopyOptions,
                SelectionOptions = rc.SelectionOptions,
                RetryOptions = rc.RetryOptions,
                LoggingOptions = rc.LoggingOptions,
                Configuration = rc.Configuration,
            };
            return command;
        }
    }

    /// <summary>
    /// Validate that the command works the same as robocopy
    /// </summary>
    [TestClass]
    public class RoboCommandPortable_Tests
    {
        /// <summary>
        /// Set by testing framework
        /// </summary>
        public TestContext TestContext { get; set; }

        const LoggingFlags DefaultLoggingAction = LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader;
        const LoggingFlags ListOnlyLoggingAction = LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader | LoggingFlags.ListOnly;

        private static RoboCommandPortable GetCommand(RoboCommand rc, IFileCopierFactory factory = null)
        {
            return new RoboCommandPortable(factory ?? RoboSharp.Extensions.StreamedCopierFactory.DefaultFactory)
            {
                CopyOptions = rc.CopyOptions,
                SelectionOptions = rc.SelectionOptions,
                RetryOptions = rc.RetryOptions,
                LoggingOptions = rc.LoggingOptions,
                Configuration = rc.Configuration,
            };
        }


        [TestMethod]
        [Timeout(1000, CooperativeCancellation = true)]
        [DataRow(true, @"C:\SomeDir")]
        [DataRow(false, @"D:\System Volume Information")]
        public void IsAllowedDir(bool expected, string path)
        {
            Assert.AreEqual(expected, RoboMover.IsAllowedRootDirectory(new DirectoryInfo(path)));
        }

        /// <summary>
        /// Copy Test will use a standard ROBOCOPY command
        /// </summary>
        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.Mirror }, DisplayName = "Mirror")]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.Default }, DisplayName = "Defaults")]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.CopySubdirectories }, DisplayName = "Subdirectories")]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.CopySubdirectoriesIncludingEmpty }, DisplayName = "EmptySubdirectories")]
        [DataRow(data: new object[] { ListOnlyLoggingAction, SelectionFlags.Default, CopyActionFlags.Mirror }, DisplayName = "ListOnly-Mirror")]
        [DataRow(data: new object[] { ListOnlyLoggingAction, SelectionFlags.Default, CopyActionFlags.Default }, DisplayName = "ListOnly-Defaults")]
        [DataRow(data: new object[] { ListOnlyLoggingAction, SelectionFlags.Default, CopyActionFlags.CopySubdirectories }, DisplayName = "ListOnly-Subdirectories")]
        [DataRow(data: new object[] { ListOnlyLoggingAction, SelectionFlags.Default, CopyActionFlags.CopySubdirectoriesIncludingEmpty }, DisplayName = "ListOnly-EmptySubdirectories")]
        public async Task CopyTest(object[] flags)
        {
            try
            {
                Test_Setup.ClearOutTestDestination();
                CopyActionFlags copyAction = (CopyActionFlags)flags[2];
                SelectionFlags selectionFlags = (SelectionFlags)flags[1];
                LoggingFlags loggingAction = (LoggingFlags)flags[0];

                var rc = TestPrep.GetRoboCommand(false, copyAction, selectionFlags, loggingAction);
                var crc = GetCommand(rc);

                Assert.AreEqual(loggingAction.HasFlag(LoggingFlags.ListOnly), rc.LoggingOptions.ListOnly);
                Assert.AreEqual(loggingAction.HasFlag(LoggingFlags.ListOnly), crc.LoggingOptions.ListOnly);

                TestContext.CancellationToken.Register(() =>
                {
                    rc.Stop();
                    crc.Stop();
                });

                var results1 = await TestPrep.RunTests(rc, crc, !rc.LoggingOptions.ListOnly, TestContext.CancellationToken);
                TestPrep.CompareTestResults(results1[0], results1[1], rc.LoggingOptions.ListOnly);
            }
            catch (OperationCanceledException) when (TestContext.CancellationToken.IsCancellationRequested)
            { }
        }

        

        private static void GetMoveCommands(CopyActionFlags copyFlags, SelectionFlags selectionFlags, LoggingFlags loggingFlags, out RoboCommand rc, out RoboCommandPortable rm)
        {
            rc = TestPrep.GetRoboCommand(false, copyFlags, selectionFlags, loggingFlags);
            rc.CopyOptions.Source = TestPrep.GetMoveSource();
            rm = GetCommand(rc, Mocks.MockFileCopierFactory.Instance);
        }



        private const CopyActionFlags Mov_ = CopyActionFlags.MoveFiles;
        private const CopyActionFlags Move = CopyActionFlags.MoveFilesAndDirectories;

        /// <summary>
        /// This uses the actual logic provided by the RoboMover object
        /// </summary>
        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        [DataRow(data: new object[] { Mov_ | CopyActionFlags.CopySubdirectories, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories | Move Files")]
        [DataRow(data: new object[] { Move | CopyActionFlags.CopySubdirectories, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories | Move Files and Directories")]
        [DataRow(data: new object[] { Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories-Empty | Move Files")]
        [DataRow(data: new object[] { Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories-Empty | Move Files and Directories")]
        public async Task MoveTest(object[] flags)
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = await TestPrep.RunTests(rc, rm, !listOnly, TestPrep.PrepMoveFiles, TestContext.CancellationToken);
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public async Task FileInclusionTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            rc.CopyOptions.FileFilter = new string[] { "*.txt" };
            var results1 = await TestPrep.RunTests(rc, rm, !listOnly, TestPrep.PrepMoveFiles, TestContext.CancellationToken);
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public async Task FileExclusionTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            rc.SelectionOptions.ExcludedFiles.Add("*.txt");
            rc.Configuration.EnableFileLogging = true;
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = await TestPrep.RunTests(rc, rm, !listOnly, TestPrep.PrepMoveFiles, TestContext.CancellationToken);
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);
        }


        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(data: new object[] { Move | CopyActionFlags.CopySubdirectories, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Include Subdirectories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public async Task ExtraFileTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2] | LoggingFlags.ReportExtraFiles, out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = await TestPrep.RunTests(rc, rm, !listOnly, CreateFile, TestContext.CancellationToken);
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);

            static async Task CreateFile(CancellationToken token)
            {
                await TestPrep.PrepMoveFiles(token);
                string path = Path.Combine(TestPrep.DestDirPath, "ExtraFileTest.txt");
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(TestPrep.DestDirPath);
                    File.WriteAllText(path, "This is an extra file");
                }
            }
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public async Task SameFileTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = await TestPrep.RunTests(rc, rm, !listOnly, CreateFile, TestContext.CancellationToken);
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);

            static async Task CreateFile(CancellationToken token)
            {
                await TestPrep.PrepMoveFiles(token);
                Directory.CreateDirectory(TestPrep.DestDirPath);
                string fn = "1024_Bytes.txt";
                string dest = Path.Combine(TestPrep.DestDirPath, fn);
                if (!File.Exists(dest))
                    File.Copy(Path.Combine(TestPrep.SourceDirPath, fn), dest);
            }
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        // purge all
        [DataRow(0, true, Mov_)]
        [DataRow(0, true, Move)]
        [DataRow(0, true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(0, true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(0, true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(0, true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(0, true, Mov_, LoggingFlags.ReportExtraFiles)]
        [DataRow(0, true, Mov_ | CopyActionFlags.CopySubdirectories, LoggingFlags.ReportExtraFiles)]
        [DataRow(0, true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.ReportExtraFiles)]
        // purge depth 1 
        [DataRow(1, true, Mov_)]
        [DataRow(1, true, Move)]
        [DataRow(1, true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(1, true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(1, true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(1, true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        // purge depth 2
        [DataRow(2, true, Mov_)]
        [DataRow(2, false, Move)]
        [DataRow(2, true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(2, true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(2, false, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(2, false, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(2, true, Mov_, LoggingFlags.ReportExtraFiles)]
        [DataRow(2, true, Mov_ | CopyActionFlags.CopySubdirectories, LoggingFlags.ReportExtraFiles)]
        [DataRow(2, true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.ReportExtraFiles)]
        public async Task Purge_Depth(int depth, bool listOnly, CopyActionFlags flags, LoggingFlags? loggs = null)
        {
            LoggingFlags log = loggs.HasValue ? loggs.Value | DefaultLoggingAction : DefaultLoggingAction;
            GetMoveCommands(flags, SelectionFlags.Default, log, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.CopyOptions.Depth = depth;
            await RunPurge(cmd, mover, TestContext.CancellationToken);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(true, Mov_)]
        [DataRow(false, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(false, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(false, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        public async Task Purge_ExcludeFiles(bool listOnly, CopyActionFlags flags)
        {
            GetMoveCommands(flags, SelectionFlags.Default, DefaultLoggingAction, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.SelectionOptions.ExcludedFiles.Add("*0*_Bytes.txt");
            await RunPurge(cmd, mover, TestContext.CancellationToken);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(true, Mov_)]
        [DataRow(true, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        public async Task Purge_ExcludeFolders(bool listOnly, CopyActionFlags flags)
        {
            GetMoveCommands(flags, SelectionFlags.Default, DefaultLoggingAction, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.SelectionOptions.ExcludedDirectories.Add("EmptyFolder1"); // Top level empty
            cmd.SelectionOptions.ExcludedDirectories.Add("EmptyFolder4"); // Bottom level empty
            cmd.SelectionOptions.ExcludedDirectories.Add("SubFolder_2a"); // folder with contents
            await RunPurge(cmd, mover, TestContext.CancellationToken);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(true, Mov_)]
        [DataRow(true, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(true, Mov_, LoggingFlags.ReportExtraFiles)]
        [DataRow(true, Move, LoggingFlags.ReportExtraFiles)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories, LoggingFlags.ReportExtraFiles)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories, LoggingFlags.ReportExtraFiles)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.ReportExtraFiles)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.ReportExtraFiles)]
        public async Task Purge_IncludedFiles(bool listOnly, CopyActionFlags flags, LoggingFlags? loggs = null)
        {
            LoggingFlags log = loggs.HasValue ? loggs.Value | DefaultLoggingAction : DefaultLoggingAction;
            GetMoveCommands(flags, SelectionFlags.Default, log, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.CopyOptions.FileFilter = new string[] { "*0*_Bytes.txt" };
            await RunPurge(cmd, mover, TestContext.CancellationToken);
        }

        private static async Task RunPurge(RoboCommand cmd, RoboCommandPortable mover, CancellationToken token)
        {
            //if (Test_Setup.IsRunningOnAppVeyor()) return;
            var results = await TestPrep.RunTests(cmd, mover, !cmd.LoggingOptions.ListOnly, TestPrep.CreateFilesToPurge, token);
            TestPrep.CompareTestResults(results[0], results[1], cmd.LoggingOptions.ListOnly);
        }


    }
}
#endif