using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Comparers
{

    /// <summary>
    /// Evaulates Source and Destination paths for the supplied items. 
    /// <br/> Objects are Equal if BOTH Source and Destinations match. 
    /// <br/> If Either source or destination or different, objects are different. 
    /// <br/> <br/> ( X.source == Y.source &amp; X.Dest == Y.dest ) --> TRUE
    /// </summary>
    public sealed class PairEqualityComparer : IEqualityComparer<IFilePair>, IEqualityComparer<IDirectoryPair>
    {
        /// <summary> A threadsafe singleton used to compare <see cref="IDirectoryPair"/> and <see cref="IFilePair"/> paths </summary>
        public readonly static PairEqualityComparer Singleton = new PairEqualityComparer();

        private readonly static FilePairEqualityComparer<IFilePair> _fileComparer = new FilePairEqualityComparer<IFilePair>();
        private readonly static DirectoryPairEqualityComparer<IDirectoryPair> _dirComparer = new DirectoryPairEqualityComparer<IDirectoryPair>();


        /// <inheritdoc cref="FilePairEqualityComparer{T}.Equals(T, T)"/>
        public static bool AreEqual(IFilePair x, IFilePair y)
        {
            return _fileComparer.Equals(x, y);
        }

        /// <inheritdoc cref="DirectoryPairEqualityComparer{T}.Equals(T, T)"/>
        public static bool AreEqual(IDirectoryPair x, IDirectoryPair y)
        {
            return _dirComparer.Equals(x, y);
        }

        /// <inheritdoc cref="FilePairEqualityComparer{T}.Equals(T, T)"/>
        public bool Equals(IFilePair x, IFilePair y)
        {
            return _fileComparer.Equals(x, y);
        }

        /// <inheritdoc cref="DirectoryPairEqualityComparer{T}.Equals(T, T)"/>
        public bool Equals(IDirectoryPair x, IDirectoryPair y)
        {
            return _dirComparer.Equals(x, y);
        }

        /// <inheritdoc/>
        public int GetHashCode(IFilePair obj)
        {
            return obj.GetHashCode();
        }

        /// <inheritdoc/>
        public int GetHashCode(IDirectoryPair obj)
        {
            return obj.GetHashCode();
        }
    }

}
