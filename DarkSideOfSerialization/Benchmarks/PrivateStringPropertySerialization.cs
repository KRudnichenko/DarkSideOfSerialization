using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using DarkSideOfSerialization.Helpers;
using FastMember;
using Types;

namespace DarkSideOfSerialization.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
    public class PrivateStringPropertySerialization
    {
        private readonly Test _test = new Test("Hello world!");

        private static readonly Type TargetType = typeof(Test);

        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;

        public PrivateStringPropertySerialization()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream, Encoding.Default, true);
            _reader = new BinaryReader(_stream, Encoding.Default, true);
            _writer.Write("Hello world!");
        }

        private const string PropertyName = "PrivateStringProperty";
        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance
                                                  | System.Reflection.BindingFlags.NonPublic
                                                  | System.Reflection.BindingFlags.Public;

        private const string WriteCategory = "Write";
        private const string ReadCategory = "Read";

        private static readonly PropertyInfo CachedPropertyInfo
            = TargetType.GetProperty(PropertyName, BindingFlags);

        private readonly Action<object, BinaryWriter> _compiledExpressionTreesWrite = CompiledExpressionTreesHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _compiledExpressionTreesRead = CompiledExpressionTreesHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaReflection()
        {
            _stream.Position = 0;
            _writer.Write((string)CachedPropertyInfo.GetValue(_test, null));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaReflection()
        {
            _stream.Position = 0;
            CachedPropertyInfo.SetValue(_test, _reader.ReadString());
        }

        [Benchmark(Baseline = true), BenchmarkCategory(WriteCategory)]
        public void WriteViaProperty()
        {
            _stream.Position = 0;
            _writer.Write(_test.StringProperty);
        }

        [Benchmark(Baseline = true), BenchmarkCategory(ReadCategory)]
        public void ReadViaProperty()
        {
            _stream.Position = 0;
            _test.StringProperty = _reader.ReadString();
        }

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaExpressionTrees()
        {
            _stream.Position = 0;
            _compiledExpressionTreesWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaExpressionTrees()
        {
            _stream.Position = 0;
            _compiledExpressionTreesRead(_test, _reader);
        }

        private readonly TypeAccessor _accessor
            = TypeAccessor.Create(TargetType, allowNonPublicAccessors: true);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaFastMember()
        {
            _stream.Position = 0;
            _writer.Write((string)_accessor[_test, PropertyName]);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaFastMember()
        {
            _stream.Position = 0;
            _accessor[_test, PropertyName] = _reader.ReadString();
        }


        private readonly Action<object, BinaryWriter> _ilGenWrite = ILGenHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _ilGenRead = ILGenHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaILGen()
        {
            _stream.Position = 0;
            _ilGenWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaIlGen()
        {
            _stream.Position = 0;
            _ilGenRead(_test, _reader);
        }

        private readonly Action<object, BinaryWriter> _createDelegateWrite = DelegateHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _createDelegateRead = DelegateHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaDelegate()
        {
            _stream.Position = 0;
            _createDelegateWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaDelegate()
        {
            _stream.Position = 0;
            _createDelegateRead(_test, _reader);
        }
    }
}