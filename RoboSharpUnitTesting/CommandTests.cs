using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RoboSharp.UnitTests
{
    /// <summary>
    /// 
    ///  Runs the full CommandTests suite against the real RoboCopy process.
    ///  If these tests pass, the expected counts in <see cref="CommandTests{T}"/> are correct.
    ///  <para/>
    ///  If they fail, fix the SourceTree constants or test expectations first
    ///  before debugging any custom implementation.
    ///
    /// </summary>
    [TestClass]
    public sealed class CommandTests : CommandTests<RoboCommand>
    {
        protected override RoboCommand GetCommand() => new();
    }

    /// <summary>
    /// Known counts derived from the static TEST_FILES/STANDARD tree.
    /// Update these constants if the test file set ever changes.
    /// </summary>
    internal static class SourceTree
    {
        private const int Level1FileCount = 5;
        private const int Level2FileCount = 4;
        private const int Level3FileCount = 0;
        private const int Level4FileCount = 4;

        private static int GetDepth(IRoboCommand command)
        {
            bool isRecursive = command.CopyOptions.Mirror
                || command.CopyOptions.CopySubdirectories 
                || command.CopyOptions.CopySubdirectoriesIncludingEmpty 
                ;
            return isRecursive 
                ? command.CopyOptions.Depth <= 0 ? 0 : command.CopyOptions.Depth
                : 1;
        }

        public static int GetFileCount(IRoboCommand command)
        {
            int depth = GetDepth(command);

            depth = (depth == 0 || depth > 4) ? 4 : depth;
            return GetFileCount(depth);
        }

        public static int GetFileCount(int depth)
        {
            return depth switch
            {
                1 => Level1FileCount,
                2 => Level1FileCount + Level2FileCount,
                3 => Level1FileCount + Level2FileCount + Level3FileCount,
                4 => Level1FileCount + Level2FileCount + Level3FileCount + Level4FileCount,
                _ => throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be between 1 and 4")
            };
        }        

        /// <summary>
        /// Get the dir count based on the command's recursion and empty dir options.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static int GetDirTotal(IRoboCommand command)
        {
            int depth = GetDepth(command);
            bool isRecursive = command.CopyOptions.Mirror
                //|| command.CopyOptions.CopySubdirectories // handledBelow
                || command.CopyOptions.CopySubdirectoriesIncludingEmpty
                ;

            bool includingEmpty = isRecursive || ((command.CopyOptions.MoveFilesAndDirectories || command.CopyOptions.CopySubdirectories) && depth > 0);
            return depth switch
            {
                // default (unlimited) depth
                0 or > 4 => 5,// root + 5 subdirs
                1 => 1,
                2 => includingEmpty ? 3 : isRecursive ? 2 : 1,
                3 => includingEmpty ? 4 : isRecursive ? 3 : 1,
                _ => 5,
            };
        }

        /// <summary>
        /// Gets the expected directory count for the standard test tree based on the command's recursion and empty dir options.
        /// <br/> This assumes no child directories exist prior to starting the command.
        /// </summary>
        public static int GetDirCopied(IRoboCommand command)
        {
            var total = GetDirTotal(command);
            
            if (command.LoggingOptions.ListOnly)
                return Directory.Exists(command.CopyOptions.Destination) ? total - 1 : total;
            
            // when not list only - "authentication" behavior that creates the destination root dir
            return total - 1; 
        }

#if NETFRAMEWORK
        public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken token)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var cancellationTask = Task.Delay(Timeout.Infinite, cts.Token);
            await Task.WhenAny(task, cancellationTask);
            if (token.IsCancellationRequested == false)
            {
                cts.Cancel(); // stop the cancellation task if command finishes first
                return await task; // command completed successfully
            }
            await cancellationTask; // will throw OperationCanceledException to be caught below
            return default!;
        }
#endif
    }



    /// <summary>
    /// CommandTests&lt;T&gt;
    /// <para/>
    /// <br/>  Abstract base that every IRoboCommand implementation test class inherits.
    /// <br/>  Design principles:
    /// <br/>   • No back-to-back RoboCopy runs — expected counts are compile-time constants.
    /// <br/>   • Each test creates its own isolated temp destination (or move-source).
    /// <br/>   • Source (TEST_FILES/STANDARD) is NEVER modified.
    /// <br/>   • GetCommand is virtual — subclasses override to supply their T instance.
    ///</summary>
    [TestClass]
    public abstract class CommandTests<T> where T : IRoboCommand
    {
        // ── MSTest plumbing ───────────────────────────────────────────────────

        public TestContext TestContext { get; set; } = null!;

        /// <summary>Cooperative cancellation token wired to the MSTest timeout.</summary>
        protected CancellationToken Token => TestContext.CancellationToken;

        // ── Directories ───────────────────────────────────────────────────────

        /// <summary>Shared read-only source. Never modified by any test.</summary>
        protected static string SharedSource => Test_Setup.Source_Standard;

        /// <summary>
        /// Per-test isolated destination directory.
        /// Created fresh in <see cref="TestInit"/> and deleted in <see cref="TestCleanup"/>.
        /// </summary>
        protected string TempDest { get; private set; } = string.Empty;

        // ── Command factory ───────────────────────────────────────────────────

        /// <summary>
        /// Create an instance of <typeparamref name="T"/> with default constructor and no properties set.
        /// </summary>
        protected virtual T GetCommand() => Activator.CreateInstance<T>();

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> and sets Source/Destination.
        /// Subclasses override to inject factories, authenticators, or other dependencies.
        /// The base implementation uses <see cref="Activator.CreateInstance{T}"/> and
        /// wires Source + Destination on <see cref="IRoboCommand.CopyOptions"/>.
        /// </summary>
        protected T GetCommand(string source, string destination)
        {
            var cmd = GetCommand();
            cmd.CopyOptions.Source = source;
            cmd.CopyOptions.Destination = destination;
            cmd.Configuration.EnableFileLogging = true;
            return cmd;
        }

        [TestInitialize]
        public void TestInit()
        {
            TempDest = Test_Setup.GetNewTempPath();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                if (Directory.Exists(TempDest))
                {
                    // Clear read-only attributes before deleting (robocopy may set them)
                    foreach (var f in new DirectoryInfo(TempDest).GetFiles("*", SearchOption.AllDirectories))
                        File.SetAttributes(f.FullName, FileAttributes.Normal);
                    Directory.Delete(TempDest, recursive: true);
                }
            }
            catch { /* best-effort — don't fail the test on cleanup */ }
        }

        // ── Run helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the command, wires cooperative cancellation, and returns results.
        /// Swallows <see cref="OperationCanceledException"/> caused by the test timeout
        /// so the test framework can report it as a timeout rather than an error.
        /// </summary>
        protected async Task<RoboCopyResults?> RunCommand(T cmd)
        {
            Token.Register(() => cmd.Stop());
            try
            {
                return await cmd.StartAsync().WaitAsync(Token);
            }
            catch (OperationCanceledException) when (Token.IsCancellationRequested)
            {
                return null; // timeout — MSTest will report [Timeout] failure
            }
        }

        // ── Move-source helper ────────────────────────────────────────────────

        /// <summary>
        /// Copies the standard source tree into a fresh temp directory so that
        /// move operations have their own expendable copy to consume.
        /// </summary>
        protected async Task<string> PrepMoveSource()
        {
            string moveSource = Path.Combine(Path.GetTempPath(),"RoboSharp_MoveSource",typeof(T).Name,Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(moveSource);
            string source = Test_Setup.Source_Standard;
            await Task.Run(() => CopyDir(source, moveSource, Token), Token);
            return moveSource;

            static void CopyDir(string sourceDir, string destRoot, CancellationToken token)
            {
                foreach(var child in Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
                {
                    token.ThrowIfCancellationRequested();
                    File.Copy(child, Path.Combine(destRoot, Path.GetFileName(child)));
                }

                foreach (var child in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
                {
                    token.ThrowIfCancellationRequested();
                    var dir = Directory.CreateDirectory(Path.Combine(destRoot, Path.GetFileName(child)));
                    CopyDir(child, dir.FullName, token);
                }
            }
        }

        // ── Assert helper ─────────────────────────────────────────────────────

        /// <summary>
        /// Compares a results object against pre-computed expected statistics,
        /// printing both to the test output before asserting.
        /// </summary>
        protected static void AssertResults(
            RoboCopyResults? results,
            string label,
            long expectedDirTotal, long expectedDirCopied, long expectedDirExtras, long expectedDirSkipped,
            long expectedFileTotal, long expectedFileCopied, long expectedFileExtras, long expectedFileSkipped, 
            long expectedFileFailed = 0, long expectedDirMismatch = 0, long expectedFileMismatch = 0)
        {
            Assert.IsNotNull(results, "Results must not be null — command may have been cancelled by timeout.");

            // Print for diagnostics
            Console.WriteLine($"── {label} ──");
            Console.WriteLine("Expected  Dirs  : {0}", new Statistic(type: Statistic.StatType.Directories, "", expectedDirTotal, expectedDirCopied, expectedDirSkipped, expectedDirMismatch, 0, expectedDirExtras));
            Console.WriteLine("  Actual  Dirs  : {0}\n", results.DirectoriesStatistic);

            Console.WriteLine("Expected  Files : {0}", new Statistic(type: Statistic.StatType.Directories, "", expectedFileTotal, expectedFileCopied, expectedFileSkipped, expectedFileMismatch, expectedFileFailed, expectedFileExtras));
            Console.WriteLine("  Actual  Files : {0}\n", results.FilesStatistic);

            //Console.WriteLine("Expected  Bytes : {0}");
            Console.WriteLine("  Actual  Bytes : {0}\n", results.BytesStatistic);

            try
            {
                Assert.AreEqual(expectedDirTotal, results.DirectoriesStatistic.Total, $"\n[{label}] Dir.Total");
                Assert.AreEqual(expectedDirCopied, results.DirectoriesStatistic.Copied, $"\n[{label}] Dir.Copied");
                Assert.AreEqual(expectedDirExtras, results.DirectoriesStatistic.Extras, $"\n[{label}] Dir.Extras");
                Assert.AreEqual(expectedDirSkipped, results.DirectoriesStatistic.Skipped, $"\n[{label}] Dir.Skipped");
                Assert.AreEqual(expectedDirMismatch, results.DirectoriesStatistic.Mismatch, $"\n[{label}] Dir.Mismatch");

                Assert.AreEqual(expectedFileMismatch, results.FilesStatistic.Mismatch, $"\n[{label}] File.Mismatch");
                Assert.AreEqual(expectedFileTotal, results.FilesStatistic.Total, $"\n[{label}] File.Total");
                Assert.AreEqual(expectedFileCopied, results.FilesStatistic.Copied, $"\n[{label}] File.Copied");
                Assert.AreEqual(expectedFileFailed, results.FilesStatistic.Failed, $"\n[{label}] File.Failed");
                Assert.AreEqual(expectedFileExtras, results.FilesStatistic.Extras, $"\n[{label}] File.Extras");
                Assert.AreEqual(expectedFileSkipped, results.FilesStatistic.Skipped, $"\n[{label}] File.Skipped");
            }
            catch(Exception e)
            {
                Console.WriteLine("\n----------------------------------------------------------------");
                Console.WriteLine($"\t>> Exception Caught :\n\t\t{e.Message}");
                Console.WriteLine($"\t>> Printing Log Lines");
                Console.WriteLine("----------------------------------------------------------------\n");
                Console.WriteLine(string.Join(Environment.NewLine, results.LogLines));
                throw;
            }
            static void WriteTree(string path)
            {
                Console.WriteLine();
                Console.WriteLine(path);
                foreach (var file in Directory.GetFiles(path))
                    Console.WriteLine(file);

                foreach (var dir in Directory.GetDirectories(path))
                    WriteTree(dir);

            }
        }

        [TestMethod]
        public void Test_FileDirectoryInfoTests()
        {
            var path = Test_Setup.Source_Standard;
            Assert.IsTrue(Directory.Exists(path));  // path IS a directory
            Assert.IsFalse(File.Exists(path));      // path IS NOT a file 
            Assert.IsTrue(File.GetAttributes(path).HasFlag(FileAttributes.Directory));

            var filePath = Path.Combine(path, "4_Bytes.txt");
            Assert.IsFalse(Directory.Exists(filePath)); // path IS NOT a directory
            Assert.IsTrue(File.Exists(filePath));       // path IS a file
            Assert.IsFalse(File.GetAttributes(filePath).HasFlag(FileAttributes.Directory));

#if NET8_0_OR_GREATER
            Assert.IsTrue(Path.Exists(path));
            Assert.IsTrue(Path.Exists(filePath));
#endif
            // assert that a path that points to directory returns Exists = false when evaluated as a FileInfo
            // But the FileInfo object should be able to indicate it is a directory via the Attributes property.
            var dirInfo = new DirectoryInfo(path);
            var fInfo = new FileInfo(path);
            Assert.IsTrue(dirInfo.Exists);
            Assert.IsTrue(fInfo.Attributes.HasFlag(FileAttributes.Directory));
            Assert.IsFalse(fInfo.Exists);

            // assert that a path that points to file returns Exists = false when evaluated as a DirectoryInfo
            // But the DirectoryInfo object should be able to indicate it is not directory via the Attributes property.
            dirInfo = new DirectoryInfo(filePath);
            fInfo = new FileInfo(filePath);
            Assert.IsFalse(dirInfo.Exists);
            Assert.IsFalse(dirInfo.Attributes.HasFlag(FileAttributes.Directory));
            Assert.IsTrue(fInfo.Exists);

            // Test getting attributes on a file or directory that does not exist
            var tp = new FileInfo($"{path}\\SomeUnkownFile.txt");
            Assert.IsFalse(Directory.Exists(tp.FullName) || File.Exists(tp.FullName) || tp.Exists);
            Assert.IsLessThanOrEqualTo(0, (int)tp.Attributes); // does not exist -> should be -1 or 0 (net8 or newer)
        }

        /// <summary>
        /// SKIP TESTS (destination already up to date)
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Test_Copy_SkipsAlreadyCopiedFiles()
        {
            // Run twice. Second run: all files exist in dest → all skipped.
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            await RunCommand(cmd); // first pass — populate dest

            // Second pass — same command, dest already populated
            var cmd2 = GetCommand(SharedSource, TempDest);
            cmd2.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            var results = await RunCommand(cmd2);

            AssertResults(results, nameof(Test_Copy_SkipsAlreadyCopiedFiles),
                expectedDirTotal: SourceTree.GetDirTotal(cmd2),
                expectedDirCopied: 0, expectedDirExtras: 0,
                expectedDirSkipped: SourceTree.GetDirTotal(cmd),
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: 0, expectedFileExtras: 0,
                expectedFileSkipped: SourceTree.GetFileCount(cmd));
        }

        /// <summary>
        /// Tests copy flags when copying into an empty destination directory
        /// </summary>
        /// <remarks>
        /// For <see cref="CopyActionFlags.CopySubdirectories"/> : <br/> 
        /// When depth is > 0, empty subdirectories are included, because the command assumes the user explicitly wants to include subdirs up to that depth.
        /// </remarks>
        [TestMethod]
        //[Timeout(5000, CooperativeCancellation = true)]
        // list only = true
        [DataRow(true, CopyActionFlags.Default, 0, DisplayName = "ListOnly - Default - Depth=Unlimited")]
        [DataRow(true, CopyActionFlags.Default, 1, DisplayName = "ListOnly - Default - Depth=1 (root only)")]
        [DataRow(true, CopyActionFlags.Default, 2, DisplayName = "ListOnly - Default - Depth=2")]
        [DataRow(true, CopyActionFlags.Default, 3, DisplayName = "ListOnly - Default - Depth=3")]
        [DataRow(true, CopyActionFlags.Default, 4, DisplayName = "ListOnly - Default - Depth=4")]
        [DataRow(true, CopyActionFlags.CopySubdirectories, 0, DisplayName = "ListOnly - CopySubdirectories - Depth=Unlimited")]
        [DataRow(true, CopyActionFlags.CopySubdirectories, 1, DisplayName = "ListOnly - CopySubdirectories - Depth=1 (root only)")]
        [DataRow(true, CopyActionFlags.CopySubdirectories, 2, DisplayName = "ListOnly - CopySubdirectories - Depth=2")]
        [DataRow(true, CopyActionFlags.CopySubdirectories, 3, DisplayName = "ListOnly - CopySubdirectories - Depth=3")]
        [DataRow(true, CopyActionFlags.CopySubdirectories, 4, DisplayName = "ListOnly - CopySubdirectories - Depth=4")]
        [DataRow(true, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 1, DisplayName = "ListOnly - CopySubdirectoriesIncludingEmpty - Depth=1 (root only)")]
        [DataRow(true, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 2, DisplayName = "ListOnly - CopySubdirectoriesIncludingEmpty - Depth=2")]
        [DataRow(true, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 3, DisplayName = "ListOnly - CopySubdirectoriesIncludingEmpty - Depth=3")]
        [DataRow(true, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 4, DisplayName = "ListOnly - CopySubdirectoriesIncludingEmpty - Depth=4")]
        [DataRow(true, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 0, DisplayName = "ListOnly - CopySubdirectoriesIncludingEmpty - Depth=Unlimited")]
        // list only = false
        [DataRow(false, CopyActionFlags.Default, 0, DisplayName = "Default - Depth=Unlimited")]
        [DataRow(false, CopyActionFlags.Default, 1, DisplayName = "Default - Depth=1 (root only)")]
        [DataRow(false, CopyActionFlags.Default, 2, DisplayName = "Default - Depth=2")]
        [DataRow(false, CopyActionFlags.Default, 3, DisplayName = "Default - Depth=3")]
        [DataRow(false, CopyActionFlags.Default, 4, DisplayName = "Default - Depth=4")]
        [DataRow(false, CopyActionFlags.CopySubdirectories, 0, DisplayName = "CopySubdirectories - Depth=Unlimited")]
        [DataRow(false, CopyActionFlags.CopySubdirectories, 1, DisplayName = "CopySubdirectories - Depth=1 (root only)")]
        [DataRow(false, CopyActionFlags.CopySubdirectories, 2, DisplayName = "CopySubdirectories - Depth=2")]
        [DataRow(false, CopyActionFlags.CopySubdirectories, 3, DisplayName = "CopySubdirectories - Depth=3")]
        [DataRow(false, CopyActionFlags.CopySubdirectories, 4, DisplayName = "CopySubdirectories - Depth=4")]
        [DataRow(false, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 0, DisplayName = "CopySubdirectoriesIncludingEmpty - Depth=Unlimited")]
        [DataRow(false, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 1, DisplayName = "CopySubdirectoriesIncludingEmpty - Depth=1 (root only)")]
        [DataRow(false, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 2, DisplayName = "CopySubdirectoriesIncludingEmpty - Depth=2")]
        [DataRow(false, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 3, DisplayName = "CopySubdirectoriesIncludingEmpty - Depth=3")]
        [DataRow(false, CopyActionFlags.CopySubdirectoriesIncludingEmpty, 4, DisplayName = "CopySubdirectoriesIncludingEmpty - Depth=4")]
        // mirror
        [DataRow(false, CopyActionFlags.Mirror, 0, DisplayName = "Mirror - Depth=Unlimited")]
        [DataRow(false, CopyActionFlags.Mirror, 1, DisplayName = "Mirror - Depth=1 (root only)")]
        [DataRow(false, CopyActionFlags.Mirror, 2, DisplayName = "Mirror - Depth=2")]
        [DataRow(false, CopyActionFlags.Mirror, 3, DisplayName = "Mirror - Depth=3")]
        [DataRow(false, CopyActionFlags.Mirror, 4, DisplayName = "Mirror - Depth=4")]
        [DataRow(true, CopyActionFlags.Mirror, 0, DisplayName = "ListOnly - Mirror - Depth=Unlimited")]
        [DataRow(true, CopyActionFlags.Mirror, 1, DisplayName = "ListOnly - Mirror - Depth=1 (root only)")]
        [DataRow(true, CopyActionFlags.Mirror, 2, DisplayName = "ListOnly - Mirror - Depth=2")]
        [DataRow(true, CopyActionFlags.Mirror, 3, DisplayName = "ListOnly - Mirror - Depth=3")]
        [DataRow(true, CopyActionFlags.Mirror, 4, DisplayName = "ListOnly - Mirror - Depth=4")]
        public async Task Test_Copy_Depth(bool listOnly, CopyActionFlags flags, int depth)
        {
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.ApplyActionFlags(flags);
            cmd.CopyOptions.Depth = depth;
            cmd.LoggingOptions.ListOnly = listOnly;
            var results = await RunCommand(cmd);

            Assert.IsFalse(flags.HasFlag(CopyActionFlags.Purge) || cmd.CopyOptions.Purge);
            Assert.IsFalse(flags.HasFlag(CopyActionFlags.MoveFiles) || cmd.CopyOptions.MoveFiles);
            Assert.IsFalse(flags.HasFlag(CopyActionFlags.MoveFilesAndDirectories) || cmd.CopyOptions.MoveFilesAndDirectories);

            int expectedDirTotal = SourceTree.GetDirTotal(cmd);
            int expectedDirCopied = SourceTree.GetDirCopied(cmd);
            int expectedFileTotal = SourceTree.GetFileCount(cmd);

            AssertResults(results, nameof(Test_Copy_Depth),
                expectedDirTotal: expectedDirTotal,
                expectedDirCopied: expectedDirCopied,
                expectedDirExtras: 0, 
                expectedDirSkipped: expectedDirTotal - expectedDirCopied,
                expectedFileTotal: expectedFileTotal,
                expectedFileCopied: expectedFileTotal,
                expectedFileExtras: 0, 
                expectedFileSkipped: 0
                );

            Assert.AreEqual(!listOnly, Directory.Exists(cmd.CopyOptions.Destination));

            bool includingEmpty = flags.HasFlag(CopyActionFlags.CopySubdirectoriesIncludingEmpty) || flags.HasFlag(CopyActionFlags.Mirror);
            bool isRecursive = includingEmpty || flags.HasFlag(CopyActionFlags.CopySubdirectories);            
            bool deep2 = depth == 0 || depth > 1;
            bool deep3 = depth == 0 || depth > 2;
            bool deep4 = depth == 0 || depth > 3;

            string subDirWithFiles = Path.Combine(cmd.CopyOptions.Destination, "SubFolder_2");

            string subDir1 = Path.Combine(cmd.CopyOptions.Destination, "SubFolder_1");
            string subDir2 = Path.Combine(subDir1, "SubFolder_1.1");
            string subDirWithFiles2 = Path.Combine(subDir2, "SubFolder_1.2");
                       

            if (!listOnly && isRecursive)
            {
                Assert.AreEqual(SourceTree.GetFileCount(1), Directory.EnumerateFiles(cmd.CopyOptions.Destination).Count());
                if (deep2)
                {
                    const string shouldNotExist = "\n >> {0} should not exist when at depth level {1}";
                    const string shouldExist = "\n >> {0} was not created at depth level {1}";

                    bool expected = includingEmpty || deep4;
                    Assert.AreEqual(expected, Directory.Exists(subDir1), string.Format(expected ? shouldExist : shouldNotExist, "Empty Directory .\\SubFolder_1", 2));

                    expected = deep4 || (includingEmpty && deep3); 
                    Assert.AreEqual(expected, Directory.Exists(subDir2), string.Format(expected ? shouldExist : shouldNotExist, "Empty Directory .\\SubFolder_1\\SubFolder_1.1", 3));

                    Assert.IsTrue(Directory.Exists(subDirWithFiles), string.Format(shouldExist, "SubFolder_2", 2));
                    Assert.AreEqual(4, Directory.EnumerateFiles(subDirWithFiles).Count());
                    Assert.AreEqual(deep4, Directory.Exists(subDirWithFiles2), string.Format(deep4 ? shouldExist : shouldNotExist, "SubFolder_1.2", 4));
                    if (deep4)
                    {
                        Assert.AreEqual(4, Directory.EnumerateFiles(subDirWithFiles2).Count());
                    }
                }
            }
            else // child directories should not exist if not recursive
            {
                Assert.IsFalse(Directory.Exists(subDir1));
                Assert.IsFalse(Directory.Exists(subDirWithFiles));
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // EXTRA FILE / DIR REPORTING
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Extra Directories are always reported in the results overview regardless of recursion mode.
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.CopySubdirectories, LoggingFlags.RoboSharpDefault, DisplayName = "CopySubdirectories")]
        [DataRow(CopyActionFlags.CopySubdirectories, LoggingFlags.ReportExtraFiles, DisplayName = "CopySubdirectories - Report Extra")]
        [DataRow(CopyActionFlags.CopySubdirectories, LoggingFlags.VerboseOutput, DisplayName = "CopySubdirectories - Verbose")]
        [DataRow(CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.RoboSharpDefault, DisplayName = "CopySubdirectoriesIncludingEmpty")]
        [DataRow(CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.ReportExtraFiles, DisplayName = "CopySubdirectoriesIncludingEmpty - Report Extra")]
        [DataRow(CopyActionFlags.CopySubdirectoriesIncludingEmpty, LoggingFlags.VerboseOutput, DisplayName = "CopySubdirectoriesIncludingEmpty - Verbose")]
        public async Task Test_Logging_ExtraDirectories(CopyActionFlags copyFlags, LoggingFlags loggingFlags)
        {
            // Pre-place extra dirs (empty) in dest root.
            int extraDirCount = 2;
            for (int i = 0; i < extraDirCount; i++)
                Directory.CreateDirectory(Path.Combine(TempDest, $"ExtraDir_{i}"));

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.ApplyActionFlags(copyFlags);
            cmd.LoggingOptions.ApplyLoggingFlags(loggingFlags | LoggingFlags.ListOnly);
            var results = await RunCommand(cmd);

            // Extra dirs appear in Extras column, not Copied.
            // Total dirs = source dirs + extra dest dirs.
            AssertResults(results, TestContext.TestDisplayName ?? nameof(Test_Logging_ExtraDirectories),
                expectedDirTotal: SourceTree.GetDirTotal(cmd), // total only includes those in source
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: extraDirCount, expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        /// <summary>
        /// Extra Files are always reported in the results overview. They are conditionally reported in the log lines.
        /// <br/> This test verifies they are reported in the log lines when ReportExtraFiles or VerboseOutput is set.
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(LoggingFlags.VerboseOutput, 1, DisplayName = "Verbose - 1 extra files in dest root")]
        [DataRow(LoggingFlags.VerboseOutput, 3, DisplayName = "Verbose - 3 extra files in dest root")]
        [DataRow(LoggingFlags.ReportExtraFiles, 1, DisplayName = "ReportExtras - 3 extra files in dest root")]
        [DataRow(LoggingFlags.ReportExtraFiles, 3, DisplayName = "ReportExtras - 3 extra files in dest root")]
        public async Task Test_Logging_ExtraFiles(LoggingFlags loggingFlags, int extraFileCount)
        {
            // Pre-place extra files in dest root.
            Directory.CreateDirectory(TempDest);
            for (int i = 0; i < extraFileCount; i++)
                File.WriteAllText(Path.Combine(TempDest, $"extra_{i}.txt"), "extra");

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.LoggingOptions.ApplyLoggingFlags(loggingFlags | LoggingFlags.ListOnly);
            var results = await RunCommand(cmd);

            // Files: 20 source copied + N extras in dest
            AssertResults(results, $"{nameof(Test_Logging_ExtraFiles)}(n={extraFileCount})",
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: extraFileCount,
                expectedFileSkipped: 0);

            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results.LogLines, "Log lines should not be empty");
            Assert.Contains(line => line.Trim().StartsWith(cmd.Configuration.LogParsing_ExtraFile) && line.Trim().EndsWith("extra_0.txt"), results.LogLines, $"\nLog lines should report extra when {loggingFlags} is set");
        }

        /// <summary>
        /// Extra Files are always reported in the results overview. They are conditionally reported in the log lines.
        /// <br/> This test verifies they are not reported in the log lines when ReportExtraFiles and VerboseOutput are both false.
        /// </summary>
        /// <remarks>
        /// Robocopy WILL select a directory if the directory name matches a wildcard pattern for the file filters.
        /// </remarks>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(LoggingFlags.None, false)]
        [DataRow(LoggingFlags.None, true)]        
        [DataRow(LoggingFlags.VerboseOutput, false)]
        [DataRow(LoggingFlags.VerboseOutput, true)]
        [DataRow(LoggingFlags.ReportExtraFiles, false)]
        [DataRow(LoggingFlags.ReportExtraFiles, true)]
        [DataRow(LoggingFlags.None, false, "*.zip")]
        [DataRow(LoggingFlags.None, true, "*.zip")]
        [DataRow(LoggingFlags.VerboseOutput, false, "*.zip")]
        [DataRow(LoggingFlags.VerboseOutput, true, "*.zip")]
        [DataRow(LoggingFlags.ReportExtraFiles, false, "*.zip")]
        [DataRow(LoggingFlags.ReportExtraFiles, true, "*.zip")]
        public async Task Test_Logging_ExcludeExtra(LoggingFlags loggingFlags, bool excludeExtra, string selectionFilter = "*")
        {
            var tmp = Directory.CreateDirectory(TempDest);
            tmp.CreateSubdirectory("ExtraDir1");
            tmp.CreateSubdirectory("ExtraDir2.zip");
            File.WriteAllText(Path.Combine(TempDest, "extra.zip"), "extra");
            File.WriteAllText(Path.Combine(TempDest, "extra.txt"), "extra");

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.LoggingOptions.ApplyLoggingFlags(loggingFlags | LoggingFlags.ListOnly);
            cmd.SelectionOptions.ExcludeExtra = excludeExtra;
            cmd.CopyOptions.AddFileFilter(selectionFilter);
            var results = await RunCommand(cmd);

            Assert.IsNotNull(results);
            Console.WriteLine(string.Join(Environment.NewLine, results.LogLines));

            // 2 extra files exist. LoggingOptions.ReportExtraFiles decides if the non-selected ones will appear in statistics. 
            // if the selection filter is default or not specified, LoggingOptions.ReportExtraFiles has no effect.
            int expectedExtras = 2;
            bool reportextraDirs = true;
            if (selectionFilter != "*")
            {
                expectedExtras = cmd.LoggingOptions.ReportExtraFiles ? 2 : 1;
                reportextraDirs = cmd.LoggingOptions.ReportExtraFiles;
            }
            Assert.AreEqual(expectedExtras, results.FilesStatistic.Extras);
            
            // check that the directory was found or excluded based on the selection filter
            expectedExtras = reportextraDirs ? 2 : 1;
            Assert.AreEqual(expectedExtras, results.DirectoriesStatistic.Extras);

            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results.LogLines, "Log lines should not be empty");
            
            // SelectionOptions.ExcludeExtra prevents writing to the log but not statistic.
            // Verbose overrides ExcludeExtra
            if (!excludeExtra || cmd.LoggingOptions.VerboseOutput)
            {
                Assert.Contains(line => line.Contains("extra.zip", StringComparison.InvariantCultureIgnoreCase), results.LogLines, $"\nLog lines should report extra under this scenario.");
            }
            else
            {
                Assert.DoesNotContain(line => line.Contains("extra.zip", StringComparison.InvariantCultureIgnoreCase), results.LogLines, $"\nLog lines should not report extra under this scenario.");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // LIST-ONLY
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(false, DisplayName = "ListOnly flat")]
        [DataRow(true, DisplayName = "ListOnly recursive")]
        public async Task Test_Logging_ListOnly_ReportsWithoutWriting(bool recursive)
        {
            var cmd = GetCommand(SharedSource, TempDest);
            Assert.IsFalse(Directory.Exists(cmd.CopyOptions.Destination));
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = recursive;
            cmd.LoggingOptions.ListOnly = true;
            var results = await RunCommand(cmd);

            long expectedFiles = SourceTree.GetFileCount(cmd);
            int dTotal = SourceTree.GetDirTotal(cmd);
            int dCopied = SourceTree.GetDirCopied(cmd);
            AssertResults(results, $"ListOnly(recursive={recursive})",
                expectedDirTotal: dTotal,
                expectedDirCopied: dCopied,
                expectedDirExtras: 0,
                expectedDirSkipped: dTotal - dCopied,
                expectedFileTotal: expectedFiles,
                expectedFileCopied: expectedFiles,
                expectedFileExtras: 0,
                expectedFileSkipped: 0);

            // Nothing should have been written to disk
            Assert.IsFalse(Directory.Exists(cmd.CopyOptions.Destination));
        }

        [TestMethod]
        public async Task Test_Mismatch_Directory()
        {
            var cmd = GetCommand(Test_Setup.Source_Standard, TempDest);
            Directory.CreateDirectory(cmd.CopyOptions.Destination);
            File.WriteAllLines(Path.Combine(cmd.CopyOptions.Destination, "SubFolder_2"), ["This is a Directory in the source and should be a mismatch"]); 
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            var results = await RunCommand(cmd);
            int dT = SourceTree.GetDirTotal(cmd);
            int dC = SourceTree.GetDirCopied(cmd);
            int fc = SourceTree.GetFileCount(cmd) - 4; // -4 because they exist within the mismatch directory
            AssertResults(results, nameof(Test_Mismatch_Directory),
                expectedDirTotal: dT,
                expectedDirCopied: dC - 1,
                expectedDirExtras: 0,
                expectedDirSkipped: dT - dC,
                expectedFileTotal: fc,
                expectedFileCopied: fc,
                expectedFileExtras: 0,
                expectedFileSkipped: 0,
                expectedFileMismatch: 0,
                expectedDirMismatch: 1);

        }

        /// <summary>
        /// Extra file (which was the mismatch against the source directory) is purged. Then copy proceeds.
        /// Mismatch is not reported because purge resolved mismatch.
        /// </summary>
        [TestMethod]
        public async Task Test_Mismatch_Directory_Purge()
        {
            var cmd = GetCommand(Test_Setup.Source_Standard, TempDest);
            Directory.CreateDirectory(cmd.CopyOptions.Destination);
            File.WriteAllLines(Path.Combine(cmd.CopyOptions.Destination, "SubFolder_2"), ["This is a Directory in the source and should be a mismatch"]);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.CopyOptions.Purge = true;
            var results = await RunCommand(cmd);
            int dT = SourceTree.GetDirTotal(cmd);
            int dC = SourceTree.GetDirCopied(cmd);
            int fc = SourceTree.GetFileCount(cmd); // -4 because they exist within the mismatch directory
            AssertResults(results, nameof(Test_Mismatch_Directory_Purge),
                expectedDirTotal: dT,
                expectedDirCopied: dC,
                expectedDirExtras: 0,
                expectedDirSkipped: dT - dC,
                expectedFileTotal: fc,
                expectedFileCopied: fc,
                expectedFileExtras: 1,
                expectedFileSkipped: 0,
                expectedFileMismatch: 0,
                expectedDirMismatch: 0);

        }


        [TestMethod]
        public async Task Test_Mismatch_File()
        {
            var cmd = GetCommand(Test_Setup.Source_Standard, TempDest);
            Directory.CreateDirectory(Path.Combine(cmd.CopyOptions.Destination, "4_Bytes.txt"));
            var results = await RunCommand(cmd);
            int dT = SourceTree.GetDirTotal(cmd);
            int dC = SourceTree.GetDirCopied(cmd);
            AssertResults(results, nameof(Test_Mismatch_File),
                expectedDirTotal: dT,
                expectedDirCopied: dC,
                expectedDirExtras: 0,
                expectedDirSkipped: dT - dC,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd) - 1,
                expectedFileExtras: 0,
                expectedFileSkipped: 0,
                expectedFileMismatch: 1,
                expectedDirMismatch: 0);

        }

        /// <summary>
        /// Extra directory (which was the mismatch against the source file) is purged. Then copy proceeds.
        /// Mismatch is not reported because purge resolved mismatch.
        /// Subdirectories and files within the mismatch are deleted and reported, but the mismatch itself is not reported due to being resolved.
        /// </summary>
        [TestMethod]
        public async Task Test_Mismatch_File_Purge()
        {
            var cmd = GetCommand(Test_Setup.Source_Standard, TempDest);
            Directory.CreateDirectory(Path.Combine(cmd.CopyOptions.Destination, "4_Bytes.txt", "SubFolder"));
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.CopyOptions.Purge = true;
            var results = await RunCommand(cmd);
            int dT = SourceTree.GetDirTotal(cmd);
            int dC = SourceTree.GetDirCopied(cmd);
            int fc = SourceTree.GetFileCount(cmd); // -4 because they exist within the mismatch directory
            AssertResults(results, nameof(Test_Mismatch_File_Purge),
                expectedDirTotal: dT,
                expectedDirCopied: dC,
                expectedDirExtras: 1,
                expectedDirSkipped: dT - dC,
                expectedFileTotal: fc,
                expectedFileCopied: fc,
                expectedFileExtras: 0,
                expectedFileSkipped: 0,
                expectedFileMismatch: 0,
                expectedDirMismatch: 0);

        }

        // ════════════════════════════════════════════════════════════════════════
        // MOVE TESTS
        // Each move test calls PrepMoveSource() to get an expendable copy.
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.MoveFiles, 0, DisplayName = "Move Files - Depth Unlimited")]
        [DataRow(CopyActionFlags.MoveFiles, 1, DisplayName = "Move Files - Depth 1")]
        [DataRow(CopyActionFlags.MoveFiles, 3, DisplayName = "Move Files - Depth 3")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories, 0, DisplayName = "MoveFilesAndDirectories - Depth Unlimited")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories, 1, DisplayName = "MoveFilesAndDirectories - Depth 1")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories, 3, DisplayName = "MoveFilesAndDirectories - Depth 3")]
        // List Only
        [DataRow(CopyActionFlags.MoveFiles, 4, true, DisplayName = "Move Files - Depth 4 (List Only)")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories, 4, true, DisplayName = "MoveFilesAndDirectories - Depth 4  (List Only)")]
        // With Copy Flags
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.CopySubdirectories, 1, true, DisplayName = "Move + CopySubdirectories  - Depth 1 (List Only)")]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.CopySubdirectories, 4, true, DisplayName = "Move + CopySubdirectories  - Depth 4 (List Only)")]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.CopySubdirectoriesIncludingEmpty, 4, true, DisplayName = "Move + CopySubdirectoriesIncludeEmpty - Depth 4  (List Only)")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.CopySubdirectories, 1, true, DisplayName = "MoveFilesAndDirectories + CopySubdirectories - Depth 1 (List Only)")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.CopySubdirectories, 4, true, DisplayName = "MoveFilesAndDirectories + CopySubdirectories - Depth 4 (List Only)")]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.CopySubdirectoriesIncludingEmpty, 4, true, DisplayName = "MoveFilesAndDirectories + CopySubdirectoriesIncludeEmpty - Depth 4  (List Only)")]
        public async Task Test_Move_Files(CopyActionFlags flags, int depth, bool listOnly = false)
        {
            string moveSource = await PrepMoveSource();
            try
            {
                var cmd = GetCommand(moveSource, TempDest);

                cmd.CopyOptions.ApplyActionFlags(flags);
                cmd.CopyOptions.Depth = depth;
                cmd.LoggingOptions.ListOnly = listOnly;

                Assert.AreEqual(flags.HasFlag(CopyActionFlags.MoveFiles), cmd.CopyOptions.MoveFiles);
                Assert.AreEqual(flags.HasFlag(CopyActionFlags.MoveFilesAndDirectories), cmd.CopyOptions.MoveFilesAndDirectories);
                Assert.IsTrue(cmd.CopyOptions.MoveFiles || cmd.CopyOptions.MoveFilesAndDirectories);
                Assert.IsFalse(cmd.CopyOptions.Purge);
                Assert.IsFalse(cmd.CopyOptions.Mirror);

                var results = await RunCommand(cmd);

                // Files moved from root only; dirs remain in source
                int dT = SourceTree.GetDirTotal(cmd);
                int dC = SourceTree.GetDirCopied(cmd);
                AssertResults(results, nameof(Test_Move_Files),
                    expectedDirTotal: dT,
                    expectedDirCopied: dC,
                    expectedDirExtras: 0,
                    expectedDirSkipped: dT - dC,
                    expectedFileTotal: SourceTree.GetFileCount(cmd),
                    expectedFileCopied: SourceTree.GetFileCount(cmd),
                    expectedFileExtras: 0, expectedFileSkipped: 0);

                // Source root files should be gone; subdirs untouched
                bool isRecursing = cmd.CopyOptions.CopySubdirectories || cmd.CopyOptions.CopySubdirectoriesIncludingEmpty;
                bool moveDirs = !listOnly && flags.HasFlag(CopyActionFlags.MoveFilesAndDirectories);

                // evaluate root directory
                switch (depth)
                {
                    case 0 when moveDirs && isRecursing:
                    case 4 when moveDirs:
                        Assert.IsFalse(Directory.Exists(moveSource), "\n >> Source Root should have been moved.");
                        break;
                    default:
                        Assert.IsTrue(Directory.Exists(moveSource), "\n >> Source Root should still exist.");
                        break;
                }

                if (listOnly)
                {
                    Assert.IsTrue(Directory.Exists(moveSource), "\n >> ListOnly should not delete source directory");
                    Assert.IsNotEmpty(Directory.GetFiles(moveSource, "*", SearchOption.TopDirectoryOnly), "\n >> ListOnly should not have moved files.");
                    Assert.IsNotEmpty(Directory.GetDirectories(moveSource), "\n >> ListOnly should not delete source subdirs");
                }
                else
                {
                    // root directory items are always moved 
                    Assert.IsEmpty(Directory.GetFiles(moveSource, "*", SearchOption.TopDirectoryOnly), "\n >> Source root files should have been moved");
                }

                // evaluate subdirectory files
                string subDir = Path.Combine(moveSource, "SubFolder_2");
                switch (depth)
                {
                    case 0 when moveDirs && isRecursing:
                    case 2 when moveDirs && isRecursing:
                    case 3 when moveDirs && isRecursing:
                    case 4 when moveDirs:
                        Assert.IsFalse(Directory.Exists(subDir), "\n >> subdirectory directory should have been moved.");
                        break;

                    default:
                        Assert.IsTrue(Directory.Exists(subDir), "\n >> Subdirectory should not have been moved");
                        Assert.IsNotEmpty(Directory.GetFiles(subDir, "*", SearchOption.TopDirectoryOnly), "\n >> Subdirectory root files should not have been moved");
                        break;
                }
            }
            finally
            {
                try { Directory.Delete(moveSource, true); } catch { }
            }
        }



        // ════════════════════════════════════════════════════════════════════════
        // FILE FILTER / EXCLUSION
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Test_Selection_FileFilter()
        {
            // Only *.txt files — excludes 4_Bytes.htm files if present.
            // In the standard tree all 4 files per dir are .txt, so count stays 20.
            // This test validates the filter is applied, not that it excludes anything —
            // override in subclasses if the file set has mixed extensions.
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.AddFileFilter("*.htm");
            var results = await RunCommand(cmd);

            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.FilesStatistic.Copied);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Test_Selection_ExcludedFiles()
        {
            // Exclude files matching "*0*_Bytes*" (hits 0_Bytes.txt in each dir)
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludedFiles.Add("0_Bytes*");
            var results = await RunCommand(cmd);

            long expectedTotal = SourceTree.GetFileCount(cmd);
            long expectedSkipped = 3;
            long expectedCopied = expectedTotal - expectedSkipped;

            AssertResults(results, nameof(Test_Selection_ExcludedFiles),
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: expectedTotal,
                expectedFileCopied: expectedCopied,
                expectedFileExtras: 0,
                expectedFileSkipped: expectedSkipped);
        }


        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Test_Selection_ExcludedDirectories()
        {
            // Exclude SubFolder_2 → loses 1 dir + 4 files
            const int excludedDirs = 1;
            const int excludedFiles = 4;

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludedDirectories.Add("SubFolder_2");
            var results = await RunCommand(cmd);

            long expectedTotal = SourceTree.GetDirTotal(cmd);
            long expectedSkipped = 1 + excludedDirs;
            long expectedCopied = SourceTree.GetDirCopied(cmd) - excludedDirs;

            long fileCount = SourceTree.GetFileCount(cmd) - excludedFiles;
            Assert.IsGreaterThanOrEqualTo(6, fileCount);

            AssertResults(results, nameof(Test_Selection_ExcludedDirectories),
                expectedDirTotal: expectedTotal,
                expectedDirCopied: expectedCopied,
                expectedDirExtras: 0, 
                expectedDirSkipped: expectedSkipped,
                expectedFileTotal: fileCount,
                expectedFileCopied: fileCount,
                expectedFileExtras: 0, 
                expectedFileSkipped: 0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)] 
        public async Task Test_Selection_ExcludeOlder(bool value)
        {
            (string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
            string destDir = TempDest;
            try
            {
                // Setup Directories
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(sourceFile, "test");
                Directory.CreateDirectory(destDir);
                var destFile = new FileInfo(Path.Combine(destDir, Path.GetFileName(sourceFile)));
                File.Copy(sourceFile, destFile.FullName);

                DateTime stamp = DateTime.UtcNow.AddMinutes(-10);
                File.SetLastWriteTimeUtc(sourceFile, stamp);
                File.SetLastWriteTimeUtc(destFile.FullName, stamp.AddMinutes(5));  // Dest is 5 minutes newer

                var sourceFileInfo = new FileInfo(sourceFile);
                destFile.Refresh();

                Assert.AreEqual(sourceFileInfo.Length, destFile.Length);
                Assert.AreEqual(sourceFileInfo.Attributes, destFile.Attributes);
                Assert.IsGreaterThan(File.GetLastWriteTimeUtc(sourceFile), File.GetLastWriteTimeUtc(destFile.FullName));

                // Run command
                var cmd = GetCommand(sourceDir, destDir);
                cmd.SelectionOptions.ExcludeOlder = value;
                var results = await RunCommand(cmd);
                Assert.IsNotNull(results);
                try
                {
                    
                    Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Copied);
                    Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Skipped);
                }
                catch
                {
                    Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(sourceDir, true); } catch { }
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Test_Selection_ExcludeNewer(bool value)
        {
            (string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
            string destDir = TempDest;
            try
            {
                // Setup Directories
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(sourceFile, "test");
                Directory.CreateDirectory(destDir);
                var destFile = new FileInfo(Path.Combine(destDir, Path.GetFileName(sourceFile)));
                File.Copy(sourceFile, destFile.FullName);
                File.SetLastWriteTimeUtc(destFile.FullName, DateTime.UtcNow.AddMinutes(-10));

                var sourceFileInfo = new FileInfo(sourceFile);
                
                Assert.AreEqual(sourceFileInfo.Length, destFile.Length);
                Assert.AreEqual(sourceFileInfo.Attributes, destFile.Attributes);
                Assert.IsLessThan(sourceFileInfo.LastWriteTimeUtc, File.GetLastWriteTimeUtc(destFile.FullName));

                // Run command
                var cmd = GetCommand(sourceDir, destDir);
                cmd.SelectionOptions.ExcludeNewer = value;
                var results = await RunCommand(cmd);
                Assert.IsNotNull(results);
                try
                {
                    Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Copied);
                    Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Skipped);
                }
                catch
                {
                    Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(sourceDir, true); } catch { }
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Test_Selection_ExcludeLonely(bool value)
        {
            (string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
            string destDir = TempDest;
            try
            {
                // Setup Directories
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(sourceFile, "test");
                Directory.CreateDirectory(destDir);

                // Run command
                var cmd = GetCommand(sourceDir, destDir);
                cmd.SelectionOptions.ExcludeLonely = value;
                var results = await RunCommand(cmd);
                Assert.IsNotNull(results);
                try
                {
                    Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Copied);
                    Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Skipped);
                }
                catch
                {
                    Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(sourceDir, true); } catch { }
            }
        }

        /// <summary>
        /// Same name, same timestamp, different sizes
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Test_Selection_ExcludeChanged(bool value)
        {
            (string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
            string destDir = TempDest;
            try
            {
                // Setup Directories
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(sourceFile, "test");
                
                var destFile = new FileInfo(Path.Combine(destDir, Path.GetFileName(sourceFile)));
                Directory.CreateDirectory(destDir);
                File.WriteAllText(destFile.FullName, "Different_Size_File");

                var lastWriteTime = File.GetLastWriteTimeUtc(sourceFile);
                File.SetLastWriteTimeUtc(destFile.FullName, lastWriteTime);

                Assert.AreNotEqual(new FileInfo(sourceFile).Length, destFile.Length);
                Assert.AreEqual(lastWriteTime, File.GetLastWriteTimeUtc(sourceFile));
                Assert.AreEqual(lastWriteTime, File.GetLastWriteTimeUtc(destFile.FullName));

                // Run command
                var cmd = GetCommand(sourceDir, destDir);
                cmd.SelectionOptions.ExcludeChanged = value;
                var results = await RunCommand(cmd);
                Assert.IsNotNull(results);
                try 
                {
                    Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Copied);
                    Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Skipped);
                }
                catch
                {
                    Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(sourceDir, true); } catch { }
            }
        }

        ///// <summary>
        ///// Same file size, different change time
        ///// </summary>
        //[TestMethod]
        //[DataRow(true)]
        //[DataRow(false)]
        //public async Task Test_Selection_IncludeModified(bool value)
        //{
        //    /*
        //     * Test is set up, but is not implemented. 
        //     * Appears to require specialized attributes or combination of flags to work.
        //     * https://ss64.org/viewtopic.php?t=408
        //     */

        //    //(string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
        //    //string destDir = TempDest;
        //    //try
        //    //{
        //    //    // Setup Directories
        //    //    Directory.CreateDirectory(sourceDir);
        //    //    File.WriteAllText(sourceFile, "test");
        //    //    Directory.CreateDirectory(destDir);
        //    //    var sourceFileInfo = new FileInfo(sourceFile);
        //    //    var destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
        //    //    File.WriteAllText(destFile, "test");

        //    //    Assert.AreEqual(new FileInfo(sourceFile).Length, new FileInfo(destFile).Length);
        //    //    Assert.AreNotEqual(File.GetLastWriteTimeUtc(sourceFile), File.GetLastWriteTimeUtc(destFile));

        //    //    // Run command
        //    //    var cmd = GetCommand(sourceDir, destDir);
        //    //    cmd.SelectionOptions.ExcludeOlder = true;
        //    //    cmd.SelectionOptions.IncludeModified = value;
        //    //    var results = await RunCommand(cmd);
        //    //    Assert.IsNotNull(results);
        //    //    try
        //    //    {
        //    //        Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Copied);
        //    //        Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Skipped);
        //    //    }
        //    //    catch
        //    //    {
        //    //        Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
        //    //        throw;
        //    //    }
        //    //}
        //    //finally
        //    //{
        //    //    try { Directory.Delete(sourceDir, true); } catch { }
        //    //}
        //}

        /// <summary>
        /// Same file size, same time, same attributes
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Test_Selection_IncludeSame(bool value)
        {
            (string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
            string destDir = TempDest;
            try
            {
                // Setup Directories
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(sourceFile, "test");
                Directory.CreateDirectory(destDir);
                var sourceFileInfo = new FileInfo(sourceFile);
                var destFile = new FileInfo(Path.Combine(destDir, Path.GetFileName(sourceFile)));
                File.Copy(sourceFileInfo.FullName, destFile.FullName);
                
                destFile.Refresh();
                Assert.AreEqual(sourceFileInfo.Length, destFile.Length);
                Assert.AreEqual(sourceFileInfo.LastWriteTimeUtc, destFile.LastWriteTimeUtc);
                Assert.AreEqual(sourceFileInfo.Attributes, destFile.Attributes);

                // Run command
                var cmd = GetCommand(sourceDir, destDir);
                cmd.SelectionOptions.IncludeSame = value;
                var results = await RunCommand(cmd);
                Assert.IsNotNull(results);
                try
                {
                    Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Copied);
                    Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Skipped);
                }
                catch
                {
                    Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(sourceDir, true); } catch { }
            }
        }

        /// <summary>
        /// Same file size, same time, different attributes
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Test_Selection_IncludeTweaked(bool value)
        {
            (string sourceDir, string sourceFile) = Test_Setup.GetNewTempPathWithChild();
            string destDir = TempDest;
            try
            {
                // Setup Directories
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(sourceFile, "test");
                Directory.CreateDirectory(destDir);
                var sourceFileInfo = new FileInfo(sourceFile);
                var destFile = new FileInfo(Path.Combine(destDir, Path.GetFileName(sourceFile)));
                File.WriteAllText(destFile.FullName, "test");
                destFile.LastWriteTimeUtc = sourceFileInfo.LastWriteTimeUtc;
                destFile.Attributes = sourceFileInfo.Attributes | FileAttributes.Hidden; // hidden removed if overwritten by robocopy

                Assert.AreEqual(sourceFileInfo.Length, destFile.Length);
                Assert.AreEqual(sourceFileInfo.LastWriteTimeUtc, destFile.LastWriteTimeUtc);
                Assert.AreNotEqual(sourceFileInfo.Attributes, destFile.Attributes);

                // Run command
                var cmd = GetCommand(sourceDir, destDir);
                cmd.SelectionOptions.IncludeTweaked = value;
                var results = await RunCommand(cmd);
                Assert.IsNotNull(results);
                try
                {
                    Assert.AreEqual(value ? 1 : 0, results.FilesStatistic.Copied);
                    Assert.AreEqual(value ? 0 : 1, results.FilesStatistic.Skipped);
                }
                catch
                {
                    Console.WriteLine(String.Join(Environment.NewLine, results.LogLines));
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(sourceDir, true); } catch { }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PURGE TESTS
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.Purge, DisplayName = "Purge via /PURGE flag")]
        [DataRow(CopyActionFlags.Mirror, DisplayName = "Purge via /MIR flag")]
        public async Task Test_Purge_ExtraDirsAndTheirFilesAreDeletedAndCounted(CopyActionFlags flags)
        {
            int extraDirCount = 2;
            int filesPerExtraDir = 3;
            // Pre-place extra dest dirs, each containing files.
            for (int d = 0; d < extraDirCount; d++)
            {
                var dir = Path.Combine(TempDest, $"PurgeDir_{d}");
                Directory.CreateDirectory(dir);
                for (int f = 0; f < filesPerExtraDir; f++)
                    File.WriteAllText(Path.Combine(dir, $"file_{f}.txt"), "purge");
            }

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.ApplyActionFlags(flags);

            var results = await RunCommand(cmd);

            AssertResults(results,
                $"Purge dirs(dirs={extraDirCount}, files={filesPerExtraDir})",
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: extraDirCount,
                expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: extraDirCount * filesPerExtraDir,
                expectedFileSkipped: 0);

            // Physical verification
            for (int d = 0; d < extraDirCount; d++)
                Assert.IsFalse(Directory.Exists(Path.Combine(TempDest, $"PurgeDir_{d}")),
                    $"PurgeDir_{d} should have been deleted");
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.Purge, DisplayName = "Purge via /PURGE flag")]
        [DataRow(CopyActionFlags.Mirror, DisplayName = "Purge via /MIR flag")]
        public async Task Test_Purge_NestedExtraDirsCountAllFiles(CopyActionFlags flags)
        {
            // Build a chain: dest/nested0/nested1/... each level has 1 file.
            int nestDepth = 3;
            string current = TempDest;
            for (int depth = 0; depth < nestDepth; depth++)
            {
                current = Path.Combine(current, $"nested_{depth}");
                Directory.CreateDirectory(current);
                File.WriteAllText(Path.Combine(current, $"file_{depth}.txt"), "purge");
            }

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.ApplyActionFlags(flags);
            var results = await RunCommand(cmd);

            // nestDepth dirs + nestDepth files inside them should all be counted
            AssertResults(results, $"NestedPurge(depth={nestDepth})",
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: nestDepth,
                expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: nestDepth,
                expectedFileSkipped: 0);

            Assert.IsFalse(Directory.Exists(Path.Combine(TempDest, "nested_0")),
                "Root of nested extra dir tree should have been purged");
        }


        /// <summary>
        /// Extra file (which was the mismatch against the source directory) is purged. Then copy proceeds.
        /// Mismatch is not reported because purge resolved mismatch.
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.Mirror, DisplayName = "Purge via /MIR flag")]
        [DataRow(CopyActionFlags.Purge, DisplayName = "Purge via /PURGE flag")]
        [DataRow(CopyActionFlags.Purge | CopyActionFlags.CopySubdirectories, DisplayName = "Purge via /PURGE + CopySubdirectories flag")]
        public async Task Test_Purge_SelectedFilesOnly(CopyActionFlags flags)
        {
            // Build a chain: dest/nested0/nested1/... each level has 1 file.
            int nestDepth = 3;
            string current = TempDest;
            for (int depth = 0; depth < nestDepth; depth++)
            {
                current = Path.Combine(current, $"nested_{depth}");
                Directory.CreateDirectory(current);
                File.WriteAllText(Path.Combine(current, $"file_{depth}.txt"), "purge");
                File.WriteAllText(Path.Combine(current, $"file_{depth}.zip"), "Do Not purge");
            }

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.AddFileFilter("*.txt");
            cmd.CopyOptions.ApplyActionFlags(flags);
            cmd.CopyOptions.Depth = 0;
            bool recurse = cmd.CopyOptions.Mirror || cmd.CopyOptions.CopySubdirectoriesIncludingEmpty || cmd.CopyOptions.CopySubdirectories;
            var results = await RunCommand(cmd);

            // nestDepth dirs + nestDepth files inside them should all be counted
            AssertResults(results, $"NestedPurge(depth={nestDepth})",
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: recurse ? nestDepth : 0,
                expectedDirSkipped: 1,
                expectedFileTotal: recurse ? 9 : 3, // due to file filter
                expectedFileCopied: recurse ? 9 : 3,
                expectedFileExtras: recurse ? nestDepth * 2 : 0,
                expectedFileSkipped: 0);

            Assert.AreEqual(!recurse, Directory.Exists(Path.Combine(TempDest, "nested_0")), "\n >> Root of nested extra dir tree should not have been purged");
            string err = recurse ? "\n >> Recursion purge should delete all extras." : "\n >> Purging without Recursion only deletes file on root.";
            Assert.AreEqual(recurse ? 0 : 3, Directory.GetFiles(cmd.CopyOptions.Destination, "file_*.zip", SearchOption.AllDirectories).Length, err);

        }
    }
}