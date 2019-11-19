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

        public static IEnumerable<StatusCode> Codes => new uint[]
        {
            0,
            42,
            43
        }
        .Select(s => new StatusCode(s));

        public static IEnumerable<object[]> ValueEqualityData =>
            from a in Codes.Select((s, i) => (code: s, index: i))
            from b in Codes.Select((s, i) => (code: s, index: i))
            select new object[] { a.code, b.code, a.index == b.index };

        [MemberData(nameof(ValueEqualityData))]
        [Theory]
        public void Equality(StatusCode a, StatusCode b, bool shouldBeEqual)
        {
            if (shouldBeEqual)
            {
                // Should().Be() is using Equal(object)
                a
                    .Should().Be(b);
                a
                    .Should().NotBe(5);

                // Test Equal(NodeId)
                a.Equals(b)
                    .Should().BeTrue();

                // operator
                (a == b)
                    .Should().BeTrue();
                (a != b)
                    .Should().BeFalse();

                a.GetHashCode()
                    .Should().Be(b.GetHashCode());
            }
            else
            {
                // Should().Be() is using Equal(object)
                a
                    .Should().NotBe(b);
                a
                    .Should().NotBe(5);

                // Test Equal(NodeId)
                a.Equals(b)
                    .Should().BeFalse();

                // operator
                (a != b)
                    .Should().BeTrue();
                (a == b)
                    .Should().BeFalse();

                // This is technically not required but the current
                // implementation fulfills this. If this should ever
                // fail it could be bad luck or the the implementation
                // is really broken.
                a.GetHashCode()
                    .Should().NotBe(b.GetHashCode());
            }
        }

        public static IEnumerable<object[]> EqualityNullData =>
            Codes.Select(c => new object[] { c });

        [MemberData(nameof(EqualityNullData))]
        [Theory]
        public void EqualityNull(StatusCode val)
        {
            // This is using Equals(object)
            val.Should()
                .NotBeNull();
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
        [InlineData(StatusCodes.UncertainDominantValueChanged)]
        [InlineData(StatusCodes.GoodDependentValueChanged)]
        [InlineData(StatusCodes.BadDominantValueChanged)]
        [InlineData(StatusCodes.UncertainDependentValueChanged)]
        [InlineData(StatusCodes.BadDependentValueChanged)]
        [Theory]
        public void NotSemanticsChanged(uint sc)
        {
            StatusCode.IsSemanticsChanged(sc)
                .Should().BeFalse();
        }

        [InlineData(StatusCodes.UncertainDominantValueChanged)]
        [InlineData(StatusCodes.GoodDependentValueChanged)]
        [InlineData(StatusCodes.BadDominantValueChanged)]
        [InlineData(StatusCodes.UncertainDependentValueChanged)]
        [InlineData(StatusCodes.BadDependentValueChanged)]
        [Theory]
        public void NotStructureChanged(uint sc)
        {
            StatusCode.IsStructureChanged(sc)
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
