using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest.HierarchicalClustering
{
    [TestClass]
    public class HierarchicalClusteringTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestHierarchicalClusteringByPeptide()
        {
            var dataRows = ReadDataSet().ToList();
            var replicateNames = dataRows[0].ReplicateNames;
            var areas = new double[dataRows.Count, replicateNames.Count];
            for (int iRow = 0; iRow < dataRows.Count; iRow++)
            {
                for (int iCol = 0; iCol < replicateNames.Count; iCol++)
                {
                    areas[iRow, iCol] = dataRows[iRow].Areas[iCol].GetValueOrDefault();
                }
            }
            alglib.clusterizercreate(out alglib.clusterizerstate s);
            alglib.clusterizersetpoints(s, areas, 2);
            alglib.clusterizerrunahc(s, out alglib.ahcreport rep);
            foreach (var row in rep.p)
            {
                Console.Out.WriteLine(dataRows[row].Peptide);
            }
        }

        [TestMethod]
        public void TestHierarchicalClusteringByReplicate()
        {
            var dataRows = ReadDataSet().ToList();
            var replicateNames = dataRows[0].ReplicateNames;
            var areas = new double[replicateNames.Count, dataRows.Count];
            for (int iRow = 0; iRow < dataRows.Count; iRow++)
            {
                for (int iCol = 0; iCol < replicateNames.Count; iCol++)
                {
                    areas[iCol, iRow] = dataRows[iRow].Areas[iCol].GetValueOrDefault();
                }
            }
            alglib.clusterizercreate(out alglib.clusterizerstate s);
            alglib.clusterizersetpoints(s, areas, 2);
            alglib.clusterizerrunahc(s, out alglib.ahcreport rep);
            Assert.AreEqual(replicateNames.Count, rep.p.Length);
            foreach (var index in Enumerable.Range(0, rep.p.Length).OrderBy(i => rep.p[i]))
            {
                Console.Out.WriteLine(replicateNames[index]);
            }
        }

        [TestMethod]
        public void TestSimpleHierarchicalCluster()
        {
            var points = new double[,] {{1}, {100}, {2}, {90}, {4}, {80}};
            alglib.clusterizercreate(out alglib.clusterizerstate s);
            alglib.clusterizersetpoints(s, points, 2);
            alglib.clusterizerrunahc(s, out alglib.ahcreport rep);
            foreach (var index in Enumerable.Range(0, rep.p.Length).OrderBy(i => rep.p[i]))
            {
                Console.Out.WriteLine(points[index,0]);
            }

            var clusters = new List<double[]>();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                clusters.Add(new []{points[i,0]});
            }
            for (int i = 0; i < rep.z.GetLength(0); i++)
            {
                var newCluster = clusters[rep.z[i, 0]].Concat(clusters[rep.z[i, 1]]).ToArray();
                clusters.Add(newCluster);
                Console.Out.WriteLine("(" + string.Join(",", newCluster) + ")");
            }
        }


        public class DataRow
        {
            public DataRow(string protein, string peptide, ImmutableList<string> replicateNames, ImmutableList<double?> areas)
            {
                Protein = protein;
                Peptide = peptide;
                ReplicateNames = replicateNames;
                Areas = areas;
            }
            public string Protein { get; }
            public string Peptide { get; }
            public ImmutableList<string> ReplicateNames { get;  }
            public ImmutableList<double?> Areas { get; }
        }

        public IEnumerable<DataRow> ReadDataSet()
        {
            using (var stream =
                typeof(HierarchicalClusteringTest).Assembly.GetManifestResourceStream(
                    typeof(HierarchicalClusteringTest), "NormalizedAreas.csv"))
            {
                var csvFileReader = new CsvFileReader(new StreamReader(stream), true);
                int icolProtein = csvFileReader.GetFieldIndex("Protein");
                int icolPeptide = csvFileReader.GetFieldIndex("Peptide");
                ImmutableList<int> replicateColumns = ImmutableList.ValueOf(Enumerable.Range(0, csvFileReader.NumberOfFields).Except(new []{icolPeptide, icolProtein}));
                ImmutableList<string> replicateNames = ImmutableList.ValueOf(replicateColumns.Select(icol=>csvFileReader.FieldNames[icol]));
                while (csvFileReader.ReadLine() != null)
                {
                    var areas = replicateColumns.Select(i =>
                    {
                        var str = csvFileReader.GetFieldByIndex(i);
                        if (string.IsNullOrEmpty(str) || str == TextUtil.EXCEL_NA)
                        {
                            return (double?) null;
                        }
                        return double.Parse(str);
                    });
                    yield return new DataRow(csvFileReader.GetFieldByIndex(icolProtein), csvFileReader.GetFieldByIndex(icolPeptide), replicateNames, ImmutableList.ValueOf(areas));
                }
            }
        }
    }
}
