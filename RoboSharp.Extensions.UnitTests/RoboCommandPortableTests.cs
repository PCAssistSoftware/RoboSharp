using Microsoft.Testing.Platform.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Extensions.Tests;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
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

        [TestInitialize]
        public void TestInit()
        {
            TestPrep.CleanDestination();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestPrep.CleanDestination();
        }


        [TestMethod]
        [Timeout(1000, CooperativeCancellation = true)]
        [DataRow(true, @"C:\SomeDir")]
        [DataRow(false, @"D:\System Volume Information")]
        public void IsAllowedDir(bool expected, string path)
        {
            Assert.AreEqual(expected, RoboMover.IsAllowedRootDirectory(new DirectoryInfo(path)));
        }

        private const CopyActionFlags Mov_ = CopyActionFlags.MoveFiles;
        private const CopyActionFlags Move = CopyActionFlags.MoveFilesAndDirectories;
        private const CopyActionFlags Copy = CopyActionFlags.Default;
        private const CopyActionFlags CopySub = CopyActionFlags.CopySubdirectories;
        private const CopyActionFlags CopyEmpty = CopyActionFlags.CopySubdirectoriesIncludingEmpty;
        private const CopyActionFlags Purge = CopyActionFlags.Purge;
        private const CopyActionFlags Mirror = CopyActionFlags.Mirror;

        private const LoggingFlags DefaultLogging = LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader;
        private const LoggingFlags ReportExtra = DefaultLogging | LoggingFlags.ReportExtraFiles;
        private const LoggingFlags Verbose = DefaultLogging | LoggingFlags.VerboseOutput;
        private const LoggingFlags ListOnly = DefaultLogging | LoggingFlags.ListOnly;
        private const LoggingFlags ListOnlyReportExtra = DefaultLogging | LoggingFlags.ListOnly | ReportExtra;
        private const LoggingFlags ListOnlyVerbose = DefaultLogging | LoggingFlags.ListOnly | LoggingFlags.VerboseOutput;

        /// <summary>
        /// Run tests against the results of RoboCopy
        /// </summary>
        [TestMethod]
        // copy items
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(Copy, SelectionFlags.Default, DefaultLogging)]
        [DataRow(CopySub, SelectionFlags.Default, DefaultLogging)]
        [DataRow(CopyEmpty, SelectionFlags.Default, DefaultLogging)]
        [DataRow(CopyEmpty, SelectionFlags.Default, Verbose)]
        // List Only
        [DataRow(Copy, SelectionFlags.Default, ListOnly)]
        [DataRow(Copy, SelectionFlags.Default, ListOnlyVerbose)]
        [DataRow(Copy, SelectionFlags.Default, ListOnlyReportExtra)]
        [DataRow(CopySub, SelectionFlags.Default, ListOnly)]
        [DataRow(CopySub, SelectionFlags.Default, ListOnlyVerbose)]
        [DataRow(CopySub, SelectionFlags.Default, ListOnlyReportExtra)]
        [DataRow(CopyEmpty, SelectionFlags.Default, ListOnly)]
        [DataRow(CopyEmpty, SelectionFlags.Default, ListOnlyVerbose)]
        [DataRow(CopyEmpty, SelectionFlags.Default, ListOnlyReportExtra)]        
        public Task CopyTests(CopyActionFlags copyFlags, SelectionFlags selectionFlags, LoggingFlags loggingFlags)
        {
            return RunCopyTest(copyFlags, loggingFlags, selectionFlags);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        // copy items
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.Default)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.ExcludeNewer)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.ExcludeOlder)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.ExcludeChanged)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.ExcludeLonely)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.ExcludeExtra)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.IncludeSame)]
        [DataRow(CopyEmpty, DefaultLogging, SelectionFlags.IncludeModified)]
        public Task SelectionTests(CopyActionFlags copyFlags, LoggingFlags loggingFlags, SelectionFlags selectionFlags)
        {
            return RunCopyTest(copyFlags, loggingFlags, selectionFlags);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        // copy items
        [DataRow(0, Copy, ReportExtra)]
        [DataRow(1, Copy, ReportExtra)]
        [DataRow(2, Copy, ReportExtra)]
        [DataRow(0, CopyEmpty, DefaultLogging)]
        [DataRow(1, CopyEmpty, DefaultLogging)]
        [DataRow(2, CopyEmpty, DefaultLogging)]
        [DataRow(0, CopyEmpty, ReportExtra)]
        [DataRow(1, CopyEmpty, ReportExtra)]
        [DataRow(2, CopyEmpty, ReportExtra)]
        public Task TestDepth(int depth, CopyActionFlags copyActionFlags, LoggingFlags loggingFlags)
        {
            return RunCopyTest(copyActionFlags, loggingFlags | ListOnly, default, depth);
        }

        private async Task RunCopyTest(CopyActionFlags copyFlags, LoggingFlags loggingFlags, SelectionFlags selectionFlags, int maxDepth = 0)
        {
            try
            {
                var rc = TestPrep.GetRoboCommand(false, copyFlags, selectionFlags, loggingFlags);
                var crc = GetCommand(rc);

                bool listOnly = loggingFlags.HasFlag(LoggingFlags.ListOnly);
                Assert.AreEqual(listOnly, rc.LoggingOptions.ListOnly);
                Assert.AreEqual(listOnly, crc.LoggingOptions.ListOnly);

                TestContext.CancellationToken.Register(() =>
                {
                    rc.Stop();
                    crc.Stop();
                });

                var results1 = await TestPrep.RunTests(rc, crc, !listOnly, TestContext.CancellationToken);
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
                string dest = Path.Combine(TestPrep.DestDirPath, Path.GetRandomFileName());
                File.WriteAllText(dest, "!!!!This is an extra File!!!!");                
                Assert.IsTrue(File.Exists(dest));
            }
        }

        [TestMethod]
        //[Timeout(5000, CooperativeCancellation = true)]
        // purge all
        [DataRow(0, true, Move)]
        [DataRow(0, true, Copy)]
        [DataRow(0, true, CopyEmpty)]
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
        // purge 3
        [DataRow(3, true, Move)]
        [DataRow(3, true, Copy)]
        [DataRow(3, true, CopyEmpty)]
        // purge 3
        [DataRow(0, true, Purge)]
        [DataRow(1, true, Purge)]
        [DataRow(3, true, Purge)]
        public async Task Test_Copy_Depth(int depth, bool listOnly, CopyActionFlags flags, LoggingFlags? loggs = null)
        {
            LoggingFlags log = loggs.HasValue ? loggs.Value | DefaultLoggingAction : DefaultLoggingAction;
            GetMoveCommands(flags, SelectionFlags.Default, log, out var cmd, out var implementation);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.CopyOptions.Depth = depth;
            Assert.AreSame(cmd.CopyOptions, implementation.CopyOptions);
            await RunSelectionTests(cmd, implementation, TestContext.CancellationToken);
        }

        private static async Task RunSelectionTests(RoboCommand cmd, RoboCommandPortable implementation, CancellationToken token)
        {
            //if (Test_Setup.IsRunningOnAppVeyor()) return;
            var results = await TestPrep.RunTests(cmd, implementation, !cmd.LoggingOptions.ListOnly, TestPrep.CreateExtraDirectories, token);
            TestPrep.CompareTestResults(results[0], results[1], cmd.LoggingOptions.ListOnly);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(true, Mov_)]
        [DataRow(false, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(false, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(false, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        public async Task Test_Selection_ExcludeFiles(bool listOnly, CopyActionFlags flags)
        {
            GetMoveCommands(flags, SelectionFlags.Default, DefaultLoggingAction, out var cmd, out var implementation);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.SelectionOptions.ExcludedFiles.Add("*0*_Bytes.txt");
            await RunSelectionTests(cmd, implementation, TestContext.CancellationToken);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        [DataRow(true, Mov_)]
        [DataRow(true, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        public async Task Test_Selection_ExcludeFolders(bool listOnly, CopyActionFlags flags)
        {
            GetMoveCommands(flags, SelectionFlags.Default, DefaultLoggingAction, out var cmd, out var implementation);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.SelectionOptions.ExcludedDirectories.Add("EmptyFolder1"); // Top level empty
            cmd.SelectionOptions.ExcludedDirectories.Add("EmptyFolder4"); // Bottom level empty
            cmd.SelectionOptions.ExcludedDirectories.Add("SubFolder_2a"); // folder with contents
            await RunSelectionTests(cmd, implementation, TestContext.CancellationToken);
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
        public async Task Test_Selection_IncludedFiles(bool listOnly, CopyActionFlags flags, LoggingFlags? loggs = null)
        {
            LoggingFlags log = loggs.HasValue ? loggs.Value | DefaultLoggingAction : DefaultLoggingAction;
            GetMoveCommands(flags, SelectionFlags.Default, log, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.CopyOptions.FileFilter = new string[] { "*0*_Bytes.txt" };
            await RunSelectionTests(cmd, mover, TestContext.CancellationToken);
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.MoveFiles)]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.Purge)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.Purge)]
        public async Task PurgeTests(CopyActionFlags copyOptions)
        {
            GetMoveCommands(
                CopyActionFlags.CopySubdirectoriesIncludingEmpty | copyOptions,
                SelectionFlags.Default,
                DefaultLoggingAction,
                out _, out var rm);
            Test_Setup.ClearOutTestDestination();
            await TestPrep.PrepMoveFiles(TestContext.CancellationToken);

            string subfolderpath = @"SubFolder_1\SubFolder_1.1\SubFolder_1.2";
            FilePair[] SourceFiles = new FilePair[] {
                new FilePair(Path.Combine(rm.CopyOptions.Source, "4_Bytes.txt"), Path.Combine(rm.CopyOptions.Destination, "4_Bytes.txt")),
                new FilePair(Path.Combine(rm.CopyOptions.Source, "1024_Bytes.txt"), Path.Combine(rm.CopyOptions.Destination, "1024_Bytes.txt")),
                new FilePair(Path.Combine(rm.CopyOptions.Source, subfolderpath, "0_Bytes.txt"), Path.Combine(rm.CopyOptions.Destination, subfolderpath, "0_Bytes.txt")),
                new FilePair(Path.Combine(rm.CopyOptions.Source, subfolderpath, "4_Bytes.htm"), Path.Combine(rm.CopyOptions.Destination, subfolderpath, "4_Bytes.htm")),
            };
            FileInfo[] purgeFiles = new FileInfo[]
            {
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFile_1.txt")),
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFile_2.txt")),
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFolder_1", "PurgeFile_3.txt")),
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFolder_2", "SubFolder","PurgeFile_4.txt")),
            };
            DirectoryInfo[] PurgeDirectories = new DirectoryInfo[]
            {
                purgeFiles[2].Directory,
                purgeFiles[3].Directory,
                purgeFiles[3].Directory.Parent,
            };

            foreach (var dir in PurgeDirectories) Directory.CreateDirectory(dir.FullName);
            foreach (var file in purgeFiles) File.WriteAllText(file.FullName, "PURGE ME");

            await rm.Start();
            foreach (var lin in rm.GetResults().LogLines)
                Console.WriteLine(lin);

            bool purge = rm.CopyOptions.Purge;
            // Evaluate purged
            foreach (var file in purgeFiles)
            {
                file.Refresh();
                Assert.AreEqual(purge, !file.Exists, purge ? "File was not purged." : "File was purged unexpectedly.");
            }
            foreach (var dir in PurgeDirectories)
            {
                dir.Refresh();
                Assert.AreEqual(purge, !dir.Exists, purge ? "Directory was not purged." : "Directory was purged unexpectedly.");
            }
            //evaluate moved
            foreach (var filepair in SourceFiles)
            {
                filepair.Refresh();
                Assert.IsTrue(filepair.IsExtra(), string.Format("\nSource:{0}\nDestination:{1}\nFile was not moved to destination directory.", filepair.Source, filepair.Destination));
            }
            bool moveDirectories = rm.CopyOptions.MoveFilesAndDirectories;
            Assert.AreEqual(moveDirectories, SourceFiles[2].Parent.IsExtra(), moveDirectories ? "Directory was not moved" : "Directory was moved unexpectedly.");
            Assert.AreEqual(moveDirectories, SourceFiles[3].Parent.IsExtra(), moveDirectories ? "Directory was not moved" : "Directory was moved unexpectedly.");

        }
    }
}
#endif