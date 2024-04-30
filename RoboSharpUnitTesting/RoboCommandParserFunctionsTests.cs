﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass()]
    public class RoboCommandParserFunctionsTests
    {



        [TestMethod()]
        public void TrimRoboCopyTest() // null expected = no change
        {
            string Trim(string text) => RoboCommandParserFunctions.TrimRobocopy(text);

            const string nonQuoted = @"robocopy ";
            const string nonQuotedExe = @"robocopy.exe ";
            const string fullPath = @"c:\windows\system32\robocopy ";
            const string fullPathExe = @"c:\windows\system32\robocopy.exe ";
            const string quotedPath = @"""c:\windows\system32\robocopy"" ";
            const string quotedPathExe = @"""D:\Some Other\Folder\robocopy.exe"" ";

            // trim entire string
            Assert.AreEqual(string.Empty, Trim(nonQuoted), "\nFailed Trim All - Test 1");
            Assert.AreEqual(string.Empty, Trim(nonQuotedExe), "\nFailed rim All - Test 2");
            Assert.AreEqual(string.Empty, Trim(fullPath), "\nFailed trim All - Test 3");
            Assert.AreEqual(string.Empty, Trim(fullPathExe), "", "\nFailed trim All - Test 4");
            Assert.AreEqual(string.Empty, Trim(quotedPath), "\nFailed trim All - Test 5");
            Assert.AreEqual(string.Empty, Trim(quotedPathExe), "", "\nFailed trim All - Test 6");

            // test text following the path to robocopy
            string[] testArray = new string[]
            {
                "*.txt /xf",
                @"""C:\source\robocopy\"" ""D:\Dest\"" /a /b /c"
            };
            int i = 0;

            foreach (string testStr in testArray) 
            {   
                Assert.AreEqual(testStr, Trim2(nonQuoted), $"\nFailed Clear start only - Test {i} - no-quotes");
                Assert.AreEqual(testStr, Trim2(nonQuotedExe), $"\nFailed Clear start only - Test {i} - no-quotes");
                Assert.AreEqual(testStr, Trim2(fullPath), $"\nFailed Clear start only - Test {i} - full path");
                Assert.AreEqual(testStr, Trim2(fullPathExe), $"\nFailed Clear start only - Test {i} - full path");
                Assert.AreEqual(testStr, Trim2(quotedPath), $"\nFailed Clear start only - Test {i} - quoted full path");
                Assert.AreEqual(testStr, Trim2(quotedPathExe), $"\nFailed Clear start only - Test {i} - quoted full path");
                i++;

                string Trim2(string text) => Trim(string.Format("{0} {1}", text, testStr.Trim()));
            }

            // No Change Tests
            testArray = new string[]
            {
                @"""C:\source\"" ""D:\Dest\"" /a /b /c",
                @"*.pdf /a /b /c ",
            };
            i = 0;
            foreach(string nc  in testArray)
            {
                Assert.AreEqual(nc, Trim(nc), "\nFailed No Change - Test Index : " + i);
                i++;
            }
        }

        [DataRow("Test_1 /E /XD", "/PURGE ", false, "Test_1 /E /XD")]
        [DataRow("Test_1 /E /XD", "/E ", true, "Test_1 /XD")]
        [TestMethod()]
        public void ExtractFlagTest(string input, string flag, bool expectedResult, string expectedOutput)
        {
            var builder = new StringBuilder(input);
            var result = RoboCommandParserFunctions.RemoveString(builder, flag);
            Assert.AreEqual(expectedResult, result, "\n Function Result Incorrect");
            Assert.AreEqual(expectedOutput, builder.Trim().ToString(), "\n Sanitized Output Mismatch");

            bool actionPerformed = false;
            builder = new StringBuilder(input);
            result = RoboCommandParserFunctions.RemoveString(builder, flag, () => actionPerformed = true);
            Assert.AreEqual(expectedResult, result, "\n Function Result Incorrect");
            Assert.AreEqual(expectedOutput, builder.Trim().ToString(), "\n Sanitized Output Mismatch");
            Assert.AreEqual(expectedResult, actionPerformed, $"/n Function {(expectedResult ? "Not Performed" : "Performed Unexpectedly")}");
        }

        [DataRow("Test_1.txt *.pdf *.txt *_SomeFile*.jpg", "Test_1.txt", "*.pdf", "*.txt", "*_SomeFile*.jpg")]
        [TestMethod()]
        public void ParseFiltersTest(params string[] itemsWhereFirstIsInputText)
        {
            string input = itemsWhereFirstIsInputText[0];
            var expected = itemsWhereFirstIsInputText.Skip(1);
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var result = RoboCommandParserFunctions.ParseFilters(input, "{0}").ToArray();
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;

            Assert.AreEqual(expected.Count(), result.Count(), "\n Number of items differs.");
            int i = 0;
            foreach (string item in expected)
            {
                Assert.AreEqual(item, result[i], "\n Parsed Item does not match!");
                i++;
            }
        }

        [DataRow("C:\\MySource \"D:\\My Destination\" /XF", @"C:\MySource", @"D:\My Destination", "/XF", DisplayName = "Quoted Destination")]
        [DataRow("\"C:\\My Source\" D:\\MyDestination /XF", @"C:\My Source", @"D:\MyDestination", "/XF", DisplayName = "Quoted Source")]
        [DataRow("\"C:\\My Source\" \"D:\\My Destination\" /XF", @"C:\My Source", @"D:\My Destination", "/XF", DisplayName = "Quotes + Spaces")]
        [DataRow("\"C:\\MySource\" \"D:\\MyDestination\" /XF", @"C:\MySource", @"D:\MyDestination", "/XF", DisplayName = "Quotes")]
        [DataRow(@"C:\MySource D:\MyDestination /XF", @"C:\MySource", @"D:\MyDestination", "/XF", DisplayName = "No Quotes")]
        [TestMethod()]
        public void ParseSourceAndDestinationTest(string input, string expectedSource, string expectedDestination, string expectedSanitizedValue)
        {
            var result = RoboCommandParserFunctions.ParsedSourceDest.Parse(input);
            Assert.AreEqual(input, result.InputString, "\n Input Value Incorrect");
            Assert.AreEqual(expectedSource, result.Source, "\n Source Value Incorrect");
            Assert.AreEqual(expectedDestination, result.Destination, "\n Destination Value Incorrect");
            Assert.AreEqual(expectedSanitizedValue, result.SanitizedString.Trim().ToString(), "\n Sanitized Value Incorrect");
        }

        [DataRow("", "D:\\Dest", DisplayName = "Empty Source")]
        [DataRow("bad_source", "D:\\Dest", DisplayName = "Unable to Parse ( source )")]
        [DataRow("D:\\Source", "bad_dest", DisplayName = "Unable to Parse ( destination )")]
        [DataRow("8:bad_source", "D:\\Dest", DisplayName = "Unqualified Source")]
        [DataRow("D:\\Source", "", DisplayName = "Empty Destination")]
        [DataRow("D:\\Source", "/:bad_dest", DisplayName = "Unqualified Destination")]
        [DataRow("", "", false, DisplayName = "No Values - Quotes")]
        [DataRow("", "", true, false, DisplayName = "No Values- No Quotes")]
        [DataRow("//Server\\myServer$\\1", "//Server\\myServer$\\2", false, false, DisplayName = "Server Test - No Quotes")]
        [DataRow("//Server\\myServer$\\1", "//Server\\myServer$\\2", false, true, DisplayName = "Server Test - Quotes")]
        [TestMethod()]
        public void ParseSourceAndDestinationExceptionTest(string source, string destination, bool shouldThrow = true, bool wrap = true)
        {
            void runTest()
            {
                try
                {
                    string quote(string input) => wrap ? $"\"{input}\"" : input;
                    RoboCommandParserFunctions.ParsedSourceDest.Parse(string.Format("{0} {1} /PURGE", quote(source), quote(destination)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    foreach (DictionaryEntry item in ex.Data)
                        Console.WriteLine(string.Format("{0} : {1}", item.Key, item.Value.ToString()));
                    throw;
                }

            }
            if (shouldThrow)
                Assert.ThrowsException<RoboCommandParserException>(runTest);
            else
                runTest();
        }

        [DataRow("Test_1 /Data:5", "/Data:{0}", "5", "Test_1", true)]
        [TestMethod()]
        public void TryExtractParameterTest(string input, string parameter, string expectedvalue, string expectedOutput, bool expectedResult)
        {
            var builder = new StringBuilder(input);
            var result = RoboCommandParserFunctions.TryExtractParameter(builder, parameter, out string value);
            Assert.AreEqual(expectedResult, result, "/n Function Result Mismatch");
            Assert.AreEqual(expectedvalue, value, "/n Expected Value Mismatch");
            Assert.AreEqual(expectedOutput, builder.Trim().ToString(), "/n Sanitized Output Mismatch");
        }

        [DataRow(@"*.* *.pdf", @"", 2, DisplayName = "Test 1")]
        [DataRow(@" *.* *.pdf ", @"", 2, DisplayName = "Test 2")]
        [DataRow(@" *.* /PURGE ", @"/PURGE ", 1, DisplayName = "Test 3")]
        [DataRow(@"""Some File.txt"" *.* *.pdf ", @"", 3, DisplayName = "Test 4")]
        [DataRow(@"*.* ""Some File.txt"" *.pdf /s", @"/s", 3, DisplayName = "Test 5")]
        [TestMethod]
        public void FileFilterParsingTest(string input, string expectedoutput, int expectedCount)
        {
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var builder = new StringBuilder(input);
            var result = RoboCommandParserFunctions.ExtractFileFilters(builder);
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;
            Assert.AreEqual(expectedCount, result.Count(), "Did not receive expected count!");
            Assert.AreEqual(expectedoutput.Trim(), builder.Trim().ToString(), "Extracted Text does not match!");
        }

        [DataRow(@"/XF C:\someDir\someFile.pdf *some_Other-File* /XD SomeDir", @"/XD SomeDir", 2, DisplayName = "Test 1")]
        [DataRow(@"/XF some-File.?df /XD *SomeDir* /XF SomeFile.*", @"/XD *SomeDir*", 2, DisplayName = "Test 2")]
        [DataRow(@"/XF some_File.*df /COPYALL /XF ""*some Other-File*"" /XD *SomeDir* ", @"/COPYALL  /XD *SomeDir*", 2, DisplayName = "Test 3")]
        [DataRow(@"/PURGE /XF ""C:\some File.pdf"" *someOtherFile* /XD SomeDir", @"/PURGE  /XD SomeDir", 2, DisplayName = "Test 4")]
        [TestMethod]
        public void ExtractExclusionFilesTest(string input, string expectedoutput, int expectedCount)
        {
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var builder = new StringBuilder(input);
            var result = RoboCommandParserFunctions.ExtractExclusionFiles(builder);
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;
            Assert.AreEqual(expectedCount, result.Count(), "Did not receive expected count of excluded Files!");
            Assert.AreEqual(expectedoutput.Trim(), builder.Trim().ToString(), "Extracted Text does not match!");
        }

        [DataRow(@"/XD C:\someDir *someOtherDir* /XF SomeFile.*", @"/XF SomeFile.*", 2, DisplayName = "Test 1")]
        [DataRow(@"/XD C:\someDir /XD *someOtherDir* /XF SomeFile.*", @"/XF SomeFile.*", 2, DisplayName = "Test 2")]
        [DataRow(@"/XD C:\someDir /XF SomeFile.* /XD *someOtherDir* ", @"/XF SomeFile.*", 2, DisplayName = "Test 3")]
        [DataRow(@"/XD ""C:\some Dir"" *someOtherDir* /XF SomeFile.*", @"/XF SomeFile.*", 2, DisplayName = "Test 4")]
        [TestMethod]
        public void ExtractExclusionDirectoriesTest(string input, string expectedoutput, int expectedCount)
        {
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var builder = new StringBuilder(input);
            var result = RoboCommandParserFunctions.ExtractExclusionDirectories(builder);
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;
            Assert.AreEqual(expectedCount, result.Count(), "Did not receive expected count of excluded directories!");
            Assert.AreEqual(expectedoutput.Trim(), builder.Trim().ToString(), "Extracted Text does not match!");
        }
    }
}