using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    /// <summary>
    /// Run a test of the <see cref="IFileCopier"/> standard operations via the provided <see cref="IFileCopierFactory"/>
    /// </summary>
    public static class IFileCopierTests
    {
        public static void CreateDummyFile(string filePath, int lengthInBytes)
        {
            using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var data = new byte[lengthInBytes];
                new Random().NextBytes(data);
                fileStream.Write(data, 0, lengthInBytes);
            }
        }

        private static string GetRandomPath(bool inSubFolder = false) => TestPrep.GetRandomPath(inSubFolder);

        public static void PrepSourceAndDest(IFileCopier copier, bool deleteDest = true)
        {
            // Create a 8MB file for testing
            long size = 1024L*1024 * 8;
            if (!copier.Source.Exists || copier.Source.Exists && copier.Source.Length < size)
            {
                copier.Source.Directory.Create();
                CreateDummyFile(copier.Source.FullName, (int)size);
                copier.Source.Refresh();
            }
            if (deleteDest && File.Exists(copier.Destination.FullName)) copier.Destination.Delete();
        }

        public static async Task Cleanup(IFileCopier copier, bool deleteSource = true)
        {
            if (copier is IAsyncDisposable adp)
                await adp.DisposeAsync();
            else if (copier is IDisposable dp)
                dp.Dispose();
            
            if (deleteSource && File.Exists(copier.Source.FullName)) copier.Source.Delete();
            if (File.Exists(copier.Destination.FullName)) copier.Destination.Delete();
            copier.Source.Refresh();
            copier.Destination.Refresh();
            if (copier.Destination.Directory.Exists && copier.Destination.Directory.FullName != copier.Source.Directory.FullName)
                copier.Destination.Directory.Delete(false);
        }

        /// <summary>
        /// Run all tests on the <paramref name="factory"/>
        /// </summary>
        /// <param name="factory"></param>
        /// <returns>The <see cref="IFileCopier"/> that can be used for additional testing if required</returns>
        public static async Task<IFileCopier> RunTest(IFileCopierFactory factory)
        {
            IFileCopier copier = RunFactoryTests(factory);
            await CopyAsyncTest(copier);
            await MoveAsyncTest(copier);
            return copier;
        }

        /// <summary>
        /// Tests the basic functionality of an <see cref="IFileCopierFactory"/>
        /// </summary>
        private static IFileCopier RunFactoryTests(IFileCopierFactory factory)
        {
            Console.WriteLine($"IFileCopierFactory Type : {factory.GetType()}");
            Test_Setup.PrintEnvironment();
            FileInfo source = new FileInfo(GetRandomPath(false));
            FileInfo dest = new FileInfo(GetRandomPath(true));
            
            IFileCopier cp;
            
            Assert.IsNotNull(factory.Create(new FilePair(source, dest)));
            
            // Create at destination dir
            Assert.IsNotNull(cp = factory.Create(new MyFileSource(source.FullName), dest.Directory));
            Assert.AreEqual(source.Name, cp.Destination.Name, "\n --- Created destination does not match source file name");
            Assert.AreEqual(dest.Directory.FullName, cp.Destination.Directory.FullName, "\n --- Created destination does reside in expected destination directory");

            // Create at destination file path
            Assert.IsNotNull(cp = factory.Create(new MyFileSource(source.FullName), dest.FullName));
            Assert.AreEqual(source.Name, cp.Source.Name, "\n --- Created source does not match expected file name");
            Assert.AreEqual(dest.Name, cp.Destination.Name, "\n --- Created destination does not match expected file name");

            // Create at destination file path
            Assert.IsNotNull(cp = factory.Create(source.FullName, dest.FullName));
            Assert.AreEqual(source.Name, cp.Source.Name, "\n --- Created source does not match expected file name");
            Assert.AreEqual(dest.Name, cp.Destination.Name, "\n --- Created destination does not match expected file name");

            Console.WriteLine($"IFileCopier Created : {cp.GetType()}");
            return cp;
        }
        private class MyFileSource : IFileSource
        {
            public MyFileSource(string path) { FilePath = path; }
            public string FilePath { get; set; }
        }

        private static async Task CopyAsyncTest(IFileCopier copier)
        {
            double progress = 0;
            var tcs = new TaskCompletionSource<object>();
            bool copyResult = false;

            try
            {
                string destPath = copier.Destination.FullName;

                //Source is missing
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(copier.CopyAsync, "\n --Did not throw when source is missing \n");

                PrepSourceAndDest(copier);

                // Test Copy
                Assert.IsTrue(await copier.CopyAsync(), "\n -- IFileCopierTests - Copy - Test 1\n");
                Assert.IsTrue(await copier.CopyAsync(true), "\n -- IFileCopierTests - Copy - Test 2\n");
                Assert.IsTrue(await copier.CopyAsync(true, CancellationToken.None), "\n -- IFileCopierTests - Copy - Test 3\n");

                //File already exists
                await Assert.ThrowsExceptionAsync<IOException>(() => copier.CopyAsync(), "\n -- IFileCopierTests - Prevent Overwrite - Test 1\n");
                await Assert.ThrowsExceptionAsync<IOException>(() => copier.CopyAsync(false), "\n -- IFileCopierTests - Prevent Overwrite - Test 2\n");
                await Assert.ThrowsExceptionAsync<IOException>(() => copier.CopyAsync(false, CancellationToken.None), "\n -- IFileCopierTests - Prevent Overwrite - Test 3\n");
                await Cleanup(copier, false);

                // Cancellation Test 1 -- BEFORE start of the operation
                CancellationTokenSource cToken = new CancellationTokenSource();
                cToken.Cancel();
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(async () => copyResult = await copier.CopyAsync(true, cToken.Token), "\n -- Cancellation Token Test (1)\n");
                Assert.IsFalse(File.Exists(destPath), "\nCancelled operation did not delete destination file (1)");
                Assert.IsFalse(copier.Destination.Exists, "\nDestination object was not refreshed (1)");


                // Cancellation Test 2 -- Mid-Write + tests ProgressUpdated
                copier.ProgressUpdated += CancelEventHandler;
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(async () => copyResult = await copier.CopyAsync(true, CancellationToken.None), "\n -- Copier.Cancel() Test (2)\n");
                Assert.IsFalse(File.Exists(destPath), "\nCancelled operation did not delete destination file (2)");
                Assert.IsFalse(copier.Destination.Exists, "\nDestination object was not refreshed (2)");
                copier.ProgressUpdated -= CancelEventHandler;

                // Cancellation Test 3 -- Mid-Write + Trigger via Cancellation Token
                cToken = new CancellationTokenSource();
                void tokenHandler(object o, EventArgs e) => cToken.Cancel();
                copier.ProgressUpdated += tokenHandler;
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(async () => copyResult = await copier.CopyAsync(true, cToken.Token), "\n -- Cancellation Test 3\n");
                Assert.IsFalse(File.Exists(destPath), "\nCancelled operation did not delete destination file (3)");
                Assert.IsFalse(copier.Destination.Exists, "\nDestination object was not refreshed (3)");
                copier.ProgressUpdated -= tokenHandler;

                // Pause & Resume
                cToken = new CancellationTokenSource();
                copier.ProgressUpdated += PauseHandler;
                copier.ProgressUpdated += ProgressUpdates;
                var copyTask = copier.CopyAsync(true, cToken.Token);
                await tcs.Task;
                await Task.Delay(150);
                var p = progress;
                await Task.Delay(150);
                Assert.AreEqual(p, progress, "\n Progress updated while paused!");
                cToken.CancelAfter(1000);
                copier.Resume();
                Assert.IsTrue(await copyTask);
                Assert.AreEqual(100, progress);
                copier.ProgressUpdated -= ProgressUpdates;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("\n----------------\nSource File Path      : {0}", copier.Source));
                Console.WriteLine(string.Format("Destination File Path : {0}", copier.Destination));
                Console.WriteLine(string.Format("\nException : {0}\n----------------", e.Message));
                throw;
            }
            finally
            {
                await Cleanup(copier);
                TestPrep.CleanAppData();
            }

            // Helper Methods

            void PauseHandler(object o, CopyProgressEventArgs e)
            {
                copier.ProgressUpdated -= PauseHandler;
                copier.Pause();
                tcs.SetResult(null);
            }
            void ProgressUpdates(object o, CopyProgressEventArgs e)
            {
                progress = e.CurrentFileProgress;
            }
            void CancelEventHandler(object sender, EventArgs e)
            {
                if (sender is IFileCopier cp)
                    cp.Cancel();
            }

        }

        public static async Task MoveAsyncTest(IFileCopier copier)
        {
            // Move used the Copy api, then checks for completion, and deletes source file if OK
            void PrepMove() => PrepSourceAndDest(copier, false);
            try
            {
                //Source is missing
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => copier.MoveAsync(), "\n --Did not throw when source is missing \n");

                const string fileNotMoved = "\n -- IFileCopierTests - Move - Source Not Moved - Test {0}\n";
                const string fileMoved = "\n -- IFileCopierTests - Move - Source Not Moved - Test {0}\n";

                // Test Move
                PrepMove();
                Assert.IsTrue(await copier.MoveAsync(), "\n -- IFileCopierTests - Move - Test 1");
                Assert.IsFalse(File.Exists(copier.Source.FullName), string.Format(fileNotMoved, 1));

                PrepMove();
                Assert.IsTrue(await copier.MoveAsync(true), "\n -- IFileCopierTests - Move - Test 2\n");
                Assert.IsFalse(File.Exists(copier.Source.FullName), string.Format(fileNotMoved,2));

                PrepMove();
                Assert.IsTrue(await copier.MoveAsync(true, CancellationToken.None), "\n -- IFileCopierTests - Move - Test 3\n");
                Assert.IsFalse(File.Exists(copier.Source.FullName), string.Format(fileNotMoved, 3));

                //File already exists
                PrepSourceAndDest(copier, false);
                await Assert.ThrowsExceptionAsync<IOException>(() => copier.MoveAsync(), "\n -- IFileCopierTests - Move - Prevent Overwrite - Test 1\n");
                Assert.IsTrue(File.Exists(copier.Source.FullName), string.Format(fileMoved, 1));

                await Assert.ThrowsExceptionAsync<IOException>(() => copier.MoveAsync(false), "\n -- IFileCopierTests - Move - Prevent Overwrite - Test 2\n");
                Assert.IsTrue(File.Exists(copier.Source.FullName), string.Format(fileMoved, 2));

                await Assert.ThrowsExceptionAsync<IOException>(() => copier.MoveAsync(false, CancellationToken.None), "\n -- IFileCopierTests - Move - Prevent Overwrite - Test 3\n");
                Assert.IsTrue(File.Exists(copier.Source.FullName), string.Format(fileMoved, 3));
                await Cleanup(copier, false);
            }
            catch(Exception e)
            {
                Console.WriteLine(string.Format("\n----------------\nSource File Path      : {0}", copier.Source));
                Console.WriteLine(string.Format("Destination File Path : {0}", copier.Destination));
                Console.WriteLine(string.Format("Exception : {0}\n\n----------------", e.Message));
                throw;
            }
            finally
            {
                await Cleanup(copier);
                TestPrep.CleanAppData();
            }
        }
    }
}
