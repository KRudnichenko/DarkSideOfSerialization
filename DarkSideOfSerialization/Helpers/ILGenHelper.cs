using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Sigil;
// ReSharper disable UnusedMember.Local

namespace DarkSideOfSerialization.Helpers
{
    public static class ILGenHelper
    {
        private static readonly ModuleBuilder Module;

        static ILGenHelper()
        {
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DarkSideOfSerialization.Emit.DynamicAssembly"), AssemblyBuilderAccess.Run);
            Module = asm.DefineDynamicModule("DynamicModule");
        }

        public static Action<TTarget, TParam> GenerateSetter<TTarget, TParam>(PropertyInfo propertyInfo)
        {
            #region ...prepare...
            var method = new DynamicMethod(propertyInfo.Name + "SetterTyped2", null,
                new[] { typeof(object), typeof(TParam) }, Module, true);

            var gen = method.GetILGenerator();
            var setMethod = propertyInfo.GetSetMethod(true);
            #endregion

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, setMethod);
            gen.Emit(OpCodes.Ret);

            return (Action<TTarget, TParam>)method.CreateDelegate(typeof(Action<TTarget, TParam>));
        }

        public static Action<object, object> GenerateSetter(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;

            var method = new DynamicMethod(propertyInfo.Name + "Setter", null,
                new[] { typeof(object), typeof(object) }, Module, true);

            var gen = method.GetILGenerator();
            var setMethod = propertyInfo.GetSetMethod(nonPublic:true);


            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(propertyType.IsValueType ?
                OpCodes.Unbox_Any : OpCodes.Castclass, propertyType);
            gen.Emit(OpCodes.Call, setMethod);
            gen.Emit(OpCodes.Ret);

            return (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
        }

        public static Action<object, TProperty> GenerateSetter<TProperty>(PropertyInfo propertyInfo)
        {
            var method = new DynamicMethod(propertyInfo.Name + "SetterTyped", null,
                new[] { typeof(object), typeof(TProperty) }, Module, true);

            var gen = method.GetILGenerator();
            var setMethod = propertyInfo.GetSetMethod(true);
            var targetType = propertyInfo.DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, targetType);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, setMethod);
            gen.Emit(OpCodes.Ret);

            return (Action<object, TProperty>)method.CreateDelegate(typeof(Action<object, TProperty>));
        }

        

        public static Func<object, TProperty> GenerateGetter<TProperty>(PropertyInfo propertyInfo)
        {
            var method = new DynamicMethod(propertyInfo.Name + "GetterTyped", typeof(TProperty),
                new[] { typeof(object) },
                Module, true);

            var gen = method.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            var targetType = propertyInfo.DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, targetType);
            gen.Emit(OpCodes.Call, getMethod);
            gen.Emit(OpCodes.Ret);

            return (Func<object, TProperty>)method.CreateDelegate(typeof(Func<object, TProperty>));
        }

        public static Func<TTarget, TProperty> GenerateGetter<TTarget, TProperty>(PropertyInfo propertyInfo)
        {
            var method = new DynamicMethod(propertyInfo.Name + "GetterTyped2", typeof(TProperty),
                new[] { typeof(object) },
                Module, true);

            var gen = method.GetILGenerator();
            var getMethod = propertyInfo.GetGetMethod(true);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, getMethod);
            gen.Emit(OpCodes.Ret);

            return (Func<TTarget, TProperty>)method.CreateDelegate(typeof(Func<TTarget, TProperty>));
        }

        public static Func<object, object> GenerateGetter(PropertyInfo propertyInfo)
        {
            var method = new DynamicMethod(propertyInfo.Name + "Getter", typeof(object),
                new[] { typeof(object) },
                Module, true);

            var gen = method.GetILGenerator();

            var targetType = propertyInfo.DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, targetType);
            gen.Emit(OpCodes.Call, propertyInfo.GetGetMethod(true));
            if (propertyInfo.PropertyType.IsValueType)
                gen.Emit(OpCodes.Box, propertyInfo.PropertyType);
            gen.Emit(OpCodes.Ret);

            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        public static Action<object, BinaryWriter> GenerateWrite(PropertyInfo propertyInfo)
        {
            var writeMethod = typeof(BinaryWriter).GetMethod("Write", new[] { propertyInfo.PropertyType });
            if (writeMethod == null)
                throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

            return Emit<Action<object, BinaryWriter>>
                .NewDynamicMethod()
                .LoadArgument(1)
                .LoadArgument(0)
                .CastClass(propertyInfo.DeclaringType)
                .Call(propertyInfo.GetGetMethod(true))
                .Call(writeMethod)
                .Return()
                .CreateDelegate();
        }

        public static Action<object, BinaryReader> GenerateRead(PropertyInfo propertyInfo)
        {
            var readMethod = Utils.GetReadMethod(propertyInfo);

            return Emit<Action<object, BinaryReader>>
                .NewDynamicMethod("ReadStringProperty")
                .LoadArgument(0)
                .CastClass(propertyInfo.DeclaringType)
                .LoadArgument(1)
                .Call(readMethod)
                .Call(propertyInfo.GetSetMethod(true))
                .Return()
                .CreateDelegate();
        }
    }
}