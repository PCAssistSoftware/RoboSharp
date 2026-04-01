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
    [TestClass]
    public sealed class StreamedCopierTests : IFileCopierTests<StreamedCopierFactory>
    {
        protected override StreamedCopierFactory GetFactory()
        {
            return new StreamedCopierFactory() { BufferSize = StreamedCopier.DefaultBufferSize };
        }
    }

    /// <summary>
    /// Run a test of the <see cref="IFileCopier"/> standard operations via the provided <see cref="IFileCopierFactory"/>
    /// </summary>
    public abstract class IFileCopierTests<TFactory> where TFactory : IFileCopierFactory
    {
        private class MyFileSource(string path) : IFileSource
        {
            public string FilePath { get; set; } = path;
        }

        public TestContext TestContext { get; set; }
        protected string Source { get; set; }
        protected string Destination { get; set; }

        /// <summary>
        /// Abstract method that derived classes must implement to provide the factory instance
        /// </summary>
        protected abstract TFactory GetFactory();

        /// <summary>
        /// Gets a copier instance from the factory using the Source and Destination paths
        /// </summary>
        protected IFileCopier GetCopier() => GetFactory().Create(Source, Destination);

        [TestInitialize]
        public void Initialize()
        {
            Test_Setup.PrintEnvironment(TestContext);
            Source = Test_Setup.GetNewTempPath();
            Destination = Test_Setup.GetNewTempPath();
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (!string.IsNullOrEmpty(Source) && File.Exists(Source))
                {
                    File.Delete(Source);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            try
            {
                if (!string.IsNullOrEmpty(Destination) && File.Exists(Destination))
                {
                    File.Delete(Destination);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }



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
            // Create 32MB file for testing
            long size = 1024L * 1024 * 16;
            if (!copier.Source.Exists || copier.Source.Exists && copier.Source.Length < size)
            {
                copier.Source.Directory.Create();
                CreateDummyFile(copier.Source.FullName, (int)size);
            }
            copier.Source.Refresh();
            Assert.AreEqual(size, copier.Source.Length);
            if (deleteDest && File.Exists(copier.Destination.FullName)) copier.Destination.Delete();
            copier.Destination.Directory.Create();
        }

        public static async Task CleanupCopier(IFileCopier copier, bool deleteSource = true)
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
                    await Assert.ThrowsAsync<PlatformNotSupportedException>(copier.CopyAsync, "\r\n failed to throw PlatformNotSupported");
                    return false;
                }
            }
#endif
            await Task.CompletedTask; // ignores 'async but not awaited' warning CS1998
            return true;
        }

        /// <summary>
        /// Tests the basic functionality of an <see cref="IFileCopierFactory"/>
        /// </summary>
        [TestMethod]
        public void IFileCopierTest_RunFactoryTests()
        {
            TFactory factory = GetFactory();
            Console.WriteLine($"IFileCopierFactory Type : {factory.GetType()}");
            
            FileInfo source = new FileInfo(Source);
            FileInfo dest = new FileInfo(Destination);

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

        #region CopyAsync Tests

        /// <summary>
        /// Tests CopyAsync with missing source file
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_MissingSourceThrows()
        {
            IFileCopier copier = GetCopier();
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                await Assert.ThrowsAsync<FileNotFoundException>(copier.CopyAsync, "\n -- Did not throw when source is missing \n");
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests CopyAsync basic copy operations with various parameter combinations
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_BasicFunctionality()
        {
            IFileCopier copier = GetCopier();
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier);

                // Test Copy - default parameters
                Assert.IsTrue(await copier.CopyAsync(), "\n -- CopyAsync_BasicFunctionality - Test 1 (default params)\n");
                Assert.IsTrue(File.Exists(copier.Destination.FullName), "\n -- Destination file not created - Test 1\n");

                // Clean destination for next test
                await CleanupCopier(copier, deleteSource: false);
                PrepSourceAndDest(copier, deleteDest: true);

                // Test Copy - with overwrite = true
                Assert.IsTrue(await copier.CopyAsync(true), "\n -- CopyAsync_BasicFunctionality - Test 2 (overwrite=true)\n");
                Assert.IsTrue(File.Exists(copier.Destination.FullName), "\n -- Destination file not created - Test 2\n");

                // Clean destination for next test
                await CleanupCopier(copier, deleteSource: false);
                PrepSourceAndDest(copier, deleteDest: true);

                // Test Copy - with overwrite = true and cancellation token
                Assert.IsTrue(await copier.CopyAsync(true, TestContext.CancellationToken), "\n -- CopyAsync_BasicFunctionality - Test 3 (overwrite=true, cancellation token)\n");
                Assert.IsTrue(File.Exists(copier.Destination.FullName), "\n -- Destination file not created - Test 3\n");
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests that CopyAsync prevents overwriting existing files
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_PreventOverwrite()
        {
            IFileCopier copier = GetCopier();
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier);

                // First copy succeeds
                Assert.IsTrue(await copier.CopyAsync(true), "\n -- CopyAsync_PreventOverwrite - Initial copy failed\n");

#pragma warning disable MSTEST0049 // Flow TestContext.CancellationToken to async operations
                // Attempt to copy again without overwrite flag - should throw IOException
                await Assert.ThrowsAsync<IOException>(() => copier.CopyAsync(), "\n -- CopyAsync_PreventOverwrite - Test 1 (default params)\n");
                await Assert.ThrowsAsync<IOException>(() => copier.CopyAsync(false), "\n -- CopyAsync_PreventOverwrite - Test 2 (overwrite=false)\n");
                await Assert.ThrowsAsync<IOException>(() => copier.CopyAsync(false, TestContext.CancellationToken), "\n -- CopyAsync_PreventOverwrite - Test 3 (overwrite=false, cancellation token)\n");
#pragma warning restore MSTEST0049 // Flow TestContext.CancellationToken to async operations

                await CleanupCopier(copier, deleteSource: false);
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests CopyAsync cancellation before the operation starts
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_CancellationBeforeStart()
        {
            IFileCopier copier = GetCopier();
            string destPath = copier.Destination.FullName;
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier);

                // Cancel before starting
                CancellationTokenSource cToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
                cToken.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await copier.CopyAsync(true, cToken.Token), "\n -- Cancellation Token Test (before start)\n");

                Assert.IsFalse(File.Exists(destPath), "\n -- Cancelled operation did not delete destination file (before start)\n");
                Assert.IsFalse(copier.Destination.Exists, "\n -- Destination object was not refreshed (before start)\n");
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests CopyAsync cancellation during operation using Pause/Cancel methods
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_CancellationMidOperation()
        {
            IFileCopier copier = GetCopier();
            string destPath = copier.Destination.FullName;
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier);

                copier.ProgressUpdated += CancelEventHandler;

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await copier.CopyAsync(true, TestContext.CancellationToken), "\n >> Failed to throw OperationCancelledException");

                Assert.IsFalse(File.Exists(destPath), "\n -- Cancelled operation did not delete destination file (mid-operation)\n");
                Assert.IsFalse(copier.Destination.Exists, "\n -- Destination object was not refreshed (mid-operation)\n");

                copier.ProgressUpdated -= CancelEventHandler;
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }

            void CancelEventHandler(object sender, EventArgs e)
            {
                copier.Cancel();
            }
        }

        /// <summary>
        /// Tests CopyAsync cancellation triggered via CancellationToken during operation
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_CancellationViaToken()
        {
            IFileCopier copier = GetCopier();
            string destPath = copier.Destination.FullName;
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier);

                CancellationTokenSource cToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
                void tokenHandler(object o, EventArgs e) => cToken.Cancel();

                copier.ProgressUpdated += tokenHandler;

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await copier.CopyAsync(true, cToken.Token), "\n -- Cancellation Test (via token during operation)\n");

                Assert.IsFalse(File.Exists(destPath), "\n -- Cancelled operation did not delete destination file (via token)\n");
                Assert.IsFalse(copier.Destination.Exists, "\n -- Destination object was not refreshed (via token)\n");

                copier.ProgressUpdated -= tokenHandler;
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests CopyAsync Pause and Resume functionality
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_CopyAsync_PauseAndResume()
        {
            IFileCopier copier = GetCopier();
            double progress = 0;
            var tcs = new TaskCompletionSource<object>();

            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier);

                CancellationTokenSource cToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
                copier.ProgressUpdated += PauseHandler;
                copier.ProgressUpdated += ProgressUpdates;

                var copyTask = copier.CopyAsync(true, cToken.Token);
                await tcs.Task; // Wait for pause signal

                await Task.Delay(150, TestContext.CancellationToken);
                var pausedProgress = progress;

                await Task.Delay(150, TestContext.CancellationToken);
                Assert.AreEqual(pausedProgress, progress, "\n -- Progress updated while paused!\n");

                cToken.CancelAfter(1000);
                copier.Resume();

                Assert.IsTrue(await copyTask, "\n -- Copy did not complete after resume\n");
                Assert.AreEqual(100, progress, "\n -- Progress not at 100% after completion\n");

                copier.ProgressUpdated -= ProgressUpdates;
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }

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
        }

        #endregion

        #region MoveAsync Tests

        /// <summary>
        /// Tests MoveAsync with missing source file
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_MoveAsync_MissingSourceThrows()
        {
            IFileCopier copier = GetCopier();
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                await Assert.ThrowsAsync<FileNotFoundException>(() => copier.MoveAsync(), "\n -- Did not throw when source is missing \n");
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests MoveAsync basic move operations with various parameter combinations
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_MoveAsync_BasicFunctionality()
        {
            IFileCopier copier = GetCopier();
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                // Test Move - default parameters
                PrepSourceAndDest(copier, deleteDest: false);
                Assert.IsTrue(await copier.MoveAsync(), "\n -- MoveAsync_BasicFunctionality - Test 1 (default params)\n");
                Assert.IsFalse(File.Exists(copier.Source.FullName), "\n -- Source file not deleted after move - Test 1\n");
                Assert.IsTrue(File.Exists(copier.Destination.FullName), "\n -- Destination file not created - Test 1\n");

                // Test Move - with overwrite = true
                PrepSourceAndDest(copier, deleteDest: false);
                Assert.IsTrue(await copier.MoveAsync(true), "\n -- MoveAsync_BasicFunctionality - Test 2 (overwrite=true)\n");
                Assert.IsFalse(File.Exists(copier.Source.FullName), "\n -- Source file not deleted after move - Test 2\n");
                Assert.IsTrue(File.Exists(copier.Destination.FullName), "\n -- Destination file not created - Test 2\n");

                // Test Move - with overwrite = true and cancellation token
                PrepSourceAndDest(copier, deleteDest: false);
                Assert.IsTrue(await copier.MoveAsync(true, TestContext.CancellationToken), "\n -- MoveAsync_BasicFunctionality - Test 3 (overwrite=true, cancellation token)\n");
                Assert.IsFalse(File.Exists(copier.Source.FullName), "\n -- Source file not deleted after move - Test 3\n");
                Assert.IsTrue(File.Exists(copier.Destination.FullName), "\n -- Destination file not created - Test 3\n");
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        /// <summary>
        /// Tests that MoveAsync prevents overwriting existing files
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_MoveAsync_PreventOverwrite()
        {
            IFileCopier copier = GetCopier();
            try
            {
                if (await ThrowsIfNotWindowsPlatform(copier) is false) return;

                PrepSourceAndDest(copier, deleteDest: false);

                // First move succeeds
                Assert.IsTrue(await copier.MoveAsync(), "\n -- MoveAsync_PreventOverwrite - Initial move failed\n");
                Assert.IsFalse(File.Exists(copier.Source.FullName), "\n -- Source file not deleted after initial move\n");

                // Attempt to move again without overwrite flag - should throw IOException
                PrepSourceAndDest(copier, deleteDest: false);

                await Assert.ThrowsAsync<IOException>(() => copier.MoveAsync(), "\n -- MoveAsync_PreventOverwrite - Test 1 (default params)\n");
                Assert.IsTrue(File.Exists(copier.Source.FullName), "\n -- Source file was deleted despite IOException - Test 1\n");

                await Assert.ThrowsAsync<IOException>(() => copier.MoveAsync(false), "\n -- MoveAsync_PreventOverwrite - Test 2 (overwrite=false)\n");
                Assert.IsTrue(File.Exists(copier.Source.FullName), "\n -- Source file was deleted despite IOException - Test 2\n");

                await Assert.ThrowsAsync<IOException>(() => copier.MoveAsync(false, TestContext.CancellationToken), "\n -- MoveAsync_PreventOverwrite - Test 3 (overwrite=false, cancellation token)\n");
                Assert.IsTrue(File.Exists(copier.Source.FullName), "\n -- Source file was deleted despite IOException - Test 3\n");

                await CleanupCopier(copier, deleteSource: false);
            }
            catch (Exception e)
            {
                LogCopierState(copier, e);
                throw;
            }
            finally
            {
                await CleanupCopier(copier);
            }
        }

        #endregion

        /// <summary>
        /// Tests that attributes and file itself are copied to the destination file properly, just like if they were copied via File.CopyTo();
        /// </summary>
        [TestMethod]
        public async Task IFileCopierTest_AttributesCopiedProperlyTest()
        {
            IFileCopier copier = GetCopier();
            string fileCopyToDest = copier.Destination.FullName + "_control";

            string sourceMD5 = null; string destinationMD5 = null; string controlMD5 = null;

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
                await CleanupCopier(copier);
            }

            static string CalculateMD5(string filename)
            {
                if (File.Exists(filename))
                {
                    using var md5 = MD5.Create();
                    using var stream = File.OpenRead(filename);
                    var hash = md5.ComputeHash(stream);
#if NET10_0_OR_GREATER
                    var hashString = System.Convert.ToHexStringLower(hash);
#else
                            var hashString = BitConverter.ToString(hash).ToLowerInvariant();
#endif
                    return hashString.Replace("-", "");
                }
                return null;
            }
        }

        /// <summary>
        /// Helper method to log copier state for debugging
        /// </summary>
        private static void LogCopierState(IFileCopier copier, Exception e)
        {
            Console.WriteLine(string.Format("\n----------------\nSource File Path      : {0}", copier.Source));
            Console.WriteLine(string.Format("Destination File Path : {0}", copier.Destination));
            Console.WriteLine(string.Format("\nException : {0}\n----------------", e.Message));
        }
    }
}