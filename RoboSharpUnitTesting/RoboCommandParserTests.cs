using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class RoboCommandParserTests
    {
        const string CmdEndText = @" /R:0 /BYTES";

        public static void DebuggerWriteLine(object sender, Debugger.DebugMessageArgs args) => Console.WriteLine("--- " + args.Message);

        
        private static RoboCommand GetNewCommand(string source = default, string destination =  default) => new RoboCommand(
            source: source.IsNullOrWhiteSpace() ?  "C:\\Source" : source,
            destination: destination.IsNullOrWhiteSpace() ? "D:\\Destination" : destination
            );

        private static string PrintCmd(IRoboCommand command ) => command.CopyOptions.Source.IsNullOrWhiteSpace() ?  $"\"\" \"\" {command}" : command.ToString();

        /// <summary>
        /// Use this one when debugging specific commands that are not deciphering for you!
        /// </summary>
        [DataRow("robocopy.exe \"C:\\MySource\" \"D:\\My Destination\" \"*.txt\" \"*.pdf\"", DisplayName = "quoted filters")]
        [DataRow("\"D:\\Some Folder\\robocopy.exe\" \"C:\\MySource\" \"D:\\My Destination\" *.txt *.pdf", DisplayName = "multiple unquoted filters")]
        [DataRow("c:\\windows\\system32\\robocopy.exe \"C:\\MySource\" \"D:\\My Destination\" *.txt", DisplayName = ".txt Only")]
        [DataRow("robocopy \"C:\\MySource\" \"D:\\My Destination\" /MOVE", DisplayName = "Example")]
        [TestMethod]
        public void PrintOnly(string command)
        {
            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmd = RoboCommandParser.Parse(command);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;
            Console.WriteLine($"\n\n Input : {command}");
            Console.WriteLine($"Output : {cmd}");
        }

        
        [DataRow(@"C:\", @"C:\")]
        [DataRow(@"""C:\Sou rce""", @"C:\Sou rce")]
        [DataRow(@"//Server/Source", @"//Server/Source")]
        [DataRow(@"""//back Tick\safe/""", @"//back Tick\safe/")]
        [DataRow(@"""//back Tick\unsafe\""", @"//back Tick\unsafe\")]  // this input causes an escapement issue, resulting in bad parameter for robocopy, so last separator must be sanitized
        [TestMethod]
        public void Test_CleanDirectoryPath(string input, string output)
        {
            string result = StringBuilderExtensions.CleanDirectoryPath(input);
            Assert.AreEqual(output, result, string.Format("\n\n    Input : {0}\n   Output : {1}\n Expected : {2}", input, result, output));
        }

        [DataRow(@"*.* *.pdf", @"", 2, DisplayName = "Test 1")]
        [DataRow(@" *.* *.pdf ", @"", 2, DisplayName = "Test 2")]
        [DataRow(@" *.* /PURGE ", @"/PURGE ", 1, DisplayName = "Test 3")]
        [DataRow(@"""Some File.txt"" *.* *.pdf ", @"", 3, DisplayName = "Test 4")]
        [DataRow(@"*.* ""Some File.txt"" *.pdf /s", @"/s", 3, DisplayName = "Test 5")]
        [TestMethod]
        public void Test_ExtractFileFilters(string input, string expectedoutput, int expectedCount)
        {
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var builder = new StringBuilder(input);
            var result = RoboCommandParser.ExtractFileFilters(builder);
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;
            Assert.AreEqual(expectedCount, result.Count(), "Did not receive expected count!");
            Assert.AreEqual(expectedoutput.Trim(), builder.Trim().ToString(), "Extracted Text does not match!");
        }

        [DataRow(@"/XF C:\someDir\someFile.pdf *some_Other-File* /XD SomeDir", @"/XD SomeDir", 2, DisplayName = "Test 1")]
        [DataRow(@"/XF some-File.?df /XD *SomeDir* /XF SomeFile.*", @"/XD *SomeDir*", 2, DisplayName = "Test 2")]
        [DataRow(@"/XF some_File.*df /COPYALL /XF ""*some Other-File*"" /XD *SomeDir* ", @"/COPYALL  /XD *SomeDir*", 2, DisplayName = "Test 3")]
        [DataRow(@"/PURGE /XF ""C:\some File.pdf"" *someOtherFile* /XD SomeDir", @"/PURGE  /XD SomeDir", 2, DisplayName = "Test 4")]
        [TestMethod]
        public void Test_ExtractExclusionFiles(string input, string expectedoutput, int expectedCount)
        {
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var builder = new StringBuilder(input);
            var result = RoboCommandParser.ExtractExclusionFiles(builder);
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;
            Assert.AreEqual(expectedCount, result.Count(), "Did not receive expected count of excluded Files!");
            Assert.AreEqual(expectedoutput.Trim(), builder.Trim().ToString(), "Extracted Text does not match!");
        }

        [DataRow(@"/XD C:\someDir *someOtherDir* /XF SomeFile.*", @"/XF SomeFile.*", 2, DisplayName = "Test 1")]
        [DataRow(@"/XD C:\someDir /XD *someOtherDir* /XF SomeFile.*", @"/XF SomeFile.*", 2, DisplayName = "Test 2")]
        [DataRow(@"/XD C:\someDir /XF SomeFile.* /XD *someOtherDir* ", @"/XF SomeFile.*", 2, DisplayName = "Test 3")]
        [DataRow(@"/XD ""C:\some Dir"" *someOtherDir* /XF SomeFile.*", @"/XF SomeFile.*", 2, DisplayName = "Test 4")]
        [TestMethod]
        public void Test_ExtractExclusionDirectories(string input, string expectedoutput, int expectedCount)
        {
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var builder = new StringBuilder(input);
            var result = RoboCommandParser.ExtractExclusionDirectories(builder);
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;
            Assert.AreEqual(expectedCount, result.Count(), "Did not receive expected count of excluded directories!");
            Assert.AreEqual(expectedoutput.Trim(), builder.Trim().ToString(), "Extracted Text does not match!");
        }

        [DataRow("ExcludedTestFile1.txt", "ExcludedFile2.pdf", "\"*wild card*\"", DisplayName = "Multiple Filters")]
        [DataRow("\"C:\\Some Folder\\Excluded.txt\"", DisplayName = "Quoted Filter")]
        [DataRow("C:\\Excluded.txt", DisplayName = "UnQuoted Filters")]
        [DataRow(DisplayName = "No Filter Specified")]
        [TestMethod]
        public void Test_ExcludedFiles(params string[] filters)
        {
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.ExcludedFiles.AddRange(filters);
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));

            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            Console.WriteLine($"\n\n Input : {cmd}");
            Console.WriteLine($"Output : {cmdResult}");
        }

        [DataRow(@""" "" "" ""/XF c:\MyFile.txt /COPYALL /XF ""d:\File 2.pdf""", @"/COPYALL /XF c:\MyFile.txt ""d:\File 2.pdf""", DisplayName = "Multiple /XF flags with Quotes")]
        [DataRow(@"robocopy """" """"  /XF c:\MyFile.txt /COPYALL /XF d:\File2.pdf", @"/COPYALL /XF c:\MyFile.txt d:\File2.pdf", DisplayName = "Multiple /XF flags")]
        [DataRow(@"robocopy """" """"  /XF c:\MyFile.txt d:\File2.pdf", @"/XF c:\MyFile.txt d:\File2.pdf", DisplayName = "Single XF Flag with multiple Filters")]
        [TestMethod]
        public void Test_ExcludedFilesRaw(string input, string expected)
        {
            IRoboCommand cmdResult = RoboCommandParser.ParseOptions(input);

            expected += CmdEndText;

            Console.WriteLine($"\n\n    Input : {input}");
            Console.WriteLine($" Expected : {expected}");
            Console.WriteLine($"   Output : {cmdResult}");

            Assert.AreEqual(expected.Trim(), cmdResult.ToString().Trim(), "Command not expected result."); // Final test : both should produce the same ToString()

        }


        [DataRow("D:\\Excluded Dir\\", DisplayName = "Single Exclusion - Spaced")]
        [DataRow("D:\\Excluded\\Dir\\", DisplayName = "Single Exclusion - No Spaces")]
        [DataRow("C:\\Windows\\System32", "D:\\Excluded\\Dir\\", DisplayName = " Multiple Exclusions - No Spaces")]
        [DataRow("C:\\Windows\\System32", "D:\\Excluded Dir\\", DisplayName = " Multiple Exclusions - Spaced")]
        [DataRow(DisplayName = "No Filter Specified")]
        [TestMethod]
        public void Test_ExcludedDirectories(params string[] filters)
        {
            // Note : Handle instances of /XD multiple times https://superuser.com/questions/482112/using-robocopy-and-excluding-multiple-directories
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.ExcludedDirectories.AddRange(filters);
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));

            Console.WriteLine($"\n\n Input : {PrintCmd(cmd)}");
            Console.WriteLine($"Expected : {cmd}");
            Console.WriteLine($"Output : {cmdResult}");

            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("*.pdf")]
        [DataRow("*.pdf", "*.txt", "*.jpg")]
        [DataRow("*.*")]
        [DataRow(DisplayName = "No Filter Specified")]
        [TestMethod]
        public void Test_FileFilter(params string[] filters)
        {
            IRoboCommand cmd = GetNewCommand();
            cmd.CopyOptions.AddFileFilter(filters);
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));

            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            Console.WriteLine($"\n\n Input : {cmd}");
            Console.WriteLine($"Output : {cmdResult}");
        }


        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\"")]
        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\" /MOVE")]
        [TestMethod]
        public void Test_FileFilterRaw(string input)
        {
            // Note : Due to how RoboCommand prints out file filters, ensure input file filters are always quoted
            IRoboCommand cmdResult = RoboCommandParser.Parse(input);
            cmdResult.LoggingOptions.PrintSizesAsBytes = false;
            string expected = input + " /R:0"; // robocommand ALWAYS prints these values

            Assert.AreEqual(expected.Trim(), cmdResult.ToString().Trim(), $"\n\nProduced Command is not equal!\nExpected:\t{expected}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
            Console.WriteLine($"\n\n Input : {input}");
            Console.WriteLine($"Output : {cmdResult}");
        }


        [TestMethod]
        public void Test_FileSize()
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.MinFileSize = 1234567890;
            cmd.SelectionOptions.MaxFileSize= 0987654321;
            string text = PrintCmd(cmd);

            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;
            
            Assert.AreEqual(cmd.SelectionOptions.MinFileSize, cmdResult.SelectionOptions.MinFileSize, "\n\nMinFileSize does not match!");
            Assert.AreEqual(cmd.SelectionOptions.MaxFileSize, cmdResult.SelectionOptions.MaxFileSize, "\n\nMaxFileSize does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("19941012", "20220910")]
        [DataRow("5", "25")]
        [TestMethod]
        public void Test_FileAge(string min, string max)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.MinFileAge= min;
            cmd.SelectionOptions.MaxFileAge = max;
            string text = PrintCmd(cmd);

            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;

            Assert.AreEqual(cmd.SelectionOptions.MinFileAge, cmdResult.SelectionOptions.MinFileAge, "\n\nMinFileAge does not match!");
            Assert.AreEqual(cmd.SelectionOptions.MaxFileAge, cmdResult.SelectionOptions.MaxFileAge, "\n\nMaxFileAge does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("19941012", "20220910")]
        [DataRow("5", "20201219")]
        [TestMethod]
        public void Test_FileLastAccessDate(string min, string max)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.SelectionOptions.MinLastAccessDate = min;
            cmd.SelectionOptions.MaxLastAccessDate = max;
            string text = PrintCmd(cmd);

            Debugger.Instance.DebugMessageEvent += DebuggerWriteLine;
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Debugger.Instance.DebugMessageEvent -= DebuggerWriteLine;

            Assert.AreEqual(cmd.SelectionOptions.MinLastAccessDate, cmdResult.SelectionOptions.MinLastAccessDate, "\n\nMinLastAccessDate does not match!");
            Assert.AreEqual(cmd.SelectionOptions.MaxLastAccessDate, cmdResult.SelectionOptions.MaxLastAccessDate, "\n\nMaxLastAccessDate does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow("\"C:\\Some Folder\\MyLogFile.txt\"")]
        [DataRow("C:\\MyLogFile.txt")]
        [TestMethod]
        public void Test_Logging(string path)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            IRoboCommand cmd = GetNewCommand();
            cmd.LoggingOptions.LogPath = path;
            cmd.LoggingOptions.AppendLogPath = path;
            cmd.LoggingOptions.AppendUnicodeLogPath = path;
            cmd.LoggingOptions.UnicodeLogPath = path;
            IRoboCommand cmdResult = RoboCommandParser.Parse(PrintCmd(cmd));

            // the source paths are trimmed here because they are functionally identical, but the wrapping is removed during the parsing and sanitization process during path qualification. End result command should be the same though.
            string trimmedPath = path.Trim('\"');
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.LogPath, "\n\nLogPath does not match!");
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.UnicodeLogPath, "\n\nUnicodeLogPath does not match!");
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.AppendLogPath, "\n\nAppendLogPath does not match!");
            Assert.AreEqual(trimmedPath, cmdResult.LoggingOptions.AppendUnicodeLogPath, "\n\nAppendUnicodeLogPath does not match!");
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}"); // Final test : both should produce the same ToString()
        }

        [DataRow((SelectionFlags)4097, (CopyActionFlags)255, (LoggingFlags)65535, "C:\\SomeSourcePath\\My Source Folder", "D:\\SomeDestination\\My Dest Folder", DisplayName = "Test_All_Flags")]
        [DataRow(SelectionFlags.Default, CopyActionFlags.Default, LoggingFlags.None, "C:\\SomeSourcePath\\", "D:\\SomeDestination\\", DisplayName = "Test_Defaults")]
        [TestMethod]
        public void Test_OptionFlags(SelectionFlags selectionFlags, CopyActionFlags copyFlags, LoggingFlags loggingFlags, string source, string destination)
        {
            // Transform the selection flags to a robocommand, generate the command, parse it, then test that both have the same flags. 
            // ( What the library generates should be able to be reparsed back into the library )
            RoboCommand cmd = new RoboCommand(source, destination, copyFlags, selectionFlags, loggingFlags);
            string text = PrintCmd(cmd);
            IRoboCommand cmdResult = RoboCommandParser.Parse(text);
            Assert.AreEqual(cmd.CopyOptions.Source, cmdResult.CopyOptions.Source, "\nCopyOptions.Source is not equal!");
            Assert.AreEqual(cmd.CopyOptions.Destination, cmdResult.CopyOptions.Destination, "\nCopyOptions.Destination is not equal!");
            Assert.AreEqual(cmd.CopyOptions.GetCopyActionFlags(), cmdResult.CopyOptions.GetCopyActionFlags(), $"\n\nCopy Flags are not the same!\n\nExpected:{cmd.CopyOptions.GetCopyActionFlags()}\nResult:{cmdResult.CopyOptions.GetCopyActionFlags()}");
            Assert.AreEqual(cmd.SelectionOptions.GetSelectionFlags(), cmdResult.SelectionOptions.GetSelectionFlags(), $"\n\nSelection Flags are not the same!\n\nExpected:{cmd.SelectionOptions.GetSelectionFlags()}\nResult:{cmdResult.SelectionOptions.GetSelectionFlags()}");
            Assert.AreEqual(cmd.LoggingOptions.GetLoggingActionFlags(), cmdResult.LoggingOptions.GetLoggingActionFlags(), $"\n\nLogging Flags are not the same!\n\nExpected:{cmd.LoggingOptions.GetLoggingActionFlags()}\nResult:{cmdResult.LoggingOptions.GetLoggingActionFlags()}");

            // Final test : both should produce the same ToString()
            Assert.AreEqual(cmd.ToString(), cmdResult.ToString(), $"\n\nProduced Command is not equal!\nExpected:\t{cmd}\n  Result:\t{cmdResult}");
        }

        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\"" + CmdEndText)]
        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\" /MOVE" + CmdEndText)]
        [DataRow("C:\\MySource \"D:\\My Destination\" \"*.txt\" /MOVE /R:10 /BYTES")]
        [TestMethod]
        public void Test_Parse(string expectedOutput)
        {
            var result = RoboCommandParser.Parse(expectedOutput);
            
            Console.WriteLine($"Source Command : {expectedOutput}");
            Console.WriteLine($"Parsed Options : {result.ToString().PadLeft(expectedOutput.Length)}");
            Assert.AreEqual(expectedOutput.Trim(), result.ToString().Trim(), string.Format("\n\nExpected : {0}\nActual   : {1}", expectedOutput, result));
        }

        [TestMethod]
        public void Test_ParseFilters()
        {
            var expected = new string[] { "Test_1.txt", "*.pdf", "*.txt", "*_SomeFile*.jpg", @"""My File""" };
            string input = string.Join(" ", expected);
            Debugger.Instance.DebugMessageEvent += RoboCommandParserTests.DebuggerWriteLine;
            var result = RoboCommandParser.ParseFilters(input, "{0}").ToArray();
            Debugger.Instance.DebugMessageEvent -= RoboCommandParserTests.DebuggerWriteLine;

            Assert.AreEqual(expected.Length, result.Length, "\n Number of items differs.");
            int i = 0;
            foreach (string item in expected)
            {
                Assert.AreEqual(item, result[i], "\n Parsed Item does not match!");
                i++;
            }
        }

        

        [DataRow("", "")]
        [DataRow("", "\"//DestOnly\"")]
        [DataRow("//SourceOnly", "")]
        [DataRow("C:\\Source", "//SomeServer")]
        [DataRow("C:/Source/", "//SomeServer\\Drv$")]
        [DataRow("C:\\Source/", "//SomeServer/Drv\\")]
        [TestMethod]
        public void Test_ParseOptions(string source, string destination)
        {
            // Unit test for parsing Options Only - Source/Destination are ignored!

            var command = GetNewCommand();
            var copyOptions = command.CopyOptions;
            copyOptions.Source = source;
            copyOptions.Destination = destination;
            copyOptions.ApplyActionFlags(CopyActionFlags.CopySubdirectoriesIncludingEmpty | CopyActionFlags.Purge | CopyActionFlags.MoveFilesAndDirectories);
            command.SelectionOptions.ApplySelectionFlags(SelectionFlags.ExcludeExtra | SelectionFlags.ExcludeJunctionPointsForDirectories);
            command.LoggingOptions.ApplyLoggingFlags(LoggingFlags.IncludeFullPathNames);

            command.LoggingOptions.LogPath = "X:/MyLogPath";
            command.CopyOptions.AddFileFilter("*.Filter1", ".Filter2");
            command.SelectionOptions.AddFileExclusion("*.Exclusion1", ".Exclusion2");
            command.SelectionOptions.AddDirectoryExclusion(@"X:\SomeDirExclusion", "SomeOtherDir");

            string commandText = command.ToString();
            var parsedCommand = RoboCommandParser.ParseOptions(commandText);

            Console.WriteLine($"Source Command : {commandText}");
            Console.WriteLine($"Parsed Options : {parsedCommand.ToString().PadLeft(commandText.Length)}");

            Assert.IsTrue(string.IsNullOrWhiteSpace(parsedCommand.CopyOptions.Source), "\n -> Source was carried over");
            Assert.IsTrue(string.IsNullOrWhiteSpace(parsedCommand.CopyOptions.Destination), "\n -> Destination was carried over");

            string expected = commandText.Substring(copyOptions.Source.Length + copyOptions.Destination.Length + 1).Trim();
            string actual = parsedCommand.ToString().Trim();
            Assert.AreEqual(expected, actual, string.Format("\n\nExpected : {0}\nActual   : {1}", expected, actual));
        }

        [DataRow(@"C:\")]
        [DataRow(@"C:\SomeFolder")]
        [DataRow(@"C:/SomeFolder/")]
        [DataRow(@"//Server/Drive$/Folder")]
        [DataRow(@"\\Server\Drive$\Folder")]
        [DataRow(@"//Server/Folder")]
        [DataRow(@"\\Server\Folder")]
        [TestMethod]
        public void Test_RegexPathDetection(string input)
        {
            const RegexOptions options = RoboCommandParser.ParsedSourceDest.regexOptions;
            const string unc = RoboCommandParser.ParsedSourceDest.uncRegex;
            const string loc = RoboCommandParser.ParsedSourceDest.localRegex;

            Assert.IsTrue(Regex.IsMatch(input, unc, options) | Regex.IsMatch(input, loc, options));
            Assert.AreNotEqual(Regex.IsMatch(input, unc, options), Regex.IsMatch(input, loc, options));
            // Test Quotes
            string quotedInput = $"\"{input}\"";
            Assert.IsTrue(Regex.IsMatch(quotedInput, unc, options) | Regex.IsMatch(quotedInput, loc, options));
        }

        [DataRow("C:\\source", "D:\\destination", DisplayName = "No Quotes")]
        [DataRow("C:\\source", "\"D:\\destination\"", DisplayName = "Destination Quotes")]
        [DataRow("\"C:\\source\"", "D:\\destination", DisplayName = "Source Quoted")]
        [DataRow("\"C:\\source\"", "\"D:\\destination\"", DisplayName = "Both Quoted")]
        [DataRow("\"C:\\source dir\"", "\"D:\\destination dir\"", DisplayName = "Both Quoted and Spaced")]
        [DataRow("\"C:\\source dir\"", "//SomeServer/TestDest", DisplayName = "Local + UNC - 1")]
        [DataRow("\"C:\\source dir\"", "//SomeServer/TestDest/", DisplayName = "Local + UNC - 2")]
        [DataRow("\"C:\\source dir\"", "//SomeServer/TestDest$/Test", DisplayName = "Local + UNC - 3")]
        [DataRow("\"C:\\source dir\"", "//SomeServer/TestDest$/Test/", DisplayName = "Local + UNC - 4")]
        [DataRow("//SomeServer/TestSource", "C:\\Dest dir\\", DisplayName = "UNC + Local - 1")]
        [DataRow("//SomeServer\\TestSource\\", "C:\\Dest dir\\", DisplayName = "UNC + Local - 2")]
        [DataRow("//SomeServer/Test$\\Source", "C:\\Dest dir\\", DisplayName = "UNC + Local - 3")]
        [DataRow(@"\\SomeServer\Test\Source/", "C:\\Dest dir\\", DisplayName = "UNC + Local - 4")]
        [TestMethod]
        public void Test_ParseSourceAndDestination(string source, string dest)
        {
            string commandOptions = $"\"D:\\File.jpg\" *.pdf /copyall";
            source = source.WrapPath().ToString();
            dest = dest.WrapPath().ToString();
            string command = $"{source} {dest} {commandOptions}"; // WrapPath ensures valid text for raw input for parsing

            var result = RoboCommandParser.ParsedSourceDest.Parse(command);
            Console.WriteLine(string.Format("Source\n- In : {0}\n- Out: {1}", source, result.Source));
            Console.WriteLine(string.Format("Dest\n- In : {0}\n- Out: {1}", dest, result.Destination));
            Assert.AreEqual(command, result.InputString, "\n Input Value Incorrect");
            Assert.AreEqual(source.UnwrapQuotes(), result.Source, "\n\nSource is not expected value");
            Assert.AreEqual(dest.UnwrapQuotes(), result.Destination, "\n\nDestination is not expected value");
            Assert.AreEqual(commandOptions, result.SanitizedString.Trim().ToString(), "\n Sanitized Value Incorrect");
        }

        [DataRow("", "D:\\Dest", DisplayName = "Empty Source")]
        [DataRow("bad_source", "D:\\Dest", DisplayName = "Unable to Parse ( source )")]
        [DataRow("D:\\Source", "bad_dest", DisplayName = "Unable to Parse ( destination )")]
        [DataRow("8:bad_source", "D:\\Dest", DisplayName = "Unqualified Source")]
        [DataRow("D:\\Source", "", DisplayName = "Empty Destination")]
        [DataRow("D:\\Source", "/:bad_dest", DisplayName = "Unqualified Destination")]
        [DataRow("", "", true, true, DisplayName = "No Values - Quotes")]
        [DataRow("", "", true, false, DisplayName = "No Values- No Quotes")]
        [DataRow("//Server\\myServer$\\1", "//Server\\myServer$\\2", false, false, DisplayName = "Server Test - No Quotes")]
        [DataRow("//Server\\myServer$\\1", "//Server\\myServer$\\2", false, true, DisplayName = "Server Test - Quotes")]
        [TestMethod]
        public void Test_ParseSourceAndDestinationException(string source, string destination, bool shouldThrow = true, bool wrap = true)
        {
            if (shouldThrow)
                Assert.Throws<RoboCommandParserException>(runTest);
            else
                runTest();

            void runTest()
            {
                try
                {
                    string quote(string input) => wrap ? $"\"{input}\"" : input;
                    RoboCommandParser.ParsedSourceDest.Parse(string.Format("{0} {1} /PURGE", quote(source), quote(destination)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    foreach (DictionaryEntry item in ex.Data)
                        Console.WriteLine(string.Format("{0} : {1}", item.Key, item.Value.ToString()));
                    throw;
                }

            }
        }

        [TestMethod]
        public void Test_TrimRoboCopy() // null expected = no change
        {
            string Trim(string text) => RoboCommandParser.TrimRobocopy(text);

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
            foreach (string nc in testArray)
            {
                Assert.AreEqual(nc, Trim(nc), "\nFailed No Change - Test Index : " + i);
                i++;
            }
        }

        [DataRow("Test_1 /Data:5", "/Data:{0}", "5", "Test_1", true)]
        [TestMethod]
        public void Test_TryExtractParameter(string input, string parameter, string expectedvalue, string expectedOutput, bool expectedResult)
        {
            var builder = new StringBuilder(input);
            var result = RoboCommandParser.TryExtractParameter(builder, parameter, out string value);
            Assert.AreEqual(expectedResult, result, "/n Function Result Mismatch");
            Assert.AreEqual(expectedvalue, value, "/n Expected Value Mismatch");
            Assert.AreEqual(expectedOutput, builder.Trim().ToString(), "/n Sanitized Output Mismatch");
        }
    }
}