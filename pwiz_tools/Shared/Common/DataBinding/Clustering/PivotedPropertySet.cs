using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding.Clustering
{
    public class PivotedPropertySet : AbstractReadOnlyList<DataPropertyDescriptor>
    {
        public PivotedPropertySet(IEnumerable<PivotKey> pivotKeys, IEnumerable<Group> columnGroups)
        {
            PivotKeys = ImmutableList.ValueOf(pivotKeys);
            Groups = ImmutableList.ValueOf(columnGroups);
        }

        public ImmutableList<PivotKey> PivotKeys { get; private set; }

        public override int Count
        {
            get { return PivotKeys.Count * Groups.Count; }
        }

        public override DataPropertyDescriptor this[int index]
        {
            get
            {
                return Groups[index % Groups.Count].Columns[index / Groups.Count];
            }
        }

        public int IndexOfPivotKey(PivotKey pivotKey)
        {
            return PivotKeys.IndexOf(pivotKey);
        }

        public ImmutableList<Group> Groups
        {
            get; private set;
        }

        public IEnumerable<DataPropertyDescriptor> PropertyDescriptors
        {
            get
            {
                return Enumerable.Range(0, PivotKeys.Count)
                    .SelectMany(i => Groups.Select(group => group.Columns[i]));
            }
        }

        public IEnumerable<DataPropertyDescriptor> NumericPropertyDescriptors
        {
            get
            {
                return Enumerable.Range(0, PivotKeys.Count)
                    .SelectMany(i => Groups.Where(group => group.IsNumeric).Select(group => group.Columns[i]));
            }
        }

        public int NumericColumnCount
        {
            get
            {
                return NumericGroupCount * PivotKeys.Count;
            }
        }

        public int NumericGroupCount
        {
            get
            {
                return Groups.Count(group => group.IsNumeric);
            }
        }

        public static bool IsNumericType(Type type)
        {
            return type == typeof(double) || type == typeof(double?) || type == typeof(float) || type == typeof(float?);
        }

        public class Group : Immutable
        {
            public Group(PropertyPath propertyPath, IEnumerable<DataPropertyDescriptor> propertyDescriptors)
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

            public Group ChangeNumeric(bool numeric)
            {
                return ChangeProp(ImClone(this), im => im.IsNumeric = numeric);
            }

            public ImmutableList<DataPropertyDescriptor> Columns { get; private set; }

            public IList<double?> GetZScores(RowItem rowItem)
            {
                var values = new List<double?>(Columns.Count);
                foreach (var column in Columns)
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
                    for (int i = 0; i < values.Count; i++)
                    {
                        var value = values[i];
                        if (value.HasValue)
                        {
                            values[i] = (value.Value - mean) / stdDev;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (values[i].HasValue)
                        {
                            values[i] = 0;
                        }
                    }
                }
                return values;
            }
        }

        public double? GetZScore(RowItem rowItem, int columnIndex)
        {
            var group = Groups[columnIndex % Groups.Count];
            if (!group.IsNumeric)
            {
                return null;
            }

            return group.GetZScores(rowItem)[columnIndex / Groups.Count];
        }

        public List<double> GetNumericZScores(RowItem rowItem)
        {
            return Groups.Where(group => group.IsNumeric).SelectMany(group => group.GetZScores(rowItem))
                .Select(val => val.GetValueOrDefault()).ToList();
        }
    }
}
