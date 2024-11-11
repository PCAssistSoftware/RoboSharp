using System;
using System.Collections.Generic;

namespace RoboSharp.Extensions.Comparers
{
    /// <summary>
    /// A generic <see cref="IFilePair"/> Equality Comparer. 
    /// <br/> Evaluates if the source and destinations of each pair are equal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FilePairEqualityComparer<T> : IEqualityComparer<T> where T : IFilePair
    {
        /// <summary> A threadsafe singleton used to compare <see cref="IFilePair"/> paths </summary>
        public static IEqualityComparer<IFilePair> Singleton => PairEqualityComparer.Singleton;

        /// <summary>
        /// Compare each path provided by the objects 
        /// </summary>
        /// <returns>TRUE if both objects have the same Source path and the same Destination path, otherwise false.</returns>
        public bool Equals(T x, T y)
        {
            return
                x.Source.FullName.Equals(y.Source.FullName, StringComparison.InvariantCultureIgnoreCase) &&
                x.Destination.FullName.Equals(y.Destination.FullName, StringComparison.InvariantCultureIgnoreCase);
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }

}
