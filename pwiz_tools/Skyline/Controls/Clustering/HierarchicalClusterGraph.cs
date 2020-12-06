﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.Controls.Clustering;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Controls.Clustering
{
    public partial class HierarchicalClusterGraph : DockableFormEx
    {
        private HierarchicalClusterResults _results;
        public HierarchicalClusterGraph()
        {
            InitializeComponent();

            zedGraphControl1.GraphPane.Title.IsVisible = false;
            zedGraphControl1.GraphPane.XAxis.Title.Text = "Replicate";
            zedGraphControl1.GraphPane.YAxis.Title.Text = "Protein";
            zedGraphControl1.GraphPane.Legend.IsVisible = false;
            zedGraphControl1.GraphPane.Margin.All = 0;
            zedGraphControl1.GraphPane.Border.IsVisible = false;
        }

        public HierarchicalClusterResults Results
        {
            get { return _results; }
            set
            {
                _results = value;
                UpdateGraph();
            }
        }

        public void UpdateGraph()
        {
            zedGraphControl1.GraphPane.CurveList.Clear();
            zedGraphControl1.GraphPane.GraphObjList.Clear();

            var dataSet = Results;
            var points = new PointPairList();
            foreach (var point in dataSet.Points)
            {
                points.Add(new PointPair(point.ColumnIndex + 1, dataSet.RowCount - point.RowIndex)
                {
                    Tag = point.Color
                });
            }
            zedGraphControl1.GraphPane.CurveList.Add(new ClusteredHeatMapItem("Points", points));

            zedGraphControl1.GraphPane.YAxis.Type = AxisType.Text;
            zedGraphControl1.GraphPane.YAxis.Scale.TextLabels = dataSet.RowHeaders.Select(header=>header.Caption).ToArray();

            zedGraphControl1.GraphPane.XAxis.Type = AxisType.Text;
            zedGraphControl1.GraphPane.XAxis.Scale.TextLabels =
                dataSet.ColumnGroups.SelectMany(group => group.Headers.Select(header=>header.Caption)).ToArray();
            AxisLabelScaler scaler = new AxisLabelScaler(zedGraphControl1.GraphPane);
            scaler.ScaleAxisLabels();
            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();

            UpdateDendrograms();
        }

        public void UpdateColumnDendrograms()
        {
            if (Results.ColumnGroups.All(group=>group.DendrogramData == null))
            {
                splitContainerHorizontal.Panel1Collapsed = true;
                return;
            }

            var datas = new List<DendrogramControl.DendrogramFormat>();
            double xStart = .5;
            foreach (var group in Results.ColumnGroups)
            {
                var locations = new List<KeyValuePair<double, double>>();
                var colors = new List<ImmutableList<Color>>();
                for (int i = 0; i < group.Headers.Count; i++)
                {
                    double x1 = (double) zedGraphControl1.GraphPane
                        .GeneralTransform(xStart + i, 0.0, CoordType.AxisXYScale).X;
                    double x2 = zedGraphControl1.GraphPane
                        .GeneralTransform(xStart + i + 1, 0.0, CoordType.AxisXYScale).X;
                    locations.Add(new KeyValuePair<double, double>(x1, x2));
                    colors.Add(group.Headers[i].Colors);
                }
                datas.Add(new DendrogramControl.DendrogramFormat(group.DendrogramData, locations, colors));
                xStart += group.Headers.Count;
            }
           
            columnDendrogram.SetDendrogramDatas(datas);
        }

        public void UpdateDendrograms()
        {
            UpdateColumnDendrograms();
            int rowDendrogramTop =
                splitContainerHorizontal.Panel1Collapsed ? 0 : splitContainerHorizontal.Panel1.Height;
            rowDendrogram.Bounds = new Rectangle(0, rowDendrogramTop, splitContainerVertical.Panel1.Width, splitContainerVertical.Panel1.Height - rowDendrogramTop);
            if (Results.RowDendrogramData == null)
            {
                splitContainerVertical.Panel1Collapsed = true;
            }
            else
            {
                splitContainerVertical.Panel1Collapsed = false;
                int rowCount = Results.RowCount;
                var rowLocations = new List<KeyValuePair<double, double>>();
                var colors = new List<ImmutableList<Color>>();
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    var y1 = zedGraphControl1.GraphPane.GeneralTransform(0.0, rowCount + .5 - rowIndex,
                        CoordType.AxisXYScale).Y;
                    var y2 = zedGraphControl1.GraphPane.GeneralTransform(0.0, rowCount - .5 - rowIndex ,
                        CoordType.AxisXYScale).Y;
                    rowLocations.Add(new KeyValuePair<double, double>(y1, y2));
                    colors.Add(Results.RowHeaders[rowIndex].Colors);
                }
                rowDendrogram.SetDendrogramDatas(new[]
                {
                    new DendrogramControl.DendrogramFormat(Results.RowDendrogramData, rowLocations, colors)
                });
            }

        }

        private void zedGraphControl1_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState, System.Drawing.PointF mousePosition)
        {
            UpdateDendrograms();
        }

        private void zedGraphControl1_Resize(object sender, EventArgs e)
        {
            UpdateDendrograms();
        }

        public class HeaderInfo
        {
            public string Caption { get; private set; }
            public ImmutableList<Color> Colors { get; private set; }
        }
    }
}
