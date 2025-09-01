﻿using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class ExtensionObjectTests
    {
        [Fact]
        public void CreateFromByteStringNull()
        {
            var nodeid = new ExpandedNodeId(54);
            var obj = new ExtensionObject(default(byte[]), nodeid);

            obj.Body
                .Should().BeNull();
            obj.BodyType
                .Should().Be(BodyType.None);
            obj.TypeId
                .Should().Be(ExpandedNodeId.Null);
        }

        [Fact]
        public void CreateFromByteString()
        {
            var nodeid = new ExpandedNodeId(54);
            var body = new byte[] { 12, 13, 14 };
            var obj = new ExtensionObject(body, nodeid);

            obj.Body
                .Should().BeSameAs(body);
            obj.BodyType
                .Should().Be(BodyType.ByteString);
            obj.TypeId
                .Should().BeSameAs(nodeid);
        }

        [Fact]
        public void CreateFromXElementNull()
        {
            var nodeid = new ExpandedNodeId(54);
            var obj = new ExtensionObject(default(XElement), nodeid);

            obj.Body
                .Should().BeNull();
            obj.BodyType
                .Should().Be(BodyType.None);
            obj.TypeId
                .Should().Be(ExpandedNodeId.Null);
        }

        [Fact]
        public void CreateFromXElement()
        {
            var nodeid = new ExpandedNodeId(54);
            var body = XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />");
            var obj = new ExtensionObject(body, nodeid);

            obj.Body
                .Should().BeSameAs(body);
            obj.BodyType
                .Should().Be(BodyType.XmlElement);
            obj.TypeId
                .Should().BeSameAs(nodeid);
        }

        [Fact]
        public void CreateFromEncodableNull()
        {
            var nodeid = new ExpandedNodeId(54);
            var obj = new ExtensionObject(default(IEncodable), nodeid);

            obj.Body
                .Should().BeNull();
            obj.BodyType
                .Should().Be(BodyType.None);
            obj.TypeId
                .Should().Be(ExpandedNodeId.Null);
        }

        [Fact]
        public void CreateFromEncodable()
        {
            var nodeid = new ExpandedNodeId(54);
            var body = new ReadRequest();
            var obj = new ExtensionObject(body, nodeid);

            obj.Body
                .Should().BeSameAs(body);
            obj.BodyType
                .Should().Be(BodyType.Encodable);
            obj.TypeId
                .Should().Be(nodeid);
        }

        [Fact]
        public void CreateFromEncodableWithoutTypeId()
        {
            var body = new ReadRequest();
            var obj = new ExtensionObject(body);

            obj.Body
                .Should().BeSameAs(body);
            obj.BodyType
                .Should().Be(BodyType.Encodable);
            obj.TypeId
                .Should().Be(ExpandedNodeId.Null);
        }

        private class DataTypeWithoutEncodingId : Structure
        {
            public override void Decode(IDecoder decoder)
            {
                throw new NotImplementedException();
            }

            public override void Encode(IEncoder encoder)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void CreateFromEncodableWithoutAnyTypeId()
        {
            var body = new DataTypeWithoutEncodingId();
            var obj = new ExtensionObject(body);

            obj.Body
                .Should().BeSameAs(body);
            obj.BodyType
                .Should().Be(BodyType.Encodable);
            obj.TypeId
                .Should().Be(ExpandedNodeId.Null);
        }
    }
}
