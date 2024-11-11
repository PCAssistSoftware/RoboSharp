using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    public static class Test_Setup
    {
        private static string TestFileRoot => Path.Combine(Directory.GetCurrentDirectory(), "TEST_FILES");
        
        public static string TestDestination { get; } = Path.Combine(TestFileRoot, "DESTINATION");
        public static string Source_LargerNewer { get; } = Path.Combine(TestFileRoot, "LargerNewer");
        public static string Source_Standard { get; } = Path.Combine(TestFileRoot, "STANDARD");

        public static void PrintEnvironment()
        {
            var assy = System.Reflection.Assembly.GetExecutingAssembly();
            var env = assy.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            string div = "----------------------";
            Console.WriteLine($"{div}\nEnvironment : {env.FrameworkName} {env.FrameworkDisplayName} : \nImageRuntimeVersion:{assy.ImageRuntimeVersion}\n{div}\n");
        }

        /// <summary>
        /// Check if running on AppVeyor -> Certain tests will always fail due to appveyor's setup -> this allows them to pass the checks on appveyor by just not running them
        /// </summary>
        /// <returns></returns>
        public static bool IsRunningOnAppVeyor(bool displayMessageIfReturnTrue = true)
        {
            isAppveyor = isAppveyor ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).StartsWith("C:\\Users\\appveyor\\", StringComparison.InvariantCultureIgnoreCase);
            if (isAppveyor.Value && displayMessageIfReturnTrue) Console.WriteLine(" - Bypassing this test due to running on AppVeyor");
            return isAppveyor.Value;
        }
        private static bool? isAppveyor; 

        /// <summary>
        /// Generate the Starter Options and Test Objects to compare
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="UseLargerFileSet">When set to TRUE, uses the larger file set (which is also newer save times)</param>
        public static RoboCommand GenerateCommand(bool UseLargerFileSet, bool ListOnlyMode)
        {
            // Build the base command
            var cmd = new RoboCommand();
            cmd.CopyOptions.Source = UseLargerFileSet ? Source_LargerNewer : Source_Standard;
            cmd.CopyOptions.Destination = TestDestination;
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.LoggingOptions.ApplyLoggingFlags(LoggingFlags.VerboseOutput | LoggingFlags.OutputToRoboSharpAndLog | LoggingFlags.PrintSizesAsBytes);
            cmd.LoggingOptions.ListOnly = ListOnlyMode;
            cmd.Configuration.EnableFileLogging = false;
            return cmd;
        }

        public static async Task<RoboSharpTestResults> RunTest(IRoboCommand cmd)
        {
            IProgressEstimator prog = null;
            cmd.OnProgressEstimatorCreated += (o, e) => prog = e.ResultsEstimate;
            var results = await cmd.StartAsync();
            return new RoboSharpTestResults(results, prog);
        }

        /// <summary>
        /// Deletes all and folders in <see cref="TestDestination"/>
        /// </summary>
        public static void ClearOutTestDestination()
        {

            if (Directory.Exists(TestDestination))
            {
                var files = new DirectoryInfo(TestDestination).GetFiles("*", SearchOption.AllDirectories);
                foreach (var f in files)
                    File.SetAttributes(f.FullName, FileAttributes.Normal);
                Directory.Delete(TestDestination, true);
            }
        }

        /// <summary>
        /// Write the LogLines to the Test Log
        /// </summary>
        /// <param name="Results"></param>
        public static void WriteLogLines(RoboCopyResults Results, bool SummaryOnly = false)
        {
            //Write the summary at the top for easier reference
            if (Results is null)
            {
                Console.WriteLine("Results Object is null!");
                return;
            }
            int i = 0;
            Console.WriteLine("SUMMARY LINES:");
            foreach (string s in Results.LogLines)
            {
                if (s.Trim().StartsWith("---------"))
                    i++;
                else if (i > 3)
                    Console.WriteLine(s);
            }
            if (!SummaryOnly)
            {
                Console.WriteLine("\n\n LOG LINES:");
                //Write the log lines
                foreach (string s in Results.LogLines)
                    Console.WriteLine(s);
            }
        }

        public static string ConvertToLinedString(this IEnumerable<string> strings)
        {
            string ret = "";
            foreach (string s in strings)
                ret += s + "\n";
            return ret;
        }

        public static void SetValues(this Statistic stat, int total, int copied, int failed, int extras, int mismatch, int skipped)
        {
            stat.Reset();
            stat.Add(total, copied, extras, failed, mismatch, skipped);
        }

    }
}

