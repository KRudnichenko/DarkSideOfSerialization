using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace DarkSideOfSerialization.Helpers
{
    public static class CompiledExpressionTreesHelper
    {
        public static Action<TTarget, TParam> GenerateSetter<TTarget, TParam>(PropertyInfo property)
        {
            var setMethod = property.GetSetMethod(true);
            var targetParam = Expression.Parameter(typeof(TTarget));
            var valueParam = Expression.Parameter(typeof(TParam));

            var callSetter = Expression.Call(targetParam, setMethod, valueParam);

            return Expression
                .Lambda<Action<TTarget, TParam>>(callSetter, targetParam, valueParam)
                .Compile();
        }

        public static Action<object, object> GenerateSetter(PropertyInfo property)
        {
            var setMethod = property.GetSetMethod(true);
            var targetParam = Expression.Parameter(typeof(object));
            var valueParam = Expression.Parameter(typeof(object));

            var convertedTarget = Expression.Convert(targetParam, property.DeclaringType);
            var convertedValue = Expression.Convert(valueParam, property.PropertyType);

            var callSetter = Expression.Call(convertedTarget, setMethod, convertedValue);

            return Expression
                .Lambda<Action<object, object>>(callSetter, targetParam, valueParam)
                .Compile();
        }

        public static Func<TTarget, TProperty> GenerateGetter<TTarget, TProperty>(PropertyInfo propertyInfo)
        {
            var sourceGetMethod = propertyInfo.GetGetMethod(true);
            Debug.Assert(sourceGetMethod != null, nameof(sourceGetMethod) + " != null");

            var param = Expression.Parameter(typeof(TTarget), "param");

            Expression getValueExpression = Expression.Property(param, propertyInfo.Name);

            return Expression
                .Lambda<Func<TTarget, TProperty>>(getValueExpression, param)
                .Compile();
        }

        public static Func<object, object> GenerateGetter(PropertyInfo propertyInfo)
        {
            var sourceGetMethod = propertyInfo.GetGetMethod(true);
            Debug.Assert(sourceGetMethod != null, nameof(sourceGetMethod) + " != null");

            var target = Expression.Parameter(typeof(object), "param");

            var targetType = propertyInfo.DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");

            var convertedTarget = Expression.Convert(target, targetType);

            var getValueExpression = Expression.Property(convertedTarget, propertyInfo.Name);

            return Expression
                .Lambda<Func<object, object>>(Expression.Convert(getValueExpression, typeof(object)), target)
                .Compile();
        }

        public static Action<object, BinaryReader> GenerateRead(PropertyInfo propertyInfo)
        {
            var targetParam = Expression.Parameter(typeof(object));
            var readerParam = Expression.Parameter(typeof(BinaryReader));

            var targetType = propertyInfo.DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");

            var convertedTarget = Expression.Convert(targetParam, targetType);

            var readMethod = Utils.GetReadMethod(propertyInfo);

            if (readMethod == null)
                throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

            var callRead = Expression.Call(readerParam, readMethod);

            var valueExpr = propertyInfo.PropertyType.IsEnum ? (Expression)Expression.Convert(callRead, propertyInfo.PropertyType) : callRead;

            var setMethodCall = Expression.Call(convertedTarget, propertyInfo.GetSetMethod(true), valueExpr);

            return
                Expression.Lambda<Action<object, BinaryReader>>(setMethodCall, targetParam, readerParam)
                .Compile();
        }

        public static Action<object, BinaryReader> GenerateRead(PropertyInfo[] propertyInfos)
        {
            Debug.Assert(propertyInfos.Length > 0, "propertyInfos.Length > 0");

            var targetParam = Expression.Parameter(typeof(object));
            var readerParam = Expression.Parameter(typeof(BinaryReader));

            var targetType = propertyInfos[0].DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");
            var convertedTarget = Expression.Convert(targetParam, targetType);

            var serializationSteps = new List<Expression>();
            foreach (var propertyInfo in propertyInfos)
            {
                var readMethod = Utils.GetReadMethod(propertyInfo);

                if (readMethod == null)
                    throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

                var callRead = Expression.Call(readerParam, readMethod);

                var valueExpr = propertyInfo.PropertyType.IsEnum ? (Expression)Expression.Convert(callRead, propertyInfo.PropertyType) : callRead;

                var setMethodCall = Expression.Call(convertedTarget, propertyInfo.GetSetMethod(true), valueExpr);
                serializationSteps.Add(setMethodCall);
            }

            return
                Expression.Lambda<Action<object, BinaryReader>>(Expression.Block(serializationSteps), targetParam, readerParam)
                    .Compile();
        }

        public static Action<object, BinaryWriter> GenerateWrite(PropertyInfo propertyInfo)
        {
            var targetParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(BinaryWriter));

            var targetType = propertyInfo.DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");

            var convertedTarget = Expression.Convert(targetParam, targetType);
            var getValue = Expression.Property(convertedTarget, propertyInfo.Name);

            var writeArgType = propertyInfo.PropertyType.IsEnum ? typeof(int) : propertyInfo.PropertyType;
            var writeMethod = typeof(BinaryWriter).GetMethod("Write", new[] { writeArgType });
            if (writeMethod == null)
                throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

            var callWrite = Expression.Call(writerParam, writeMethod, getValue);

            return Expression.Lambda<Action<object, BinaryWriter>>(callWrite, targetParam, writerParam)
                .Compile();
        }

        public static Action<object, BinaryWriter> GenerateWrite(PropertyInfo[] propertyInfos)
        {
            Debug.Assert(propertyInfos.Length > 0, "propertyInfos.Length > 0");

            var targetParam = Expression.Parameter(typeof(object));
            var writerParam = Expression.Parameter(typeof(BinaryWriter));

            var targetType = propertyInfos[0].DeclaringType;
            Debug.Assert(targetType != null, nameof(targetType) + " != null");
            var convertedTarget = Expression.Convert(targetParam, targetType);

            var serializationSteps = new List<Expression>();
            foreach (var propertyInfo in propertyInfos)
            {
                var getValue = Expression.Property(convertedTarget, propertyInfo.Name);

                var writeArgType = propertyInfo.PropertyType.IsEnum ? typeof(int) : propertyInfo.PropertyType;
                var writeMethod = typeof(BinaryWriter).GetMethod("Write", new[] { writeArgType });
                if (writeMethod == null)
                    throw new NotSupportedException($"Not supported serialization type: {propertyInfo.PropertyType} ");

                var callWrite = Expression.Call(writerParam, writeMethod, getValue);
                serializationSteps.Add(callWrite);
            }

            var block = Expression.Block(serializationSteps);

            return Expression.Lambda<Action<object, BinaryWriter>>(block, targetParam, writerParam)
                .Compile();
        }
    }
}