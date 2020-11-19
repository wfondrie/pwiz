using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ResultColumns : Immutable
    {
        public static readonly ResultColumns EMPTY = new ResultColumns(ItemProperties.EMPTY);

        public ResultColumns(ItemProperties itemProperties)
        {
            ItemProperties = itemProperties;
            ColumnSets = ImmutableList.Singleton(new ClusteredColumns(itemProperties, ClusterMergeIndices.EMPTY));
        }

        public ResultColumns(IEnumerable<ClusteredColumns> clusteredColumns)
        {
            ColumnSets = ImmutableList.ValueOf(clusteredColumns);
            ItemProperties = new ItemProperties(ColumnSets.SelectMany(set=>set.Columns));
        }

        public ItemProperties ItemProperties { get; private set; }

        public ImmutableList<ClusteredColumns> ColumnSets { get; private set; }

        public IEnumerable<PivotedPropertySet> PivotedPropertySets
        {
            get { return ColumnSets.Select(col => col.Columns).OfType<PivotedPropertySet>(); }
        }

        private bool TryFindColumnSet(ref int columnIndex, out ClusteredColumns result)
        {
            result = null;
            if (columnIndex < 0)
            {
                return false;
            }

            foreach (var set in ColumnSets)
            {
                if (columnIndex < set.ColumnCount)
                {
                    result = set;
                    return true;
                }

                columnIndex -= set.ColumnCount;
            }

            return false;
        }

        public static ResultColumns FromProperties(IEnumerable<DataPropertyDescriptor> dataProperties)
        {
            var allColumns = ImmutableList.ValueOf(dataProperties);
            if (allColumns.Count == 0)
            {
                return EMPTY;
            }
            var columnsByPropertyDescriptor = allColumns.OfType<ColumnPropertyDescriptor>()
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
                return new ResultColumns(new ItemProperties(allColumns));
            }

            var clusterablePropertyPaths = columnClusters
                .SelectMany(cluster => cluster.Groups.Select(group => group.PropertyPath)).ToHashSet();


            var flatColumns = new List<DataPropertyDescriptor>();
            foreach (var column in allColumns)
            {
                var propertyPath = (column as ColumnPropertyDescriptor)?.DisplayColumn?.ColumnDescriptor?.PropertyPath;
                if (propertyPath == null || !clusterablePropertyPaths.Contains(propertyPath))
                {
                    flatColumns.Add(column);
                }
            }

            IEnumerable<ClusteredColumns> columnSets = columnClusters.Select(set=>new ClusteredColumns(set));
            if (flatColumns.Count != 0)
            {
                columnSets = columnSets.Prepend(new ClusteredColumns(flatColumns));
            }

            return new ResultColumns(ImmutableList.ValueOf(columnSets));
        }

        public double? GetZScore(DataPropertyDescriptor propertyDescriptor, RowItem rowItem)
        {
            int index = ItemProperties.IndexOfName(propertyDescriptor.Name);
            if (index < 0)
            {
                return null;
            }

            ClusteredColumns columnSet;
            if (!TryFindColumnSet(ref index, out columnSet))
            {
                return null;
            }

            return columnSet.GetZScore(rowItem, index);
        }
    }
}
