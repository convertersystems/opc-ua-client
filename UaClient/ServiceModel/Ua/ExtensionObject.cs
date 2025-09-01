// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua
{
    public enum BodyType
    {
        None,
        ByteString,
        XmlElement,
        Encodable
    }

    public sealed class ExtensionObject
    {
        public ExtensionObject(byte[]? body, ExpandedNodeId typeId)
        {
            if (body == null)
            {
                BodyType = BodyType.None;
                TypeId = ExpandedNodeId.Null;
                return;
            }

            Body = body;
            BodyType = BodyType.ByteString;
            TypeId = typeId;
        }

        public ExtensionObject(XElement? body, ExpandedNodeId typeId)
        {
            if (body == null)
            {
                BodyType = BodyType.None;
                TypeId = ExpandedNodeId.Null;
                return;
            }

            Body = body;
            BodyType = BodyType.XmlElement;
            TypeId = typeId;
        }

        public ExtensionObject(IEncodable? body, ExpandedNodeId typeId)
        {
            if (body == null)
            {
                BodyType = BodyType.None;
                TypeId = ExpandedNodeId.Null;
                return;
            }

            Body = body;
            BodyType = BodyType.Encodable;
            TypeId = typeId;
        }

        public ExtensionObject(IEncodable? body)
        {
            if (body == null)
            {
                BodyType = BodyType.None;
                TypeId = ExpandedNodeId.Null;
                return;
            }

            Body = body;
            BodyType = BodyType.Encodable;
            TypeId = ExpandedNodeId.Null;
        }

        public object? Body { get; }

        public ExpandedNodeId TypeId { get; }

        public BodyType BodyType { get; }
    }
}