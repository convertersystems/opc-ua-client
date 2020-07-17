using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Collections;

namespace Workstation.ServiceModel.Ua
{
    public class BinaryEncodingTable : IEnumerable<(ExpandedNodeId, Type)>
    {
        private static readonly IEnumerable<(ExpandedNodeId, Type)> table;
        
        public static EncodingDictionary EncodingDictionary { get; }

        static BinaryEncodingTable()
        {
            var assembley = typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly;
            table = new CustomEncodingTable<BinaryEncodingIdAttribute>(assembley);
        
            EncodingDictionary = new EncodingDictionary(table);
        }

        public IEnumerator<(ExpandedNodeId, Type)> GetEnumerator()
            => table.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();
    }
}
