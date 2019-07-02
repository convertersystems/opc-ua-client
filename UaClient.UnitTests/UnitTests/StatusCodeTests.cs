using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class StatusCodeTests
    {
        [Fact]
        public void Constructor()
        {
            var sc = new StatusCode(42);

            sc.Value
                .Should().Be(42);
        }

        [Fact]
        public void ImplicitConversionToStatusCode()
        {
            StatusCode sc = 42;

            sc.Value
                .Should().Be(42);
        }

        [Fact]
        public void ImplicitConversionToUint()
        {
            uint sc = new StatusCode(42);

            sc
                .Should().Be(42);
        }

        [Fact]
        public void Equality()
        {
            var a = new StatusCode(42);
            var b = a;

            b
                .Should().Be(a);
        }

        [Fact]
        public void Unequality()
        {
            var a = new StatusCode(42);
            var b = new StatusCode(43);

            b
                .Should().NotBe(a);
        }

        [InlineData(StatusCodes.Good)]
        [InlineData(StatusCodes.GoodCallAgain)]
        [InlineData(StatusCodes.GoodResultsMayBeIncomplete)]
        [InlineData(StatusCodes.GoodSubscriptionTransferred)]
        [Theory]
        public void Good(uint sc)
        {
            StatusCode.IsGood(sc)
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
            StatusCode.IsGood(sc)
                .Should().BeFalse();
        }

        [InlineData(StatusCodes.BadBrowseDirectionInvalid)]
        [InlineData(StatusCodes.BadAggregateConfigurationRejected)]
        [InlineData(StatusCodes.BadDataLost)]
        [InlineData(StatusCodes.BadNoData)]
        [Theory]
        public void Bad(uint sc)
        {
            StatusCode.IsBad(sc)
                .Should().BeTrue();
        }

        [InlineData(StatusCodes.GoodCallAgain)]
        [InlineData(StatusCodes.GoodShutdownEvent)]
        [InlineData(StatusCodes.UncertainInitialValue)]
        [InlineData(StatusCodes.UncertainSubstituteValue)]
        [Theory]
        public void NotBad(uint sc)
        {
            StatusCode.IsBad(sc)
                .Should().BeFalse();
        }

        [InlineData(StatusCodes.UncertainReferenceNotDeleted)]
        [InlineData(StatusCodes.UncertainSubNormal)]
        [InlineData(StatusCodes.UncertainLastUsableValue)]
        [InlineData(StatusCodes.UncertainNotAllNodesAvailable)]
        [Theory]
        public void Uncertain(uint sc)
        {
            StatusCode.IsUncertain(sc)
                .Should().BeTrue();
        }

        [InlineData(StatusCodes.GoodEntryInserted)]
        [InlineData(StatusCodes.GoodCompletesAsynchronously)]
        [InlineData(StatusCodes.BadAttributeIdInvalid)]
        [InlineData(StatusCodes.BadCertificateUntrusted)]
        [Theory]
        public void NotUncertain(uint sc)
        {
            StatusCode.IsUncertain(sc)
                .Should().BeFalse();
        }

        [Fact]
        public void TestToString()
        {
            var sc = new StatusCode(0x0000_1234);

            sc.ToString()
                .Should().Be("0x00001234");
        }
    }
}
