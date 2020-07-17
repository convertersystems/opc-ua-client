using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public class EncodingDictionary
    {
        private readonly Dictionary<NodeId, Type> encodingIdToTypeDictionary = new Dictionary<NodeId, Type>();
        private readonly Dictionary<Type, NodeId> typeToEncodingIdDictionary = new Dictionary<Type, NodeId>();

        public EncodingDictionary(IEnumerable<(ExpandedNodeId,Type)> table, IList<string> namespaceUris)
        {
            foreach (var (nodeId, type) in table)
            {
                ushort ns = nodeId.NodeId.NamespaceIndex;
                var nsu = nodeId.NamespaceUri;
                if (!string.IsNullOrEmpty(nsu))
                {
                    var i = namespaceUris.IndexOf(nsu!);
                    if (i == -1)
                    {
                        continue;
                    }
                    ns = (ushort)i;
                }

                var encodingId = nodeId.NodeId.IdType switch
                {
                    IdType.Numeric  => new NodeId((uint)nodeId.NodeId.Identifier, ns),
                    IdType.String   => new NodeId((string)nodeId.NodeId.Identifier, ns),
                    IdType.Guid     => new NodeId((Guid)nodeId.NodeId.Identifier, ns),
                    _               => new NodeId((byte[])nodeId.NodeId.Identifier, ns)
                };

                this.encodingIdToTypeDictionary[encodingId] = type;
                this.typeToEncodingIdDictionary[type] = encodingId;
            }
        }
        
        public EncodingDictionary(IEnumerable<(ExpandedNodeId,Type)> table)
        {
            foreach (var (nodeId, type) in table)
            {
                ushort ns = nodeId.NodeId.NamespaceIndex;
                var nsu = nodeId.NamespaceUri;
                if (!string.IsNullOrEmpty(nsu))
                {
                    continue;
                }

                var encodingId = nodeId.NodeId.IdType switch
                {
                    IdType.Numeric  => new NodeId((uint)nodeId.NodeId.Identifier, ns),
                    IdType.String   => new NodeId((string)nodeId.NodeId.Identifier, ns),
                    IdType.Guid     => new NodeId((Guid)nodeId.NodeId.Identifier, ns),
                    _               => new NodeId((byte[])nodeId.NodeId.Identifier, ns)
                };

                this.encodingIdToTypeDictionary[encodingId] = type;
                this.typeToEncodingIdDictionary[type] = encodingId;
            }
        }

        public bool TryGetEncodingId(Type type, [NotNullWhen(returnValue: true)] out NodeId? encodingId)
        {
            return this.typeToEncodingIdDictionary.TryGetValue(type, out encodingId);
        }

        public bool TryGetType(NodeId encodingId, [NotNullWhen(returnValue: true)] out Type? type)
        {
            return this.encodingIdToTypeDictionary.TryGetValue(encodingId, out type);
        }
    }
}
