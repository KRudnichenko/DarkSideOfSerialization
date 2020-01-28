using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using DarkSideOfSerialization.Helpers;
using DarkSideOfSerialization.Proto;
using Google.Protobuf;

namespace DarkSideOfSerialization.Benchmarks
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ProtoVsCustomSerialization
    {
        private readonly MemoryStream _stream;
        private readonly MemoryStream _streamForProto;
        private readonly MemoryStream _streamForILGen;

        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;

        private readonly BinaryWriter _writerForILGen;
        private readonly BinaryReader _readerForILGen;

        public ProtoVsCustomSerialization()
        {
            _streamForProto = new MemoryStream();
            new Test {IntProperty = 42, StringProperty = "Hello!"}.WriteTo(_streamForProto);

            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream, Encoding.Default, true);
            _reader = new BinaryReader(_stream, Encoding.Default, true);
            _writer.Write("Hello!");
            _writer.Write(42);


            _streamForILGen = new MemoryStream();
            _writerForILGen = new BinaryWriter(_streamForILGen, Encoding.Default, true);
            _readerForILGen = new BinaryReader(_streamForILGen, Encoding.Default, true);

            _write(_protoTest, _writerForILGen);

            _fmtStream = new MemoryStream();
            _formatter.Serialize(_fmtStream, _test);
        }

        private readonly Test _protoTest = new Test {IntProperty = 45, StringProperty = "Hello!"};
        private readonly Types.Test _test = new Types.Test(42) { StringProperty = "Hello!" };

        [Benchmark, BenchmarkCategory("Write")]
        public void WriteProto()
        {
            _streamForProto.Position = 0;
            _protoTest.WriteTo(_streamForProto);
        }

        [Benchmark, BenchmarkCategory("Read")]
        public Test ReadProto()
        {
            _streamForProto.Position = 0;
            return Test.Parser.ParseFrom(_streamForProto);
        }


        [Benchmark(Baseline = true), BenchmarkCategory("Write")]
        public void WriteViaProperty()
        {
            _stream.Position = 0;

            _writer.Write(_protoTest.StringProperty);
            _writer.Write(_protoTest.IntProperty);
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Read")]
        public void ReadViaProperty()
        {
            _stream.Position = 0;
            _protoTest.StringProperty = _reader.ReadString();
            _protoTest.IntProperty= _reader.ReadInt32();
            
        }

        private static readonly PropertyInfo[] Properties = typeof(Test).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private readonly Action<object, BinaryWriter> _write = CompiledExpressionTreesHelper.GenerateWrite(Properties);
        private readonly Action<object, BinaryReader> _read = CompiledExpressionTreesHelper.GenerateRead(Properties);

        private readonly BinaryFormatter _formatter = new BinaryFormatter();
        private readonly MemoryStream _fmtStream;

        [Benchmark, BenchmarkCategory("Write")]
        public void WriteViaExpressionTrees()
        {
            _streamForILGen.Position = 0;

            _write(_protoTest, _writerForILGen);
        }

        [Benchmark, BenchmarkCategory("Read")]
        public void ReadViaExpressionTrees()
        {
            _streamForILGen.Position = 0;

            _read(_protoTest, _readerForILGen);
        }

        [Benchmark, BenchmarkCategory("Write")]
        public void WriteViaBinaryFormatter()
        {
            _fmtStream.Position = 0;
            _formatter.Serialize(_fmtStream, _test);
        }

        [Benchmark, BenchmarkCategory("Read")]
        public Types.Test ReadViaBinaryFormatter()
        {
            _fmtStream.Position = 0;

            return (Types.Test) _formatter.Deserialize(_fmtStream);
        }
    }
}
