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

        public static IEnumerable<Func<QualifiedName>> QualifiedNames { get; } = new Func<QualifiedName>[]
        {
            () => new QualifiedName("A"),
            () => new QualifiedName("B"),
            () => new QualifiedName("A", 2),
            () => new QualifiedName("B", 2),
        };

        public static IEnumerable<object[]> ValueEqualityData =>
            from a in QualifiedNames.Select((f, i) => (id: f(), index: i))
            from b in QualifiedNames.Select((f, i) => (id: f(), index: i))
            select new object[] { a.id, b.id, a.index == b.index };

        public static IEnumerable<object[]> ReferenceEqualityData
        {
            get
            {
                var list = QualifiedNames.Select((f, i) => (id: f(), index: i)).ToList();
                return from a in list
                       from b in list
                       select new object[] { a.id, b.id, a.index == b.index };
            }
        }

        [MemberData(nameof(ValueEqualityData))]
        [MemberData(nameof(ReferenceEqualityData))]
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
