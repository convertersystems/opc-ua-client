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
    public class LocalizedTextTests
    {
        public static IEnumerable<Func<LocalizedText>> LocalizedTexts { get; } = new Func<LocalizedText>[]
            {
                () => new LocalizedText("un po' di testo", "it-IT"),
                () => new LocalizedText("some text", "en-US"),
                () => new LocalizedText("etwas Text", "de-DE"),
                () => new LocalizedText("some text"),
                () => new LocalizedText("some text", null),
                () => new LocalizedText("", "en-US"),
                () => new LocalizedText(null, "en-US"),
                () => new LocalizedText(null),
                () => new LocalizedText(null, null)
            };

        public static IEnumerable<object[]> ValueEqualityData =>
            from a in LocalizedTexts.Select((f, i) => (text: f(), index: i))
            from b in LocalizedTexts.Select((f, i) => (text: f(), index: i))
            select new object[] { a.text, b.text, a.index == b.index };

        public static IEnumerable<object[]> ReferenceEqualityData
        {
            get
            {
                var list = LocalizedTexts.Select((f, i) => (text: f(), index: i)).ToList();
                return from a in list
                    from b in list
                    select new object[] { a.text, b.text, a.index == b.index };
            }
        }

        [MemberData(nameof(ValueEqualityData))]
        [MemberData(nameof(ReferenceEqualityData))]
        [Theory]
        public void Equality(LocalizedText a, LocalizedText b, bool shouldBeEqual)
        {
            if (shouldBeEqual)
            {
                a
                    .Should().Be(b);
                a.GetHashCode()
                    .Should().Be(b.GetHashCode());
            }
            else
            {
                a.Should()
                    .NotBe(b);

                // This is technically not required but the current
                // implementation fulfills this. If this should ever
                // fail it could be bad luck or the the implementation
                // is really broken.
                a.GetHashCode()
                    .Should().NotBe(b.GetHashCode());
            }
        }
    }
}
