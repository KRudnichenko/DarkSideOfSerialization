using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DarkSideOfSerialization.Helpers
{
    public static class DelegateHelper
    {
        private const string methodHelperName = nameof(GetGetMethodHelper);
        private const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly Type helperClass = typeof(DelegateHelper);

        public static Func<object, object> GetGetMethod(PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod(true);
            var generic = helperClass.GetMethod(methodHelperName, bindingFlags);

            var targetType = propertyInfo.DeclaringType;
            var returnType = getMethod.ReturnType;

            var constructed = generic.MakeGenericMethod(targetType, returnType);

            var result = constructed.Invoke(null, new object[] { getMethod });

            return (Func<object, object>)result;
        }

        private static Func<object, object> GetGetMethodHelper<TTarget, TReturn>(MethodInfo method)
            where TTarget : class
        {
            var func = CreateDelegate<Func<TTarget, TReturn>>(method);

            return target => (object)func((TTarget)target);
        }

        private static TDelegate CreateDelegate<TDelegate>(MethodInfo method)
            where TDelegate : Delegate
        {
            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), method);
        }

        public static Action<object, object> GetSetMethod(PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.GetSetMethod(true);

            var genericHelper = helperClass.GetMethod(nameof(GetSetMethodHelper), bindingFlags);

            var targetType = propertyInfo.DeclaringType;
            var propertyType = propertyInfo.PropertyType;

            var constructedHelper = genericHelper.MakeGenericMethod(targetType, propertyType);

            var result = constructedHelper.Invoke(null, new object[] { setMethod });

            return (Action<object, object>)result;
        }

        static Action<object, object> GetSetMethodHelper<TTarget, TParam>(MethodInfo method)
            where TTarget : class
        {
            var setter = CreateDelegate<Action<TTarget, TParam>>(method);

            return (object target, object value) => setter((TTarget)target, (TParam)value);
        }

        public static Action<object, BinaryWriter> GenerateWrite(PropertyInfo propertyInfo)
        {
            var getPropDelegateType = typeof(Func<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            var getPropDelegate = Delegate.CreateDelegate(getPropDelegateType, propertyInfo.GetGetMethod(true));

            var writeMethod = typeof(BinaryWriter).GetMethod("Write", new[] { propertyInfo.PropertyType });
            if (writeMethod == null)
                throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

            var writerDelegateType = typeof(Action<,>).MakeGenericType(typeof(BinaryWriter), propertyInfo.PropertyType);
            var writeDelegate = Delegate.CreateDelegate(writerDelegateType, writeMethod);

            var helperGeneric = typeof(DelegateHelper).GetMethod(nameof(WriteMethodHelper), BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(helperGeneric != null, nameof(helperGeneric) + " != null");

            var constructedHelper = helperGeneric.MakeGenericMethod(typeof(BinaryWriter), propertyInfo.DeclaringType, propertyInfo.PropertyType);

            return (Action<object, BinaryWriter>)constructedHelper.Invoke(null, new object[] { writeDelegate, getPropDelegate });
        }
            
        private static Action<object, TWriter> WriteMethodHelper<TWriter, TTarget, TProperty>(Action<TWriter, TProperty> write, Func<TTarget, TProperty> getter)
        {
            return (t, w) => write(w, getter((TTarget)t));
        }

        public static Action<object, BinaryReader> GenerateRead(PropertyInfo propertyInfo)
        {
            var setPropDelegateType = typeof(Action<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            var setPropDelegate = Delegate.CreateDelegate(setPropDelegateType, propertyInfo.GetSetMethod(true));

            var readMethod = Utils.GetReadMethod(propertyInfo);
            if (readMethod == null)
                throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

            var readerDelegateType = typeof(Func<,>).MakeGenericType(typeof(BinaryReader), propertyInfo.PropertyType);
            var readDelegate = Delegate.CreateDelegate(readerDelegateType, readMethod);

            var helperGeneric = typeof(DelegateHelper).GetMethod(nameof(ReadMethodHelper), BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(helperGeneric != null, nameof(helperGeneric) + " != null");
            var constructedHelper = helperGeneric.MakeGenericMethod(typeof(BinaryReader), propertyInfo.DeclaringType, propertyInfo.PropertyType);

            return (Action<object, BinaryReader>)constructedHelper.Invoke(null, new object[] { readDelegate, setPropDelegate });
        }

        private static Action<object, TReader> ReadMethodHelper<TReader, TTarget, TProperty>(Func<TReader, TProperty> read, Action<TTarget, TProperty> setter)
        {
            return (target, reader) => setter((TTarget)target, read(reader));
        }
    }
}