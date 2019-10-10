using FluentAssertions;
using FluentAssertions.Xml;
using FluentAssertions.Equivalency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;

namespace Workstation.UaClient.UnitTests.Channels
{
    public partial class BinaryDecoderTests
    {
        private abstract class TypeMappingEquivalency<TSubject, TExpectation> : IEquivalencyStep
        {
            public bool CanHandle(IEquivalencyValidationContext context,
                IEquivalencyAssertionOptions config)
                => context.Subject is TSubject && context.Expectation is TExpectation;

            public bool Handle(IEquivalencyValidationContext context, IEquivalencyValidator
                parent, IEquivalencyAssertionOptions config)
            {
                var subject = (TSubject)context.Subject;
                var expectation = (TExpectation)context.Expectation;

                Test(subject, expectation, context.Because, context.BecauseArgs);

                return true;
            }

            protected abstract void Test(TSubject subject, TExpectation expectation, string because, object[] becauseArgs);
        }

        private class GuidEquivalency : TypeMappingEquivalency<Guid, Opc.Ua.Uuid>
        {
            protected override void Test(Guid subject, Opc.Ua.Uuid expectation, string because, object[] becauseArgs)
            {
                subject
                    .Should().Be((Guid)expectation);
            }
        }
        
        private class VariantEquivalency : TypeMappingEquivalency<Variant, Opc.Ua.Variant>
        {
            protected override void Test(Variant subject, Opc.Ua.Variant expectation, string because, object[] becauseArgs)
            {
                subject.Value
                    .Should().BeEquivalentTo(expectation.Value, because, becauseArgs);

                ((int)subject.Type)
                    .Should().Be((int)expectation.TypeInfo.BuiltInType, because, becauseArgs);
            }
        }

        private class StatusCodeEquivalency : TypeMappingEquivalency<StatusCode, Opc.Ua.StatusCode>
        {
            protected override void Test(StatusCode subject, Opc.Ua.StatusCode expectation, string because, object[] becauseArgs)
            {
                subject.Value
                    .Should().Be(expectation.Code, because, becauseArgs);
            }
        }

        private class NodeIdEquivalency : TypeMappingEquivalency<NodeId, Opc.Ua.NodeId>
        {
            protected override void Test(NodeId subject, Opc.Ua.NodeId expectation, string because, object[] becauseArgs)
            {
                subject.Identifier
                    .Should().BeEquivalentTo(expectation.Identifier, because, becauseArgs);

                subject.NamespaceIndex
                    .Should().Be(expectation.NamespaceIndex, because, becauseArgs);

                ((int)subject.IdType)
                    .Should().Be((int)expectation.IdType, because, becauseArgs);
            }
        }

        private class ExpandedNodeIdEquivalency : TypeMappingEquivalency<ExpandedNodeId, Opc.Ua.ExpandedNodeId>
        {
            protected override void Test(ExpandedNodeId subject, Opc.Ua.ExpandedNodeId expectation, string because, object[] becauseArgs)
            {
                subject.NamespaceUri
                    .Should().Be(expectation.NamespaceUri, because, becauseArgs);

                subject.ServerIndex
                    .Should().Be(expectation.ServerIndex, because, becauseArgs);

                subject.NodeId.Identifier
                    .Should().BeEquivalentTo(expectation.Identifier, because, becauseArgs);

                ((int)subject.NodeId.IdType)
                    .Should().Be((int)expectation.IdType, because, becauseArgs);
            }
        }

        private class DiagnosticInfoEquivalency : TypeMappingEquivalency<DiagnosticInfo, Opc.Ua.DiagnosticInfo>
        {
            protected override void Test(DiagnosticInfo subject, Opc.Ua.DiagnosticInfo expectation, string because, object[] becauseArgs)
            {
                subject.AdditionalInfo
                    .Should().Be(expectation.AdditionalInfo, because, becauseArgs);

                subject.InnerDiagnosticInfo
                    .Should().Be(expectation.InnerDiagnosticInfo, because, becauseArgs);

                subject.InnerStatusCode.Value
                    .Should().Be(expectation.InnerStatusCode.Code, because, becauseArgs);

                subject.Locale
                    .Should().Be(expectation.Locale, because, becauseArgs);

                subject.LocalizedText
                    .Should().Be(expectation.LocalizedText, because, becauseArgs);

                subject.NamespaceUri
                    .Should().Be(expectation.NamespaceUri, because, becauseArgs);

                subject.SymbolicId
                    .Should().Be(expectation.SymbolicId, because, becauseArgs);
            }
        }

        private class DataValueEquivalency : TypeMappingEquivalency<DataValue, Opc.Ua.DataValue>
        {
            protected override void Test(DataValue subject, Opc.Ua.DataValue expectation, string because, object[] becauseArgs)
            {
                subject.Value
                    .Should().Be(expectation.Value);

                subject.StatusCode.Value
                    .Should().Be(expectation.StatusCode.Code);

                subject.SourceTimestamp
                    .Should().Be(expectation.SourceTimestamp);

                subject.SourcePicoseconds
                    .Should().Be(expectation.SourcePicoseconds);

                subject.ServerTimestamp
                    .Should().Be(expectation.ServerTimestamp);

                subject.ServerPicoseconds
                    .Should().Be(expectation.ServerPicoseconds);
            }
        }

        private class XmlEquivalency : TypeMappingEquivalency<XElement, XmlElement>
        {
            protected override void Test(XElement subject, XmlElement expectation, string because, object[] becauseArgs)
            {
                var xml = ToXmlNode(subject);
                
                xml
                    .Should().BeEquivalentTo(expectation, because, becauseArgs);
            }
        }

        private static XmlNode ToXmlNode(XElement element)
        {
            using (XmlReader reader = element.CreateReader())
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                return doc;
            }
        }

        static BinaryDecoderTests()
        {
            // Qualified name
            AssertionOptions.AssertEquivalencyUsing(options => options.ComparingByMembers<Opc.Ua.QualifiedName>());

            // Localizable text
            AssertionOptions.AssertEquivalencyUsing(options => options.ComparingByMembers<Opc.Ua.LocalizedText>());

            // StatusCode
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new StatusCodeEquivalency()));

            // Guid
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new GuidEquivalency()));

            // Variant
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new VariantEquivalency()));

            // NodeId
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new NodeIdEquivalency()));
            
            // ExpandedNodeId
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new ExpandedNodeIdEquivalency()));

            // DiagnosticInfo
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new DiagnosticInfoEquivalency()));

            // DataValue
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new DataValueEquivalency()));

            // Xml
            AssertionOptions.AssertEquivalencyUsing(options => options.Using(new XmlEquivalency()));

            // TimeZoneDataType
            AssertionOptions.AssertEquivalencyUsing(options => options.ComparingByMembers<TimeZoneDataType>().ExcludingMissingMembers());
        }
    }
}
