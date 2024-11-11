using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.SymbolicLinkSupport;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RoboSharp.Extensions.Windows.UnitTests
{
    // These tests must be run as administrator to pass!
    [TestClass]
    public class SymbolicLinkTests
    {
        static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RoboSharp.SymbolicLinkTesting");
        const string ERD = "\n>>\t";
        static string RandomName() => Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        [TestInitialize]
        public void Initialize_SymbolicLinkTest()
        {
            Console.WriteLine("----------------------\n THIS TEST REQUIRES ADMIN PRIVILEGES AT RUN-TIME");
            RoboSharp.UnitTests.Test_Setup.PrintEnvironment();
            Directory.CreateDirectory(Root);
        }

        [TestCleanup]
        public void Cleanup_SymbolicLinkTest()
        {
            Directory.Delete(Root, true);
        }

        [TestMethod]
        public void Test_SymbolicDirectory()
        {
            DirectoryInfo link = new DirectoryInfo(Path.Combine(Root, RandomName()));
            DirectoryInfo target = new DirectoryInfo(Path.Combine(Root, RandomName(), RandomName()));
            target.Create();
            Console.WriteLine($"Link = {link.FullName}\nTarget = {target.FullName}");

            string targetFileName = "JoeDirt.txt";
            string targetText = "Good Movie, you should watch it.";
            Console.Write($"\n- Writing to target file");
            using (var writer = File.CreateText(Path.Combine(target.FullName, targetFileName)))
            {
                writer.WriteLine(targetText);
                writer.Dispose();
            }
            Console.WriteLine(" -- Success");
            
            Console.Write("- Creating Symbolic Link");
            link.CreateAsSymbolicLink(target.FullName);
            link.Refresh();
            Assert.IsTrue(link.Exists, $"{ERD}Link was not created");
            Console.WriteLine(" -- Success");

            Console.Write("- Running Link Assertions");
            Assert.IsTrue(link.IsSymbolicLink(), $"{ERD}Failed IsSymbolicLink() -- test 1");
            Assert.IsTrue(SymbolicLink.IsSymbolicLink(link.FullName, true), $"{ERD}Failed IsSymbolicLink() -- test 2");
            Assert.AreEqual(target.FullName, link.ResolveLinkTarget(true)?.FullName, $"{ERD}Failed to resolve target path - test 1");
            Assert.AreEqual(target.FullName, SymbolicLink.GetReparseDataTarget(link.FullName, true), $"{ERD}Failed to resolve target path - test 2");
            string fpath = SymbolicLink.GetFinalPathNameByHandle(link);
            Assert.AreEqual(target.FullName, fpath, $"{ERD}GetFinalPathNameByHandle() reported unexpected result\nExpected : {target.FullName}\n  Actual : {fpath}");
            Assert.AreEqual(1, link.GetFiles().Length, $"{ERD}Incorrect amount of files detected when reading between link to target");
            Console.WriteLine(" -- Success");

            Console.Write("- Resetting Symbolic Link");
            link.Delete(); link.Refresh(); target.Refresh();
            Assert.IsFalse(Directory.Exists(link.FullName), $"{ERD}Link was not deleted.");
            Assert.IsTrue(Directory.Exists(target.FullName), $"{ERD}Target directory Deleted.");
            Console.WriteLine(" -- Success");

            // Create new relative link
            Console.WriteLine("\n- Begin testing Relative Path");
            SymbolicLink.CreateAsSymbolicLink(link.FullName, target.FullName, true, true);
            link.Refresh();
            Assert.IsTrue(link.Exists, $"{ERD}Link was not created");
            Assert.IsTrue(link.IsSymbolicLink(), $"{ERD}Not detected as symbolic link - test 1");
            Assert.IsTrue(SymbolicLink.IsSymbolicLink(link.FullName, true), $"{ERD}Not detected as symbolic link - test 2");
            fpath = SymbolicLink.GetFinalPathNameByHandle(link);
            Assert.AreEqual(target.FullName, fpath, $"{ERD}GetFinalPathNameByHandle() reported unexpected result\nExpected : {target.FullName}\n  Actual : {fpath}");

            string relTarget = SymbolicLink.GetReparseDataTarget(link.FullName, true);
            Assert.IsNotNull(relTarget, $"{ERD}Relative Link Reparse Data returned null");
            Console.WriteLine($"- Relative Target : {relTarget}");
            Assert.AreEqual(1, link.GetFiles().Length, $"{ERD}Incorrect amount of files detected when reading between link to target");
        }

        [TestMethod]
        public void Test_SymbolicFile()
        {
            FileInfo target = new FileInfo(Path.GetTempFileName());
            try
            {
                FileInfo link = new FileInfo(Path.Combine(Root, Path.GetFileName(Path.GetRandomFileName())));
                Console.WriteLine($"Link = {link.FullName}\nTarget = {target.FullName}");
                string targetText = "This is the Target File";
                Console.Write($"\n- Writing to target");
                using (var writer = target.AppendText())
                {
                    writer.WriteLine(targetText);
                    writer.Dispose();
                }
                Console.WriteLine(" -- Success");
                Console.Write("- Creating Symbolic Link");
                link.CreateAsSymbolicLink(target.FullName);
                link.Refresh();
                Assert.IsTrue(link.Exists, "Link was not created");
                Console.WriteLine(" -- Success");

                Console.Write("- Running Link Assertions");
                Assert.IsTrue(link.IsSymbolicLink(), $"{ERD}Failed IsSymbolicLink() -- test 1");
                Assert.IsTrue(SymbolicLink.IsSymbolicLink(link.FullName, false), $"{ERD}Failed IsSymbolicLink() -- test 2");
                Assert.AreEqual(target.FullName, link.ResolveLinkTarget(true)?.FullName, $"{ERD}Failed to resolve target path - test 1");
                Assert.AreEqual(target.FullName, SymbolicLink.GetReparseDataTarget(link.FullName, false), $"{ERD}Failed to resolve target path - test 2");
                string fpath = SymbolicLink.GetFinalPathNameByHandle(link);
                Assert.AreEqual(target.FullName, fpath, $"{ERD}GetFinalPathNameByHandle() reported unexpected result\nExpected : {target.FullName}\n  Actual : {fpath}");
                Console.WriteLine(" -- Success");

                Console.Write("- Reading Link");
                using (var reader = link.OpenText())
                {
                    string line = reader.ReadLine();
                    Assert.AreEqual(targetText, line, $"{ERD}Reading the Symbolic Link failed to get the text from the target");
                }
                Console.WriteLine(" -- Success");

                Console.Write("- Resetting Symbolic Link");
                link.Delete(); link.Refresh(); target.Refresh();
                Assert.IsFalse(File.Exists(link.FullName), $"{ERD}Link was not deleted.");
                Assert.IsTrue(File.Exists(target.FullName), $"{ERD}Target directory Deleted.");
                Console.WriteLine(" -- Success");

                // Create new relative link
                Console.WriteLine("\n- Begin testing Relative Path");
                SymbolicLink.CreateAsSymbolicLink(link.FullName, target.FullName, false, true);
                link.Refresh();
                Assert.IsTrue(link.Exists, $"{ERD}Link was not created");
                Assert.IsTrue(link.IsSymbolicLink(), $"{ERD}Not detected as symbolic link - test 1");
                Assert.IsTrue(SymbolicLink.IsSymbolicLink(link.FullName, true), $"{ERD}Not detected as symbolic link - test 2");

                fpath = SymbolicLink.GetFinalPathNameByHandle(link);
                Assert.AreEqual(target.FullName, fpath, $"{ERD}GetFinalPathNameByHandle() reported unexpected result\nExpected : {target.FullName}\n  Actual : {fpath}");

                string relTarget = SymbolicLink.GetReparseDataTarget(link.FullName, false);
                Assert.IsNotNull(relTarget, $"{ERD}Relative Link Reparse Data returned null");
                Console.WriteLine($"- Relative Target : {relTarget}");

                using (var reader = link.OpenText())
                {
                    Assert.AreEqual(targetText, reader.ReadLine(), $"{ERD}Failed when reading the linked file");
                }
            }
            finally
            {
                if (File.Exists(target.FullName))
                    File.Delete(target.FullName);
            }
        }

    }
}
