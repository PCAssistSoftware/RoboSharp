using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Helpers.UnitTests
{
    [TestClass()]
    public class ResultsBuilderTests
    {
        private static (IRoboCommand cmd, ResultsBuilder builder) GetBuilder()
        {
            var cmd = new RoboCommand()
            {
                Configuration = new RoboSharpConfiguration() { EnableFileLogging = true },
                LoggingOptions = new LoggingOptions()
                {
                    NoJobSummary = true,
                    NoJobHeader = true,
                    NoDirectoryList = false,
                    NoFileList = false,
                }
            };
            return (cmd, new ResultsBuilder(cmd));
        }

        [TestMethod()]
        public void ResultsBuilderTest()
        {
            Assert.IsNotNull(GetBuilder());
            Assert.IsNotNull(GetBuilder().builder);
        }

        [TestMethod()]
        public void UnsubscribeTest()
        {
            GetBuilder().builder.Unsubscribe();
        }

        [TestMethod()]
        public void AddFileTest()
        {
            var items = GetBuilder();
            var cmd = items.cmd;
            var builder = items.builder;

            var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
            cmd.LoggingOptions.NoFileList = true;
            builder.AddFile(testFile);
            Assert.AreEqual(0, builder.CurrentLogLines.LongLength);
            cmd.LoggingOptions.NoFileList = false;
            builder.AddFile(testFile);
            Assert.AreEqual(1, builder.CurrentLogLines.LongLength);
            Assert.AreEqual(2, builder.GetResults().FilesStatistic.Skipped); // Files are marked as SKIPPED if they are never completed as copied
        }

        [TestMethod()]
        public void AddFileCopiedTest()
        {
            var items = GetBuilder();
            var cmd = items.cmd;
            var builder = items.builder;

            var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
            cmd.LoggingOptions.NoFileList = true;
            builder.AddFileCopied(testFile);
            Assert.AreEqual(0, builder.CurrentLogLines.LongLength);
            cmd.LoggingOptions.NoFileList = false;
            builder.AddFileCopied(testFile);
            Assert.AreEqual(1, builder.CurrentLogLines.LongLength);
            Assert.AreEqual(2, builder.GetResults().FilesStatistic.Copied);
        }

        [TestMethod()]
        public void AddFileExtraTest()
        {
            var items = GetBuilder();
            var cmd = items.cmd;
            var builder = items.builder;

            var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
            cmd.LoggingOptions.NoFileList = true;
            builder.AddFileExtra(testFile);
            Assert.AreEqual(0, builder.CurrentLogLines.LongLength);
            cmd.LoggingOptions.NoFileList = false;
            builder.AddFileExtra(testFile);
            Assert.AreEqual(1, builder.CurrentLogLines.LongLength);
            Assert.AreEqual(2, builder.GetResults().FilesStatistic.Extras);
        }

        [TestMethod()]
        public void AddFileFailedTest()
        {
            //Failed Files are ALWAYS reported by robocopy :   [DateTime] [Error Code] [Action] [Path]
            var items = GetBuilder();
            var cmd = items.cmd;
            var builder = items.builder;

            var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
            cmd.LoggingOptions.NoFileList = true;
            builder.AddFileFailed(testFile);
            Assert.AreEqual(1, builder.CurrentLogLines.LongLength, "Log Lines do not match expected value");
            Assert.AreEqual(1, builder.GetResults().FilesStatistic.Failed, "File Statistics do not match expected value");
        }

        [TestMethod()]
        public void AddFilePurgedTest()
        {
            var items = GetBuilder();
            var cmd = items.cmd;
            var builder = items.builder;

            var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
            cmd.LoggingOptions.NoFileList = true;
            builder.AddFilePurged(testFile);
            Assert.AreEqual(0, builder.CurrentLogLines.LongLength);
            cmd.LoggingOptions.NoFileList = false;
            builder.AddFilePurged(testFile);
            Assert.AreEqual(1, builder.CurrentLogLines.LongLength);
            Assert.AreEqual(2, builder.GetResults().FilesStatistic.Extras);
        }

        [TestMethod()]
        public void AddFileSkippedTest()
        {
            var items = GetBuilder();
            var cmd = items.cmd;
            var builder = items.builder;

            var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
            cmd.LoggingOptions.NoFileList = true;
            builder.AddFileSkipped(testFile);
            Assert.AreEqual(0, builder.CurrentLogLines.LongLength);
            cmd.LoggingOptions.NoFileList = false;
            builder.AddFileSkipped(testFile);
            Assert.AreEqual(1, builder.CurrentLogLines.LongLength);
            Assert.AreEqual(2, builder.GetResults().FilesStatistic.Skipped);
        }

        // Requires custom IRoboCommand implementation to force CopyProgressUpdated
        //[TestMethod()]
        //public void SetCopyOpStartedTest()
        //{
        //    var items = GetBuilder();
        //    var cmd = items.cmd;
        //    var builder = items.builder;

        //    var testFile = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_NewFile, FileClassType = FileClassType.File, Name = "TestFileName", Size = 100 };
        //    var testFile2 = new ProcessedFileInfo() { FileClass = cmd.Configuration.LogParsing_ExtraFile, FileClassType = FileClassType.File, Name = "TestFileName2", Size = 100 };
        //    cmd.LoggingOptions.NoFileList = false;
        //    builder.AddFile(testFile);
        //    Assert.AreEqual(1, builder.CurrentLogLines.LongLength, "Failed Log Line Test #1");
        //    builder.SetCopyOpStarted(testFile);
        //    builder.AddFile(testFile2);
        //    Assert.AreEqual(2, builder.CurrentLogLines.LongLength, "Failed Log Line Test #2");
        //    var results = builder.GetResults();
        //    Assert.AreEqual(1, results.FilesStatistic.Copied, "\nFailed Copied Statistic Test");
        //    Assert.AreEqual(1, results.FilesStatistic.Extras, "\nFailed Secondary Statistic Test");
        //}

        [TestMethod()]
        public void AddDirTest()
        {
            var items = GetBuilder();
            items.builder.AddDir(new ProcessedFileInfo("Test Dir", FileClassType.NewDir, items.cmd.Configuration.LogParsing_DirectoryExclusion));
            items.builder.AddDir(new ProcessedFileInfo("Test Dir2", FileClassType.NewDir, items.cmd.Configuration.LogParsing_NewDir));
            Assert.AreEqual(2, items.builder.CurrentLogLines.Length, "Failed LogLines check");
            Assert.AreEqual(1, items.builder.GetResults().DirectoriesStatistic.Skipped, "\nFailed Skipped Evaluation");
            Assert.AreEqual(1, items.builder.GetResults().DirectoriesStatistic.Copied, "\nFailed Copied Evaluation");
        }

        [TestMethod()]
        public void AddDirSkippedTest()
        {
            var items = GetBuilder();
            items.builder.AddDirSkipped(new ProcessedFileInfo("Test Dir", FileClassType.NewDir, items.cmd.Configuration.LogParsing_DirectoryExclusion));
            items.builder.AddDir(new ProcessedFileInfo("Test Dir2", FileClassType.NewDir, items.cmd.Configuration.LogParsing_DirectoryExclusion));
            Assert.AreEqual(2, items.builder.CurrentLogLines.Length, "Failed LogLines check");
            Assert.AreEqual(2, items.builder.GetResults().DirectoriesStatistic.Skipped, "\nFailed Skipped Evaluation");
        }

        [TestMethod()]
        public void AddSystemMessageTest()
        {
            var items = GetBuilder();
            items.cmd.Configuration.EnableFileLogging = false;
            items.builder.AddSystemMessage("Test Message");
            items.builder.AddSystemMessage(new ProcessedFileInfo("Test File", FileClassType.File, items.cmd.Configuration.LogParsing_MismatchFile, 120));
            Assert.AreEqual(2, items.builder.CurrentLogLines.Length);
        }
    }
}
