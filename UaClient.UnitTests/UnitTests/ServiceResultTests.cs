using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class ServiceResultTests
    {
        private static readonly string[] stringList = new[]
        {
            "first element",
            "namespace",
            "symbolic id",
            "locale",
            "localized text",
            "inner text"
        };

        [Fact]
        public void Create()
        {
            var result = new ServiceResult(StatusCodes.Good);

            result.AdditionalInfo
                .Should().BeNull();

            result.InnerResult
                .Should().BeNull();

            result.LocalizedText
                .Should().BeNull();

            result.NamespaceUri
                .Should().BeNull();

            result.StatusCode
                .Should().Be(new StatusCode(StatusCodes.Good));

            result.SymbolicId
                .Should().BeNull();
        }

        [Fact]
        public void CreateFromDiagnosticInfo1()
        {
            var diag = new DiagnosticInfo(1, 2, 3, 4, "additional info", StatusCodes.BadDataLost, new DiagnosticInfo(1, localizedText: 5));

            var result = new ServiceResult(StatusCodes.GoodClamped, diag, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped, "symbolic id", "namespace", new LocalizedText("localized text", "locale"), "additional info", 
                       new ServiceResult(StatusCodes.BadDataLost, namespaceUri: "namespace", localizedText: new LocalizedText("inner text", null)));

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfo2()
        {
            var diag = new DiagnosticInfo(1, 2, 3, 4, "additional info", StatusCodes.GoodCallAgain, new DiagnosticInfo(1, localizedText: 5));

            var result = new ServiceResult(StatusCodes.GoodClamped, diag, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped, "symbolic id", "namespace", new LocalizedText("localized text", "locale"), "additional info");

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfo3()
        {
            var diag = new DiagnosticInfo(1, 2);

            var result = new ServiceResult(StatusCodes.GoodClamped, diag, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped, "symbolic id", "namespace", new LocalizedText(null, null));

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfo4()
        {

            var diag = new DiagnosticInfo(1, 2, 3, 4, null, StatusCodes.GoodCallAgain, new DiagnosticInfo(1, localizedText: 5));

            var result = new ServiceResult(StatusCodes.GoodClamped, diag, null);
            var expected = new ServiceResult(StatusCodes.GoodClamped, localizedText: new LocalizedText(null, null));

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfo5()
        {

            var result = new ServiceResult(StatusCodes.GoodClamped, null, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped);

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfoArray1()
        {
            var diag = new[]
            {
                new DiagnosticInfo(1, 2, 3, 4, "additional info", StatusCodes.BadDataLost, new DiagnosticInfo(1, localizedText: 5))
            };

            var result = new ServiceResult(StatusCodes.GoodClamped, 0, diag, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped, "symbolic id", "namespace", new LocalizedText("localized text", "locale"), "additional info", 
                       new ServiceResult(StatusCodes.BadDataLost, namespaceUri: "namespace", localizedText: new LocalizedText("inner text", null)));

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfoArray2()
        {
            var diag = new[]
            {
                new DiagnosticInfo(1, 2, 3, 4, "additional info", StatusCodes.GoodCallAgain, new DiagnosticInfo(1, localizedText: 5))
            };

            var result = new ServiceResult(StatusCodes.GoodClamped, 0, diag, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped, "symbolic id", "namespace", new LocalizedText("localized text", "locale"), "additional info");

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfoArray3()
        {
            var diag = new[]
            {
                new DiagnosticInfo(1, 2)
            };

            var result = new ServiceResult(StatusCodes.GoodClamped, 0, diag, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped, "symbolic id", "namespace", new LocalizedText(null, null));

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfoArray4()
        {

            var diag = new[]
            {
                new DiagnosticInfo(1, 2, 3, 4, null, StatusCodes.GoodCallAgain, new DiagnosticInfo(1, localizedText: 5))
            };

            var result = new ServiceResult(StatusCodes.GoodClamped, 0, diag, null);
            var expected = new ServiceResult(StatusCodes.GoodClamped, localizedText: new LocalizedText(null, null));

            result
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CreateFromDiagnosticInfoArray5()
        {

            var result = new ServiceResult(StatusCodes.GoodClamped, 0, null, stringList);
            var expected = new ServiceResult(StatusCodes.GoodClamped);

            result
                .Should().BeEquivalentTo(expected);
        }

        [InlineData(StatusCodes.Good)]
        [InlineData(StatusCodes.GoodCallAgain)]
        [InlineData(StatusCodes.GoodResultsMayBeIncomplete)]
        [InlineData(StatusCodes.GoodSubscriptionTransferred)]
        [Theory]
        public void Good(uint sc)
        {
            var result = new ServiceResult(sc);
            ServiceResult.IsGood(result)
                .Should().BeTrue();
            
            // implicit conversion from uint
            ServiceResult.IsGood(sc)
                .Should().BeTrue();
            
            // implicit conversion from StatusCode
            ServiceResult.IsGood((StatusCode)sc)
                .Should().BeTrue();
        }

        [InlineData(StatusCodes.BadAggregateConfigurationRejected)]
        [InlineData(StatusCodes.BadArgumentsMissing)]
        [InlineData(StatusCodes.BadCertificateRevocationUnknown)]
        [InlineData(StatusCodes.UncertainDataSubNormal)]
        [InlineData(StatusCodes.UncertainDominantValueChanged)]
        [Theory]
        public void NotGood(uint sc)
        {
            var result = new ServiceResult(sc);
            ServiceResult.IsGood(result)
                .Should().BeFalse();
        }

        [Fact]
        public void NullIsGood()
        {
            ServiceResult.IsGood(null)
                .Should().BeTrue();
        }

        [InlineData(StatusCodes.BadBrowseDirectionInvalid)]
        [InlineData(StatusCodes.BadAggregateConfigurationRejected)]
        [InlineData(StatusCodes.BadDataLost)]
        [InlineData(StatusCodes.BadNoData)]
        [Theory]
        public void Bad(uint sc)
        {
            var result = new ServiceResult(sc);
            ServiceResult.IsBad(result)
                .Should().BeTrue();
        }

        [InlineData(StatusCodes.GoodCallAgain)]
        [InlineData(StatusCodes.GoodShutdownEvent)]
        [InlineData(StatusCodes.UncertainInitialValue)]
        [InlineData(StatusCodes.UncertainSubstituteValue)]
        [Theory]
        public void NotBad(uint sc)
        {
            var result = new ServiceResult(sc);
            ServiceResult.IsBad(result)
                .Should().BeFalse();
        }

        [Fact]
        public void NullIsNotBad()
        {
            ServiceResult.IsBad(null)
                .Should().BeFalse();
        }

        [InlineData(StatusCodes.UncertainReferenceNotDeleted)]
        [InlineData(StatusCodes.UncertainSubNormal)]
        [InlineData(StatusCodes.UncertainLastUsableValue)]
        [InlineData(StatusCodes.UncertainNotAllNodesAvailable)]
        [Theory]
        public void Uncertain(uint sc)
        {
            var result = new ServiceResult(sc);
            ServiceResult.IsUncertain(result)
                .Should().BeTrue();
        }

        [InlineData(StatusCodes.GoodEntryInserted)]
        [InlineData(StatusCodes.GoodCompletesAsynchronously)]
        [InlineData(StatusCodes.BadAttributeIdInvalid)]
        [InlineData(StatusCodes.BadCertificateUntrusted)]
        [Theory]
        public void NotUncertain(uint sc)
        {
            var result = new ServiceResult(sc);
            ServiceResult.IsUncertain(result)
                .Should().BeFalse();
        }
        
        [Fact]
        public void NullIsNotUncertain()
        {
            ServiceResult.IsUncertain(null)
                .Should().BeFalse();
        }

        public static IEnumerable<object[]> ToStringData { get; } = new (ServiceResult, string)[]
        {
            (ServiceResult.Good, "The operation completed successfully."),
            (new ServiceResult(StatusCodes.GoodClamped), "The value written was accepted but was clamped."),
            (new ServiceResult(StatusCodes.GoodClamped, null, "uri", "text"), "The value written was accepted but was clamped. 'text'"),
            (new ServiceResult(StatusCodes.GoodClamped, "symbolicid", null, "text"), "The value written was accepted but was clamped. (symbolicid) 'text'"),
            (new ServiceResult(StatusCodes.GoodClamped, "symbolicid", "uri"), "The value written was accepted but was clamped. (uri:symbolicid)"),
            (new ServiceResult(StatusCodes.GoodClamped, "symbolicid", "uri", "text", "additional"), "The value written was accepted but was clamped. (uri:symbolicid) 'text'"),
            (new ServiceResult(0xBEEF, "symbolicid", "uri", "text", "additional"), "The operation completed successfully. (uri:symbolicid) 'text' [BEEF]")
        }
        .Select(t => new object[] { t.Item1, t.Item2 });

        [MemberData(nameof(ToStringData))]
        [Theory]
        public void ToStringTest(ServiceResult result, string expected)
        {
            result.ToString()
                .Should().Be(expected);
        }
    }
}
