using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RoboSharp.Extensions.Tests
{
    [TestClass]
    public class CachedEnumerableTests
    {
        [TestMethod]
        public void Test_CTOR_List()
        {
            List<string> myList = new List<string>() { "Item1", "Item2", "Item3", "Item4", "Item5" };
            var CE = myList.AsCachedEnumerable();
            myList.Add("Item6");
            Assert.IsFalse(CE.Contains(myList.Last()), "CachedEnumerable list was updated unexpectedly.");
        }

        [TestMethod]
        public void Test_CTOR_Array()
        {
            string[] myList = new string[] { "Item1", "Item2", "Item3", "Item4", "Item5" };
            var CE = myList.AsCachedEnumerable();
            myList[0] = null;
            Assert.IsFalse(CE.First() is null, "CachedEnumerable list was updated unexpectedly.");
        }
    }
}
