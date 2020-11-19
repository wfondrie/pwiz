using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pwiz.Common.DataBinding.Clustering;
using pwiz.Common.DataBinding.Controls;

namespace pwiz.Common.Controls.Clustering
{
    public partial class DendrogramControl : UserControl
    {
        private BoundDataGridView _boundDataGridView;
        public DendrogramControl()
        {
            InitializeComponent();
        }

        public BoundDataGridView BoundDataGridView
        {
            get { return _boundDataGridView;}
            set
            {
                if (BoundDataGridView != null)
                {
                    BoundDataGridView.DataBindingComplete -= BoundDataGridView_DataBindingComplete;
                    BoundDataGridView.Scroll -= BoundDataGridView_OnScroll;
                    BoundDataGridView.SizeChanged -= BoundDataGridView_OnSizeChanged;
                }
                _boundDataGridView = value;
                if (BoundDataGridView != null)
                {
                    BoundDataGridView.DataBindingComplete += BoundDataGridView_DataBindingComplete;
                    BoundDataGridView.Scroll += BoundDataGridView_OnScroll;
                    BoundDataGridView.SizeChanged += BoundDataGridView_OnSizeChanged;
                }

            }
        }

        private void BoundDataGridView_OnSizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void BoundDataGridView_OnScroll(object sender, ScrollEventArgs e)
        {
            Invalidate();
        }

        private void BoundDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var viewResults = (BoundDataGridView.DataSource as BindingListSource)?.ViewResults;
            if (viewResults == null)
            {
                return;
            }
            var pen = new Pen(Color.Black, 1);
            foreach (var columnSet in viewResults.ResultColumns.ColumnSets)
            {
                DrawDendrogram(e.Graphics, columnSet);
            }
        }

        private void DrawDendrogram(Graphics graphics, ClusteredColumns clusteredColumns)
        {
            var pivotedColumns = clusteredColumns.Columns as PivotedPropertySet;
            if (pivotedColumns == null)
            {
                return;
            }
            var columnsByName = new Dictionary<string, DataGridViewColumn>();
            foreach (var column in BoundDataGridView.Columns.OfType<DataGridViewColumn>())
            {
                if (column.DataPropertyName != null)
                {
                    columnsByName[column.DataPropertyName] = column;
                }
            }

            var points = new List<Tuple<int, double>>();
            for (int i = 0; i < pivotedColumns.PivotKeys.Count; i++)
            {
                List<DataGridViewColumn> gridColumns = new List<DataGridViewColumn>();
                foreach (var group in pivotedColumns.Groups)
                {
                    var propertyDescriptor = group.Columns[i];
                    DataGridViewColumn dataGridViewColumn;
                    if (!columnsByName.TryGetValue(propertyDescriptor.Name, out dataGridViewColumn))
                    {
                        continue;
                    }
                    gridColumns.Add(dataGridViewColumn);
                }

                var position = gridColumns.Select(col =>
                {
                    if (col.Index < BoundDataGridView.FirstDisplayedCell.ColumnIndex)
                    {
                        return 0;
                    }
                    var rect = BoundDataGridView.GetColumnDisplayRectangle(col.Index, false);
                    if (rect.Left == rect.Right)
                    {
                        return Width;
                    }

                    return (rect.Left + rect.Right) / 2;// - BoundDataGridView.HorizontalScrollingOffset;
                }).Average();
                points.Add(Tuple.Create(0, position));
            }

            var lines = new List<Tuple<Tuple<int, double>, Tuple<int, double>>>();
            var mergeIndices = clusteredColumns.MergeIndices;
            foreach (var pair in mergeIndices.Pairs)
            {
                var left = points[pair.Key];
                var right = points[pair.Value];
                var newPoint = Tuple.Create(Math.Max(left.Item1, right.Item1) + 1, (left.Item2 + right.Item2) / 2);
                points.Add(newPoint);
                lines.Add(Tuple.Create(left, newPoint));
                lines.Add(Tuple.Create(right, newPoint));
            }

            if (lines.Count == 0)
            {
                return;
            }
            var pen = new Pen(Color.Black, 1);

            var highestGroup = lines.Max(line => line.Item2.Item1);
            foreach (var line in lines)
            {
                int x1 = (int) line.Item1.Item2;
                int y1 = (int)line.Item1.Item1 * Height / highestGroup;
                int x2 = (int)line.Item2.Item2;
                int y2 = (int)(line.Item2.Item1 * Height / highestGroup);
                graphics.DrawLine(pen, x1, Height - y1, x2, Height - y2);
            }

        }
    }
}
