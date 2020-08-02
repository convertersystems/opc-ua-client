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
        public ExtensionObject(byte[]? body, ExpandedNodeId? typeId)
        {
            if (body == null)
            {
                this.BodyType = BodyType.None;
                return;
            }

            this.Body = body;
            this.BodyType = BodyType.ByteString;
            this.TypeId = typeId;
        }

        public ExtensionObject(XElement? body, ExpandedNodeId? typeId)
        {
            if (body == null)
            {
                this.BodyType = BodyType.None;
                return;
            }

            this.Body = body;
            this.BodyType = BodyType.XmlElement;
            this.TypeId = typeId;
        }

        public ExtensionObject(IEncodable? body, ExpandedNodeId? typeId)
        {
            if (body == null)
            {
                this.BodyType = BodyType.None;
                return;
            }

            this.Body = body;
            this.BodyType = BodyType.Encodable;
            this.TypeId = typeId;
        }

        public ExtensionObject(IEncodable? body)
        {
            if (body == null)
            {
                this.BodyType = BodyType.None;
                return;
            }

            this.Body = body;
            this.BodyType = BodyType.Encodable;
            if (!TypeLibrary.Default.EncodingDictionary.TryGetValue(body.GetType(), out var binaryEncodingId))
            {
                throw new ServiceResultException(StatusCodes.BadDataEncodingUnsupported);
            }
            this.TypeId = binaryEncodingId;

        }

        public object? Body { get; }

        public ExpandedNodeId? TypeId { get; }

        public BodyType BodyType { get; }
    }
}