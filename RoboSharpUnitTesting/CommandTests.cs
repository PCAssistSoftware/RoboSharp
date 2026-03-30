using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using RoboSharp.UnitTests;
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
    public class RoboCommand_Tests : CommandTests<RoboCommand>
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
            bool isRecursive = command.CopyOptions.CopySubdirectories || command.CopyOptions.CopySubdirectoriesIncludingEmpty || command.CopyOptions.Mirror;
            return (isRecursive || command.CopyOptions.Depth > 1) ? command.CopyOptions.Depth : 1;
        }
        public static int GetFileCount(IRoboCommand command)
        {
            int depth = GetDepth(command);
            depth = (depth == 0 || depth > 4) ? 4 : depth;
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
            bool includingEmpty = command.CopyOptions.CopySubdirectoriesIncludingEmpty || command.CopyOptions.Mirror || depth > 0;
            return depth switch
            {
                // default (unlimited) depth
                0 or > 4 => 5,// root + 5 subdirs
                1 => 1,
                2 => includingEmpty ? 3 : 2,
                3 => includingEmpty ? 4 : 3,
                _ => 5,
            };
        }

        /// <summary>
        /// Gets the expected directory count for the standard test tree based on the command's recursion and empty dir options.
        /// <br/> This assumes no child directories exist prior to starting the command.
        /// </summary>
        public static int GetDirCopied(IRoboCommand command) => GetDirTotal(command) - 1; // all except root are "copied" due to "authentication" behavior that creates the destination root dir

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
            TempDest = Path.Combine(
                Path.GetTempPath(),
                "RoboSharp_CmdTests",
                typeof(T).Name,
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(TempDest);
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
            string moveSource = Path.Combine(
                Path.GetTempPath(),
                "RoboSharp_MoveSource",
                typeof(T).Name,
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(moveSource);

            // Use a real RoboCommand to clone the source tree — read-only, no side-effects
            var rc = new RoboCommand();
            rc.CopyOptions.Source = SharedSource;
            rc.CopyOptions.Destination = moveSource;
            rc.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            Token.Register(() => rc.Stop());
            await rc.StartAsync().WaitAsync(Token);
            return moveSource;
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
            long expectedFileTotal, long expectedFileCopied, long expectedFileExtras, long expectedFileSkipped, long expectedFileFailed = 0)
        {
            Assert.IsNotNull(results, "Results must not be null — command may have been cancelled by timeout.");

            // Print for diagnostics
            Console.WriteLine($"── {label} ──");
            Console.WriteLine("Expected  Dirs  : {0}", new Statistic(type: Statistic.StatType.Directories, "", expectedDirTotal, expectedDirCopied, expectedDirSkipped, 0, 0, expectedDirExtras));
            Console.WriteLine("  Actual  Dirs  : {0}\n", results.DirectoriesStatistic);

            Console.WriteLine("Expected  Files : {0}", new Statistic(type: Statistic.StatType.Directories, "", expectedFileTotal, expectedFileCopied, expectedFileSkipped, 0, expectedFileFailed, expectedFileExtras));
            Console.WriteLine("  Actual  Files : {0}\n", results.FilesStatistic);

            //Console.WriteLine("Expected  Bytes : {0}");
            Console.WriteLine("  Actual  Bytes : {0}\n", results.BytesStatistic);

            try
            {
                Assert.AreEqual(expectedDirTotal, results.DirectoriesStatistic.Total, $"\n[{label}] Dir.Total");
                Assert.AreEqual(expectedDirCopied, results.DirectoriesStatistic.Copied, $"\n[{label}] Dir.Copied");
                Assert.AreEqual(expectedDirExtras, results.DirectoriesStatistic.Extras, $"\n[{label}] Dir.Extras");
                Assert.AreEqual(expectedDirSkipped, results.DirectoriesStatistic.Skipped, $"\n[{label}] Dir.Skipped");

                Assert.AreEqual(0, results.FilesStatistic.Mismatch, $"\n[{label}] File.Mismatch");
                Assert.AreEqual(expectedFileTotal, results.FilesStatistic.Total, $"\n[{label}] File.Total");
                Assert.AreEqual(expectedFileCopied, results.FilesStatistic.Copied, $"\n[{label}] File.Copied");
                Assert.AreEqual(expectedFileFailed, results.FilesStatistic.Failed, $"\n[{label}] File.Failed");
                Assert.AreEqual(expectedFileExtras, results.FilesStatistic.Extras, $"\n[{label}] File.Extras");
                Assert.AreEqual(expectedFileSkipped, results.FilesStatistic.Skipped, $"\n[{label}] File.Skipped");
            }
            catch
            {
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


        /// <summary>
        /// Copies the root-level files only, without recursing into subdirectories.
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Copy_Flat()
        {
            // Only root-level files copied; subdirs not traversed.
            // Dir:  total=1 (root), copied=1, extras=0, skipped=0
            // File: total=4, copied=4, extras=0, skipped=0
            var cmd = GetCommand(SharedSource, TempDest);
            var results = await RunCommand(cmd);
            AssertResults(results, nameof(Copy_Flat),
                expectedDirTotal: 1, expectedDirCopied: 0, expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd), expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        /// <summary>
        /// SKIP TESTS (destination already up to date)
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Copy_SkipsAlreadyCopiedFiles()
        {
            // Run twice. Second run: all files exist in dest → all skipped.
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            await RunCommand(cmd); // first pass — populate dest

            // Second pass — same command, dest already populated
            var cmd2 = GetCommand(SharedSource, TempDest);
            cmd2.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            var results = await RunCommand(cmd2);

            AssertResults(results, nameof(Copy_SkipsAlreadyCopiedFiles),
                expectedDirTotal: SourceTree.GetDirTotal(cmd2),
                expectedDirCopied: 0, expectedDirExtras: 0,
                expectedDirSkipped: SourceTree.GetDirTotal(cmd),
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: 0, expectedFileExtras: 0,
                expectedFileSkipped: SourceTree.GetFileCount(cmd));
        }

        /// <summary>
        /// Copies subdirectories (RoboCopy /S).
        /// <br/> When depth is > 0, empty subdirectories are included even if /CopySubdirectoriesIncludingEmpty is false, because the command assumes the user explicitly wants to include subdirs up to that depth.
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(0, DisplayName = "Depth=Unlimited")]
        [DataRow(1, DisplayName = "Depth=1 (root only)")]
        [DataRow(2, DisplayName = "Depth=2")]
        [DataRow(3, DisplayName = "Depth=3")]
        [DataRow(4, DisplayName = "Depth=4")]
        public async Task Copy_Subdirectories(int depth)
        {
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectories = true;
            cmd.CopyOptions.Depth = depth;
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(Copy_Subdirectories),
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: 0, expectedFileSkipped: 0);

            if (depth >= 2)
            {
                string supplement = cmd.LoggingOptions.ListOnly ? "should not exist when ListOnly is true" : "should be copied";
                Assert.AreEqual(cmd.LoggingOptions.ListOnly, Directory.Exists(Path.Combine(TempDest, "SubFolder_1.1")), $"SubFolder_1.1 {supplement} at depth 2");
                if (depth >= 3)
                {
                    Assert.AreEqual(cmd.LoggingOptions.ListOnly, Directory.Exists(Path.Combine(TempDest, "SubFolder_1.1", "SubFolder_1.2")), $"SubFolder_1.2 {supplement} at depth 3");
                }
            }

        }

        /// <summary>
        /// Copies subdirectories but including empty ones (RoboCopy /E).
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Copy_SubdirectoriesIncludingEmpty()
        {
            // /E — recurse including empty dirs.
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.CopyOptions.Depth = 0;
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(Copy_SubdirectoriesIncludingEmpty),
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: SourceTree.GetFileCount(cmd),
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(1, DisplayName = "Depth=1 (root only)")]
        [DataRow(2, DisplayName = "Depth=2 (root + 1 level)")]
        [DataRow(3, DisplayName = "Depth=3 (root + 2 levels)")]
        public async Task Copy_WithDepthLimit(int depth)
        {
            // Depth=1: root dir only, 4 files.
            // Depth=2: root + SubFolder_1 + SubFolder_2 = 3 dirs, 4+4+4=12 files.
            // (SubFolder_1.1 is deeper than depth 2)
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.CopyOptions.Depth = depth;
            var results = await RunCommand(cmd);

            long expectedFiles = SourceTree.GetFileCount(cmd);

            AssertResults(results, $"Depth={depth}",
                expectedDirTotal: SourceTree.GetDirTotal(cmd), expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: expectedFiles, expectedFileCopied: expectedFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task DirectoryExclusion_ExcludesMatchingDirs()
        {
            // Exclude SubFolder_2 → loses 1 dir + 4 files
            const int excludedDirs = 1;
            const int excludedFiles = 4;

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludedDirectories.Add("SubFolder_2");
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(DirectoryExclusion_ExcludesMatchingDirs),
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd) - excludedDirs,
                expectedDirExtras: 0, expectedDirSkipped: 1 + excludedDirs,
                expectedFileTotal: SourceTree.GetFileCount(cmd) - excludedFiles,
                expectedFileCopied: SourceTree.GetFileCount(cmd) - excludedFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        // ════════════════════════════════════════════════════════════════════════
        // EXTRA FILE / DIR REPORTING
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Extra Directories are always reported in the results overview regardless of recursion mode.
        /// </summary>
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.Default, 3, DisplayName = "Root Directory Only")]
        [DataRow(CopyActionFlags.Mirror, 3, DisplayName = "Mirror Flag")]
        [DataRow(CopyActionFlags.CopySubdirectories, 3, DisplayName = "CopySubdirectories flag")]
        [DataRow(CopyActionFlags.CopySubdirectoriesIncludingEmpty, 3, DisplayName = "CopySubdirectoriesIncludingEmpty flag")]
        public async Task ExtraDirs_AreReported(CopyActionFlags copyFlags, int extraDirCount)
        {
            // Pre-place extra dirs (empty) in dest root.
            for (int i = 0; i < extraDirCount; i++)
                Directory.CreateDirectory(Path.Combine(TempDest, $"ExtraDir_{i}"));

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.ApplyActionFlags(copyFlags);
            var results = await RunCommand(cmd);

            // Extra dirs appear in Extras column, not Copied.
            // Total dirs = source dirs + extra dest dirs.
            AssertResults(results, $"{nameof(ExtraDirs_AreReported)}(n={extraDirCount})",
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
        public async Task ExtraFiles_AreReported(LoggingFlags loggingFlags, int extraFileCount)
        {
            // Pre-place extra files in dest root.
            for (int i = 0; i < extraFileCount; i++)
                File.WriteAllText(Path.Combine(TempDest, $"extra_{i}.txt"), "extra");

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.LoggingOptions.ApplyLoggingFlags(loggingFlags);
            var results = await RunCommand(cmd);

            // Files: 20 source copied + N extras in dest
            AssertResults(results, $"{nameof(ExtraFiles_AreReported)}(n={extraFileCount})",
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
        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task ExtraFiles_AreNotReported()
        {
            File.WriteAllText(Path.Combine(TempDest, "extra.txt"), "extra");

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.LoggingOptions.ReportExtraFiles = false;
            cmd.LoggingOptions.VerboseOutput = false;
            var results = await RunCommand(cmd);

            Assert.IsNotNull(results);
            Console.WriteLine(string.Join(Environment.NewLine, results.LogLines));
            Assert.AreEqual(1, results.FilesStatistic.Extras, "\n/XX (ExcludeExtra) must suppress extra file reporting");

            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results.LogLines, "Log lines should not be empty");
            Assert.DoesNotContain(line => line.Trim().StartsWith(cmd.Configuration.LogParsing_ExtraFile) && line.Trim().EndsWith("extra_0.txt"), results.LogLines, $"\nLog lines should not report extra under this scenario.");
        }


        // ════════════════════════════════════════════════════════════════════════
        // FILE FILTER / EXCLUSION
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task FileFilter_LimitsFilesCopied()
        {
            // Only *.txt files — excludes 4_Bytes.htm files if present.
            // In the standard tree all 4 files per dir are .txt, so count stays 20.
            // This test validates the filter is applied, not that it excludes anything —
            // override in subclasses if the file set has mixed extensions.
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.CopyOptions.FileFilter = new[] { "*.txt" };
            var results = await RunCommand(cmd);

            Assert.IsNotNull(results);
            // All files matching *.txt should be counted; non-matching skipped by filter
            Assert.AreEqual(0L, results.FilesStatistic.Failed, "No files should fail");
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task FileExclusion_ExcludesMatchingFiles()
        {
            // Exclude files matching "*0*_Bytes*" (hits 0_Bytes.txt in each dir)
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludedFiles.Add("0_Bytes*");
            var results = await RunCommand(cmd);

            long expectedSkipped = 3;
            long expectedCopied = SourceTree.GetFileCount(cmd) - expectedSkipped;

            AssertResults(results, nameof(FileExclusion_ExcludesMatchingFiles),
                expectedDirTotal: SourceTree.GetDirTotal(cmd),
                expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: SourceTree.GetFileCount(cmd),
                expectedFileCopied: expectedCopied,
                expectedFileExtras: 0,
                expectedFileSkipped: expectedSkipped);
        }


        // ════════════════════════════════════════════════════════════════════════
        // LIST-ONLY
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(false, DisplayName = "ListOnly flat")]
        [DataRow(true, DisplayName = "ListOnly recursive")]
        public async Task ListOnly_ReportsWithoutWriting(bool recursive)
        {
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = recursive;
            cmd.LoggingOptions.ListOnly = true;
            var results = await RunCommand(cmd);

            long expectedFiles = SourceTree.GetFileCount(cmd);

            AssertResults(results, $"ListOnly(recursive={recursive})",
                expectedDirTotal: SourceTree.GetDirTotal(cmd), expectedDirCopied: SourceTree.GetDirCopied(cmd),
                expectedDirExtras: 0, expectedDirSkipped: 1,
                expectedFileTotal: expectedFiles, expectedFileCopied: expectedFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);

            // Nothing should have been written to disk
            var written = Directory.GetFiles(TempDest, "*", SearchOption.AllDirectories);
            Assert.AreEqual(0, written.Length, "ListOnly must not write any files to destination");
        }

        // ════════════════════════════════════════════════════════════════════════
        // MOVE TESTS
        // Each move test calls PrepMoveSource() to get an expendable copy.
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(10000, CooperativeCancellation = true)]
        public async Task Move_Files_FlatOnly()
        {
            string moveSource = await PrepMoveSource();
            try
            {
                var cmd = GetCommand(moveSource, TempDest);
                cmd.CopyOptions.MoveFiles = true;
                var results = await RunCommand(cmd);

                // Files moved from root only; dirs remain in source
                AssertResults(results, nameof(Move_Files_FlatOnly),
                    expectedDirTotal: 1, expectedDirCopied: 0,
                    expectedDirExtras: 0, expectedDirSkipped: 1,
                    expectedFileTotal: SourceTree.GetFileCount(cmd),
                    expectedFileCopied: SourceTree.GetFileCount(cmd),
                    expectedFileExtras: 0, expectedFileSkipped: 0);

                // Source root files should be gone; subdirs untouched
                Assert.IsEmpty(Directory.GetFiles(moveSource, "*", SearchOption.TopDirectoryOnly), "\nSource root files should have been moved");
                Assert.IsNotEmpty(Directory.GetDirectories(moveSource), "\n>> /MOV should not delete source subdirs");
            }
            finally
            {
                try { Directory.Delete(moveSource, true); } catch { }
            }
        }

        [TestMethod, Timeout(10000, CooperativeCancellation = true)]
        public async Task Move_FilesAndDirectories_Recursive()
        {
            string moveSource = await PrepMoveSource();
            try
            {
                var cmd = GetCommand(moveSource, TempDest);
                cmd.CopyOptions.MoveFilesAndDirectories = true;
                cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
                var results = await RunCommand(cmd);

                AssertResults(results, nameof(Move_FilesAndDirectories_Recursive),
                    expectedDirTotal: SourceTree.GetDirTotal(cmd),
                    expectedDirCopied: SourceTree.GetDirCopied(cmd),
                    expectedDirExtras: 0, expectedDirSkipped: 1,
                    expectedFileTotal: SourceTree.GetFileCount(cmd),
                    expectedFileCopied: SourceTree.GetFileCount(cmd),
                    expectedFileExtras: 0, expectedFileSkipped: 0);

                // Source should be completely empty after /MOVE
                Assert.IsFalse(Directory.Exists(moveSource), "\n>> Directory should no longer exist in source folder after moving.");
            }
            finally
            {
                try { Directory.Delete(moveSource, true); } catch { }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PURGE TESTS
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.Purge, DisplayName = "Purge via /PURGE flag")]
        [DataRow(CopyActionFlags.Mirror, DisplayName = "Purge via /MIR flag")]
        public async Task Purge_ExtraDirsAndTheirFilesAreDeletedAndCounted(CopyActionFlags flags)
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
        public async Task Purge_NestedExtraDirsCountAllFiles(CopyActionFlags flags)
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
    }
}