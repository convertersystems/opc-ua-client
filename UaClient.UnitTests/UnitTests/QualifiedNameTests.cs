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
            () => new QualifiedName(null),
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
            {
                // Should().Be() is using Equal(object)
                a
                    .Should().Be(b);
                a
                    .Should().NotBe(5);

                // Test Equal(QualifiedName)
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

                // Test Equal(QualifiedName)
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
            QualifiedNames.Select(id => new[] { id() });

        [MemberData(nameof(EqualityNullData))]
        [Theory]
        public void EqualityNull(QualifiedName val)
        {
            (val == null)
                .Should().BeFalse();
            (val != null)
                .Should().BeTrue();
            (null == val)
                .Should().BeFalse();
            (null != val)
                .Should().BeTrue();

            // This is using Equals(object)
            val.Should()
                .NotBeNull();

            val.Equals((QualifiedName)null)
                .Should().BeFalse();
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
            Action act = () => QualifiedName.Parse(text);
            act.Should().Throw<ArgumentException>();
        }
    }
}
