using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Serialization;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Clustering;
using pwiz.Common.DataBinding.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest.HierarchicalClustering
{
    [TestClass]
    public class DataboundHierarchicalClusteringTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestDataboundHierarchicalClustering()
        {
            SrmDocument document;
            using (var stream =
                typeof(DataboundHierarchicalClusteringTest).Assembly.GetManifestResourceStream(
                    typeof(DataboundHierarchicalClusteringTest), "Rat_plasma.sky"))
            {
                document = (SrmDocument) new XmlSerializer(typeof(SrmDocument)).Deserialize(stream);
            }

            var viewSpec = new ViewSpec()
                .SetRowType(typeof(Skyline.Model.Databinding.Entities.Peptide))
                .SetColumns(new[]
                {
                    new ColumnSpec(PropertyPath.Root),
                    new ColumnSpec(
                        PropertyPath.Root.Property(nameof(Skyline.Model.Databinding.Entities.Peptide.Protein))),
                    new ColumnSpec(PropertyPath.Root
                        .Property(nameof(Skyline.Model.Databinding.Entities.Peptide.Results))
                        .DictionaryValues().Property(nameof(PeptideResult.Quantification))
                        .Property(nameof(QuantificationResult.NormalizedArea)))
                });
            var dataSchema = SkylineDataSchema.MemoryDataSchema(document, DataSchemaLocalizer.INVARIANT);
            var viewInfo = new ViewInfo(dataSchema, typeof(Skyline.Model.Databinding.Entities.Peptide), viewSpec);
            var viewContext = new DocumentGridViewContext(dataSchema);
            var bindingListSource = new BindingListSource(CancellationToken.None);
            bindingListSource.SetViewContext(viewContext, viewInfo);
            var clusterer = new Clusterer(ResultColumns.FromProperties(bindingListSource.ItemProperties));
            var rowItems = bindingListSource.OfType<RowItem>().ToList();
            var clusteredRows = clusterer.ClusterRows(rowItems);
            Assert.AreEqual(rowItems.Count - 1, clusteredRows.Item2.Pairs.Count);
        }
    }
}
