using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using DarkSideOfSerialization.Helpers;
using FastMember;
using Types;

namespace DarkSideOfSerialization.Benchmarks
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [Config(typeof(Config))]
    [SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
    public class StringPropertyTypedVsReflectionStyleAccess
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

        private static readonly PropertyInfo CachedPropertyInfo
            = TargetType.GetProperty(PropertyName, BindingFlags);

        [BenchmarkCategory("Reflection.Get"), Benchmark]
        public object GetViaReflectionCached()
            => CachedPropertyInfo.GetValue(_test, null);

        [BenchmarkCategory("Reflection.Set"), Benchmark]
        public void SetViaReflectionCached()
            => CachedPropertyInfo.SetValue(_test, nameof(SetViaReflectionCached));

        private readonly TypeAccessor _accessor
            = TypeAccessor.Create(TargetType, allowNonPublicAccessors: true);

        [BenchmarkCategory("FastMember.Set"), Benchmark]
        public object GetViaFastMember()
            => _accessor[_test, PropertyName];

        [BenchmarkCategory("FastMember.Get"), Benchmark]
        public void SetViaFastMember()
            => _accessor[_test, PropertyName] = nameof(SetViaFastMember);

        private readonly Func<object, object> _getDelegate = DelegateHelper.GetGetMethod(CachedPropertyInfo);
        private readonly Action<object, object> _setDelegate = DelegateHelper.GetSetMethod(CachedPropertyInfo);

        private readonly Func<Test, string> _getDelegateTyped
            = (Func<Test, string>)Delegate.CreateDelegate(typeof(Func<Test, string>), CachedPropertyInfo.GetGetMethod(true));

        private readonly Action<Test, string> _setDelegateTyped
            = (Action<Test, string>)Delegate.CreateDelegate(typeof(Action<Test, string>), CachedPropertyInfo.GetSetMethod(true));

        [BenchmarkCategory("Delegate.Get"), Benchmark]
        public object GetViaDelegateReflectionStyle()
            => _getDelegate(_test);

        [BenchmarkCategory("Delegate.Set"), Benchmark]
        public void SetViaDelegateReflectionStyle()
            => _setDelegate(_test, nameof(SetViaDelegateReflectionStyle));

        [BenchmarkCategory("Delegate.Get"), Benchmark(Baseline = true)]
        public object GetViaDelegateTyped()
            => _getDelegateTyped(_test);

        [BenchmarkCategory("Delegate.Set"), Benchmark(Baseline = true)]
        public void SetViaDelegateTyped()
            => _setDelegateTyped(_test, nameof(SetViaDelegateReflectionStyle));

        private readonly Func<object, object> _ilGenGetter = ILGenHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _ilGenSetter = ILGenHelper.GenerateSetter(CachedPropertyInfo);
        private readonly Func<Test, string> _ilGenGetterTyped = ILGenHelper.GenerateGetter<Test, string>(CachedPropertyInfo);
        private readonly Action<Test, string> _ilGenSetterTyped = ILGenHelper.GenerateSetter<Test, string>(CachedPropertyInfo);

        [BenchmarkCategory("ILGen.Get"), Benchmark]
        public object GetViaILGenReflectionStyle()
            => _ilGenGetter(_test);

        [BenchmarkCategory("ILGen.Set"), Benchmark]
        public void SetViaILGenReflectionStyle()
            => _ilGenSetter(_test, nameof(SetViaILGenReflectionStyle));

        [BenchmarkCategory("ILGen.Get"), Benchmark(Baseline = true)]
        public object GetViaILGenTyped()
            => _ilGenGetterTyped(_test);

        [BenchmarkCategory("ILGen.Set"), Benchmark(Baseline = true)]
        public void SetViaILGenTyped()
            => _ilGenSetterTyped(_test, nameof(SetViaILGenReflectionStyle));

        private readonly Func<object, object> _compiledExpressionTreesGetter = CompiledExpressionTreesHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _compiledExpressionTreesSetter = CompiledExpressionTreesHelper.GenerateSetter(CachedPropertyInfo);
        private readonly Func<Test, string> _compiledExpressionTreesGetterTyped = CompiledExpressionTreesHelper.GenerateGetter<Test, string>(CachedPropertyInfo);
        private readonly Action<Test, string> _compiledExpressionTreesSetterTyped = CompiledExpressionTreesHelper.GenerateSetter<Test, string>(CachedPropertyInfo);

        [BenchmarkCategory("ET.Get"), Benchmark]
        public object GetViaCompiledExpressionTreesReflectionStyle()
            => _compiledExpressionTreesGetter(_test);

        [BenchmarkCategory("ET.Set"), Benchmark]
        public void SetViaCompiledExpressionTreesReflectionStyle()
            => _compiledExpressionTreesSetter(_test, nameof(SetViaCompiledExpressionTreesReflectionStyle));


        [BenchmarkCategory("ET.Get"), Benchmark(Baseline = true)]
        public object GetViaCompiledExpressionTreesTyped()
            => _compiledExpressionTreesGetterTyped(_test);

        [BenchmarkCategory("ET.Set"), Benchmark(Baseline = true)]
        public void SetViaCompiledExpressionTreesTyped()
            => _compiledExpressionTreesSetterTyped(_test, nameof(SetViaCompiledExpressionTreesReflectionStyle));
    }
}