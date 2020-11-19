using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ClusteredColumns
    {
        public ClusteredColumns(IList<DataPropertyDescriptor> columns) : this(columns, ClusterMergeIndices.EMPTY)
        {

        }
        public ClusteredColumns(IList<DataPropertyDescriptor> columns, ClusterMergeIndices mergedIndices)
        {
            Columns = columns;
            MergeIndices = mergedIndices;
        }

        public int ColumnCount
        {
            get { return Columns.Count; }
        }
        public IList<DataPropertyDescriptor> Columns { get; private set; }
        public ClusterMergeIndices MergeIndices { get; private set; }

        public double? GetZScore(RowItem rowItem, int columnIndex)
        {
            var pivotedPropertySet = Columns as PivotedPropertySet;
            if (pivotedPropertySet == null)
            {
                return null;
            }

            return pivotedPropertySet.GetZScore(rowItem, columnIndex);
        }
    }
}
