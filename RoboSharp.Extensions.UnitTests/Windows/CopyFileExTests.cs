using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.Tests;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1416 // Validate platform compatibility

namespace RoboSharp.Extensions.Windows.UnitTests
{
    [TestClass]
    public class CopyFileExTests : IFileCopierTests<CopyFileExFactory>
    {
        protected override CopyFileExFactory GetFactory()
        {
            // use restartable mode to artifically slow down operations for copy operation tests
            return new CopyFileExFactory() { Options = CopyFileExOptions.RESTARTABLE };
        }

        private Progress<ProgressUpdate> ProgressFull => new Progress<ProgressUpdate>();
        private Progress<double> ProgressPercent => new Progress<double>();
        private Progress<long> ProgressSize => new Progress<long>();

        private void CreateDummyFile() => CreateDummyFile(Source, 1024 & 1024 * 20);

        /// <summary>
        /// Test the static method <see cref="CopyFileEx.CopyFile(string, string, CopyFileExOptions, CopyProgressCallback, CancellationToken)"/>
        /// </summary>
        [TestMethod]
        public void CopyFileEx_CopyFile_SourceMissing()
        {
            if (VersionManager.IsPlatformWindows)
                Assert.Throws<FileNotFoundException>(() => CopyFileEx.CopyFile(Source, Destination, CopyFileExOptions.FAIL_IF_EXISTS, cancellationToken: TestContext.CancellationToken));
            else
                Assert.Throws<PlatformNotSupportedException>(() => CopyFileEx.CopyFile(Source, Destination, CopyFileExOptions.FAIL_IF_EXISTS, cancellationToken: TestContext.CancellationToken));
        }

        [TestMethod]
        public void CopyFileEx_CopyFile_Fail_IF_EXISTS()
        {
            if (!VersionManager.IsPlatformWindows) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            Directory.CreateDirectory(Path.GetDirectoryName(Destination));
            File.WriteAllText(Source, " ");
            File.WriteAllText(Destination, " ");
            Assert.Throws<IOException>(() => CopyFileEx.CopyFile(Source, Destination, CopyFileExOptions.FAIL_IF_EXISTS, cancellationToken: TestContext.CancellationToken), "\n >> Copy Operation Succeeded when CopyFileExOptions.FAIL_IF_EXISTS was set");
        }

        [TestMethod]
        public void CopyFileEx_CopyFile_Overwrite()
        {
            if (!VersionManager.IsPlatformWindows) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            Directory.CreateDirectory(Path.GetDirectoryName(Destination));
            File.WriteAllText(Source, "Source Text");
            File.WriteAllText(Destination, "Destination Text");
            Assert.AreEqual("Destination Text", File.ReadAllText(Destination));
            Assert.IsTrue(CopyFileEx.CopyFile(Source, Destination, CopyFileExOptions.NONE, cancellationToken: TestContext.CancellationToken), "\n >> Copy Operation Failed when CopyFileExOptions.NONE was set");
            Assert.AreEqual("Source Text", File.ReadAllText(Destination));
        }

        [TestMethod]
        public void CopyFileEx_CopyFile_Callback_Cancellation()
        {
            if (!VersionManager.IsPlatformWindows) return;

            bool callbackHit = false;
            int callbackHitCount = 0;

            // Cancellation
            var cancelCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
            {
                callbackHit = true;
                callbackHitCount++;
                return CopyProgressCallbackResult.CANCEL;
            }, token: TestContext.CancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            File.WriteAllText(Source, "Source Text");
            
            Assert.IsFalse(callbackHit);
            Assert.Throws<OperationCanceledException>(() => CopyFileEx.CopyFile(Source, Destination, default, cancelCallback, TestContext.CancellationToken), "\nOperation was not cancelled");
            Assert.IsTrue(callbackHit, "\nCallback was not hit");
            Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
        }

        [TestMethod]
        public void CopyFileEx_CopyFile_Callback_Quiet()
        {
            if (!VersionManager.IsPlatformWindows) return;

            bool callbackHit = false;
            int callbackHitCount = 0;

            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            File.WriteAllText(Source, "Source Text");

            var quietCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
            {
                callbackHit = true;
                callbackHitCount++;
                return CopyProgressCallbackResult.QUIET;
            }, token: TestContext.CancellationToken);
            Assert.IsFalse(callbackHit);
            Assert.IsTrue(CopyFileEx.CopyFile(Source, Destination, default, quietCallback, TestContext.CancellationToken));
            Assert.IsTrue(callbackHit, "\nCallback was not hit");
            Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
        }

        [TestMethod]
        public void CopyFileEx_CopyFile_Callback_Continue()
        {
            if (!VersionManager.IsPlatformWindows) return;

            bool callbackHit = false;
            int callbackHitCount = 0;

            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            File.WriteAllText(Source, "Source Text");

            var continueCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
            {
                callbackHit = true;
                callbackHitCount++;
                return CopyProgressCallbackResult.CONTINUE;
            }, token: TestContext.CancellationToken);
            Assert.IsFalse(callbackHit);
            Assert.IsTrue(CopyFileEx.CopyFile(Source, Destination, default, continueCallback, TestContext.CancellationToken));
            Assert.IsTrue(callbackHit, "\nCallback was not hit");
            Assert.IsGreaterThanOrEqualTo(2, callbackHitCount, "\nCallback count incorrect");
        }

        [TestMethod]
        public void CopyFileEx_CopyFile_CreatesDestinationDirectory()
        {
            if (!VersionManager.IsPlatformWindows) return;

            string sourceFile = Source;
            string destFolder = Destination;
            string destFile = Path.Combine(destFolder, "Target.txt");

            try
            {
                File.WriteAllText(sourceFile, "Test Contents");
                // Verify test prep
                Assert.IsTrue(File.Exists(sourceFile), "Source File not created!");
                Assert.IsFalse(Directory.Exists(destFolder), "Destination folder already Exists!");
                Assert.IsFalse(File.Exists(destFile), "Destination File Already Exists!");
                //Test the function
                Assert.IsTrue(CopyFileEx.CopyFile(sourceFile, destFile, default, cancellationToken: TestContext.CancellationToken), "Function returned False (copy failed)");
                Assert.IsTrue(File.Exists(destFile), "File does not exist at destination");
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(destFile)) File.Delete(destFile);
                if (Directory.Exists(destFolder)) Directory.Delete(destFolder, false);
            }
        }

        /// <summary>
        /// Test the static method <see cref="CopyFileEx.CopyFileAsync(string, string)"/>
        /// </summary>
        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_SourceMissing()
        {
            if (VersionManager.IsPlatformWindows)
            {
                await Assert.ThrowsAsync<FileNotFoundException>(() => CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.FAIL_IF_EXISTS, token: TestContext.CancellationToken), "\n >> Test 1");
                await Assert.ThrowsAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, TestContext.CancellationToken), "\n >> Test 2");
                await Assert.ThrowsAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, false, TestContext.CancellationToken), "\n >> Test 3");
                await Assert.ThrowsAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, true, TestContext.CancellationToken), "\n >> Test 4");
                await Assert.ThrowsAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, ProgressFull, 100, true, TestContext.CancellationToken), "\n >> Test 5");
                await Assert.ThrowsAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, ProgressPercent, 100, true, TestContext.CancellationToken), "\n >> Test 6");
                await Assert.ThrowsAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, ProgressSize, 100, true, TestContext.CancellationToken), "\n >> Test 7");
            }
            else
                await Assert.ThrowsAsync<PlatformNotSupportedException>(() => CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.FAIL_IF_EXISTS, token: TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Fail_IF_EXISTS()
        {
            if (!VersionManager.IsPlatformWindows) return;

            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            Directory.CreateDirectory(Path.GetDirectoryName(Destination));
            File.WriteAllText(Source, " ");
            File.WriteAllText(Destination, " ");

            await Assert.ThrowsAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.FAIL_IF_EXISTS, token: TestContext.CancellationToken), "\n >> Copy Operation Succeeded when CopyFileExOptions.FAIL_IF_EXISTS was set");
            await Assert.ThrowsAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, TestContext.CancellationToken), "\n >> Test 2");
            await Assert.ThrowsAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, false, TestContext.CancellationToken), "\n >> Test 3");
            await Assert.ThrowsAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, ProgressFull, 100, false, TestContext.CancellationToken), "\n >> Test 3");
            await Assert.ThrowsAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, ProgressPercent, 100, false, TestContext.CancellationToken), "\n >> Test 4");
            await Assert.ThrowsAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, ProgressSize, 100, false, TestContext.CancellationToken), "\n >> Test 5");
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Overwrite()
        {
            if (!VersionManager.IsPlatformWindows) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Source));
            Directory.CreateDirectory(Path.GetDirectoryName(Destination));
            File.WriteAllText(Destination, "Destination Text");
            
            await Run(1, () => CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.RESTARTABLE, token: TestContext.CancellationToken));
            await Run(2, () =>  CopyFileEx.CopyFileAsync(Source, Destination, true, TestContext.CancellationToken));
            await Run(3, () =>  CopyFileEx.CopyFileAsync(Source, Destination, ProgressFull, 100, true, TestContext.CancellationToken));
            await Run(4, () =>  CopyFileEx.CopyFileAsync(Source, Destination, ProgressPercent, 100, true, TestContext.CancellationToken));
            await Run(5, () =>  CopyFileEx.CopyFileAsync(Source, Destination, ProgressSize, 100, true, TestContext.CancellationToken));

            async Task Run(int testNumber, Func< Task<bool>> copyTask)
            {
                string text = Path.GetRandomFileName();
                File.WriteAllText(Source, text);
                Assert.IsTrue(await copyTask(), $"\n >> Task Failed - Test {testNumber}");
                Assert.AreEqual(text, File.ReadAllText(Destination), $"\n >> Destination contents are different - Test {testNumber}");
            }
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Callback_Cancel()
        {
            if (!VersionManager.IsPlatformWindows) return;
            bool callbackHit = false;
            int callbackHitCount = 0;

            var cancelCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
            {
                callbackHit = true;
                callbackHitCount++;
                return CopyProgressCallbackResult.CANCEL;
            }, token: TestContext.CancellationToken);

            Assert.IsFalse(callbackHit);
            CreateDummyFile();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.RESTARTABLE, cancelCallback, TestContext.CancellationToken), "\nOperation was not cancelled");
            Assert.IsTrue(callbackHit, "\nCallback was not hit");
            Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Callback_Quiet()
        {
            if (!VersionManager.IsPlatformWindows) return;
            bool callbackHit = false;
            int callbackHitCount = 0;

            var quietCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
            {
                callbackHit = true;
                callbackHitCount++;
                return CopyProgressCallbackResult.QUIET;
            }, token: TestContext.CancellationToken);

            Assert.IsFalse(callbackHit);
            CreateDummyFile();
            Assert.IsTrue(await CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.RESTARTABLE, quietCallback, TestContext.CancellationToken));
            Assert.IsTrue(callbackHit, "\nCallback was not hit");
            Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Callback_Continue()
        {
            if (!VersionManager.IsPlatformWindows) return;
            bool callbackHit = false;
            int callbackHitCount = 0;

            var continueCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
            {
                callbackHit = true;
                callbackHitCount++;
                return CopyProgressCallbackResult.CONTINUE;
            }, token: TestContext.CancellationToken);

            Assert.IsFalse(callbackHit);
            CreateDummyFile(Source, 1024 * 1024 * 10);
            Assert.IsTrue(await CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.RESTARTABLE, continueCallback, TestContext.CancellationToken));
            Assert.IsTrue(callbackHit, "\nCallback was not hit");
            Assert.IsGreaterThanOrEqualTo(2, callbackHitCount, "\nCallback count incorrect");
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Cancelled_BeforeStart()
        {
            if (!VersionManager.IsPlatformWindows) return;
            var cdToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
            cdToken.Cancel();
            await Run(1, () => CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.RESTARTABLE, token: cdToken.Token));
            await Run(2, () => CopyFileEx.CopyFileAsync(Source, Destination, true, cdToken.Token));
            await Run(3, () => CopyFileEx.CopyFileAsync(Source, Destination, ProgressFull, 100, true, cdToken.Token));
            await Run(4, () => CopyFileEx.CopyFileAsync(Source, Destination, ProgressPercent, 100, true, cdToken.Token));
            await Run(5, () => CopyFileEx.CopyFileAsync(Source, Destination, ProgressSize, 100, true, cdToken.Token));
            await Run(6, () => CopyFileEx.CopyFileAsync(Source, Destination, cdToken.Token));
            Assert.IsFalse(File.Exists(Destination));

            static async Task Run(int testNumber, Func<Task<bool>> copyTask)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(copyTask, $"\n >> Task Failed - Test {testNumber}");
            }
        }

        /// <rewmarks>
        /// A failure on this test indicates that the <see cref="Progress{T}"/> overloads may not be working!
        /// </rewmarks>
        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync_Cancelled_WhileWriting()
        {
            if (!VersionManager.IsPlatformWindows) return;
            var cdToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
            cdToken.Cancel();

            CopyProgressCallback midWriteCancelCallback = new CopyProgressCallback((a, b, c, d, e, f) => CopyProgressCallbackResult.CANCEL);

            var pFull = ProgressFull;
            var pPercent = ProgressPercent;
            var pSize = ProgressSize;

            CreateDummyFile(Source, 1024 * 1024 * 10);
            await Run(1, () => CopyFileEx.CopyFileAsync(Source, Destination, CopyFileExOptions.RESTARTABLE, progressCallback: midWriteCancelCallback, token: cdToken.Token));
            await Run(2, () => CopyFileEx.CopyFileAsync(Source, Destination, true, cdToken.Token));
            await Run(3, () => CopyFileEx.CopyFileAsync(Source, Destination, pFull, 25, true, GetProgToken(pFull)));
            await Run(4, () => CopyFileEx.CopyFileAsync(Source, Destination, pPercent, 25, true, GetProgToken(pPercent)));
            await Run(5, () => CopyFileEx.CopyFileAsync(Source, Destination, pSize, 25, true, GetProgToken(pSize)));

            Assert.IsFalse(File.Exists(Destination));

            static async Task Run(int testNumber, Func<Task<bool>> copyTask)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(copyTask, $"\n >> Task Failed - Test {testNumber}");
            }

            CancellationToken GetProgToken<T>(Progress<T> progress)
            {
                var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
                progress.ProgressChanged += Cancel;
                return source.Token;
                void Cancel(object o, T obj)
                {
                    source.Cancel();
                    progress.ProgressChanged -= Cancel;
                }
            }
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility