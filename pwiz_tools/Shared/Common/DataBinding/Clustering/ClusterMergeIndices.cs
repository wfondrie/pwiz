using System;
using System.Collections.Generic;
using System.Linq;
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
            Pairs = ImmutableList.ValueOf(indices);
        }

        public ImmutableList<KeyValuePair<int, int>> Pairs { get; private set; }

        public int ItemCount
        {
            get { return Pairs.Count + 1; }
        }
    }
}
