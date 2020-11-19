using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Common.DataBinding.Clustering
{
    public class Clusterer
    {
        public Clusterer(IEnumerable<DataPropertyDescriptor> columns)
        {
            var allColumns = columns.ToList();
            var columnsByPropertyDescriptor = allColumns.OfType<ColumnPropertyDescriptor>()
                .ToLookup(col => col.DisplayColumn?.ColumnDescriptor?.PropertyPath);
            var columnGroups = columnsByPropertyDescriptor.Where(group => 1 < group.Count())
                .ToLookup(group => ImmutableList.ValueOf(group.Select(col => col.PivotKey)));
            List<ColumnCluster> columnClusters = new List<ColumnCluster>();
            foreach (var columnGroupGroup in columnGroups)
            {
                var columnCluster = new ColumnCluster(columnGroupGroup.Key, columnGroupGroup.Select(group => new ColumnCluster.ColumnGroup(group.Key, group)));
                if (columnCluster.ColumnGroups.Any(group => group.IsNumeric))
                {
                    columnClusters.Add(columnCluster);
                }
            }

            var clusterablePropertyPaths = columnClusters
                .SelectMany(cluster => cluster.ColumnGroups.Select(group => group.PropertyPath)).ToHashSet();

            var flatColumns = new List<DataPropertyDescriptor>();
            foreach (var column in allColumns)
            {
                var propertyPath = (column as ColumnPropertyDescriptor)?.DisplayColumn?.ColumnDescriptor?.PropertyPath;
                if (propertyPath == null || !clusterablePropertyPaths.Contains(propertyPath))
                {
                    flatColumns.Add(column);
                }
            }

            FlatColumns = ImmutableList.ValueOf(flatColumns);
            ClusterableColumns = ImmutableList.ValueOf(columnClusters);
        }

        public ImmutableList<DataPropertyDescriptor> FlatColumns { get; private set; }
        public ImmutableList<ColumnCluster> ClusterableColumns { get; private set; }

        public Tuple<ImmutableList<RowItem>, ClusterMergeIndices> ClusterRows(IList<RowItem> rowItems)
        {
            var numericColumns = ClusterableColumns.SelectMany(cluster => cluster.NumericPropertyDescriptors).ToList();
            if (numericColumns.Count == 0)
            {
                return null;
            }

            var points = new double[rowItems.Count, numericColumns.Count];
            for (int iRow = 0; iRow < rowItems.Count; iRow++)
            {
                var rowItem = rowItems[iRow];
                int iCol = 0;
                foreach (var value in ClusterableColumns.SelectMany(cluster => cluster.GetZScores(rowItem)))
                {
                    points[iRow, iCol++] = value;
                }
            }
            alglib.clusterizercreate(out alglib.clusterizerstate s);
            alglib.clusterizersetpoints(s, points, 2);
            alglib.clusterizerrunahc(s, out alglib.ahcreport rep);
            var newRows = ImmutableList.ValueOf(Reorder(rowItems, rep));
            return Tuple.Create(newRows, MakeClusterMergeIndices(rep));
        }

        public static ClusterMergeIndices MakeClusterMergeIndices(alglib.ahcreport clusterReport)
        {
            return new ClusterMergeIndices(Enumerable.Range(0, clusterReport.pz.GetLength(0)).Select(i=>new KeyValuePair<int, int>(clusterReport.pz[i, 0], clusterReport.pz[i, 1])));
        }

        public static IEnumerable<T> Reorder<T>(IList<T> items, alglib.ahcreport clusterReport)
        {
            if (items.Count != clusterReport.p.Length)
            {
                throw new ArgumentException();
            }

            return Enumerable.Range(0, clusterReport.p.Length).OrderBy(i => clusterReport.p[i])
                .Select(i => items[i]);
        }
    }
}
