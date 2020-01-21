using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
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
    [SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
    public class IntPropertyTypedVsReflectionStyleAccess
    {

        private readonly Test _test = new Test(555);

        private static readonly Type TargetType = typeof(Test);

        private const string PropertyName = nameof(Test.IntProperty);

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
            => CachedPropertyInfo.SetValue(_test, 42);

        private readonly TypeAccessor _accessor
            = TypeAccessor.Create(TargetType, allowNonPublicAccessors: true);

        [BenchmarkCategory("FastMember.Set"), Benchmark]
        public object GetViaFastMember()
            => _accessor[_test, PropertyName];

        [BenchmarkCategory("FastMember.Get"), Benchmark]
        public void SetViaFastMember()
            => _accessor[_test, PropertyName] = 42;

        private readonly Func<object, object> _getDelegate = DelegateHelper.GetGetMethod(CachedPropertyInfo);
        private readonly Action<object, object> _setDelegate = DelegateHelper.GetSetMethod(CachedPropertyInfo);

        private readonly Func<Test, int> _getDelegateTyped
            = (Func<Test, int>)Delegate.CreateDelegate(typeof(Func<Test, int>), CachedPropertyInfo.GetGetMethod(true));

        private readonly Action<Test, int> _setDelegateTyped
            = (Action<Test, int>)Delegate.CreateDelegate(typeof(Action<Test, int>), CachedPropertyInfo.GetSetMethod(true));

        [BenchmarkCategory("Delegate.Get"), Benchmark]
        public object GetViaDelegateReflectionStyle()
            => _getDelegate(_test);

        [BenchmarkCategory("Delegate.Set"), Benchmark]
        public void SetViaDelegateReflectionStyle()
            => _setDelegate(_test, 42);

        [BenchmarkCategory("Delegate.Get"), Benchmark(Baseline = true)]
        public int GetViaDelegateTyped()
            => _getDelegateTyped(_test);

        [BenchmarkCategory("Delegate.Set"), Benchmark(Baseline = true)]
        public void SetViaDelegateTyped()
            => _setDelegateTyped(_test, 42);

        private readonly Func<object, object> _ilGenGetter = ILGenHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _ilGenSetter = ILGenHelper.GenerateSetter(CachedPropertyInfo);
        private readonly Func<Test, int> _ilGenGetterTyped = ILGenHelper.GenerateGetter<Test, int>(CachedPropertyInfo);
        private readonly Action<Test, int> _ilGenSetterTyped = ILGenHelper.GenerateSetter<Test, int>(CachedPropertyInfo);

        [BenchmarkCategory("ILGen.Get"), Benchmark]
        public object GetViaILGenReflectionStyle()
            => _ilGenGetter(_test);

        [BenchmarkCategory("ILGen.Set"), Benchmark]
        public void SetViaILGenReflectionStyle()
            => _ilGenSetter(_test, 42);

        [BenchmarkCategory("ILGen.Get"), Benchmark(Baseline = true)]
        public int GetViaILGenTyped()
            => _ilGenGetterTyped(_test);

        [BenchmarkCategory("ILGen.Set"), Benchmark(Baseline = true)]
        public void SetViaILGenTyped()
            => _ilGenSetterTyped(_test, 42);

        private readonly Func<object, object> _compiledExpressionTreesGetter = CompiledExpressionTreesHelper.GenerateGetter(CachedPropertyInfo);
        private readonly Action<object, object> _compiledExpressionTreesSetter = CompiledExpressionTreesHelper.GenerateSetter(CachedPropertyInfo);
        private readonly Func<Test, int> _compiledExpressionTreesGetterTyped = CompiledExpressionTreesHelper.GenerateGetter<Test, int>(CachedPropertyInfo);
        private readonly Action<Test, int> _compiledExpressionTreesSetterTyped = CompiledExpressionTreesHelper.GenerateSetter<Test, int>(CachedPropertyInfo);

        [BenchmarkCategory("ET.Get"), Benchmark]
        public object GetViaCompiledExpressionTreesReflectionStyle()
            => _compiledExpressionTreesGetter(_test);

        [BenchmarkCategory("ET.Set"), Benchmark]
        public void SetViaCompiledExpressionTreesReflectionStyle()
            => _compiledExpressionTreesSetter(_test, 42);


        [BenchmarkCategory("ET.Get"), Benchmark(Baseline = true)]
        public int GetViaCompiledExpressionTreesTyped()
            => _compiledExpressionTreesGetterTyped(_test);

        [BenchmarkCategory("ET.Set"), Benchmark(Baseline = true)]
        public void SetViaCompiledExpressionTreesTyped()
            => _compiledExpressionTreesSetterTyped(_test, 42);
    }
}