using System.Collections.Generic;
using pwiz.Common.Collections;

namespace pwiz.Common.DataBinding.Clustering
{
    public class ItemProperties : AbstractReadOnlyList<DataPropertyDescriptor>
    {
        public static readonly ItemProperties EMPTY = new ItemProperties(ImmutableList.Empty<DataPropertyDescriptor>());
        private Dictionary<string, int> _propertyIndexes;

        public ItemProperties(IEnumerable<DataPropertyDescriptor> propertyDescriptors)
        {
            PropertyDescriptors = ImmutableList.ValueOf(propertyDescriptors);
            _propertyIndexes = new Dictionary<string, int>(Count);
            int index = 0;
            foreach (var propertyDescriptor in this)
            {
                _propertyIndexes.Add(propertyDescriptor.Name, index++);
            }
        }

        public ImmutableList<DataPropertyDescriptor> PropertyDescriptors { get; private set; }


        public sealed override int Count
        {
            get { return PropertyDescriptors.Count; }
        }

        public int IndexOfName(string name)
        {
            int index;
            if (_propertyIndexes.TryGetValue(name, out index))
            {
                return index;
            }

            return -1;
        }

        public override DataPropertyDescriptor this[int index]
        {
            get
            {
                return PropertyDescriptors[index];
            }
        }
    }
}
