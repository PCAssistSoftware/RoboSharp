#if WINDOWS

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.SymbolicLinkSupport;
using System;
using System.IO;
using System.Security.Principal;


namespace RoboSharp.Extensions.Windows.UnitTests
{
    // These tests must be run as administrator to pass!
    [TestClass]
    public class SymbolicLinkTests
    {
        static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RoboSharp.SymbolicLinkTesting");
        static readonly string TargetRoot = Path.Combine(Root, "Targets");

        const string ERD = "\n>>\t";
        static string RandomName() => Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        public TestContext TestContext { get; set;  }

        [TestInitialize]
        public void Initialize_SymbolicLinkTest()
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("Test Requires Admin Privileges");
                return;
            }
            Console.WriteLine("----------------------\n THIS TEST REQUIRES ADMIN PRIVILEGES (OR DEVELOPER-MODE ENABLED) AT RUN-TIME ");
            RoboSharp.UnitTests.Test_Setup.PrintEnvironment(TestContext);
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(TargetRoot);
            SymbolicLink.ALLOW_UNPRIVILEGED_CREATE = true;
        }


        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        [TestCleanup]
        public void Cleanup_SymbolicLinkTest()
        {
            if (!IsAdministrator())
                return;
            Directory.Delete(Root, true);
        }

        [TestMethod]
        public void Test_SymbolicDirectory()
        {
            if (!IsAdministrator()) 
                return;

            DirectoryInfo target = new DirectoryInfo(Path.Combine(TargetRoot, RandomName(), RandomName()));
            DirectoryInfo link = new DirectoryInfo(Path.Combine(Root, RandomName()));

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
            SymbolicLink.CreateAsSymbolicLink(link, target.FullName);
            //link.CreateAsSymbolicLink(target.FullName);
            link.Refresh();
            Assert.IsTrue(link.Exists, $"{ERD}Link was not created");
            Console.WriteLine(" -- Success");

            Console.Write("- Running Link Assertions");
            Assert.IsTrue(SymbolicLink.IsSymbolicLink(link), $"{ERD}Failed IsSymbolicLink() -- test 1");
            Assert.IsTrue(SymbolicLink.IsSymbolicLink(link.FullName, true), $"{ERD}Failed IsSymbolicLink() -- test 2");
            Assert.AreEqual(target.FullName, SymbolicLink.ResolveLinkTarget(link, true)?.FullName, $"{ERD}Failed to resolve target path - test 1");
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
            Assert.IsTrue(SymbolicLink.IsSymbolicLink(link), $"{ERD}Not detected as symbolic link - test 1");
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
            if (!IsAdministrator())
                return;

            FileInfo target = new FileInfo(Path.Combine(TargetRoot, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".txt"));
            FileInfo link = new FileInfo(Path.Combine(Root, target.Name));
            try
            {

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
                SymbolicLink.CreateAsSymbolicLink(link, target.FullName);
                link.Refresh();
                Assert.IsTrue(link.Exists, "Link was not created");
                Console.WriteLine(" -- Success");

                Console.Write("- Running Link Assertions");
                Assert.IsTrue(SymbolicLink.IsSymbolicLink(link), $"{ERD}Failed IsSymbolicLink() -- test 1");
                Assert.IsTrue(SymbolicLink.IsSymbolicLink(link.FullName, false), $"{ERD}Failed IsSymbolicLink() -- test 2");
                Assert.AreEqual(target.FullName, SymbolicLink.ResolveLinkTarget(link, true)?.FullName, $"{ERD}Failed to resolve target path - test 1");
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

                if (File.Exists(link.FullName))
                    File.Delete(link.FullName);
            }
        }

    }
}

#endif