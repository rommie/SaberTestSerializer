using System;
using Xunit;
using Xunit.Abstractions;
using SerializerTests.Nodes;
using SerializerTests.Implementations;
using System.IO;
using System.Threading.Tasks;
using SerializerTests.Interfaces;
using System.Text;

namespace SerializerTests.Test
{
    public class RomanCherepanovSerializerTests: IClassFixture<ListNodeFixture>
    {
        private ListNodeFixture _fixture;

        private IListSerializer _serializer = new RomanCherepanovSerializer();

        public RomanCherepanovSerializerTests(ListNodeFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ListNodeSerialization_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _serializer.Serialize(null, Stream.Null));
        }

        [Fact]
        public async Task ListNodeDerialization_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _serializer.Deserialize(null));
        }

        [Fact]
        public async Task ListNodeSerialization_NoThrows()
        {
            using var stream = new MemoryStream();
            var list = _fixture.GenerateList(1000);

            await _serializer.Serialize(list, stream);
        }

        [Fact]
        public async Task ListNodeSerializationHugeStrings_NoThrows()
        {
            using var stream = new MemoryStream();
            var list = _fixture.GenerateList(100, true);

            await _serializer.Serialize(list, stream);
        }

        [Theory]
        [InlineData(@"[]")]
        [InlineData(@"[{""id"":0, ""data"":""Test""}]")]
        public async Task ListNodeDeserialization_NoThrows(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            await _serializer.Deserialize(stream);
        }

        [Theory]
        [InlineData(@"[{""id"":0, ""data"":""Test""}, {""id"":1, ""data"":""Test2""}, {""id"":2, ""data"":""Test2"", ""ref"": 0}, {""id"":3, ""data"":""Test3""}]", 2, "Test2")]
        [InlineData(@"[{""id"":0, ""data"":""Test""}, {""id"":1, ""data"":""Test2""}, {""id"":2, ""data"":""Test2"", ""ref"": 0}, {""id"":3}]", 3, null)]
        public async Task ListNodeDeserialization_CorrectData(string json, int nodeShift, string expectedValue)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            var node = await _serializer.Deserialize(stream);

            Assert.NotNull(node);

            node = node.MoveNext(nodeShift);

            Assert.Equal(expectedValue, node.Data);
        }

        [Theory]
        [InlineData(@"[{""id"":0, ""data"":""Test""}, {""id"":1, ""data"":""Test2""}, {""id"":2, ""data"":""Test3"", ""ref"": 1}, {""id"":3, ""data"":""Test4""}]", 3, 2)]
        public async Task ListNodeDeserialization_CorrectRefs(string json, int nodeNumber, int shouldRefToNodeNumber)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            var node = await _serializer.Deserialize(stream);

            var nodeForTest = node.MoveNext(nodeNumber - 1);
            var nodeTarget = node.MoveNext(shouldRefToNodeNumber - 1);

            Assert.Same(nodeForTest.Random, nodeTarget);

        }


        [Theory]
        [InlineData("")]
        [InlineData(@"[{""id"":0, ""data"":""Test""}, {""id"":2, ""data"":""Test2"", ""ref"": 4}, {""id"":3, ""data"":""Test3""}]")]
        [InlineData(@"[{""id"":0, ""data"":""Test""}, {""id"":1, ""data"": { ""embedded"": ""data""}}, {""id"":2, ""data"":""Test2"", ""ref"": 0}, {""id"":3}]")]
        [InlineData(@"[{""id"":0, ""data"":""Test""}, ")]
        [InlineData(@"[{""id"":0}, {""id"":0}")]
        [InlineData(@"[{""id"":1}, {""id"":0}")]
        public async Task ListNodeDeserialization_ThrowsArgumentException(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            await Assert.ThrowsAsync<ArgumentException>(() => _serializer.Deserialize(stream));
        }

        [Fact]
        public async Task ListNodeDeepCopy_DifferentRefsSameData()
        {
            var list = _fixture.SampleList;

            var copy = await _serializer.DeepCopy(list);

            while (list != null && copy != null)
            {
                Assert.NotSame(list, copy);
                Assert.Equal(list.Data, copy.Data);

                if (list.Random != null)
                {
                    Assert.NotSame(list.Random, copy.Random);
                }

                list = list.Next;
                copy = copy.Next;
            }
        }
    }
}