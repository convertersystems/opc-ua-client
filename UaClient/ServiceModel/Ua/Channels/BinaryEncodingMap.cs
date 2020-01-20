// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Workstation.ServiceModel.Ua.Channels
{
    public class BinaryEncodingMap : EncodingMap
    {
        private static readonly Dictionary<NodeId, Type> BinaryEncodingIdToTypeDictionary = new Dictionary<NodeId, Type>();
        private static readonly Dictionary<Type, NodeId> TypeToBinaryEncodingIdDictionary = new Dictionary<Type, NodeId>();

        static BinaryEncodingMap()
        {
            foreach (var type in typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly.ExportedTypes)
            {
                var info = type.GetTypeInfo();
                if (info.ImplementedInterfaces.Contains(typeof(IEncodable)))
                {
                    var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                    if (attr != null)
                    {
                        var id = ExpandedNodeId.ToNodeId(attr.NodeId, null);
                        BinaryEncodingIdToTypeDictionary[id] = type;
                        TypeToBinaryEncodingIdDictionary[type] = id;
                    }
                }
            }
        }

        public BinaryEncodingMap() : base(
            new [] { "http://opcfoundation.org/UA/" },
            BinaryEncodingIdToTypeDictionary,
            TypeToBinaryEncodingIdDictionary)
        {
        }
    }
}