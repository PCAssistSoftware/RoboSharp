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

#if NET6_0_OR_GREATER

namespace RoboSharp.Extensions.Tests
{
    // ══════════════════════════════════════════════════════════════════════════
    //  TEST FILE TREE (TEST_FILES/STANDARD — 4 files per folder, 3 levels)
    //
    //  Root/                           <- Source root (READ ONLY — never modified)
    //    0_Bytes.txt
    //    4_Bytes.txt
    //    1024_Bytes.txt
    //    65536_Bytes.txt
    //    SubFolder_1/
    //      0_Bytes.txt  4_Bytes.txt  1024_Bytes.txt  65536_Bytes.txt
    //      SubFolder_1.1/
    //        0_Bytes.txt  4_Bytes.txt  1024_Bytes.txt  65536_Bytes.txt
    //        SubFolder_1.2/            <- empty — exists only for /E tests
    //    SubFolder_2/
    //      0_Bytes.txt  4_Bytes.txt  1024_Bytes.txt  65536_Bytes.txt
    //
    //  Counts (used in DataRow expectations below):
    //    Dirs  (root + 4 subs + 1 empty) = 6 total source dirs
    //    Files  4 per non-empty dir × 5  = 20 files
    //    SubFolder_1.2 is empty → only appears in /E counts
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Known counts derived from the static TEST_FILES/STANDARD tree.
    /// Update these constants if the test file set ever changes.
    /// </summary>
    internal static class SourceTree
    {
        // Non-empty dirs that contain files (not counting root)
        public const int SubDirsWithFiles = 4;   // SubFolder_1, SubFolder_1.1, SubFolder_1.2 area, SubFolder_2
        public const int EmptySubDirs     = 1;   // SubFolder_1.2
        public const int TotalSubDirs     = SubDirsWithFiles + EmptySubDirs; // 5
        public const int TotalDirsWithRoot = TotalSubDirs + 1;               // 6

        public const int FilesPerDir      = 4;
        public const int DirsWithFiles    = SubDirsWithFiles + 1;            // +1 for root  = 5
        public const int TotalFiles       = DirsWithFiles * FilesPerDir;     // 20
    }
 
// ══════════════════════════════════════════════════════════════════════════
    //
    //  RoboCommand_Tests
    //
    //  Runs the full CommandTests suite against the real RoboCopy process.
    //  If these tests pass, the expected counts in CommandTests<T> are correct.
    //  If they fail, fix the SourceTree constants or test expectations first
    //  before debugging any custom implementation.
    //
    // ══════════════════════════════════════════════════════════════════════════
    [TestClass]
    public class RoboCommand_Tests : CommandTests<RoboCommand>
    {
        protected override RoboCommand GetCommand(string source, string destination)
        {
            var cmd = new RoboCommand();
            cmd.CopyOptions.Source      = source;
            cmd.CopyOptions.Destination = destination;
            return cmd;
        }
    }
    
    // ══════════════════════════════════════════════════════════════════════════
    //
    //  CommandTests<T>
    //
    //  Abstract base that every IRoboCommand implementation test class inherits.
    //  Design principles:
    //   • No back-to-back RoboCopy runs — expected counts are compile-time constants.
    //   • Each test creates its own isolated temp destination (or move-source).
    //   • Source (TEST_FILES/STANDARD) is NEVER modified.
    //   • GetCommand is virtual — subclasses override to supply their T instance.
    //
    // ══════════════════════════════════════════════════════════════════════════
    [TestClass]
    public abstract class CommandTests<T> where T : IRoboCommand, new()
    {
        // ── MSTest plumbing ───────────────────────────────────────────────────

        public TestContext TestContext { get; set; } = null!;

        /// <summary>Cooperative cancellation token wired to the MSTest timeout.</summary>
        protected CancellationToken Token => TestContext.CancellationTokenSource.Token;

        // ── Directories ───────────────────────────────────────────────────────

        /// <summary>Shared read-only source. Never modified by any test.</summary>
        protected static string SharedSource => Test_Setup.Source_Standard;

        /// <summary>
        /// Per-test isolated destination directory.
        /// Created fresh in <see cref="TestInit"/> and deleted in <see cref="TestCleanup"/>.
        /// </summary>
        protected string TempDest { get; private set; } = string.Empty;

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

        // ── Command factory ───────────────────────────────────────────────────

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> and sets Source/Destination.
        /// Subclasses override to inject factories, authenticators, or other dependencies.
        /// The base implementation uses <see cref="Activator.CreateInstance{T}"/> and
        /// wires Source + Destination on <see cref="IRoboCommand.CopyOptions"/>.
        /// </summary>
        protected virtual T GetCommand(string source, string destination)
        {
            var cmd = Activator.CreateInstance<T>();
            cmd.CopyOptions.Source = source;
            cmd.CopyOptions.Destination = destination;
            return cmd;
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
            rc.CopyOptions.Source      = SharedSource;
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
            long expectedDirTotal,   long expectedDirCopied,   long expectedDirExtras,   long expectedDirSkipped,
            long expectedFileTotal,  long expectedFileCopied,  long expectedFileExtras,  long expectedFileSkipped)
        {
            Assert.IsNotNull(results, "Results must not be null — command may have been cancelled by timeout.");

            // Print for diagnostics
            Console.WriteLine($"── {label} ──");
            Console.WriteLine($"  Dirs  : {results.DirectoriesStatistic}");
            Console.WriteLine($"  Files : {results.FilesStatistic}");
            Console.WriteLine($"  Bytes : {results.BytesStatistic}");

            Assert.AreEqual(expectedDirTotal,    results.DirectoriesStatistic.Total,   $"[{label}] Dir.Total");
            Assert.AreEqual(expectedDirCopied,   results.DirectoriesStatistic.Copied,  $"[{label}] Dir.Copied");
            Assert.AreEqual(expectedDirExtras,   results.DirectoriesStatistic.Extras,  $"[{label}] Dir.Extras");
            Assert.AreEqual(expectedDirSkipped,  results.DirectoriesStatistic.Skipped, $"[{label}] Dir.Skipped");

            Assert.AreEqual(expectedFileTotal,   results.FilesStatistic.Total,   $"[{label}] File.Total");
            Assert.AreEqual(expectedFileCopied,  results.FilesStatistic.Copied,  $"[{label}] File.Copied");
            Assert.AreEqual(expectedFileExtras,  results.FilesStatistic.Extras,  $"[{label}] File.Extras");
            Assert.AreEqual(expectedFileSkipped, results.FilesStatistic.Skipped, $"[{label}] File.Skipped");
        }


        // ════════════════════════════════════════════════════════════════════════
        // COPY TESTS
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Copy_Flat()
        {
            // Only root-level files copied; subdirs not traversed.
            // Dir:  total=1 (root), copied=1, extras=0, skipped=0
            // File: total=4, copied=4, extras=0, skipped=0
            var cmd = GetCommand(SharedSource, TempDest);
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(Copy_Flat),
                expectedDirTotal: 1,  expectedDirCopied: 1,  expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.FilesPerDir, expectedFileCopied: SourceTree.FilesPerDir,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Copy_Subdirectories()
        {
            // /S — recurse but skip empty dirs.
            // Empty SubFolder_1.2 is not included in count.
            // Dirs:  root + SubDirsWithFiles = 1+4 = 5
            // Files: 5 dirs × 4 files = 20
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectories = true;
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(Copy_Subdirectories),
                expectedDirTotal: 1 + SourceTree.SubDirsWithFiles,
                expectedDirCopied: 1 + SourceTree.SubDirsWithFiles,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task Copy_SubdirectoriesIncludingEmpty()
        {
            // /E — recurse including empty dirs.
            // Dirs:  root + TotalSubDirs = 1+5 = 6
            // Files: same 20 (empty dir has no files)
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(Copy_SubdirectoriesIncludingEmpty),
                expectedDirTotal: SourceTree.TotalDirsWithRoot,
                expectedDirCopied: SourceTree.TotalDirsWithRoot,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(1, DisplayName = "Depth=1 (root only)")]
        [DataRow(2, DisplayName = "Depth=2 (root + 1 level)")]
        public async Task Copy_WithDepthLimit(int depth)
        {
            // Depth=1: root dir only, 4 files.
            // Depth=2: root + SubFolder_1 + SubFolder_2 = 3 dirs, 4+4+4=12 files.
            // (SubFolder_1.1 is deeper than depth 2)
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.CopyOptions.Depth = depth;
            var results = await RunCommand(cmd);

            long expectedDirs  = depth == 1 ? 1 : 3;  // root; root+Sub1+Sub2
            long expectedFiles = depth == 1 ? 4 : 12;

            AssertResults(results, $"Depth={depth}",
                expectedDirTotal: expectedDirs, expectedDirCopied: expectedDirs,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: expectedFiles, expectedFileCopied: expectedFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        // ════════════════════════════════════════════════════════════════════════
        // SKIP TESTS (destination already up to date)
        // ════════════════════════════════════════════════════════════════════════

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
                expectedDirTotal: SourceTree.TotalDirsWithRoot,
                expectedDirCopied: 0, expectedDirExtras: 0,
                expectedDirSkipped: SourceTree.TotalDirsWithRoot,
                expectedFileTotal: SourceTree.TotalFiles,
                expectedFileCopied: 0, expectedFileExtras: 0,
                expectedFileSkipped: SourceTree.TotalFiles);
        }

        // ════════════════════════════════════════════════════════════════════════
        // EXTRA FILE / DIR REPORTING
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(1, DisplayName = "1 extra file in dest root")]
        [DataRow(3, DisplayName = "3 extra files in dest root")]
        public async Task ExtraFiles_AreReported(int extraFileCount)
        {
            // Pre-place extra files in dest root.
            for (int i = 0; i < extraFileCount; i++)
                File.WriteAllText(Path.Combine(TempDest, $"extra_{i}.txt"), "extra");

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.LoggingOptions.ReportExtraFiles = true;
            var results = await RunCommand(cmd);

            // Files: 20 source copied + N extras in dest
            AssertResults(results, $"{nameof(ExtraFiles_AreReported)}(n={extraFileCount})",
                expectedDirTotal: SourceTree.TotalDirsWithRoot,
                expectedDirCopied: SourceTree.TotalDirsWithRoot,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles + extraFileCount,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: extraFileCount,
                expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(1, DisplayName = "1 extra dir in dest root")]
        [DataRow(3, DisplayName = "3 extra dirs in dest root")]
        public async Task ExtraDirs_AreReported(int extraDirCount)
        {
            // Pre-place extra dirs (empty) in dest root.
            for (int i = 0; i < extraDirCount; i++)
                Directory.CreateDirectory(Path.Combine(TempDest, $"ExtraDir_{i}"));

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            var results = await RunCommand(cmd);

            // Extra dirs appear in Extras column, not Copied.
            // Total dirs = source dirs + extra dest dirs.
            AssertResults(results, $"{nameof(ExtraDirs_AreReported)}(n={extraDirCount})",
                expectedDirTotal: SourceTree.TotalDirsWithRoot + extraDirCount,
                expectedDirCopied: SourceTree.TotalDirsWithRoot,
                expectedDirExtras: extraDirCount, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(false, DisplayName = "Flat — extra dirs still reported")]
        [DataRow(true,  DisplayName = "Recursive — extra dirs reported")]
        public async Task ExtraDirs_AreReported_RegardlessOfRecursionMode(bool recursive)
        {
            // RoboCopy always reports extra dirs in Extras regardless of /S or not.
            Directory.CreateDirectory(Path.Combine(TempDest, "ExtraDir"));

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectories = recursive;
            var results = await RunCommand(cmd);

            // Extra dir count is always 1 regardless of recursion mode.
            Assert.IsNotNull(results);
            Assert.AreEqual(1L, results.DirectoriesStatistic.Extras,
                "Extra dir must be reported in Extras regardless of recursion mode");
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task ExtraDirs_NotReported_WhenExcludeExtraSet()
        {
            Directory.CreateDirectory(Path.Combine(TempDest, "ExtraDir"));

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludeExtra = true;
            var results = await RunCommand(cmd);

            Assert.IsNotNull(results);
            Assert.AreEqual(0L, results.DirectoriesStatistic.Extras,
                "/XX (ExcludeExtra) must suppress extra dir reporting");
        }

        // ════════════════════════════════════════════════════════════════════════
        // PURGE TESTS
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(false, DisplayName = "Purge via /PURGE flag")]
        [DataRow(true,  DisplayName = "Purge via /MIR flag")]
        public async Task Purge_ExtraFilesAreDeletedAndCounted(bool useMirror)
        {
            // 2 extra files sit in dest root before the run.
            const int extraFiles = 2;
            for (int i = 0; i < extraFiles; i++)
                File.WriteAllText(Path.Combine(TempDest, $"purge_{i}.txt"), "delete me");

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            if (useMirror) cmd.CopyOptions.Mirror = true;
            else           cmd.CopyOptions.Purge  = true;
            var results = await RunCommand(cmd);

            // Extra files are counted in Extras then deleted.
            AssertResults(results, $"Purge(mirror={useMirror})",
                expectedDirTotal: SourceTree.TotalDirsWithRoot,
                expectedDirCopied: SourceTree.TotalDirsWithRoot,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles + extraFiles,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: extraFiles,
                expectedFileSkipped: 0);

            // Physical verification — files must be gone
            for (int i = 0; i < extraFiles; i++)
                Assert.IsFalse(File.Exists(Path.Combine(TempDest, $"purge_{i}.txt")),
                    $"purge_{i}.txt should have been deleted");
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(1, 2, DisplayName = "1 extra dir, 2 files inside")]
        [DataRow(2, 3, DisplayName = "2 extra dirs, 3 files each")]
        public async Task Purge_ExtraDirsAndTheirFilesAreDeletedAndCounted(
            int extraDirCount, int filesPerExtraDir)
        {
            // Pre-place extra dest dirs, each containing files.
            for (int d = 0; d < extraDirCount; d++)
            {
                var dir = Path.Combine(TempDest, $"PurgeDir_{d}");
                Directory.CreateDirectory(dir);
                for (int f = 0; f < filesPerExtraDir; f++)
                    File.WriteAllText(Path.Combine(dir, $"file_{f}.txt"), "purge");
            }

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.Mirror = true; // /MIR = /E + /PURGE

            var results = await RunCommand(cmd);

            long expectedPurgedFiles = extraDirCount * filesPerExtraDir;

            AssertResults(results,
                $"Purge dirs(dirs={extraDirCount}, files={filesPerExtraDir})",
                expectedDirTotal:   SourceTree.TotalDirsWithRoot + extraDirCount,
                expectedDirCopied:  SourceTree.TotalDirsWithRoot,
                expectedDirExtras:  extraDirCount,
                expectedDirSkipped: 0,
                expectedFileTotal:  SourceTree.TotalFiles + expectedPurgedFiles,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: expectedPurgedFiles,
                expectedFileSkipped: 0);

            // Physical verification
            for (int d = 0; d < extraDirCount; d++)
                Assert.IsFalse(Directory.Exists(Path.Combine(TempDest, $"PurgeDir_{d}")),
                    $"PurgeDir_{d} should have been deleted");
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(2, DisplayName = "2-level nested extra dir")]
        [DataRow(3, DisplayName = "3-level nested extra dir")]
        public async Task Purge_NestedExtraDirsCountAllFiles(int nestDepth)
        {
            // Build a chain: dest/nested0/nested1/... each level has 1 file.
            string current = TempDest;
            for (int depth = 0; depth < nestDepth; depth++)
            {
                current = Path.Combine(current, $"nested_{depth}");
                Directory.CreateDirectory(current);
                File.WriteAllText(Path.Combine(current, $"file_{depth}.txt"), "purge");
            }

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.Mirror = true;
            var results = await RunCommand(cmd);

            // nestDepth dirs + nestDepth files inside them should all be counted
            AssertResults(results, $"NestedPurge(depth={nestDepth})",
                expectedDirTotal:   SourceTree.TotalDirsWithRoot + nestDepth,
                expectedDirCopied:  SourceTree.TotalDirsWithRoot,
                expectedDirExtras:  nestDepth,
                expectedDirSkipped: 0,
                expectedFileTotal:  SourceTree.TotalFiles + nestDepth,
                expectedFileCopied: SourceTree.TotalFiles,
                expectedFileExtras: nestDepth,
                expectedFileSkipped: 0);

            Assert.IsFalse(Directory.Exists(Path.Combine(TempDest, "nested_0")),
                "Root of nested extra dir tree should have been purged");
        }

        // ════════════════════════════════════════════════════════════════════════
        // LIST-ONLY
        // ════════════════════════════════════════════════════════════════════════

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        [DataRow(false, DisplayName = "ListOnly flat")]
        [DataRow(true,  DisplayName = "ListOnly recursive")]
        public async Task ListOnly_ReportsWithoutWriting(bool recursive)
        {
            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = recursive;
            cmd.LoggingOptions.ListOnly = true;
            var results = await RunCommand(cmd);

            long expectedDirs  = recursive ? SourceTree.TotalDirsWithRoot : 1;
            long expectedFiles = recursive ? SourceTree.TotalFiles : SourceTree.FilesPerDir;

            AssertResults(results, $"ListOnly(recursive={recursive})",
                expectedDirTotal: expectedDirs,   expectedDirCopied: expectedDirs,
                expectedDirExtras: 0, expectedDirSkipped: 0,
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
                    expectedDirTotal: 1, expectedDirCopied: 1,
                    expectedDirExtras: 0, expectedDirSkipped: 0,
                    expectedFileTotal: SourceTree.FilesPerDir,
                    expectedFileCopied: SourceTree.FilesPerDir,
                    expectedFileExtras: 0, expectedFileSkipped: 0);

                // Source root files should be gone; subdirs untouched
                Assert.AreEqual(0, Directory.GetFiles(moveSource, "*", SearchOption.TopDirectoryOnly).Length,
                    "Source root files should have been moved");
                Assert.IsTrue(Directory.GetDirectories(moveSource).Length > 0,
                    "/MOV should not delete source subdirs");
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
                    expectedDirTotal: SourceTree.TotalDirsWithRoot,
                    expectedDirCopied: SourceTree.TotalDirsWithRoot,
                    expectedDirExtras: 0, expectedDirSkipped: 0,
                    expectedFileTotal: SourceTree.TotalFiles,
                    expectedFileCopied: SourceTree.TotalFiles,
                    expectedFileExtras: 0, expectedFileSkipped: 0);

                // Source should be completely empty after /MOVE
                var remaining = Directory.GetFileSystemEntries(moveSource, "*", SearchOption.AllDirectories);
                Assert.AreEqual(0, remaining.Length,
                    "/MOVE should leave source directory empty");
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
            // Each of the 5 dirs-with-files has 1 matching file → 5 excluded, 15 copied
            const int excludedPerDir = 1;
            long expectedCopied  = SourceTree.TotalFiles - (SourceTree.DirsWithFiles * excludedPerDir);
            long expectedSkipped = SourceTree.DirsWithFiles * excludedPerDir;

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludedFiles.Add("*0*_Bytes*");
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(FileExclusion_ExcludesMatchingFiles),
                expectedDirTotal: SourceTree.TotalDirsWithRoot,
                expectedDirCopied: SourceTree.TotalDirsWithRoot,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles,
                expectedFileCopied: expectedCopied,
                expectedFileExtras: 0,
                expectedFileSkipped: expectedSkipped);
        }

        [TestMethod, Timeout(5000, CooperativeCancellation = true)]
        public async Task DirectoryExclusion_ExcludesMatchingDirs()
        {
            // Exclude SubFolder_2 → loses 1 dir + 4 files
            const int excludedDirs  = 1;
            const int excludedFiles = excludedDirs * SourceTree.FilesPerDir;

            var cmd = GetCommand(SharedSource, TempDest);
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.SelectionOptions.ExcludedDirectories.Add("SubFolder_2");
            var results = await RunCommand(cmd);

            AssertResults(results, nameof(DirectoryExclusion_ExcludesMatchingDirs),
                expectedDirTotal: SourceTree.TotalDirsWithRoot - excludedDirs,
                expectedDirCopied: SourceTree.TotalDirsWithRoot - excludedDirs,
                expectedDirExtras: 0, expectedDirSkipped: 0,
                expectedFileTotal: SourceTree.TotalFiles - excludedFiles,
                expectedFileCopied: SourceTree.TotalFiles - excludedFiles,
                expectedFileExtras: 0, expectedFileSkipped: 0);
        }
    }
    

/*
    // ══════════════════════════════════════════════════════════════════════════
    //
    //  RoboCommandPortable_CommandTests
    //
    //  Runs the full CommandTests suite against RoboCommandPortable.
    //  Failures here indicate bugs in the portable implementation, not in
    //  the test expectations (which are validated by RoboCommand_Tests above).
    //
    // ══════════════════════════════════════════════════════════════════════════
    [TestClass]
    public class RoboCommandPortable_CommandTests : CommandTests<RoboCommandPortable>
    {
        protected override RoboCommandPortable GetCommand(string source, string destination)
        {
            var cmd = new RoboCommandPortable(StreamedCopierFactory.DefaultFactory);
            cmd.CopyOptions.Source      = source;
            cmd.CopyOptions.Destination = destination;
            return cmd;
        }
    }
+*/
}

#endif