using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using Microsoft.Diagnostics.Tracing.Parsers.IIS_Trace;
using Microsoft.Diagnostics.Tracing.StackSources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RoboSharp.Benchmarks
{
    [ShortRunJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net462)]
    [ShortRunJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class ProcessedDirectoryInfoBenchmarks
    {
        [Params("", "TenChars__", "MoreThanTen")] public static string FileClass;
        [Params(8L, 12345678910)] public static long FileSize;
        [Params(true, false)] public static bool IncludeSize;

        [GlobalSetup(Targets = new string[] { nameof(ToString), nameof(ToStringOptimized) })]
        public void Setup() => PInfo = new ProcessedFileInfo(@"C:\SomeDirectory", FileClassType.NewDir, FileClass, FileSize);
        private ProcessedFileInfo PInfo;

        [Benchmark] public string ToStringOptimized() // interpolation with switched cases for optimal time and allocation
        {
            if (IncludeSize)
                return $"\t{PInfo.FileClass,-10}            \t{PInfo.Name}";
            else
                return $"\t{PInfo.FileClass,-10}{PInfo.Size,12}\t{PInfo.Name}";
        }

        [Benchmark]
        public override string ToString()
        {
            string fc = PInfo.FileClass.PadRight(10);
            string fs = (IncludeSize ? PInfo.Size.ToString() : "").PadLeft(10);
            return string.Format("\t{0}{1}\t{2}", fc, fs, PInfo.Name);
        }
    }


    [ShortRunJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net462)]
    [ShortRunJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class ProcessedFileInfoBenchmarks
    {
        [Params("Typical", "Worst_Case", "MoreThanTwelve")]  public static string FileClass;
        [Params(8L, 12345678910)] public static long FileSize;
        [Params(true, false)] public static bool IncludeClass;
        [Params(true, false)] public static bool IncludeSize;

        
        [GlobalSetup(Targets = new string[] { nameof(ToString), nameof(ToStringOptimized) })]
        public void Setup() => PInfo = new ProcessedFileInfo("FileName", FileClassType.File, FileClass, FileSize);
        private ProcessedFileInfo PInfo;

        [Benchmark] // interpolation with switched cases for optimal time and allocation
        public string ToStringOptimized()
        {
            if (IncludeClass)
            {
                if (IncludeSize)
                    return $"\t{PInfo.FileClass,10}  \t\t{PInfo.Size,8}\t{PInfo.Name}";
                else
                    return $"\t{PInfo.FileClass,10}  \t\t        \t{PInfo.Name}";
            }                
            else if (IncludeSize)
                return $"\t            \t\t{PInfo.Size,8}\t{PInfo.Name}";
            else // name only
                return $"\t            \t\t        \t{PInfo.Name}";
        }


        [Benchmark] // more time than stringbuilder, but less allocation
        public override string ToString()
        {
            string fc = (IncludeClass ? PInfo.FileClass : "").PadLeft(10).PadRight(12);
            string fs = (IncludeSize ? PInfo.Size.ToString() : "").PadLeft(8);
            return string.Format("\t{0}\t\t{1}\t{2}", fc, fs, PInfo.Name);
        }
    }
}
