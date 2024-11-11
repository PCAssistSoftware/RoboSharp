
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("RoboSharp.Extensions.UnitTests")]
namespace RoboSharp.Extensions
{

#if NETSTANDARD2_0 || NETFRAMEWORK
    internal interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
#endif

    internal static class SystemExtensions
    {
        public static async Task CatchCancellation(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }catch(OperationCanceledException)
            {

            }
        }

        public static async Task<T> CatchCancellation<T>(this Task<T> task)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return default;
            }
        }

        public static ConfiguredTaskAwaitable CatchCancellation(this Task task, bool continueOnCapturedContext) => CatchCancellation(task).ConfigureAwait(continueOnCapturedContext);
        public static ConfiguredTaskAwaitable<T> CatchCancellation<T>(this Task<T> task, bool continueOnCapturedContext) => CatchCancellation(task).ConfigureAwait(continueOnCapturedContext);
    }
}
