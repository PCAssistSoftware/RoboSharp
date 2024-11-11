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
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Tests
{
    [TestClass]
    public class BatchCommandTests
    {
        [TestMethod]
        public async Task TestCopyOperation()
        {
            string destination = TestPrep.GetRandomPath(true);
            try
            {
                Test_Setup.ClearOutTestDestination();
                var source = Test_Setup.GenerateCommand(true, false);
                var root = new DirectoryPair(source.CopyOptions.Source, destination);
                var files = root.EnumerateSourceFilePairs(FilePair.CreatePair).ToArray();
                var cmd = new BatchCommand(new StreamedCopierFactory());
                cmd.LoggingOptions.IncludeFullPathNames = true;
                cmd.Configuration.EnableFileLogging = true;
                cmd.AddCopiers(files);
                var results = await Test_Setup.RunTest(cmd);
                Test_Setup.WriteLogLines(results.Results);
                Assert.AreEqual(files.Count(), results.Results.FilesStatistic.Copied); // expect 4
            }
            finally
            {
                Directory.Delete(destination, true);
            }
        }
        
        [TestMethod]
        public async Task TestCancellation()
        {
            CancellationTokenSource cToken = new CancellationTokenSource();

            Test_Setup.ClearOutTestDestination();
            var source = Test_Setup.GenerateCommand(true, false);
            var root = new DirectoryPair(source.CopyOptions.Source, source.CopyOptions.Destination);
            var files = root.EnumerateSourceFilePairs(FilePair.CreatePair).ToArray();
            var cmd = new BatchCommand(new StreamedCopierFactory());
            cmd.Configuration.EnableFileLogging = true;
            cmd.LoggingOptions.NoFileList = false;
            cmd.AddCopiers(files);

            // Simulates cancellating via a UI by cancelling AFTER it was started
            cmd.OnCopyProgressChanged += (o, e) =>
            {
                if (e.CurrentFileProgress == 100)
                    cmd.Stop();
            };

            cmd.OnError += (o, e) => Console.WriteLine(e.Error);
            var results = await Test_Setup.RunTest(cmd);
            Test_Setup.WriteLogLines(results.Results);
            Assert.IsTrue(results.Results.Status.WasCancelled, "Results.Status.WasCancelled flag not set!");
            var numCopied = results.Results.FilesStatistic.Copied;
            Assert.IsTrue(numCopied < 4 && numCopied > 0, $"number copied : {numCopied}");
        }
    }
}
