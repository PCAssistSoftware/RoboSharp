using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.DefaultConfigurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class ProcessedFileInfoTests
    {
        private RoboSharpConfiguration configuration;
        
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            configuration ??= new RoboSharp.DefaultConfigurations.RoboSharpConfig_EN();
        }


        [TestMethod]
        [DataRow("")]
        [DataRow("50%")]
        [DataRow("100%")]
        [DataRow("Source : C:\\")]
        [DataRow("Dirs :         1         0         1         0         0         0%")]
        [DataRow("Speed :           17,890,192 Bytes/sec.")]
        public void Test_ParsingSystemMessageReturnsNull(string message)
        {
            Assert.IsNull(RoboCommand.ParseFileorDirectory(message, configuration));
        }

        [TestMethod]
        [DataRow(ProcessedDirectoryFlag.MisMatch, "*Mismatch", DisplayName = "MISMATCH Directory")]
        [DataRow(ProcessedDirectoryFlag.NewDir, "New Dir", DisplayName = "NEW Directory")]
        [DataRow(ProcessedDirectoryFlag.ExtraDir, "*EXTRA Dir", DisplayName = "EXTRA Directory")]
        [DataRow(ProcessedDirectoryFlag.ExistingDir, "Existing Dir", "", DisplayName = "EXISTING Directory")] // robocopy does not include a file class for existing dirs
        [DataRow(ProcessedDirectoryFlag.Exclusion, "named", DisplayName = "Excluded Directory")]
        public void Test_Directory(RoboSharp.ProcessedDirectoryFlag expectedType, string expectedFileClass, string? robocopyFileClass = null)
        {
            string data = string.Format("{0,-20}4\tC:\\", robocopyFileClass ?? expectedFileClass);
            var info = RoboSharp.RoboCommand.ParseFileorDirectory(data, configuration);
            Assert.AreEqual(FileClassType.NewDir, info.FileClassType);
            Assert.AreEqual(expectedFileClass, info.FileClass, ignoreCase: true);
            Assert.AreEqual(expectedType, info.GetProcessedDirectoryFlag());
            Assert.AreEqual("C:\\", info.Name);
            Assert.AreEqual(4, info.Size);           
        }

        [TestMethod]
        [DataRow(ProcessedFileFlag.ModifiedInclusion, "*Mismatch", DisplayName = "ModifiedInclusion", IgnoreMessage = "Relies on /IM flag - treated as Modified Attribute Inclusion")] 
        [DataRow(ProcessedFileFlag.TweakedInclusion, "Tweaked", DisplayName = "TweakedInclusion")]
        [DataRow(ProcessedFileFlag.SameFile, "same",  DisplayName = "SameFile")]
        [DataRow(ProcessedFileFlag.OlderFile, "older", DisplayName = "OlderFile")]
        [DataRow(ProcessedFileFlag.NewFile, "New File", DisplayName = "NewFile")]
        [DataRow(ProcessedFileFlag.NewerFile, "newer", DisplayName = "NewerFile")]
        [DataRow(ProcessedFileFlag.MisMatch, "*Mismatch",DisplayName = "MISMATCH")]
        [DataRow(ProcessedFileFlag.MinFileSizeExclusion, "small",  DisplayName = "MinFileSizeExclusion")]
        [DataRow(ProcessedFileFlag.MaxFileSizeExclusion, "large", DisplayName = "MaxFileSizeExclusion")]
        [DataRow(ProcessedFileFlag.MinAgeSizeExclusion, "too new", DisplayName = "MinAgeSizeExclusion")]
        [DataRow(ProcessedFileFlag.MaxAgeSizeExclusion, "too old", DisplayName = "MaxAgeSizeExclusion")]
        [DataRow(ProcessedFileFlag.FileExclusion, "named", DisplayName = "FileExclusion")]
        [DataRow(ProcessedFileFlag.Failed, "*Failed", DisplayName = "Failed")]
        [DataRow(ProcessedFileFlag.ExtraFile, "*EXTRA File", DisplayName = "ExtraFile")]
        [DataRow(ProcessedFileFlag.ChangedExclusion, "changed", DisplayName = "ChangedExclusion")]
        [DataRow(ProcessedFileFlag.AttribExclusion, "attrib", DisplayName = "AttribExclusion")]
        public void Test_File(RoboSharp.ProcessedFileFlag expectedType, string expectedFileClass)
        {
            string data = string.Format("{0}\t4\tTest.txt", expectedFileClass);
            var info = RoboSharp.RoboCommand.ParseFileorDirectory(data, configuration);
            Assert.AreEqual(FileClassType.File, info.FileClassType);
            Assert.AreEqual(expectedFileClass, info.FileClass, ignoreCase: true);
            Assert.AreEqual(expectedType, info.GetProcessedFileFlag());
            Assert.AreEqual("Test.txt", info.Name);
            Assert.AreEqual(4, info.Size);
        }
    }
}
