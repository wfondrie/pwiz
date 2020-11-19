using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ColumnCluster : Immutable
    {
        public ColumnCluster(IEnumerable<PivotKey> pivotKeys, IEnumerable<ColumnGroup> columnGroups)
        {
            PivotKeys = ImmutableList.ValueOf(pivotKeys);
            MergeIndices = ClusterMergeIndices.EMPTY;
            ColumnGroups = ImmutableList.ValueOf(columnGroups);
        }

        public ImmutableList<PivotKey> PivotKeys { get; private set; }

        public int IndexOfPivotKey(PivotKey pivotKey)
        {
            return PivotKeys.IndexOf(pivotKey);
        }
        public ClusterMergeIndices MergeIndices { get; private set; }

        public ColumnCluster ChangeMergeIndices(ClusterMergeIndices mergeIndices)
        {
            return ChangeProp(ImClone(this), im => im.MergeIndices = mergeIndices);
        }

        public bool IsFlat
        {
            get { return MergeIndices.Indices.Count == 0; }
        }

        public ImmutableList<ColumnGroup> ColumnGroups
        {
            get; private set;
        }

        public IEnumerable<DataPropertyDescriptor> PropertyDescriptors
        {
            get
            {
                return Enumerable.Range(0, PivotKeys.Count)
                    .SelectMany(i => ColumnGroups.SelectMany(group => group.Columns));
            }
        }

        public IEnumerable<DataPropertyDescriptor> NumericPropertyDescriptors
        {
            get
            {
                return Enumerable.Range(0, PivotKeys.Count)
                    .SelectMany(i => ColumnGroups.Where(group => group.IsNumeric).SelectMany(group => group.Columns));
            }
        }

        public int NumericColumnCount
        {
            get
            {
                return ColumnGroups.Where(group => group.IsNumeric).Sum(group => group.Columns.Count);
            }
        }

        public static bool IsNumericType(Type type)
        {
            return type == typeof(double) || type == typeof(double?) || type == typeof(float) || type == typeof(float?);
        }

        public class ColumnGroup : Immutable
        {
            public ColumnGroup(PropertyPath propertyPath, IEnumerable<DataPropertyDescriptor> propertyDescriptors)
            {
                PropertyPath = propertyPath;
                Columns = ImmutableList.ValueOf(propertyDescriptors);
                IsNumeric = IsNumericType(Columns.First().PropertyType);
            }

            public PropertyPath PropertyPath
            {
                get;
                private set;
            }

            public bool IsNumeric { get; private set; }

            public ColumnGroup ChangeNumeric(bool numeric)
            {
                return ChangeProp(ImClone(this), im => im.IsNumeric = numeric);
            }

            public ImmutableList<DataPropertyDescriptor> Columns { get; private set; }
        }

        public List<double> GetZScores(RowItem rowItem)
        {
            var zScores = new List<double>(NumericColumnCount);
            foreach (var columnGroup in ColumnGroups)
            {
                if (!columnGroup.IsNumeric)
                {
                    continue;
                }

                var values = new List<double?>(columnGroup.Columns.Count);
                foreach (var column in columnGroup.Columns)
                {
                    var value = column.GetValue(rowItem);
                    if (value != null)
                    {
                        var doubleValue = Convert.ToDouble(value);
                        if (!double.IsInfinity(doubleValue) && !double.IsNaN(doubleValue))
                        {
                            values.Add(doubleValue);
                            continue;
                        }
                    }
                    values.Add(null);
                }

                var validValues = values.OfType<double>().ToList();
                if (validValues.Count > 1)
                {
                    var mean = validValues.Mean();
                    var stdDev = validValues.StandardDeviation();
                    foreach (var value in values)
                    {
                        if (value.HasValue)
                        {
                            zScores.Add((value.Value - mean) / stdDev);
                        }
                        else
                        {
                            zScores.Add(0);
                        }
                    }
                }
                else
                {
                    zScores.AddRange(Enumerable.Repeat(0.0, values.Count));
                }
            }

            return zScores;
        }
    }
}
