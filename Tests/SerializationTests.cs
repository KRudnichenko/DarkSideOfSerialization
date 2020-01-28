using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using DarkSideOfSerialization.Helpers;
using DarkSideOfSerialization.Proto;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public sealed class SerializationTests
    {
        private readonly ITestOutputHelper _output;

        public SerializationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CompiledExpressionTreesHelperTest()
        {
            var type = typeof(Test);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var write = CompiledExpressionTreesHelper.GenerateWrite(properties);
            var read = CompiledExpressionTreesHelper.GenerateRead(properties);

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, Encoding.Default, true);
            var reader = new BinaryReader(stream, Encoding.Default, true);

            var test = new Test
            {
                IntProperty =  42, 
                StringProperty = "Hello, world!"
            };

            write(test,  writer);

            _output.WriteLine($"ExpressionTrees: {stream.Length}");

            stream.Position = 0;
            var result = new Test();
            read(result, reader);

            Assert.Equal(42, result.IntProperty);
            Assert.Equal("Hello, world!", result.StringProperty);
        }

        [Fact]
        public void ProtobufTest()
        {
            Test _protoTest = new Test { IntProperty = 42, StringProperty = "Hello, world!" };
            var _streamForProto = new MemoryStream();
            _protoTest.WriteTo(_streamForProto);

            _output.WriteLine($"Proto: {_streamForProto.Length}");

            _streamForProto.Position = 0;
            var result = Test.Parser.ParseFrom(_streamForProto);
            Assert.Equal(42, result.IntProperty);
            Assert.Equal("Hello, world!", result.StringProperty);
        }

        [Fact]
        public void BinaryFormatterTest()
        {
            var formatter = new BinaryFormatter();

            var stream = new MemoryStream();
            var test = new Types.Test(42) { StringProperty = "Hello, world!" };
            formatter.Serialize(stream, test);

            _output.WriteLine($"BinaryFormatter: {stream.Length}");

            stream.Position = 0;
            var result = (Types.Test)formatter.Deserialize(stream);
            Assert.Equal(42, result.IntProperty);
            Assert.Equal("Hello, world!", result.StringProperty);
        }
    }
}
