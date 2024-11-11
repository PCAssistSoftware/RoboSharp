using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Extensions.Helpers;
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
            Test_Setup.ClearOutTestDestination();
            DirectoryInfo source = new DirectoryInfo(Test_Setup.Source_Standard);
            DirectoryInfo dest = new DirectoryInfo(Test_Setup.TestDestination);
            if (!dest.Exists) dest.Create();
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
            Test_Setup.ClearOutTestDestination();
            DirectoryInfo source = new DirectoryInfo(Test_Setup.Source_Standard);
            DirectoryInfo dest = new DirectoryInfo(Test_Setup.TestDestination);
            string sub1 = Path.Combine(dest.FullName, "Sub1", "Sub1.1");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(Path.Combine(dest.FullName, "Sub2", "Sub2.1"));
            var dp = DirectoryPair.CreatePair(source, dest);
            Assert.AreEqual(2, dp.ExtraDirectories.Count());
            Assert.IsFalse(dp.SourceDirectories.Any(d => d.Destination.FullName == sub1));
        }
    }
}
