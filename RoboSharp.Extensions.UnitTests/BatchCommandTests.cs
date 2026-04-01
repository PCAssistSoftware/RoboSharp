using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
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
        public TestContext TestContext { get; set; }

        private string Destination { get; set; }

        [TestInitialize]
        public void Initialize() 
        {
            Destination = Test_Setup.GetNewTempPath();
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(Destination, true); } catch { }
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task TestCopyOperation()
        {
            var root = new DirectoryPair(Test_Setup.Source_LargerNewer, Destination);
            var files = root.EnumerateSourceFilePairs(FilePair.CreatePair).ToArray();
            var cmd = new BatchCommand(new StreamedCopierFactory());
            cmd.LoggingOptions.IncludeFullPathNames = true;
            cmd.Configuration.EnableFileLogging = true;
            cmd.AddCopiers(files);
            var results = await Test_Setup.RunTest(cmd, TestContext.CancellationToken);
            Test_Setup.WriteLogLines(results.Results);
            Assert.AreEqual(files.LongLength, results.Results.FilesStatistic.Copied); // expect 4
        }
        
        [TestMethod]
        [Timeout(5000, CooperativeCancellation =true)]
        public async Task TestCancellation()
        {
            CancellationTokenSource cToken = new CancellationTokenSource();

            var root = new DirectoryPair(Test_Setup.Source_LargerNewer, Destination);
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
            var results = await Test_Setup.RunTest(cmd, TestContext.CancellationToken);
            Test_Setup.WriteLogLines(results.Results);
            Assert.IsTrue(results.Results.Status.WasCancelled, "Results.Status.WasCancelled flag not set!");
            var numCopied = results.Results.FilesStatistic.Copied;
            Assert.IsTrue(numCopied < 4 && numCopied > 0, $"number copied : {numCopied}");
        }
    }
}
