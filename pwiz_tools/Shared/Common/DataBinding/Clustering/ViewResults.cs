using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ViewResults : Immutable
    {
        public static readonly ViewResults EMPTY = new ViewResults(ResultColumns.EMPTY, ImmutableList<RowItem>.EMPTY, ClusterMergeIndices.EMPTY);
        public ViewResults(ResultColumns resultColumns, ImmutableList<RowItem> rowItems,
            ClusterMergeIndices rowMergeIndices)
        {
            ResultColumns = resultColumns;
            RowItems = rowItems;
            RowMergeIndices = rowMergeIndices;
        }

        public ResultColumns ResultColumns { get; private set; }
        public ItemProperties ItemProperties
        {
            get { return ResultColumns.ItemProperties; }
        }
        public ImmutableList<RowItem> RowItems { get; private set; }
        public ClusterMergeIndices RowMergeIndices { get; private set; }
    }
}
