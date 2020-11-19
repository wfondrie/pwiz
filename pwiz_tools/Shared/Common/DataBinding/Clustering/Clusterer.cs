using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Common.DataBinding.Clustering
{
    public class Clusterer
    {
        public Clusterer(ResultColumns resultColumns)
        {
            ResultColumns = resultColumns;
        }

        public ResultColumns ResultColumns { get; private set; }

        public Tuple<ImmutableList<RowItem>, ClusterMergeIndices> ClusterRows(IList<RowItem> rowItems)
        {
            var pivotedPropertySets = ResultColumns.PivotedPropertySets.ToList();
            var numericColumns = pivotedPropertySets
                .SelectMany(set => set.NumericPropertyDescriptors).ToList();
            if (numericColumns.Count == 0)
            {
                return null;
            }

            var points = new double[rowItems.Count, numericColumns.Count];
            for (int iRow = 0; iRow < rowItems.Count; iRow++)
            {
                var rowItem = rowItems[iRow];
                int iCol = 0;
                foreach (var value in pivotedPropertySets.SelectMany(set => set.GetNumericZScores(rowItem)))
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

        public ResultColumns ClusterColumns(IList<RowItem> rowItems)
        {
            return new ResultColumns(ResultColumns.ColumnSets.Select(columns=>ClusterPropertySet(columns, rowItems)));
        }

        public ClusteredColumns ClusterPropertySet(ClusteredColumns clusteredColumns, IList<RowItem> rowItems)
        {
            var pivotedPropertySet = clusteredColumns.Columns as PivotedPropertySet;
            if (pivotedPropertySet == null)
            {
                return clusteredColumns;
            }

            var points = new double[pivotedPropertySet.PivotKeys.Count, 
                rowItems.Count * pivotedPropertySet.NumericGroupCount];
            int iFeature = 0;
            foreach (var rowItem in rowItems)
            {
                foreach (var group in pivotedPropertySet.Groups)
                {
                    if (!group.IsNumeric)
                    {
                        continue;
                    }

                    var zScores = group.GetZScores(rowItem);
                    for (int iPivotKey = 0; iPivotKey < zScores.Count; iPivotKey++)
                    {
                        var zScore = zScores[iPivotKey];
                        if (zScore.HasValue)
                        {
                            points[iPivotKey, iFeature] = zScore.Value;
                        }
                    }
                }
            }
            alglib.clusterizercreate(out alglib.clusterizerstate s);
            alglib.clusterizersetpoints(s, points, 2);
            alglib.clusterizerrunahc(s, out alglib.ahcreport rep);
            var newPivotedPropertySet = new PivotedPropertySet(Reorder(pivotedPropertySet.PivotKeys, rep),
                pivotedPropertySet.Groups.Select(group =>
                    new PivotedPropertySet.Group(group.PropertyPath, Reorder(group.Columns, rep)))
            );
            return new ClusteredColumns(newPivotedPropertySet, MakeClusterMergeIndices(rep));
        }

        public static ClusterMergeIndices MakeClusterMergeIndices(alglib.ahcreport clusterReport)
        {
            return new ClusterMergeIndices(Enumerable.Range(0, clusterReport.pz.GetLength(0)).Select(i=>new KeyValuePair<int, int>(clusterReport.pz[i, 0], clusterReport.pz[i, 1])));
        }

        public static IEnumerable<T> Reorder<T>(IList<T> itemList, alglib.ahcreport clusterReport)
        {
            if (itemList.Count != clusterReport.p.Length)
            {
                throw new ArgumentException();
            }

            return Enumerable.Range(0, clusterReport.p.Length).OrderBy(i => clusterReport.p[i])
                .Select(i => itemList[i]);
        }
    }
}
