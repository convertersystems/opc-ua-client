using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class ArraySegmentExtensionTests
    {
        [Fact]
        public void AsArraySegment1()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };

            array.AsArraySegment()
                .Should().BeEquivalentTo(array);
        }
        
        [Fact]
        public void AsArraySegmentWithOffset()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };

            array.AsArraySegment(1)
                .Should().BeEquivalentTo(array.Skip(1));
        }
        
        [Fact]
        public void AsArraySegmentWithOffsetAndCount()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };

            array.AsArraySegment(1, 3)
                .Should().BeEquivalentTo(array.Skip(1).Take(3));
        }

        [Fact]
        public void CreateStream()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            using (var stream = ArraySegmentExtensions.CreateStream(array))
            {
                var buffer = new byte[array.Length];

                stream.Length
                    .Should().Be(array.Length);

                stream.Read(buffer, 0, buffer.Length);

                buffer
                    .Should().BeEquivalentTo(array);
            }
        }
        
        [Fact]
        public void CreateBinaryReader()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            using (var reader = ArraySegmentExtensions.CreateBinaryReader(array))
            {
                foreach (var b in array)
                {
                    reader.ReadByte()
                        .Should().Be(b);
                }
            }
        }
        
        [Fact]
        public void CreateBinaryWriter()
        {
            var array = new byte[10];

            using (var writer = ArraySegmentExtensions.CreateBinaryWriter(array))
            {
                for (byte b = 0; b < array.Length; b++)
                {
                    writer.Write(b);
                }

                array
                    .Should().BeEquivalentTo(new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            }
        }

        [Fact]
        public void Take()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            array.AsArraySegment().Take(3)
                .Should().BeEquivalentTo(array.Take(3));
        }
        
        [Fact]
        public void Skip()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            array.AsArraySegment().Skip(3)
                .Should().BeEquivalentTo(array.Skip(3));
        }

        [Fact]
        public void Slice()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            array.AsArraySegment().Slice(3, 4)
                .Should().BeEquivalentTo(array.Skip(3).Take(4));
        }

        [Fact]
        public void TakeLast()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            array.AsArraySegment().TakeLast(3)
                .Should().BeEquivalentTo(array.TakeLast(3));
        }
        
        [Fact]
        public void SkipLast()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            array.AsArraySegment().SkipLast(3)
                .Should().BeEquivalentTo(array.SkipLast(3));
        }

        [Fact]
        public void CopyToArraySegment()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var input = array.AsArraySegment(2, 5);
            var output = new byte[10].AsArraySegment(4, 4);

            ArraySegmentExtensions.CopyTo(input, output);

            output
                .Should().BeEquivalentTo(input.SkipLast(1));
        }
        
        [Fact]
        public void CopyToArray()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var input = array.AsArraySegment(2, 5);
            var output = new byte[10];

            ArraySegmentExtensions.CopyTo(input, output);

            output.Take(5)
                .Should().BeEquivalentTo(input);
        }
        
        [Fact]
        public void ToArray()
        {
            var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var input = array.AsArraySegment(2, 5);

            var output = ArraySegmentExtensions.ToArray(input);

            output
                .Should().BeEquivalentTo(input);
        }
    }
}
