using System;
using System.Collections;
using System.Linq;

namespace ORM.Core.Helpers
{
    public static class TypeExtensions
    {
        public static bool IsSimpleType(this Type type)
        {
            return type.IsValueType || type == typeof(string);
        }
        
        public static bool IsCollection(this Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type);
        }

        public static bool IsCollectionOfAType(this Type type)
        {
            if (!type.IsCollection())
            {
                return false;
            }

            if (!type.IsGenericType)
            {
                return false;
            }

            var arguments = type.GetGenericArguments();

            if (arguments.Length != 1)
            {
                return false;
            }

            var underlyingType = arguments.First();
            return !underlyingType.IsSimpleType();
        }
    }
}