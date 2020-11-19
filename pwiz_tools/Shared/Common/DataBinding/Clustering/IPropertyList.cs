using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Common.DataBinding.Clustering
{
    public abstract class DataPropertyList : IEnumerable<DataPropertyList>
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract IEnumerator<DataPropertyList> GetEnumerator();

        public abstract int Count { get; }
        public abstract int IndexOf(string name);
    }
}
