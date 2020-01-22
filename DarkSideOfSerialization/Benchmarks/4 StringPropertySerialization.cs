using System;
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
    public class StringPropertySerialization
    {
        private readonly Test _test = new Test("Hello world!");

        private static readonly Type TargetType = typeof(Test);

        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;

        public StringPropertySerialization()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream, Encoding.Default, true);
            _reader = new BinaryReader(_stream, Encoding.Default, true);
            _writer.Write("Hello world!");
        }

        private const string PropertyName = nameof(Test.StringProperty);
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
        public void WriteViaExpressionTreesGenerated()
        {
            _stream.Position = 0;
            _compiledExpressionTreesWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaExpressionTreesGenerated()
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
        public void WriteViaILGenGenerated()
        {
            _stream.Position = 0;
            _ilGenWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaIlGenGenerated()
        {
            _stream.Position = 0;
            _ilGenRead(_test, _reader);
        }

        private readonly Action<object, BinaryWriter> _createDelegateWrite = DelegateHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _createDelegateRead = DelegateHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaDelegateGenerated()
        {
            _stream.Position = 0;
            _createDelegateWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaDelegateGenerated()
        {
            _stream.Position = 0;
            _createDelegateRead(_test, _reader);
        }

        private readonly Func<object, object> _compiledExpressionTreesGet = CompiledExpressionTreesHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _compiledExpressionTreesSet = CompiledExpressionTreesHelper.GenerateSetter(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaExpressionTreesReflection()
        {
            _stream.Position = 0;
            _writer.Write((string)_compiledExpressionTreesGet(_test));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaExpressionTreesReflection()
        {
            _stream.Position = 0;
            _compiledExpressionTreesSet(_test, _reader.ReadString());
        }

        private readonly Func<object, object> _ilGenGet = ILGenHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _ilGenSet = ILGenHelper.GenerateSetter(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaILGenReflection()
        {
            _stream.Position = 0;
            _writer.Write((string)_ilGenGet(_test));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaIlGenReflection()
        {
            _stream.Position = 0;
            _ilGenSet(_test, _reader.ReadString());
        }

        private readonly Func<object, object> _createDelegateGet = DelegateHelper.GetGetMethod(CachedPropertyInfo);
        private readonly Action<object, object> _createDelegateSet = DelegateHelper.GetSetMethod(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaDelegateReflection()
        {
            _stream.Position = 0;
            _writer.Write((string)_createDelegateGet(_test));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaDelegateReflection()
        {
            _stream.Position = 0;
            _createDelegateSet(_test, _reader.ReadString());
        }
    }
}