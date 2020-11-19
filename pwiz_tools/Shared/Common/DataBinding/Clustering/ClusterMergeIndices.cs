using System.Collections.Generic;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ClusterMergeIndices : Immutable
    {
        public static readonly ClusterMergeIndices EMPTY =
            new ClusterMergeIndices(ImmutableList.Empty<KeyValuePair<int, int>>());
        public ClusterMergeIndices(IEnumerable<KeyValuePair<int, int>> indices)
        {
            Indices = ImmutableList.ValueOf(indices);
        }

        public ImmutableList<KeyValuePair<int, int>> Indices { get; private set; }
    }
}
