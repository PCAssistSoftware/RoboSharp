using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Tests
{
    [TestClass()]
    public class StreamedCopierTests
    {
        [TestMethod()]
        public async Task IFileCopierFactoryTests_StreamedCopierFactory()
        {
            StreamedCopierFactory factory = new StreamedCopierFactory() { BufferSize = StreamedCopier.DefaultBufferSize };
            _ = await IFileCopierTests.RunTest(factory);
        }

        [TestMethod()]
        public async Task TestProgressProperty()
        {
            var copier = new StreamedCopier(TestPrep.GetRandomPath(), TestPrep.GetRandomPath());
            try
            {
                IFileCopierTests.PrepSourceAndDest(copier);

                // Pause & Resume
                var tcs = new TaskCompletionSource<object>();
                void PauseHandler(object o, CopyProgressEventArgs e)
                {
                    copier.ProgressUpdated -= PauseHandler;
                    copier.Pause();
                    tcs.SetResult(null);
                }

                var cToken = new CancellationTokenSource();
                copier.ProgressUpdated += PauseHandler;
                var copyTask = copier.CopyAsync(true, cToken.Token);
                await tcs.Task;
                await Task.Delay(250);
                var p = copier.Progress;
                await Task.Delay(250);
                Assert.AreEqual(p, copier.Progress, "\n Progress updated while paused!");
                cToken.CancelAfter(1000);
                copier.Resume();
                Assert.IsTrue(await copyTask);
                Assert.AreEqual(100, copier.Progress);
            }
            finally
            {
                await IFileCopierTests.Cleanup(copier);
            }
        }
    }
}