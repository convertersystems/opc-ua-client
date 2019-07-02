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
    public class QualifiedNameTests
    {
        [Fact]
        public void Constructor()
        {
            var qn = new QualifiedName("TestName", 2);

            qn.Name
                .Should().Be("TestName");

            qn.NamespaceIndex
                .Should().Be(2);
        }

        [Fact]
        public void ConstructorDefaultValues()
        {
            var qn = new QualifiedName("TestName");

            qn.Name
                .Should().Be("TestName");

            qn.NamespaceIndex
                .Should().Be(0);
        }

        public static IEnumerable<QualifiedName> QualifiedNames { get; } = new[]
        {
            new QualifiedName("A"),
            new QualifiedName("B"),
            new QualifiedName("A", 2),
            new QualifiedName("B", 2),
        }
        .ToList();

        public static IEnumerable<object[]> EqualityData =>
            from a in QualifiedNames.Select((n, i) => (name: n, index: i))
            from b in QualifiedNames.Select((n, i) => (name: n, index: i))
            select new object[] { a.name, b.name, a.index == b.index };

        [MemberData(nameof(EqualityData))]
        [Theory]
        public void Equality(QualifiedName a, QualifiedName b, bool shouldBeEqual)
        {
            if (shouldBeEqual)
                a.Should().Be(b);
            else
                a.Should().NotBe(b);
        }

        [InlineData("0:ABC", 0, "ABC")]
        [InlineData("ABC", 0, "ABC")]
        [InlineData("2:ABC", 2, "ABC")]
        [Theory]
        public void Parse(string text, ushort ns, string name)
        {
            QualifiedName.Parse(text)
                .Should().Be(new QualifiedName(name, ns));
        }

        [InlineData(null)]
        [InlineData("c:foo")]
        [InlineData(":foo")]
        [InlineData(" :foo")]
        [Theory]
        public void NotParsable(string text)
        {
            text.Invoking(t => QualifiedName.Parse(t))
                .Should().Throw<ArgumentException>();
        }
    }
}
