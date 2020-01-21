using System;
using System.IO;
using System.Reflection;

namespace DarkSideOfSerialization.Helpers
{
    internal static class Utils
    {
        public static MethodInfo GetReadMethod(PropertyInfo propertyInfo)
        {
            var memberType = propertyInfo.PropertyType;
            var readerType = typeof(BinaryReader);

            if (memberType.IsEnum)

                return readerType.GetMethod("ReadInt32");
            if (memberType == typeof(string))
                return readerType.GetMethod("ReadString");
            if (memberType == typeof(int))
                return readerType.GetMethod("ReadInt32");
            if (memberType == typeof(double))
                return readerType.GetMethod("ReadDouble");
            if (memberType == typeof(long))
                return readerType.GetMethod("ReadInt64");
            if (memberType == typeof(byte))
                return readerType.GetMethod("ReadByte");
            if (memberType == typeof(bool))
                return readerType.GetMethod("ReadBoolean");

            throw new NotSupportedException();
        }
    }
}