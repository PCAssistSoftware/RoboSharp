using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using RoboSharp.UnitTests;
using System;
using System.Text;
using System.Threading;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class SpeedStatisticTests
    {
        [TestMethod]
        public void Test_Conversions()
        {
            int seconds = 67;
            decimal BytesPerSecond = 1024 * 1024;
            decimal MegabytesPerMin = 60; // rounded to 3 digits
            long fileLength = Convert.ToInt64(BytesPerSecond * seconds);

            var stat = new SpeedStatistic(fileLength, TimeSpan.FromSeconds(seconds));
            Assert.AreEqual(BytesPerSecond, stat.BytesPerSec, "\n-- Bytes Per Second calculated incorrectly!");
            Assert.AreEqual(MegabytesPerMin, stat.MegaBytesPerMin, "\n--MegaBytes Per Minute calculated incorrectly!");

            var average = new AverageSpeedStatistic();
            average.Average(fileLength, TimeSpan.FromSeconds(seconds));
            Assert.AreEqual(BytesPerSecond, stat.BytesPerSec, "\n-- Average Bytes Per Second calculated incorrectly!");
            Assert.AreEqual(MegabytesPerMin, stat.MegaBytesPerMin, "\n-- Average MegaBytes Per Minute calculated incorrectly!");

            average.Average(new ISpeedStatistic[] { stat, stat, stat });
            Assert.AreEqual(BytesPerSecond, stat.BytesPerSec, "\n-- Average Bytes Per Second calculated incorrectly!");
            Assert.AreEqual(MegabytesPerMin, stat.MegaBytesPerMin, "\n-- Average MegaBytes Per Minute calculated incorrectly!");
        }

        [DataRow("538 252 733", 538252733)]
        [DataRow("538,252,733", 538252733)]
        [DataRow("538.252.733", 538252733)]
        [TestMethod]
        public void Test_ParseBytesPerSecond(string input, object expected)
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                //test using the invariant culture
                Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                decimal expectedValue = Convert.ToDecimal(expected);
                Assert.AreEqual(expectedValue, SpeedStatistic.ParseBytesPerSecond(input));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [DataRow("30 799,068", 30799.068)]
        [DataRow("30 799.068", 30799.068)]
        [DataRow("30,799.068", 30799.068)]
        [DataRow("30.799,068", 30799.068)]
        [TestMethod]
        public void Test_ParseMegaBytesPerMinute(string input, object expected)
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                //test using the invariant culture
                Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                decimal expectedValue = Convert.ToDecimal(expected);
                Assert.AreEqual(expectedValue, SpeedStatistic.ParseMegabytesPerMinute(input));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
            
        }

        [DataRow(" Débit :           6 621 000 Octets/sec.", " Débit :            378,857 Méga-octets/min.", "fr-FR")]
        [DataRow(" Velocità:           257.555.063 Byte/sec.", " Velocità:             14737,419 MB/min.")]
        [TestMethod]
        public void ParseText(string sBPS, string sMB, string culture = "en-us") 
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                //test using the invariant culture
                Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                var value = SpeedStatistic.Parse(sBPS, sMB);
                Assert.IsNotNull(value);
                Console.WriteLine(value.ToString());
                Assert.IsTrue(value.BytesPerSec > 0 && value.MegaBytesPerMin > 0, $"\nInvariant Culture Parsing Failed - Returned value of {value}");

                // test again using specified culture
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
                value = SpeedStatistic.Parse(sBPS, sMB);
                Assert.IsNotNull(value);
                Console.WriteLine(value.ToString());
                Assert.IsTrue(value.BytesPerSec > 0 && value.MegaBytesPerMin > 0, $"\nCulture '{culture}' Parsing Failed - Returned value of {value}");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
