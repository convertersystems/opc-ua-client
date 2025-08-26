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
    public partial class BinaryEncoderTests
    {
        private abstract class TypeMappingEquivalency<TSubject, TExpectation> : IEquivalencyStep
        {

            public EquivalencyResult Handle(Comparands comparands, IEquivalencyValidationContext context, IValidateChildNodeEquivalency valueChildNodes)
            {
                if (comparands.Subject is TSubject subject)
                {
                    if (comparands.Expectation is TExpectation expectation)
                    {
                        Test(subject, expectation, context.Reason.FormattedMessage, context.Reason.Arguments);
                        return EquivalencyResult.EquivalencyProven;
                    }
                }
                return EquivalencyResult.ContinueWithNext;
            }

            protected abstract void Test(TSubject subject, TExpectation expectation, string because, object[] becauseArgs);
        }

        private class VariantEquivalency : TypeMappingEquivalency<Opc.Ua.Variant,Variant>
        {
            protected override void Test(Opc.Ua.Variant subject,Variant expectation, string because, object[] becauseArgs)
            {
                subject.Value
                    .Should().BeEquivalentTo(expectation.Value, because, becauseArgs);

                ((int)subject.TypeInfo.BuiltInType)
                    .Should().Be((int)expectation.Type, because, becauseArgs);
            }
        }

        private class StatusCodeEquivalency : TypeMappingEquivalency<Opc.Ua.StatusCode,StatusCode>
        {
            protected override void Test(Opc.Ua.StatusCode subject, StatusCode expectation, string because, object[] becauseArgs)
            {
                subject.Code
                    .Should().Be(expectation.Value, because, becauseArgs);
            }
        }

        private class NodeIdEquivalency : TypeMappingEquivalency<Opc.Ua.NodeId, NodeId>
        {
            protected override void Test(Opc.Ua.NodeId subject, NodeId expectation, string because, object[] becauseArgs)
            {
                subject.Identifier
                    .Should().BeEquivalentTo(expectation.Identifier, because, becauseArgs);

                subject.NamespaceIndex
                    .Should().Be(expectation.NamespaceIndex, because, becauseArgs);

                ((int)subject.IdType)
                    .Should().Be((int)expectation.IdType, because, becauseArgs);
            }
        }

        private class ExpandedNodeIdEquivalency : TypeMappingEquivalency<Opc.Ua.ExpandedNodeId, ExpandedNodeId>
        {
            protected override void Test(Opc.Ua.ExpandedNodeId subject, ExpandedNodeId expectation, string because, object[] becauseArgs)
            {
                subject.NamespaceUri
                    .Should().Be(expectation.NamespaceUri, because, becauseArgs);

                subject.ServerIndex
                    .Should().Be(expectation.ServerIndex, because, becauseArgs);

                subject.Identifier
                    .Should().BeEquivalentTo(expectation.NodeId.Identifier, because, becauseArgs);

                ((int)subject.IdType)
                    .Should().Be((int)expectation.NodeId.IdType, because, becauseArgs);
            }
        }

        private class DiagnosticInfoEquivalency : TypeMappingEquivalency<Opc.Ua.DiagnosticInfo, DiagnosticInfo>
        {
            protected override void Test(Opc.Ua.DiagnosticInfo subject, DiagnosticInfo expectation, string because, object[] becauseArgs)
            {
                subject.AdditionalInfo
                    .Should().Be(expectation.AdditionalInfo, because, becauseArgs);

                subject.InnerDiagnosticInfo
                    .Should().Be(expectation.InnerDiagnosticInfo, because, becauseArgs);

                subject.InnerStatusCode.Code
                    .Should().Be(expectation.InnerStatusCode, because, becauseArgs);

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

        private class DataValueEquivalency : TypeMappingEquivalency<Opc.Ua.DataValue, DataValue>
        {
            protected override void Test(Opc.Ua.DataValue subject, DataValue expectation, string because, object[] becauseArgs)
            {
                subject.Value
                    .Should().Be(expectation.Value);

                subject.StatusCode.Code
                    .Should().Be(expectation.StatusCode.Value);

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
        
        private class MatrixEquivalency : TypeMappingEquivalency<Opc.Ua.Matrix, Array>
        {
            protected override void Test(Opc.Ua.Matrix subject, Array expectation, string because, object[] becauseArgs)
            {
                var arr = subject.ToArray();
                arr
                    .Should().BeEquivalentTo(expectation);
            }
        }

        private class XmlEquivalency : TypeMappingEquivalency<XmlNode, XElement>
        {
            protected override void Test(XmlNode subject, XElement expectation, string because, object[] becauseArgs)
            {
                var xelem = XElement.Load(subject.CreateNavigator().ReadSubtree());
                
                xelem
                    .Should().BeEquivalentTo(expectation, because, becauseArgs);
            }
        }


        static BinaryEncoderTests()
        {
            // Qualified name
            AssertionConfiguration.Current.Equivalency.Modify(options => options.ComparingByMembers<QualifiedName>());

            // Localizable text
            AssertionConfiguration.Current.Equivalency.Modify(options => options.ComparingByMembers<LocalizedText>());

            // StatusCode
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new StatusCodeEquivalency()));

            // Variant
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new VariantEquivalency()));

            // NodeId
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new NodeIdEquivalency()));
            
            // ExpandedNodeId
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new ExpandedNodeIdEquivalency()));

            // DiagnosticInfo
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new DiagnosticInfoEquivalency()));

            // DataValue
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new DataValueEquivalency()));

            // Xml
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new XmlEquivalency()));
            
            // Matrix/Multidim array
            AssertionConfiguration.Current.Equivalency.Modify(options => options.Using(new MatrixEquivalency()));
        }
    }
}
