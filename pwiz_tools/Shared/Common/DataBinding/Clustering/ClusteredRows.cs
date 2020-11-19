using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ClusteredRows : Immutable
    {
        public ClusteredRows(IEnumerable<RowItem> rowItems, ClusterMergeIndices mergeIndices)
        {
            RowItems = ImmutableList.ValueOf(rowItems);
            RowMergeIndices = mergeIndices;
        }
        public ImmutableList<RowItem> RowItems { get; private set; }
        public ClusterMergeIndices RowMergeIndices { get; private set; }
        public ImmutableList<ColumnCluster> ColumnClusters { get; private set; }

        public IEnumerable<DataPropertyDescriptor> ItemProperties
        {
            get
            {
                return ColumnClusters.SelectMany(cluster => cluster.PropertyDescriptors);
            }
        }
    }
}
