using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using DarkSideOfSerialization.Helpers;
using FastMember;
using Types;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace DarkSideOfSerialization.Benchmarks
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [Config(typeof(Config))]
    [SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
    public class StringPropertyAccess
    {
        private sealed class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default.WithUnrollFactor(64));
            }
        }

        private readonly Test _test = new Test("Hello world!");

        private static readonly Type TargetType = typeof(Test);

        private const string PropertyName = nameof(Test.StringProperty);

        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance
                                                          | System.Reflection.BindingFlags.NonPublic
                                                          | System.Reflection.BindingFlags.Public;

        private const string GetCategory = "Get";
        private const string SetCategory = "Set";

        private static readonly PropertyInfo CachedPropertyInfo
                                = TargetType.GetProperty(PropertyName, BindingFlags);

        [BenchmarkCategory(GetCategory), Benchmark(Baseline = true)]
        public string GetViaProperty()
             => _test.StringProperty;

        [BenchmarkCategory(SetCategory), Benchmark(Baseline = true)]
        public void SetViaProperty() => _test.StringProperty = nameof(SetViaProperty);

        [BenchmarkCategory(GetCategory), Benchmark]
        public string GetViaReflection()
        {
            var property = TargetType.GetProperty(PropertyName, BindingFlags);
            Debug.Assert(property != null, nameof(property) + " != null");

            return (string)property.GetValue(_test, null);
        }

        [BenchmarkCategory(SetCategory), Benchmark]
        public void SetViaReflection()
        {
            var property = TargetType.GetProperty(PropertyName, BindingFlags);
            Debug.Assert(property != null, nameof(property) + " != null");

            property.SetValue(_test, nameof(SetViaReflection));
        }

        [BenchmarkCategory(GetCategory), Benchmark]
        public string GetViaReflectionCached()
            => (string)CachedPropertyInfo.GetValue(_test, null);

        [BenchmarkCategory(SetCategory), Benchmark]
        public void SetViaReflectionCached()
            => CachedPropertyInfo.SetValue(_test, nameof(SetViaReflectionCached));


        private readonly TypeAccessor _accessor
                            = TypeAccessor.Create(TargetType, allowNonPublicAccessors: true);

        [BenchmarkCategory(GetCategory), Benchmark]
        public string GetViaFastMember()
            => (string)_accessor[_test, PropertyName];

        [BenchmarkCategory(SetCategory), Benchmark]
        public void SetViaFastMember()
            => _accessor[_test, PropertyName] = nameof(SetViaFastMember);

        private readonly Func<Test, string> _getDelegate
            = (Func<Test, string>)Delegate.CreateDelegate(typeof(Func<Test, string>), CachedPropertyInfo.GetGetMethod(true));

        private readonly Action<Test, string> _setDelegate
            = (Action<Test, string>)Delegate.CreateDelegate(typeof(Action<Test, string>), CachedPropertyInfo.GetSetMethod(true));

        [BenchmarkCategory(GetCategory), Benchmark]
        public void GetViaDelegate() => _getDelegate(_test);

        [BenchmarkCategory(SetCategory), Benchmark]
        public void SetViaDelegate() => _setDelegate(_test, nameof(SetViaDelegate));

        Func<object, string> _ilGenGetter 
            = ILGenHelper.GenerateGetter<string>(CachedPropertyInfo);

        private readonly Action<object, string> _ilGenSetter = ILGenHelper.GenerateSetter<string>(CachedPropertyInfo);

        [BenchmarkCategory(GetCategory), Benchmark]
        public string GetViaILGen()
            => _ilGenGetter(_test);

        [BenchmarkCategory(SetCategory), Benchmark]
        public void SetViaILGen()
            => _ilGenSetter(_test, nameof(SetViaILGen));

        Func<object, string> _compiledExpressionTreesGetter 
            = CompiledExpressionTreesHelper.GenerateGetter<string>(CachedPropertyInfo);

        private readonly Action<object, string> _compiledExpressionTreesSetter = CompiledExpressionTreesHelper.GenerateSetter<string>(CachedPropertyInfo);

        [BenchmarkCategory(GetCategory), Benchmark]
        public string GetViaCompiledExpressionTrees()
            => _compiledExpressionTreesGetter(_test);

        [BenchmarkCategory(SetCategory), Benchmark]
        public void SetViaCompiledExpressionTrees()
            => _compiledExpressionTreesSetter(_test, nameof(SetViaCompiledExpressionTrees));
    }
}