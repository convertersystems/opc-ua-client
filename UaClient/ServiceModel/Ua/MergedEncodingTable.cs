using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public class MergedEncodingTable : IEnumerable<(ExpandedNodeId, Type)>
    {
        private IEnumerable<(ExpandedNodeId, Type)> table = Enumerable.Empty<(ExpandedNodeId,Type)>();

        public void Add(IEnumerable<(ExpandedNodeId, Type)> list)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            this.table = this.table.Concat(list);
        }

        public IEnumerator<(ExpandedNodeId, Type)> GetEnumerator()
            => this.table.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.table.GetEnumerator();
    }
}
