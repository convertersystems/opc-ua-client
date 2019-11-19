using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Workstation.Collections;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class ErrorsContainerTests
    {
        private const string TestProperty1 = "Property1";
        private const string TestProperty2 = "Property2";

        public static IEnumerable<object[]> TestProperties { get; } = new[]
        {
            null,
            "",
            "TestProperty"
        }
        .Select(v => new object[] { v });

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void Create(string property)
        {
            var container = new ErrorsContainer<int>(s => { });

            container.HasErrors
                .Should().BeFalse();

            container.GetErrors(property)
                .Should().BeEmpty();
        }

        [Fact]
        public void CreateNull()
        {
            Action<string> action = null;

            action.Invoking(a => new ErrorsContainer<int>(a))
                .Should().Throw<ArgumentNullException>();
        }

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertSingle(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            called
                .Should().Be(0);

            container.SetErrors(property, new[] { 1 });

            called
                .Should().Be(1);

            container.HasErrors
                .Should().BeTrue();

            container.GetErrors(property)
                .Should().ContainSingle()
                .Which
                .Should().Be(1);
            
            called
                .Should().Be(1);
        }

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertEmpty(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(property, Enumerable.Empty<int>());

            container.HasErrors
                .Should().BeFalse();

            container.GetErrors(property)
                .Should().BeEmpty();
            
            called
                .Should().Be(0);
        }

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertNull(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(property, null);

            container.HasErrors
                .Should().BeFalse();

            container.GetErrors(property)
                .Should().BeEmpty();

            called
                .Should().Be(0);
        }

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertSingleAndRemove(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(property, new[] { 1 });
            container.SetErrors(property, null);

            called
                .Should().Be(2);

            container.HasErrors
                .Should().BeFalse();

            container.GetErrors(property)
                .Should().BeEmpty();
        }

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertSingleAndClear(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(property, new[] { 1 });
            container.ClearErrors(property);

            called
                .Should().Be(2);

            container.HasErrors
                .Should().BeFalse();

            container.GetErrors(property)
                .Should().BeEmpty();
        }

        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertMany(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(property, new[] { 1 });
            container.SetErrors(property, new[] { 2, 3, 6 });

            called
                .Should().Be(2);

            container.HasErrors
                .Should().BeTrue();

            container.GetErrors(property)
                .Should().BeEquivalentTo(new[] { 2, 3, 6 });
        }
        
        [MemberData(nameof(TestProperties))]
        [Theory]
        public void InsertSame(string property)
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(property, new[] { 1, 2, 4 });
            container.SetErrors(property, new[] { 1, 2, 4 });

            called
                .Should().Be(2);

            container.HasErrors
                .Should().BeTrue();

            container.GetErrors(property)
                .Should().BeEquivalentTo(new[] { 1, 2, 4 });
        }

        [Fact]
        public void InsertForTwoProperties()
        {
            var called = 0;
            var container = new ErrorsContainer<int>(_ => called++);

            container.SetErrors(TestProperty1, new[] { 1, 2, 4 });
            container.SetErrors(TestProperty2, new[] { 5, 6 });

            called
                .Should().Be(2);

            container.HasErrors
                .Should().BeTrue();

            container.GetErrors(TestProperty1)
                .Should().BeEquivalentTo(new[] { 1, 2, 4 });
            container.GetErrors(TestProperty2)
                .Should().BeEquivalentTo(new[] { 5, 6 });
        }
    }
}
