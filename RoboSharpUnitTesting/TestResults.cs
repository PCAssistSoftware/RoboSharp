﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSharp;
using RoboSharp.Results;
using RoboSharp.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RoboSharpUnitTesting
{
    class RoboSharpTestResults
    {
        private RoboSharpTestResults() { }
        public RoboSharpTestResults(RoboCopyResults results, IProgressEstimator estimator)
        {
            Results = results;
            Estimator = estimator;
            Errors = CompareIResults(results, (ProgressEstimator)estimator, false).ToArray();
        }

        public IProgressEstimator Estimator { get; }
        public RoboCopyResults Results { get; }
        public string[] Errors{ get; }
        public bool IsErrored => Errors.Length > 0;

        


        /// <summary>
        /// Throws <see cref="AssertFailedException"/> if either Results or Estimator are errored.
        /// </summary>
        public void AssertTest()
        {
            try
            {
                foreach(string s in Results.LogLines)
                {
                    Console.WriteLine(s);
                }
                Assert.IsTrue(!IsErrored);
            }catch(AssertFailedException e)
            {
                throw CustomAssertException.Factory(this, e);
            }
        }

        class CustomAssertException : AssertFailedException
        {
            private CustomAssertException() { }
            public CustomAssertException(string message, AssertFailedException e) : base(message, e) { }

            public static CustomAssertException Factory(RoboSharpTestResults testResult, AssertFailedException e)
            {
                string msg = testResult.Errors.ConvertToLinedString();
                return new CustomAssertException(msg, e);
            }
        }

        public static List<string> CompareIResults(IResults expected, IResults actual, bool IsEstimator)
        {
            var Errors = new List<string>();
            CompareStatistics(expected.DirectoriesStatistic, actual.DirectoriesStatistic, IsEstimator, ref Errors);
            CompareStatistics(expected.FilesStatistic, actual.FilesStatistic, IsEstimator, ref Errors);
            CompareStatistics(expected.BytesStatistic, actual.BytesStatistic, IsEstimator, ref Errors);
            return Errors;
        }

        public static bool CompareStatistics(IStatistic expectedResults, IStatistic results, bool IsEstimator, ref List<string> Errors)
        {
            ErrGenerator("Total", IsEstimator, expectedResults, results, ref Errors);
            ErrGenerator("Copied", IsEstimator, expectedResults, results, ref Errors);
            ErrGenerator("Extras", IsEstimator, expectedResults, results, ref Errors);
            ErrGenerator("Skipped", IsEstimator, expectedResults, results, ref Errors);
            ErrGenerator("Failed", IsEstimator, expectedResults, results, ref Errors);
            ErrGenerator("Mismatch", IsEstimator, expectedResults, results, ref Errors);
            return Errors.Count == 0;
        }

        private static void ErrGenerator(string propName, bool IsEstimator, IStatistic expected, IStatistic actual, ref List<string> Errors)
        {
            string whatErrored = IsEstimator ? $"ProgressEstimator.{expected.Type}" : $"Results.{expected.Type}";
            long eSize = 0; long aSize = 0;
            switch(propName)
            {
                case "Total": eSize = expected.Total; aSize = actual.Total; break;
                case "Copied": eSize = expected.Copied; aSize = actual.Copied; break;
                case "Extras": eSize = expected.Extras; aSize = actual.Extras; break;
                case "Failed": eSize = expected.Failed; aSize = actual.Failed; break;
                case "Mismatch": eSize = expected.Mismatch; aSize = actual.Mismatch; break;
                case "Skipped": eSize = expected.Skipped; aSize = actual.Skipped; break;
            }
            if (eSize != aSize)
                Errors.Add($"{whatErrored}.{propName} -- Expected: {eSize}  || Actual: {aSize}");
        }
    }
}
