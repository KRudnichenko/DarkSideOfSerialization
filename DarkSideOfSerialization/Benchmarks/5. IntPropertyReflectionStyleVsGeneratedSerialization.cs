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
    public class IntPropertyReflectionStyleVsGeneratedSerialization
    {
        private readonly Test _test = new Test(43);

        private static readonly Type TargetType = typeof(Test);

        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;

        public IntPropertyReflectionStyleVsGeneratedSerialization()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream, Encoding.Default, true);
            _reader = new BinaryReader(_stream, Encoding.Default, true);
            _writer.Write("Hello world!");
        }

        private const string PropertyName = nameof(Test.IntProperty);
        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance
                                                  | System.Reflection.BindingFlags.NonPublic
                                                  | System.Reflection.BindingFlags.Public;

        private static readonly PropertyInfo CachedPropertyInfo
            = TargetType.GetProperty(PropertyName, BindingFlags);

        [Benchmark, BenchmarkCategory("Property.Write")]
        public void WriteViaProperty()
        {
            _stream.Position = 0;
            _writer.Write(_test.IntProperty);
        }

        [Benchmark, BenchmarkCategory("Property.Read")]
        public void ReadViaProperty()
        {
            _stream.Position = 0;
            _test.IntProperty = _reader.ReadInt32();
        }

        [Benchmark, BenchmarkCategory("Reflection.Write")]
        public void WriteViaReflection()
        {
            _stream.Position = 0;
            _writer.Write((int)CachedPropertyInfo.GetValue(_test, null));
        }

        [Benchmark, BenchmarkCategory("Reflection.Read")]
        public void ReadViaReflection()
        {
            _stream.Position = 0;
            CachedPropertyInfo.SetValue(_test, _reader.ReadInt32());
        }

        private readonly Func<object, object> _compiledExpressionTreesGet = CompiledExpressionTreesHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _compiledExpressionTreesSet = CompiledExpressionTreesHelper.GenerateSetter(CachedPropertyInfo);

        private readonly Action<object, BinaryWriter> _compiledExpressionTreesWrite = CompiledExpressionTreesHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _compiledExpressionTreesRead = CompiledExpressionTreesHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark(Baseline = true), BenchmarkCategory("ET.Write")]
        public void WriteViaExpressionTreesReflection()
        {
            _stream.Position = 0;
            _writer.Write((int)_compiledExpressionTreesGet(_test));
        }

        [Benchmark(Baseline = true), BenchmarkCategory("ET.Read")]
        public void ReadViaExpressionTreesReflection()
        {
            _stream.Position = 0;
            _compiledExpressionTreesSet(_test, _reader.ReadInt32());
        }

        [Benchmark, BenchmarkCategory("ET.Write")]
        public void WriteViaExpressionTreesGenerated()
        {
            _stream.Position = 0;
            _compiledExpressionTreesWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory("ET.Read")]
        public void ReadViaExpressionTreesGenerated()
        {
            _stream.Position = 0;
            _compiledExpressionTreesRead(_test, _reader);
        }

        private readonly TypeAccessor _accessor
            = TypeAccessor.Create(TargetType, allowNonPublicAccessors: true);

        [Benchmark, BenchmarkCategory("FastMember.Write")]
        public void WriteViaFastMember()
        {
            _stream.Position = 0;
            _writer.Write((int)_accessor[_test, PropertyName]);
        }

        [Benchmark, BenchmarkCategory("FastMember.Read")]
        public void ReadViaFastMember()
        {
            _stream.Position = 0;
            _accessor[_test, PropertyName] = _reader.ReadInt32();
        }

        private readonly Func<object, object> _ilGenGet = ILGenHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _ilGenSet = ILGenHelper.GenerateSetter(CachedPropertyInfo);
        private readonly Action<object, BinaryWriter> _ilGenWrite = ILGenHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _ilGenRead = ILGenHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark(Baseline = true), BenchmarkCategory("IL.Write")]
        public void WriteViaILGenReflection()
        {
            _stream.Position = 0;
            _writer.Write((int)_ilGenGet(_test));
        }

        [Benchmark(Baseline = true), BenchmarkCategory("IL.Read")]
        public void ReadViaIlGenReflection()
        {
            _stream.Position = 0;
            _ilGenSet(_test, _reader.ReadInt32());
        }

        [Benchmark, BenchmarkCategory("IL.Write")]
        public void WriteViaILGenGenerated()
        {
            _stream.Position = 0;
            _ilGenWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory("IL.Read")]
        public void ReadViaIlGenGenerated()
        {
            _stream.Position = 0;
            _ilGenRead(_test, _reader);
        }

        private readonly Func<object, object> _createDelegateGet = DelegateHelper.GetGetMethod(CachedPropertyInfo);
        private readonly Action<object, object> _createDelegateSet = DelegateHelper.GetSetMethod(CachedPropertyInfo);
        private readonly Action<object, BinaryWriter> _createDelegateWrite = DelegateHelper.GenerateWrite(CachedPropertyInfo);
        private readonly Action<object, BinaryReader> _createDelegateRead = DelegateHelper.GenerateRead(CachedPropertyInfo);

        [Benchmark(Baseline = true), BenchmarkCategory("Delegate.Write")]
        public void WriteViaDelegateReflection()
        {
            _stream.Position = 0;
            _writer.Write((int)_createDelegateGet(_test));
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Delegate.Read")]
        public void ReadViaDelegateReflection()
        {
            _stream.Position = 0;
            _createDelegateSet(_test, _reader.ReadInt32());
        }

        [Benchmark, BenchmarkCategory("Delegate.Write")]
        public void WriteViaDelegateGenerated()
        {
            _stream.Position = 0;
            _createDelegateWrite(_test, _writer);
        }

        [Benchmark, BenchmarkCategory("Delegate.Read")]
        public void ReadViaDelegateGenerated()
        {
            _stream.Position = 0;
            _createDelegateRead(_test, _reader);
        }
    }
}