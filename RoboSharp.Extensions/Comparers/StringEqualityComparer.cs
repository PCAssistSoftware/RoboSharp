using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Comparers
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// <see cref="IEqualityComparer{T}"/> for <see langword="string"/> objects
    /// </summary>
    public sealed class StringEqualityComparer : IEqualityComparer<string>
    {
        /// <summary> Create a new Comparer </summary>
        public StringEqualityComparer(StringComparison type)
        {
            ComparisonType = type;
        }

        /// <summary>Compares <see langword="strings"/> using <see cref="StringComparison.Ordinal"/></summary>
        public static readonly StringEqualityComparer Ordinal = new StringEqualityComparer(StringComparison.Ordinal);

        /// <summary>Compares <see langword="strings"/> using <see cref="StringComparison.OrdinalIgnoreCase"/></summary>
        public static readonly StringEqualityComparer OrdinalIgnoreCase = new StringEqualityComparer(StringComparison.OrdinalIgnoreCase);

        /// <summary>Compares <see langword="strings"/> using <see cref="StringComparison.InvariantCulture"/></summary>
        public static readonly StringEqualityComparer InvariantCulture = new StringEqualityComparer(StringComparison.InvariantCulture);

        /// <summary>Compares <see langword="strings"/> using <see cref="StringComparison.InvariantCultureIgnoreCase"/></summary>
        public static readonly StringEqualityComparer InvariantCultureIgnoreCase = new StringEqualityComparer(StringComparison.InvariantCultureIgnoreCase);

        /// <summary>Compares <see langword="strings"/> using <see cref="StringComparison.CurrentCulture"/></summary>
        public static readonly StringEqualityComparer CurrentCulture = new StringEqualityComparer(StringComparison.CurrentCulture);

        /// <summary>Compares <see langword="strings"/> using <see cref="StringComparison.CurrentCultureIgnoreCase"/></summary>
        public static readonly StringEqualityComparer CurrentCultureIgnoreCase = new StringEqualityComparer(StringComparison.CurrentCultureIgnoreCase);

        /// <summary>
        /// The comparison enum to use within <see cref="string.Equals(string, StringComparison)"/>
        /// </summary>
        public StringComparison ComparisonType { get; }

        /// <inheritdoc/>
        public bool Equals(string x, string y)
        {
            return x.Equals(y, ComparisonType);
        }

        /// <inheritdoc/>
        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
