using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

namespace Workstation.ServiceModel.Ua
{
    public class CustomEncodingTable : IEnumerable<(ExpandedNodeId, Type)>
    {
        protected readonly List<(ExpandedNodeId, Type)> encodingTable;

        public CustomEncodingTable()
        {
            this.encodingTable = new List<(ExpandedNodeId, Type)>();
        }
        
        public CustomEncodingTable(IEnumerable<(ExpandedNodeId, Type)> encodingTable)
        {
            this.encodingTable = encodingTable.ToList();
        }
        
        public void Add(ExpandedNodeId encodingId, Type type)
        {
            this.encodingTable.Add((encodingId, type));
        }
        
        public void Add((ExpandedNodeId, Type) item)
        {
            this.encodingTable.Add(item);
        }

        public virtual IEnumerator<(ExpandedNodeId, Type)> GetEnumerator()
        {
            return encodingTable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();
    }

    public class CustomEncodingTable<TAttribute> : CustomEncodingTable
        where TAttribute : Attribute, IEncodingIdAttribute
    {
        public CustomEncodingTable()
        {
        }
        
        public CustomEncodingTable(IEnumerable<(ExpandedNodeId, Type)> encodingTable)
            : base(encodingTable)
        {
        }
        
        public CustomEncodingTable(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                this.Add(type);
            }
        }

        public CustomEncodingTable(Assembly assembly)
        {
            var table = from type in assembly.ExportedTypes
                        let info = type.GetTypeInfo()
                        where info.ImplementedInterfaces.Contains(typeof(IEncodable))
                        let attr = info.GetCustomAttribute<TAttribute>(false)
                        where attr != null
                        select (attr.NodeId, type);
            this.encodingTable.AddRange(table);
        }
        
        public void Add(Type type)
        {
            var info = type.GetTypeInfo();

            if (!info.ImplementedInterfaces.Contains(typeof(IEncodable)))
            {
                throw new ArgumentException($"Type '{type}' does not implement the {typeof(IEncodable)} interface.");
            }

            var attr = info.GetCustomAttribute<TAttribute>(false);
            if (attr == null)
            {
                throw new ArgumentException($"Type '{type}' does not have a {typeof(TAttribute)} attribute.");
            }
            
            this.Add(attr.NodeId, type);
        }
    }
}
