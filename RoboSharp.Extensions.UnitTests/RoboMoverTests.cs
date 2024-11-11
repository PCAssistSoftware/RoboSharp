using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RoboSharp.Extensions.Tests
{
    [TestClass]
    public class RoboMoverEventTests : RoboSharp.UnitTests.RoboCommandEventTests
    {
        protected override IRoboCommand GenerateCommand(bool UseLargerFileSet, bool ListOnlyMode)
        {
            return TestPrep.GetIRoboCommand<RoboMover>(base.GenerateCommand(UseLargerFileSet, ListOnlyMode));
        }
    }

    [TestClass]
    public class RoboMoverTests
    {
        const LoggingFlags DefaultLoggingAction = LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader;

        static string GetMoveSource()
        {
            string original = TestPrep.SourceDirPath;
            return Path.Combine(original.Replace(Path.GetFileName(original), ""), "MoveSource");
        }

        [DataRow(true, @"C:\SomeDir")]
        [DataRow(false, @"D:\System Volume Information")]
        [TestMethod]
        public void IsAllowedDir(bool expected, string path)
        {
            Assert.AreEqual(expected, RoboMover.IsAllowedRootDirectory(new DirectoryInfo(path)));
        }

        /// <summary>
        /// Copy Test will use a standard ROBOCOPY command
        /// </summary>
        [TestMethod]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.Mirror }, DisplayName = "Mirror")]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.Default }, DisplayName = "Defaults")]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.CopySubdirectories }, DisplayName = "Subdirectories")]
        [DataRow(data: new object[] { DefaultLoggingAction, SelectionFlags.Default, CopyActionFlags.CopySubdirectoriesIncludingEmpty }, DisplayName = "EmptySubdirectories")]
        public void CopyTest(object[] flags)
        {
            Test_Setup.ClearOutTestDestination();
            CopyActionFlags copyAction = (CopyActionFlags)flags[2];
            SelectionFlags selectionFlags = (SelectionFlags)flags[1];
            LoggingFlags loggingAction = (LoggingFlags)flags[0];

            var rc = TestPrep.GetRoboCommand(false, copyAction, selectionFlags, loggingAction);
            var crc = TestPrep.GetIRoboCommand<RoboMover>(rc);

            rc.LoggingOptions.ListOnly = true;
            var results1 = TestPrep.RunTests(rc, crc, false).Result;
            TestPrep.CompareTestResults(results1[0], results1[1], rc.LoggingOptions.ListOnly);

            rc.LoggingOptions.ListOnly = false;
            var results2 = TestPrep.RunTests(rc, crc, true).Result;
            TestPrep.CompareTestResults(results2[0], results2[1], rc.LoggingOptions.ListOnly);
        }

        private static void GetMoveCommands(CopyActionFlags copyFlags, SelectionFlags selectionFlags, LoggingFlags loggingFlags, out RoboCommand rc, out RoboMover rm)
        {
            rc = TestPrep.GetRoboCommand(false, copyFlags, selectionFlags, loggingFlags);
            rc.CopyOptions.Source = GetMoveSource();
            rm = TestPrep.GetIRoboCommand<RoboMover>(rc);
        }

        private static void PrepMoveFiles()
        {
            var rc = TestPrep.GetRoboCommand(false, CopyActionFlags.CopySubdirectoriesIncludingEmpty, SelectionFlags.Default, DefaultLoggingAction);
            rc.CopyOptions.Destination = GetMoveSource();
            Directory.CreateDirectory(rc.CopyOptions.Destination);
            rc.Start().Wait();
            var results = rc.GetResults();
            if (results.RoboCopyErrors.Length > 0)
                throw new Exception(
                    "Prep Failed  \n" +
                    string.Concat(args: results.RoboCopyErrors.Select(e => "\n RoboCommandError :\t" + e.GetType() + "\t" + e.ErrorDescription + "\t:\t" + e.ErrorPath).ToArray()) +
                    "\n"
                    );
        }

        private const CopyActionFlags Mov_ = CopyActionFlags.MoveFiles;
        private const CopyActionFlags Move = CopyActionFlags.MoveFilesAndDirectories;

        /// <summary>
        /// This uses the actual logic provided by the RoboMover object
        /// </summary>
        [TestMethod]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        [DataRow(data: new object[] { Mov_ | CopyActionFlags.CopySubdirectories, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories | Move Files")]
        [DataRow(data: new object[] { Move | CopyActionFlags.CopySubdirectories, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories | Move Files and Directories")]
        [DataRow(data: new object[] { Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories-Empty | Move Files")]
        [DataRow(data: new object[] { Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Subdirectories-Empty | Move Files and Directories")]
        public void MoveTest(object[] flags)
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = TestPrep.RunTests(rc, rm, !listOnly, PrepMoveFiles).Result;
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);
        }

        [TestMethod]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public void FileInclusionTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            rc.CopyOptions.FileFilter = new string[] { "*.txt" };
            var results1 = TestPrep.RunTests(rc, rm, !listOnly, PrepMoveFiles).Result;
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);
        }

        [TestMethod]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public void FileExclusionTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            rc.SelectionOptions.ExcludedFiles.Add("*.txt");
            rc.Configuration.EnableFileLogging = true;
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = TestPrep.RunTests(rc, rm, !listOnly, PrepMoveFiles).Result;
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);
        }


        [TestMethod]
        [DataRow(data: new object[] { Move | CopyActionFlags.CopySubdirectories, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Include Subdirectories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public void ExtraFileTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2] | LoggingFlags.ReportExtraFiles, out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = TestPrep.RunTests(rc, rm, !listOnly, CreateFile).Result;
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);

            void CreateFile()
            {
                PrepMoveFiles();
                string path = Path.Combine(TestPrep.DestDirPath, "ExtraFileTest.txt");
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(TestPrep.DestDirPath);
                    File.WriteAllText(path, "This is an extra file");
                }
            }
        }

        [TestMethod]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction }, DisplayName = "Move Files and Directories")]
        [DataRow(data: new object[] { Mov_, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files")]
        [DataRow(data: new object[] { Move, SelectionFlags.Default, DefaultLoggingAction | LoggingFlags.ListOnly }, DisplayName = "ListOnly | Move Files and Directories")]
        public void SameFileTest(object[] flags) //CopyActionFlags copyAction, SelectionFlags selectionFlags, LoggingFlags loggingAction
        {
            if (Test_Setup.IsRunningOnAppVeyor()) return;
            GetMoveCommands((CopyActionFlags)flags[0], (SelectionFlags)flags[0], (LoggingFlags)flags[2], out var rc, out var rm);
            bool listOnly = rc.LoggingOptions.ListOnly;
            var results1 = TestPrep.RunTests(rc, rm, !listOnly, CreateFile).Result;
            TestPrep.CompareTestResults(results1[0], results1[1], listOnly);

            void CreateFile()
            {
                PrepMoveFiles();
                Directory.CreateDirectory(TestPrep.DestDirPath);
                string fn = "1024_Bytes.txt";
                string dest = Path.Combine(TestPrep.DestDirPath, fn);
                if (!File.Exists(dest))
                    File.Copy(Path.Combine(TestPrep.SourceDirPath, fn), dest);
            }
        }
        
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
        [TestMethod]
        public void Purge_Depth(int depth, bool listOnly, CopyActionFlags flags, LoggingFlags? loggs = null)
        {
            LoggingFlags log = loggs.HasValue ? loggs.Value | DefaultLoggingAction : DefaultLoggingAction;
            GetMoveCommands(flags, SelectionFlags.Default, log, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.CopyOptions.Depth = depth;
            RunPurge(cmd, mover);
        }

        [DataRow(true, Mov_)]
        [DataRow(false, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(false, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(false, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [TestMethod]
        public void Purge_ExcludeFiles(bool listOnly, CopyActionFlags flags)
        {
            GetMoveCommands(flags, SelectionFlags.Default, DefaultLoggingAction, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.SelectionOptions.ExcludedFiles.Add("*0*_Bytes.txt");
            RunPurge(cmd, mover);
        }

        [DataRow(true, Mov_)]
        [DataRow(true, Move)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectories)]
        [DataRow(true, Mov_ | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [DataRow(true, Move | CopyActionFlags.CopySubdirectoriesIncludingEmpty)]
        [TestMethod]
        public void Purge_ExcludeFolders(bool listOnly, CopyActionFlags flags)
        {
            GetMoveCommands(flags, SelectionFlags.Default, DefaultLoggingAction, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.SelectionOptions.ExcludedDirectories.Add("EmptyFolder1"); // Top level empty
            cmd.SelectionOptions.ExcludedDirectories.Add("EmptyFolder4"); // Bottom level empty
            cmd.SelectionOptions.ExcludedDirectories.Add("SubFolder_2a"); // folder with contents
            RunPurge(cmd, mover);
        }

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
        [TestMethod]
        public void Purge_IncludedFiles(bool listOnly, CopyActionFlags flags, LoggingFlags? loggs = null)
        {
            LoggingFlags log = loggs.HasValue ? loggs.Value | DefaultLoggingAction : DefaultLoggingAction;
            GetMoveCommands(flags, SelectionFlags.Default, log, out var cmd, out var mover);
            cmd.LoggingOptions.ListOnly = listOnly;
            cmd.CopyOptions.FileFilter = new string[] { "*0*_Bytes.txt" };
            RunPurge(cmd, mover);
        }

        private void RunPurge(RoboCommand cmd, RoboMover mover)
        {
            //if (Test_Setup.IsRunningOnAppVeyor()) return;
            var results = TestPrep.RunTests(cmd, mover, !cmd.LoggingOptions.ListOnly, CreateFilesToPurge).Result;
            TestPrep.CompareTestResults(results[0], results[1], cmd.LoggingOptions.ListOnly);
        }

        [DataRow(CopyActionFlags.MoveFiles)]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.Purge)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.Purge)]
        [DataTestMethod]
        public void ValidateRoboMover(CopyActionFlags copyOptions)
        {
            GetMoveCommands(
                CopyActionFlags.CopySubdirectoriesIncludingEmpty | copyOptions,
                SelectionFlags.Default,
                DefaultLoggingAction,
                out _, out var rm);
            Test_Setup.ClearOutTestDestination();
            PrepMoveFiles();
            
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

            rm.Start().Wait();
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
            foreach(var filepair in SourceFiles)
            {
                filepair.Refresh();
                Assert.IsTrue(filepair.IsExtra(), string.Format("\nSource:{0}\nDestination:{1}\nFile was not moved to destination directory.", filepair.Source, filepair.Destination));
            }
            bool moveDirectories = rm.CopyOptions.MoveFilesAndDirectories;
            Assert.AreEqual(moveDirectories, SourceFiles[2].Parent.IsExtra(), moveDirectories ? "Directory was not moved" : "Directory was moved unexpectedly.");
            Assert.AreEqual(moveDirectories, SourceFiles[3].Parent.IsExtra(), moveDirectories ? "Directory was not moved" : "Directory was moved unexpectedly.");

        }

        [DataTestMethod]
        public void CreateFilesToPurge()
        {
            PrepMoveFiles();
            RoboCommand prep = new RoboCommand();
            prep.CopyOptions.Source = Path.Combine(Test_Setup.Source_Standard, "SubFolder_1");
            prep.CopyOptions.Destination = Path.Combine(Test_Setup.TestDestination, "SubFolder_3");
            prep.CopyOptions.ApplyActionFlags(CopyActionFlags.CopySubdirectoriesIncludingEmpty);
            Directory.CreateDirectory(Path.Combine(prep.CopyOptions.Destination, "EmptyFolder1", "EmptyFolder2"));
            prep.Start().Wait();
            prep.CopyOptions.Source = Path.Combine(Test_Setup.Source_Standard, "SubFolder_2");
            prep.CopyOptions.Destination = Path.Combine(prep.CopyOptions.Destination, "SubFolder_2a");
            prep.Start().Wait();
            Directory.CreateDirectory(Path.Combine(prep.CopyOptions.Destination, "EmptyFolder3", "EmptyFolder4"));
        }
    }
}