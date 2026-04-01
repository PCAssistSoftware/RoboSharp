using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RoboSharp.Extensions.Tests
{
    [TestClass]
    public class DirectoryPairTests
    {
        public TestContext TestContext { get; set; }
        private string Destination { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            Destination = Test_Setup.GetNewTempPath();
        }

        [TestCleanup]
        public void Cleanup() => Test_Setup.ClearOutTestDestination(Destination);

        [DataRow(true, @"C:\")]
        [DataRow(false, @"C:\MyDocuments")]
        [DataRow(true, @"\\someServerShare\MyDrive$\")]
        [DataRow(false, @"\\someServerShare\MyDrive$\SomeFolder")]
        [TestMethod]
        public void Test_IsRootSource(bool expected, string path)
        {
            var dir = new DirectoryInfo(path);
            Assert.AreEqual(expected, new DirectoryPair(dir, dir).IsRootSource());
        }

        [DataRow(true, @"C:\")]
        [DataRow(false, @"C:\MyDocuments")]
        [DataRow(true, @"\\someServerShare\MyDrive$\")]
        [DataRow(false, @"\\someServerShare\MyDrive$\SomeFolder")]
        [TestMethod]
        public void Test_IsRootDestination(bool expected, string path)
        {
            var dir = new DirectoryInfo(path);
            Assert.AreEqual(expected, new DirectoryPair(dir, dir).IsRootDestination());
        }

        [DataRow(true, @"C:\")]
        [DataRow(false, @"C:\MyDocuments")]
        [DataRow(true, @"\\someServerShare\MyDrive$\")]
        [DataRow(false, @"\\someServerShare\MyDrive$\SomeFolder")]
        [TestMethod]
        public void Test_IsRootDIr(bool expected, string path)
        {
            Assert.AreEqual(expected, new DirectoryInfo(path).IsRootDir());
        }

        [TestMethod]
        public void Test_ExtraFiles()
        {
            DirectoryInfo source = new DirectoryInfo(Test_Setup.Source_Standard);
            DirectoryInfo dest = Directory.CreateDirectory(Destination);
            
            dest.Refresh();
            string f1 = Path.Combine(dest.FullName, "TestFile.txt");
            File.WriteAllText(f1, "MyText");
            File.WriteAllText(Path.Combine(dest.FullName, "TestFile2.txt"), "MyText");
            var dp = DirectoryPair.CreatePair(source, dest);
            Assert.AreEqual(2, dp.DestinationFiles.Count());
            Assert.IsFalse(dp.SourceFiles.Any(d => d.Destination.FullName == f1));
        }

        [TestMethod]
        public void Test_ExtraDirectories()
        {
            DirectoryInfo source = new DirectoryInfo(Test_Setup.Source_Standard);
            DirectoryInfo dest = Directory.CreateDirectory(Destination);
            string sub1 = Path.Combine(dest.FullName, "Sub1", "Sub1.1");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(Path.Combine(dest.FullName, "Sub2", "Sub2.1"));
            var dp = DirectoryPair.CreatePair(source, dest);
            Assert.AreEqual(2, dp.ExtraDirectories.Count());
            Assert.IsFalse(dp.SourceDirectories.Any(d => d.Destination.FullName == sub1));
        }

        [TestMethod]
        public void Test_IsMismatch()
        {
            var dir = Test_Setup.GetNewTempPath();
            var file = Test_Setup.GetNewTempPath();
            try
            {

                File.WriteAllText(file, "test");
                var fInfo = new FileInfo(file);
                var fInfo2 = new FileInfo(file);
                var dInfo = Directory.CreateDirectory(dir);
                var dInfo2 = Directory.CreateDirectory(dir);

                // is mismatch because one if a file and other is a directory
                Assert.IsTrue(IDirectoryPairExtensions.IsMismatch(dInfo, fInfo));
                Assert.IsTrue(IDirectoryPairExtensions.IsMismatch(fInfo, dInfo));

                // is not mismatch because other path does not exist
                string notExist = Test_Setup.GetNewTempPath();
                Assert.IsFalse(IDirectoryPairExtensions.IsMismatch(fInfo, new FileInfo(notExist)));
                Assert.IsFalse(IDirectoryPairExtensions.IsMismatch(fInfo, new DirectoryInfo(notExist)));
                Assert.IsFalse(IDirectoryPairExtensions.IsMismatch(dInfo, new FileInfo(notExist)));
                Assert.IsFalse(IDirectoryPairExtensions.IsMismatch(dInfo, new DirectoryInfo(notExist)));

                // is not mismatch because both are of same type
                Assert.IsFalse(IDirectoryPairExtensions.IsMismatch(fInfo, fInfo2));
                Assert.IsFalse(IDirectoryPairExtensions.IsMismatch(dInfo, dInfo2));
            }
            finally
            {
                File.Delete(file);
                Directory.Delete(dir);
            }
        }
    }
}
