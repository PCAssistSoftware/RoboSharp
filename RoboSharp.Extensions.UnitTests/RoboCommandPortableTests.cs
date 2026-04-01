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
    /// <br/> Runs the full <see cref="CommandTests{T}"/> suite against <see cref="RoboCommandPortable"/>.
    /// <br/> Failures here indicate bugs in the portable implementation, not in the test expectations (which are validated by <see cref="CommandTests"/>).
    /// </summary>
    [TestClass]
    public class RoboCommandPortable_StreamedCopier_CommandTests : RoboCommandPortable_CommandTestsBase
    {
        protected override RoboCommandPortable GetCommand() => new RoboCommandPortable(StreamedCopierFactory.DefaultFactory);
    }

#if WINDOWS || NETFRAMEWORK
    [TestClass]
    public class RoboCommandPortable_CopyFileEx_CommandTests : RoboCommandPortable_CommandTestsBase
    {
        protected override RoboCommandPortable GetCommand() => new RoboCommandPortable(new Windows.CopyFileExFactory());
    }
#endif


    public abstract class RoboCommandPortable_CommandTestsBase : CommandTests<RoboCommandPortable>
    {
        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.MoveFiles)]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.Purge)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.Purge)]
        public async Task Test_Purge_Validation(CopyActionFlags copyOptions)
        {
            var obj = new RoboSharp.UnitTests.CommandTests() { TestContext = this.TestContext };
            obj.TestInit();
            var source = await obj.PrepMoveSource();

            try
            {
                var rm = new RoboCommandPortable(StreamedCopierFactory.DefaultFactory)
                {
                    CopyOptions = new CopyOptions()
                    {
                        Source = source,
                        Destination = base.TempDest,
                    },
                };

                rm.CopyOptions.ApplyActionFlags(CopyActionFlags.CopySubdirectoriesIncludingEmpty | copyOptions);
                rm.SelectionOptions.ApplySelectionFlags(SelectionFlags.Default);
                rm.LoggingOptions.ApplyLoggingFlags(LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader | LoggingFlags.ListOnly);

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
                    Assert.AreEqual(purge, !file.Exists, purge ? "\n >> File was not purged." : "\n >> File was purged unexpectedly.");
                }
                foreach (var dir in PurgeDirectories)
                {
                    dir.Refresh();
                    Assert.AreEqual(purge, !dir.Exists, purge ? "\n >> Directory was not purged." : "\n >> Directory was purged unexpectedly.");
                }
                //evaluate moved
                foreach (var filepair in SourceFiles)
                {
                    filepair.Refresh();
                    Assert.IsTrue(filepair.Destination.Exists);
                    Assert.IsTrue(filepair.IsExtra(), string.Format("\n >> Source:{0}\nDestination:{1}\nFile was not moved to destination directory.", filepair.Source, filepair.Destination));
                }
                bool moveDirectories = rm.CopyOptions.MoveFilesAndDirectories;
                Assert.AreEqual(moveDirectories, SourceFiles[2].Parent.IsExtra(), moveDirectories ? "\n >> Directory was not moved" : "\n >> Directory was moved unexpectedly.");
                Assert.AreEqual(moveDirectories, SourceFiles[3].Parent.IsExtra(), moveDirectories ? "\n >> Directory was not moved" : "\n >> Directory was moved unexpectedly.");
            }
            finally
            {
                obj.TestCleanup();
                try { Directory.Delete(source, true); } catch { }
            }
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


        private static RoboCommandPortable GetCommand(RoboCommand rc, IFileCopierFactory factory = null)
        {
            return new RoboCommandPortable(factory ?? RoboSharp.Extensions.StreamedCopierFactory.DefaultFactory)
            {
                CopyOptions = new CopyOptions(rc.CopyOptions),
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
            var obj = new RoboSharp.UnitTests.CommandTests() { TestContext = this.TestContext };
            obj.TestInit();
            var rcSource = await obj.PrepMoveSource();
            obj.TestInit();
            var rmSource = await obj.PrepMoveSource();

            try
            {
                var rc = TestPrep.GetRoboCommand(false, copyFlags, selectionFlags, loggingFlags);
                var crc = GetCommand(rc);

                rc.CopyOptions.Source = rcSource;
                rc.CopyOptions.Destination = Test_Setup.GetNewTempPath();

                crc.CopyOptions.Source = rmSource;
                crc.CopyOptions.Destination = Test_Setup.GetNewTempPath();

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
        
    }
}
#endif