using Microsoft.Testing.Platform.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Extensions.Tests;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER

namespace RoboSharp.Extensions.Tests
{
    
    /// <summary>
    /// <br/> Runs the full <see cref="CommandTests{T}"/> suite against <see cref="RoboCommandPortable"/>.
    /// <br/> Failures here indicate bugs in the portable implementation, not in the test expectations (which are validated by <see cref="CommandTests"/>).
    /// </summary>
    [TestClass]
    public class RoboCommandPortable_StreamedCopier_CommandTests : RoboCommandPortable_CommandTestsBase
    {
        protected override RoboCommandPortable GetCommand() => new RoboCommandPortable(StreamedCopierFactory.DefaultFactory);
    }

#if WINDOWS || NETFRAMEWORK
    [TestClass]
    public class RoboCommandPortable_CopyFileEx_CommandTests : RoboCommandPortable_CommandTestsBase
    {
        protected override RoboCommandPortable GetCommand() => new RoboCommandPortable(new Windows.CopyFileExFactory());
    }
#endif


    public abstract class RoboCommandPortable_CommandTestsBase : CommandTests<RoboCommandPortable>
    {
        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.MoveFiles)]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.Purge)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.Purge)]
        public async Task Test_Purge_Validation(CopyActionFlags copyOptions)
        {
            var source = await base.PrepMoveSource();

            try
            {
                var rm = new RoboCommandPortable(StreamedCopierFactory.DefaultFactory)
                {
                    CopyOptions = new CopyOptions()
                    {
                        Source = source,
                        Destination = base.TempDest,
                    },
                };

                rm.CopyOptions.ApplyActionFlags(CopyActionFlags.CopySubdirectoriesIncludingEmpty | copyOptions);
                rm.SelectionOptions.ApplySelectionFlags(SelectionFlags.Default);
                rm.LoggingOptions.ApplyLoggingFlags(LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader);

                string subfolderpath = @"SubFolder_1\SubFolder_1.1\SubFolder_1.2";
                FilePair[] SourceFiles = new FilePair[] {
                new FilePair(Path.Combine(rm.CopyOptions.Source, "4_Bytes.txt"), Path.Combine(rm.CopyOptions.Destination, "4_Bytes.txt")),
                new FilePair(Path.Combine(rm.CopyOptions.Source, "1024_Bytes.txt"), Path.Combine(rm.CopyOptions.Destination, "1024_Bytes.txt")),
                new FilePair(Path.Combine(rm.CopyOptions.Source, subfolderpath, "0_Bytes.txt"), Path.Combine(rm.CopyOptions.Destination, subfolderpath, "0_Bytes.txt")),
                new FilePair(Path.Combine(rm.CopyOptions.Source, subfolderpath, "4_Bytes.htm"), Path.Combine(rm.CopyOptions.Destination, subfolderpath, "4_Bytes.htm")),
            };
                FileInfo[] purgeFiles = new FileInfo[]
                {
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFile_1.txt")),
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFile_2.txt")),
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFolder_1", "PurgeFile_3.txt")),
                new FileInfo(Path.Combine(rm.CopyOptions.Destination, "PurgeFolder_2", "SubFolder","PurgeFile_4.txt")),
                };
                DirectoryInfo[] PurgeDirectories = new DirectoryInfo[]
                {
                purgeFiles[2].Directory,
                purgeFiles[3].Directory,
                purgeFiles[3].Directory.Parent,
                };

                foreach (var dir in PurgeDirectories) Directory.CreateDirectory(dir.FullName);
                foreach (var file in purgeFiles) File.WriteAllText(file.FullName, "PURGE ME");

                await rm.Start();
                foreach (var lin in rm.GetResults().LogLines)
                    Console.WriteLine(lin);

                bool purge = rm.CopyOptions.Purge;
                // Evaluate purged
                foreach (var file in purgeFiles)
                {
                    file.Refresh();
                    Assert.AreEqual(purge, !file.Exists, purge ? "\n >> File was not purged." : "\n >> File was purged unexpectedly.");
                }
                foreach (var dir in PurgeDirectories)
                {
                    dir.Refresh();
                    Assert.AreEqual(purge, !dir.Exists, purge ? "\n >> Directory was not purged." : "\n >> Directory was purged unexpectedly.");
                }
                //evaluate moved
                foreach (var filepair in SourceFiles)
                {
                    filepair.Refresh();
                    Assert.IsTrue(filepair.Destination.Exists);
                    Assert.IsTrue(filepair.IsExtra(), string.Format("\n >> Source:{0}\nDestination:{1}\nFile was not moved to destination directory.", filepair.Source, filepair.Destination));
                }
                bool moveDirectories = rm.CopyOptions.MoveFilesAndDirectories;
                Assert.AreEqual(moveDirectories, SourceFiles[2].Parent.IsExtra(), moveDirectories ? "\n >> Directory was not moved" : "\n >> Directory was moved unexpectedly.");
                Assert.AreEqual(moveDirectories, SourceFiles[3].Parent.IsExtra(), moveDirectories ? "\n >> Directory was not moved" : "\n >> Directory was moved unexpectedly.");
            }
            finally
            {
                try { Directory.Delete(source, true); } catch { }
            }
        }
    }
}
#endif