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
    public class RoboMoverTests : RoboSharp.UnitTests.CommandTests<RoboMover>
    {
        const LoggingFlags DefaultLoggingAction = LoggingFlags.RoboSharpDefault | LoggingFlags.NoJobHeader;

        [DataRow(true, @"C:\SomeDir")]
        [DataRow(false, @"D:\System Volume Information")]
        [TestMethod]
        public void IsAllowedDir(bool expected, string path)
        {
            Assert.AreEqual(expected, RoboMover.IsAllowedRootDirectory(new DirectoryInfo(path)));
        }

        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        [DataRow(CopyActionFlags.MoveFiles)]
        [DataRow(CopyActionFlags.MoveFiles | CopyActionFlags.Purge)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories)]
        [DataRow(CopyActionFlags.MoveFilesAndDirectories | CopyActionFlags.Purge)]
        public async Task Test_RoboMover(CopyActionFlags copyOptions)
        {
            var source= await base.PrepMoveSource();
            try
            {
                var rm = new RoboMover()
                {
                    CopyOptions =
                    {
                        Source = source,
                        Destination = TempDest,
                    },
                    LoggingOptions =
                    {
                         NoJobHeader =false,
                    },
                };
                rm.CopyOptions.ApplyActionFlags(CopyActionFlags.CopySubdirectoriesIncludingEmpty | copyOptions);
                rm.LoggingOptions.ApplyLoggingFlags(DefaultLoggingAction);
                rm.SelectionOptions.ApplySelectionFlags(SelectionFlags.Default);

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
                    Assert.AreEqual(purge, !file.Exists, purge ? "File was not purged." : "File was purged unexpectedly.");
                }
                foreach (var dir in PurgeDirectories)
                {
                    dir.Refresh();
                    Assert.AreEqual(purge, !dir.Exists, purge ? "Directory was not purged." : "Directory was purged unexpectedly.");
                }
                //evaluate moved
                foreach (var filepair in SourceFiles)
                {
                    filepair.Refresh();
                    Assert.IsTrue(filepair.IsExtra(), string.Format("\nSource:{0}\nDestination:{1}\nFile was not moved to destination directory.", filepair.Source, filepair.Destination));
                }
                bool moveDirectories = rm.CopyOptions.MoveFilesAndDirectories;
                Assert.AreEqual(moveDirectories, SourceFiles[2].Parent.IsExtra(), moveDirectories ? "Directory was not moved" : "Directory was moved unexpectedly.");
                Assert.AreEqual(moveDirectories, SourceFiles[3].Parent.IsExtra(), moveDirectories ? "Directory was not moved" : "Directory was moved unexpectedly.");
            }
            finally
            {
                try { Directory.Delete(source, true); } catch { }
            }

        }
    }
}