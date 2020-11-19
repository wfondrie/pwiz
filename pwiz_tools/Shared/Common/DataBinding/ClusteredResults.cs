using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding.Clustering;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding
{
#if false
    public class ClusteredResults : Immutable
    {
        private IDictionary<string, int> _propertyNameToIndex;

        public ClusteredResults(IEnumerable<DataPropertyDescriptor> itemProperties, IEnumerable<RowItem> rows)
        {
            ItemProperties = ImmutableList.ValueOf(itemProperties);
            Rows = ImmutableList.ValueOf(rows);
            _propertyNameToIndex = new Dictionary<string, int>(ItemProperties.Count);
            int columnIndex = 0;
            foreach (var property in ItemProperties)
            {
                _propertyNameToIndex.Add(property.Name, columnIndex++);
            }
        }

        public ImmutableList<DataPropertyDescriptor> ItemProperties { get; private set; }
        public ImmutableList<IList<DataPropertyDescriptor>> PropertySets { get; private set; }
        public ImmutableList<ClusterMergeIndices> ColumnMergeIndexSets { get; private set; }
        public ImmutableList<RowItem> Rows { get; private set; }
        public ClusterMergeIndices RowMergeIndexSet { get; private set; }

        public ClusteredResults FindPropertySets()
        {
            var columnsByPropertyDescriptor = ItemProperties.OfType<ColumnPropertyDescriptor>()
                .ToLookup(col => col.DisplayColumn?.ColumnDescriptor?.PropertyPath);
            var columnGroups = columnsByPropertyDescriptor.Where(group => 1 < group.Count())
                .ToLookup(group => ImmutableList.ValueOf(group.Select(col => col.PivotKey)));
            List<PivotedPropertySet> columnClusters = new List<PivotedPropertySet>();
            foreach (var columnGroupGroup in columnGroups)
            {
                var columnCluster = new PivotedPropertySet(columnGroupGroup.Key, columnGroupGroup.Select(group => new PivotedPropertySet.Group(group.Key, group)));
                if (columnCluster.Groups.Any(group => group.IsNumeric))
                {
                    columnClusters.Add(columnCluster);
                }
            }

            if (!columnClusters.Any())
            {
                return this;
            }

            var clusterablePropertyPaths = columnClusters
                .SelectMany(cluster => cluster.Groups.Select(group => group.PropertyPath)).ToHashSet();


            var flatColumns = new List<DataPropertyDescriptor>();
            foreach (var column in ItemProperties)
            {
                var propertyPath = (column as ColumnPropertyDescriptor)?.DisplayColumn?.ColumnDescriptor?.PropertyPath;
                if (propertyPath == null || !clusterablePropertyPaths.Contains(propertyPath))
                {
                    flatColumns.Add(column);
                }
            }

            IEnumerable<IList<DataPropertyDescriptor>> columnSets = columnClusters;
            if (flatColumns.Count != 0)
            {
                columnSets = columnSets.Prepend(flatColumns);
            }

            return ChangeProp(ImClone(this), im =>
            {
                im.PropertySets = ImmutableList.ValueOf(columnSets);
                im.ColumnMergeIndexSets = 
            });
        }
    }
#endif
}
