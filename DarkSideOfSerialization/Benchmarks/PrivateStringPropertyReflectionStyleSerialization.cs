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
    public class PrivateStringPropertyReflectionStyleSerialization
    {
        private readonly Test _test = new Test("Hello world!");

        private static readonly Type TargetType = typeof(Test);

        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;

        public PrivateStringPropertyReflectionStyleSerialization()
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

        private readonly Func<object, object> _compiledExpressionTreesGet = CompiledExpressionTreesHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _compiledExpressionTreesSet = CompiledExpressionTreesHelper.GenerateSetter(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaExpressionTrees()
        {
            _stream.Position = 0;
            _writer.Write((string)_compiledExpressionTreesGet(_test));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaExpressionTrees()
        {
            _stream.Position = 0;
            _compiledExpressionTreesSet(_test, _reader.ReadString());
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

        private readonly Func<object, object> _ilGenGet = ILGenHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _ilGenSet = ILGenHelper.GenerateSetter(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaILGen()
        {
            _stream.Position = 0;
            _writer.Write((string)_ilGenGet(_test));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaIlGen()
        {
            _stream.Position = 0;
            _ilGenSet(_test, _reader.ReadString());
        }

        private readonly Func<object, object> _createDelegateGet = DelegateHelper.GetGetMethod(CachedPropertyInfo);
        private readonly Action<object, object> _createDelegateSet = DelegateHelper.GetSetMethod(CachedPropertyInfo);

        [Benchmark, BenchmarkCategory(WriteCategory)]
        public void WriteViaDelegate()
        {
            _stream.Position = 0;
            _writer.Write((string)_createDelegateGet(_test));
        }

        [Benchmark, BenchmarkCategory(ReadCategory)]
        public void ReadViaDelegate()
        {
            _stream.Position = 0;
            _createDelegateSet(_test, _reader.ReadString());
        }
    }
}