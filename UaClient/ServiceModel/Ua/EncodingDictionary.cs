// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A bidirectional map to associate encoding ids with the corresponding types.
    /// The encoding dictionary is usual created by the channel.
    /// </summary>
    public class EncodingDictionary
    {
        /// <summary>
        /// Dictionary for the standard types of the OPC foundation and their binary encoding ids.
        /// </summary>
        static public EncodingDictionary BinaryEncodingDictionary { get; }
        
        static EncodingDictionary()
        {
            var assembley = typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly;
            var table = new CustomEncodingTable<BinaryEncodingIdAttribute>(assembley);
        
            BinaryEncodingDictionary = new EncodingDictionary(table);
        }

        private readonly Dictionary<NodeId, Type> encodingIdToTypeDictionary;
        private readonly Dictionary<Type, NodeId> typeToEncodingIdDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingDictionary"/> class.
        /// </summary>
        /// <param name="standardTypes">An encoding dictionary containing the standard OPC types.</param>
        /// <param name="table">The encoding table containing the additional types.</param>
        /// <param name="namespaceUris">The namespace URIs.</param>
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
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingDictionary"/> class.
        /// </summary>
        /// <param name="table">The encoding table containing the standard OPC types.</param>
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

        /// <summary>
        /// Gets the encoding id associated with the system type.
        /// </summary>
        /// <param name="type">The system type.</param>
        /// <param name="encodingId">The encoding id.</param>
        /// <returns>True if successfull.</returns>
        public bool TryGetEncodingId(Type type, [NotNullWhen(returnValue: true)] out NodeId? encodingId)
        {
            return this.typeToEncodingIdDictionary.TryGetValue(type, out encodingId);
        }

        /// <summary>
        /// Gets the system type associated with the encoding id.
        /// </summary>
        /// <param name="encodingId">The encoding id.</param>
        /// <param name="type">The system type.</param>
        /// <returns>True if successfull.</returns>
        public bool TryGetType(NodeId encodingId, [NotNullWhen(returnValue: true)] out Type? type)
        {
            return this.encodingIdToTypeDictionary.TryGetValue(encodingId, out type);
        }
    }
}
