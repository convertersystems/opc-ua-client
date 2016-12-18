// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public ExtensionObject(byte[] body, ExpandedNodeId typeId)
        {
            Body = body;
            BodyType = body != null ? BodyType.ByteString : BodyType.None;
            TypeId = typeId;
        }

        public ExtensionObject(XElement body, ExpandedNodeId typeId)
        {
            Body = body;
            BodyType = body != null ? BodyType.XmlElement : BodyType.None;
            TypeId = typeId;
        }

        public ExtensionObject(IEncodable body)
        {
            Body = body;
            BodyType = body != null ? BodyType.Encodable : BodyType.None;
        }

        public object Body { get; }

        public ExpandedNodeId TypeId { get; }

        public BodyType BodyType { get; }
    }
}