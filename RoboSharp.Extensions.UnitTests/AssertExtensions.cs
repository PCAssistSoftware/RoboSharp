using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Tests
{
    public static class AssertExtensions
    {
        /// <summary>
        ///  Allows catching derived types - Meant for OperationCancelledException
        /// </summary>
        public static async Task AssertThrowsExceptionAsync<T>(this Task task, string message = "") where T : Exception
        {
            try { await task; }
            catch (T) { return; }
            catch (Exception e) { Assert.ThrowsException<T>(() => throw e, message); }
            Assert.ThrowsException<T>(() => { }, message);
        }

        /// <summary>
        ///  Allows catching derived types - Meant for OperationCancelledException
        /// </summary>
        public static async Task AssertThrowsExceptionAsync<T>(this Func<Task> task, string message = "") where T : Exception
        {
            try { await task(); }
            catch (T) { return; }
            catch (Exception e) { Assert.ThrowsException<T>(() => throw e, message); }
            Assert.ThrowsException<T>(() => { }, message);
        }
    }
}
