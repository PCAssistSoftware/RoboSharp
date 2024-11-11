using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using TestSetup = RoboSharp.UnitTests.Test_Setup;

namespace RoboSharp.Extensions.Tests
{
    public static class TestPrep
    {
        public static string SourceDirPath => RoboSharp.UnitTests.Test_Setup.Source_Standard;
        public static string DestDirPath => RoboSharp.UnitTests.Test_Setup.TestDestination;

        public static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RoboSharpUnitTesting");

        static TestPrep()
        {
            Directory.CreateDirectory(AppDataFolder);
        }

        /// <inheritdoc cref="TestSetup.ClearOutTestDestination"/>
        public static void CleanDestination() => TestSetup.ClearOutTestDestination();

        /// <inheritdoc cref="Test_Setup.GenerateCommand(bool, bool)"/>
        public static RoboCommand GetRoboCommand(bool useLargerFileSet, CopyActionFlags copyActionFlags, SelectionFlags selectionFlags, LoggingFlags loggingAction)
        {
            var cmd = TestSetup.GenerateCommand(useLargerFileSet, false);
            cmd.CopyOptions.ApplyActionFlags(copyActionFlags);
            cmd.SelectionOptions.ApplySelectionFlags(selectionFlags);
            cmd.LoggingOptions.ApplyLoggingFlags(loggingAction);
            cmd.CopyOptions.MultiThreadedCopiesCount = 0;
            return cmd;
        }

        /// <summary>
        /// Generate a new IRoboCommand of type T that shares the same Options objects as the <paramref name="baseCommand"/>
        /// </summary>
        /// <returns></returns>
        public static T GetIRoboCommand<T>(IRoboCommand baseCommand) where T : IRoboCommand, new()
        {
            var cmd = new T
            {
                CopyOptions = baseCommand.CopyOptions,
                SelectionOptions = baseCommand.SelectionOptions,
                LoggingOptions = baseCommand.LoggingOptions,
                RetryOptions = baseCommand.RetryOptions
            };
            try { cmd.JobOptions.Merge(baseCommand.JobOptions); }catch (NotImplementedException) { }
            return cmd;
        }

        public static async Task<RoboSharpTestResults[]> RunTests(RoboCommand roboCommand, IRoboCommand customCommand, bool CleanBetweenRuns, Action actionBetweenRuns = null)
        {
            var results = new List<RoboSharpTestResults>();
            BetweenRuns();
            results.Add(await TestSetup.RunTest(roboCommand));
            if (!roboCommand.LoggingOptions.ListOnly) BetweenRuns();
            
            customCommand.OnError += CachedRoboCommand_OnError;
            customCommand.OnCommandError += CachedRoboCommand_OnCommandError;
            
            results.Add(await TestSetup.RunTest(customCommand));
            
            customCommand.OnError -= CachedRoboCommand_OnError;
            customCommand.OnCommandError -= CachedRoboCommand_OnCommandError;

            if (CleanBetweenRuns) TestSetup.ClearOutTestDestination();
            return results.ToArray();

            void BetweenRuns()
            {
                if (CleanBetweenRuns) TestSetup.ClearOutTestDestination();
                actionBetweenRuns?.Invoke();
            }


        }
        private static void CachedRoboCommand_OnCommandError(IRoboCommand sender, CommandErrorEventArgs e) => Console.WriteLine(e.Exception);
        private static void CachedRoboCommand_OnError(IRoboCommand sender, RoboSharp.ErrorEventArgs e) => Console.WriteLine(e.Error);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ListOnly"></param>
        public static void CompareTestResults(RoboSharpTestResults roboCommandResults, RoboSharpTestResults iCommandResults, bool ListOnly)
        {
            var RCResults = roboCommandResults.Results;
            var customResults = iCommandResults.Results;
            Console.Write("---------------------------------------------------");
            Console.WriteLine($"Is List Only: {ListOnly}");
            Console.WriteLine(string.Format("RoboCopy Completion Time     : {0} ms", RCResults.TimeSpan.TotalMilliseconds));
            Console.WriteLine(string.Format("IRoboCommand Completion Time : {0} ms", customResults.TimeSpan.TotalMilliseconds));
            IStatistic RCStat = null, CRCStat = null;
            string evalSection = "";

            try
            {
                //Files
                //Console.Write("Evaluating File Stats...");
                AssertStat(RCResults.FilesStatistic, customResults.FilesStatistic, "Files");
                //Console.WriteLine("OK");

                //Bytes
                //Console.Write("Evaluating Byte Stats...");
                AssertStat(RCResults.BytesStatistic, customResults.BytesStatistic, "Bytes");
                //Console.WriteLine("OK");

                //Directories
                //Console.Write("Evaluating Directory Stats...");
                AssertStat(RCResults.DirectoriesStatistic, customResults.DirectoriesStatistic, "Directory");
                //Console.WriteLine("OK");

                Console.WriteLine("Test Passed.");

                Console.WriteLine("");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("RoboCopy Results:");
                Console.Write("Directory : "); Console.WriteLine(RCResults.DirectoriesStatistic);
                Console.Write("    Files : "); Console.WriteLine(RCResults.FilesStatistic);
                Console.Write("    Bytes : "); Console.WriteLine(RCResults.BytesStatistic);
                Console.WriteLine(RCResults.SpeedStatistic);
                Console.WriteLine("-----------------------------");
                Console.WriteLine("");
                Console.WriteLine("IRoboCommand Results:");
                Console.Write("Directory : "); Console.WriteLine(customResults.DirectoriesStatistic);
                Console.Write("    Files : "); Console.WriteLine(customResults.FilesStatistic);
                Console.Write("    Bytes : "); Console.WriteLine(customResults.BytesStatistic);
                Console.WriteLine(customResults.SpeedStatistic);
                Console.WriteLine("-----------------------------");
                Console.WriteLine("");
                Console.WriteLine("");

                void AssertStat(IStatistic rcStat, IStatistic crcSTat, string eval)
                {
                    RCStat = rcStat;
                    CRCStat = crcSTat;
                    try
                    {
                        Assert.AreEqual(RCStat.Copied, CRCStat.Copied, $"\n{eval} Stat: COPIED");
                        Assert.AreEqual(RCStat.Skipped, CRCStat.Skipped, $"\n{eval} Stat: SKIPPED");
                        Assert.AreEqual(RCStat.Extras, CRCStat.Extras, $"\n{eval} Stat: EXTRAS");
                        Assert.AreEqual(RCStat.Total, CRCStat.Total, $"\n{eval} Stat: TOTAL");
                    }
                    catch
                    {
                        Console.WriteLine("\n    RoboCopy Result : " + rcStat);
                        Console.WriteLine("IRoboCommand Result : " + crcSTat);
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("-----------------------------");
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine("-----------------------------");

                throw new AssertFailedException(e.Message +
                    $"\nIs List Only: {ListOnly}" +
                    $"\n{evalSection} Stats: \n" +
                    $"RoboCopy Results: {RCStat}\n" +
                    $"IRoboCommand Results: {CRCStat}" +
                    (e.GetType() == typeof(AssertFailedException) ? "" : $" \nStackTrace: \n{e.StackTrace}"));
            }

            finally
            {

                Console.WriteLine("");
                Console.WriteLine("///////////////////////////////////////////////////////");
                Console.WriteLine("RoboCopy Log Lines:");
                foreach (string s in RCResults.LogLines)
                    Console.WriteLine(s);

                Console.WriteLine("///////////////////////////////////////////////////////");
                Console.WriteLine("");
                Console.WriteLine("IRoboCommand Log Lines:");
                Console.WriteLine("");
                foreach (string s in customResults.LogLines)
                    Console.WriteLine(s);
            }
        }

        /// <summary>
        /// Gets a randomly generated fully qualified path for a file within the Test_Files directory in the unit test output folder
        /// </summary>
        public static string GetRandomPath(bool randomSubFolder = false)
        {
            if (randomSubFolder)
                return new FileInfo(Path.Combine(AppDataFolder, Path.GetRandomFileName().Replace(".",""), Path.GetRandomFileName())).FullName;
            else
                return new FileInfo(Path.Combine(AppDataFolder, Path.GetRandomFileName())).FullName;
        }

        /// <summary>
        /// Cleans the AppData directory which is used for various unit tests
        /// </summary>
        public static void CleanAppData()
        {
            if (Directory.Exists(AppDataFolder))
            {
                Directory.Delete(AppDataFolder, true); // delete children
                Directory.CreateDirectory(AppDataFolder);
            }
        }
       
    }
}
