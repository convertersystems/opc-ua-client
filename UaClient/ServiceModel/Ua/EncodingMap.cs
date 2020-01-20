// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    public class EncodingMap : IEncodingMap
    {
        private readonly Dictionary<NodeId, Type> encodingIdToTypeDictionary;
        private readonly Dictionary<Type, NodeId> typeToEncodingIdDictionary;

        public List<string> NamespaceUris { get; }

        public EncodingMap(IEnumerable<string> namespaceUris, IDictionary<NodeId, Type> encodingIdToTypeDictionary, IDictionary<Type, NodeId> typeToEncodingIdDictionary)
        {
            this.NamespaceUris = new List<string>(namespaceUris);
            this.encodingIdToTypeDictionary = new Dictionary<NodeId, Type>(encodingIdToTypeDictionary);
            this.typeToEncodingIdDictionary = new Dictionary<Type, NodeId>(typeToEncodingIdDictionary);
        }

        /// <inheritdoc />
        public bool TryGetEncodingId(Type type, out NodeId encodingId)
        {
            return this.typeToEncodingIdDictionary.TryGetValue(type, out encodingId);
        }

        /// <inheritdoc />
        public bool TryGetType(NodeId encodingId, out Type type)
        {
            return this.encodingIdToTypeDictionary.TryGetValue(encodingId, out type);
        }

        public void Add(NodeId encodingId, Type type)
        {
            this.typeToEncodingIdDictionary[type] = encodingId;
            this.encodingIdToTypeDictionary[encodingId] = type;
        }
        
        public void Add(ExpandedNodeId nodeId, Type type)
        {
            var encodingId = ExpandedNodeId.ToNodeId(nodeId, this.NamespaceUris);
            this.Add(encodingId, type);
        }
    }
}
