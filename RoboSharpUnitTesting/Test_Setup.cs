using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        
        public static string Source_LargerNewer { get; } = Path.Combine(TestFileRoot, "LargerNewer");
        public static string Source_Standard { get; } = Path.Combine(TestFileRoot, "STANDARD");

        public static void PrintEnvironment(TestContext context)
        {
            var assy = System.Reflection.Assembly.GetExecutingAssembly();
            var env = assy.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            const string div = "----------------------";
            const string format = $"\n{div}\nEnvironment : {{0}} {{1}} : ImageRuntimeVersion: {{2}} \n Test Class : {{3}}\nTest Method : {{4}}\nDisplay Name: {{5}}\n{div}";
            Console.WriteLine(string.Format(format, env.FrameworkName, env.FrameworkDisplayName, assy.ImageRuntimeVersion, context?.FullyQualifiedTestClassName, context?.TestName, context?.TestDisplayName));
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
        public static RoboCommand GenerateCommand(string destination, bool UseLargerFileSet, bool ListOnlyMode)
        {
            // Build the base command
            var cmd = new RoboCommand();
            cmd.CopyOptions.Source = UseLargerFileSet ? Source_LargerNewer : Source_Standard;
            cmd.CopyOptions.Destination = destination;
            cmd.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            cmd.LoggingOptions.ApplyLoggingFlags(LoggingFlags.VerboseOutput | LoggingFlags.OutputToRoboSharpAndLog | LoggingFlags.PrintSizesAsBytes);
            cmd.LoggingOptions.ListOnly = ListOnlyMode;
            cmd.Configuration.EnableFileLogging = false;
            return cmd;
        }

        /// <summary>
        /// Gets a new path to a directory (not yet created) within the temp folder
        /// </summary>
        public static string GetNewTempPath()
        {
            return Path.Combine(Path.GetTempPath(), "RoboSharp.UnitTesting", Guid.NewGuid().ToString("N"));
        }
        /// <summary>
        /// Gets a new directory and some child path
        /// </summary>
        public static (string dir, string file) GetNewTempPathWithChild()
        {
            string dir = GetNewTempPath();
            return (dir, Path.Combine(dir, Path.GetRandomFileName()));
        }

        public static async Task<RoboSharpTestResults> RunTest(IRoboCommand cmd, CancellationToken token)
        {
            IProgressEstimator prog = null;
            cmd.OnProgressEstimatorCreated += (o, e) => prog = e.ResultsEstimate;
            token.ThrowIfCancellationRequested();
            token.Register(() => cmd.Stop());
            var results = await cmd.StartAsync();
            return new RoboSharpTestResults(results, prog);
        }

        /// <summary>
        /// Deletes all and folders in <see cref="TestDestination"/>
        /// </summary>
        public static void ClearOutTestDestination(string directory)
        {
            if (Directory.Exists(directory))
            {
                var files = new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories);
                foreach (var f in files)
                    File.SetAttributes(f.FullName, FileAttributes.Normal);
                Directory.Delete(directory, true);
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

