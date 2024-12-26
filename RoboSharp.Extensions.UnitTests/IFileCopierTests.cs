using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Tests
{
    /// <summary>
    /// Run a test of the <see cref="IFileCopier"/> standard operations via the provided <see cref="IFileCopierFactory"/>
    /// </summary>
    [TestClass]
    public class IFileCopierTests
    {
        private class MyFileSource(string path) : IFileSource
        {
            public string FilePath { get; set; } = path;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Required for testing - Platform support is tested as well")]
        private static IEnumerable<object[]> GetCopierFactory()
        {
            yield return Wrap(new StreamedCopierFactory() { BufferSize = StreamedCopier.DefaultBufferSize });
            yield return Wrap(new Windows.CopyFileExFactory() { Options = Windows.CopyFileExOptions.RESTARTABLE }); // run in restartable mode to artificially slow down the copy operation, otherwise cancellation test may fail due to very quick copy times
        }
        private static IEnumerable<object[]> GetCopier() // takes the above factories and gets the copier from it
        {
            return GetCopierFactory().Select(o => Wrap(
                ((IFileCopierFactory)o[0]).Create(GetRandomPath(false), GetRandomPath(true))
            ));
        }

        private static object[] Wrap(params object[] objects) => objects;
        public static String GetCopierName(MethodInfo info, object[] objects) => objects[0].GetType().Name;

        private static string GetRandomPath(bool isSubFolder = false) => TestPrep.GetRandomPath(isSubFolder);

        public static void CreateDummyFile(string filePath, int lengthInBytes)
        {
            using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var data = new byte[lengthInBytes];
                new Random().NextBytes(data);
                fileStream.Write(data, 0, lengthInBytes);
            }
        }

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
            copier.Destination.Directory.Create();
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

        public static async Task<bool> ThrowsIfNotWindowsPlatform(IFileCopier copier)
        {
#if NET5_0_OR_GREATER
            if (VersionManager.IsPlatformWindows)
            {
                // do nothing
            }
            else if (copier.GetType().GetCustomAttribute(typeof(System.Runtime.Versioning.SupportedOSPlatformAttribute)) is System.Runtime.Versioning.SupportedOSPlatformAttribute attr)
            {
                if (attr.PlatformName.StartsWith("windows"))
                {
                    await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(copier.CopyAsync, "\r\n failed to throw PlatformNotSupported");
                    return false;
                }
            }
#endif
            await Task.CompletedTask; // ignores 'async but not awaited' warning CS1998
            return true;
        }

        /// <summary>
        /// Runs the various tests against a specific factory
        /// </summary>
        internal static async Task RunTests(IFileCopierFactory factory)
        {
            var runner = new IFileCopierTests();
            runner.RunFactoryTests(factory);
            IFileCopier copier = factory.Create(GetRandomPath(false), GetRandomPath(true));
            await runner.CopyAsyncTest(copier);
            await runner.MoveAsyncTest(copier);
            await runner.AttributesCopiedProperlyTest(copier);
        }

        /// <summary>
        /// Tests the basic functionality of an <see cref="IFileCopierFactory"/>
        /// </summary>
        [DynamicData(nameof(GetCopierFactory), dynamicDataSourceType:DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetCopierName))]
        [TestMethod]
        public void RunFactoryTests(IFileCopierFactory factory)
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
        }

        /// <summary>
        /// Tests the basic functionality of an <see cref="IFileCopier.CopyAsync()"/>
        /// </summary>
        [DynamicData(nameof(GetCopier), dynamicDataSourceType: DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetCopierName))]
        [TestMethod]
        public async Task CopyAsyncTest(IFileCopier copier)
        {
            double progress = 0;
            var tcs = new TaskCompletionSource<object>();
            bool copyResult = false;

            try
            {
                string destPath = copier.Destination.FullName;

                // check platform support
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

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

        /// <summary>
        /// Tests the basic functionality of an <see cref="IFileCopier.MoveAsync()"/>
        /// </summary>
        [DynamicData(nameof(GetCopier), dynamicDataSourceType: DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetCopierName))]
        [TestMethod]
        public async Task MoveAsyncTest(IFileCopier copier)
        {
            // Move used the Copy api, then checks for completion, and deletes source file if OK
            void PrepMove() => PrepSourceAndDest(copier, false);
            try
            {
                // check platform support
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

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

        /// <summary>
        /// Tests that attributes and file itself are copied to the destination file properly, just like if they were copied via File.CopyTo();
        /// </summary>
        [DynamicData(nameof(GetCopier), dynamicDataSourceType: DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetCopierName))]
        [TestMethod]
        public async Task AttributesCopiedProperlyTest(IFileCopier copier)
        {
            string fileCopyToDest = copier.Destination.FullName + "_control";

            string? sourceMD5 = null; string? destinationMD5 = null; string controlMD5 = null;

            try
            {
                // check platform support
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier, true);
                sourceMD5 = CalculateMD5(copier.Source.FullName);

                File.Copy(copier.Source.FullName, fileCopyToDest, true);
                FileInfo control = new(fileCopyToDest);
                await copier.CopyAsync();
                FileInfo dest = copier.Destination;
                dest.Refresh();

                // Validate File.CopyTo functionality (ensure test is valid)
                Assert.AreEqual(copier.Source.Length, control.Length, "\r\n control.Length != source.Length");
                Assert.AreEqual(copier.Source.Attributes, control.Attributes, "\r\n control.Attributes != source.Attributes");

                Assert.AreEqual(copier.Source.LastWriteTime, control.LastWriteTime, "\r\n control.LastWriteTime != source.LastWriteTime");
                Assert.AreEqual(copier.Source.LastWriteTimeUtc, control.LastWriteTimeUtc, "\r\n control.LastWriteTimeUtc != source.LastWriteTimeUtc");

                controlMD5 = CalculateMD5(fileCopyToDest);
                Assert.AreEqual(sourceMD5, controlMD5, "\r\n control.MD5 != source.MD5");


#if NET8_0_OR_GREATER
                Assert.AreEqual(copier.Source.UnixFileMode, dest.UnixFileMode, "\r\n control UnixFileMode  != source!");
#endif

                // Begin validation
                Assert.AreEqual(control.Length, dest.Length, "\r\n ifileCopier.Length != File.CopyTo.Length");
                Assert.AreEqual(control.Attributes, dest.Attributes, "\r\n ifileCopier.Attributes != File.CopyTo.Attributes");

                Assert.AreEqual(control.LastWriteTime, dest.LastWriteTime, "\r\n ifileCopier.LastWriteTime != File.CopyTo.LastWriteTime");
                Assert.AreEqual(control.LastWriteTimeUtc, dest.LastWriteTimeUtc, "\r\n ifileCopier.LastWriteTimeUtc != File.CopyTo.LastWriteTimeUtc");

                destinationMD5 = CalculateMD5(copier.Destination.FullName);
                Assert.AreEqual(sourceMD5, destinationMD5, "\r\n ifileCopier.MD5 != source.MD5");

#if NET8_0_OR_GREATER
                Assert.AreEqual(control.UnixFileMode, dest.UnixFileMode, "\r\n UnixFileMode does not match!");
#endif
            }
            catch
            {
                FileInfo c = new(fileCopyToDest);
                string dateTimeMs = "yyyy/MM/dd hh:mm:ss.fff tt";
                Console.WriteLine("-----"); 
                Console.WriteLine($"Source      Length: {copier.Source.Length}");
                Console.WriteLine($"File.CopyTo Length: {c.Length}");
                Console.WriteLine($"IFileCopier Length: {copier.Destination.Length}");
                Console.WriteLine($"Difference in length (ifileCopier - source) : {copier.Destination.Length - copier.Source.Length}");
                Console.WriteLine("-----");
                Console.WriteLine($"Source      Attributes: {copier.Source.Attributes}");
                Console.WriteLine($"File.CopyTo Attributes: {c.Attributes}");
                Console.WriteLine($"IFileCopier Attributes: {copier.Destination.Attributes}");
                Console.WriteLine("-----");
                Console.WriteLine($"Source      LastWriteTimeUTC: {copier.Source.LastWriteTimeUtc.ToString(dateTimeMs)}");
                Console.WriteLine($"File.CopyTo LastWriteTimeUTC: {c.LastWriteTimeUtc.ToString(dateTimeMs)}");
                Console.WriteLine($"IFileCopier LastWriteTimeUTC: {copier.Destination.LastWriteTimeUtc.ToString(dateTimeMs)}");
                Console.WriteLine("-----");
                Console.WriteLine($"Source      MD5: {sourceMD5 ?? CalculateMD5(copier.Source.FullName)}");
                Console.WriteLine($"File.CopyTo MD5: {controlMD5 ?? CalculateMD5(c.FullName)}");
                Console.WriteLine($"IFileCopier MD5: {destinationMD5 ?? CalculateMD5(copier.Destination.FullName)}");
                throw;
            }
            finally
            {
                if (File.Exists(fileCopyToDest)) File.Delete(fileCopyToDest);
                await Cleanup(copier);
                TestPrep.CleanAppData();
            }

            static string CalculateMD5(string filename)
            {
                if (File.Exists(filename))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            var hash = md5.ComputeHash(stream);
                            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
                return null;
            }
        }
    }
}
