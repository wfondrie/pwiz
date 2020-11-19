using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class KMeansTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestKMeans()
        {
            var points = new double[,] {{0, 0}, {1, 1}, {3, 3}};
            alglib.clusterizercreate(out alglib.clusterizerstate clusterizerstate);
            // disttype=2: Euclidean distance
            alglib.clusterizersetpoints(clusterizerstate, points, 2);
            alglib.clusterizerrunkmeans(clusterizerstate, 2, out alglib.kmeansreport rep);
            Assert.AreEqual(2, rep.k);
        }

    }
}
