using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public class EncodingDictionary
    {
        static public EncodingDictionary BinaryEncodingDictionary { get; }
        
        static EncodingDictionary()
        {
            var assembley = typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly;
            var table = new CustomEncodingTable<BinaryEncodingIdAttribute>(assembley);
        
            BinaryEncodingDictionary = new EncodingDictionary(table);
        }

        private readonly Dictionary<NodeId, Type> encodingIdToTypeDictionary;
        private readonly Dictionary<Type, NodeId> typeToEncodingIdDictionary;

        public EncodingDictionary(EncodingDictionary standardTypes, IEnumerable<(ExpandedNodeId,Type)> table, IList<string> namespaceUris)
        {
            if (standardTypes is null)
            {
                throw new ArgumentNullException(nameof(standardTypes));
            }
            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            if (namespaceUris is null)
            {
                throw new ArgumentNullException(nameof(namespaceUris));
            }

            encodingIdToTypeDictionary = new Dictionary<NodeId, Type>(standardTypes.encodingIdToTypeDictionary);
            typeToEncodingIdDictionary = new Dictionary<Type, NodeId>(standardTypes.typeToEncodingIdDictionary);

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

                this.encodingIdToTypeDictionary.Add(encodingId, type);
                this.typeToEncodingIdDictionary.Add(type, encodingId);
            }
        }
        
        public EncodingDictionary(IEnumerable<(ExpandedNodeId,Type)> table)
        {
            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            encodingIdToTypeDictionary = new Dictionary<NodeId, Type>();
            typeToEncodingIdDictionary = new Dictionary<Type, NodeId>();

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

                this.encodingIdToTypeDictionary.Add(encodingId, type);
                this.typeToEncodingIdDictionary.Add(type, encodingId);
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
