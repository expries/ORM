using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ORM.Core.Helpers
{
    public static class TypeExtensions
    {
        public static bool IsConvertibleToDbColumn(this Type type)
        {
            return type.IsValueType || type == typeof(string);
        }

        public static PropertyInfo GetPropertyOfTypeFirstOrDefault(this Type type, Type other)
        {
            var propertyOnOtherType = type
                .GetProperties()
                .FirstOrDefault(p => 
                    p.PropertyType == other || 
                    p.PropertyType.IsCollectionOfAType() && 
                    p.PropertyType.GetGenericArguments().First() == other);

            return propertyOnOtherType;
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

            Type[] arguments = type.GetGenericArguments();
            return arguments.Length == 1;
        }
        
        private static bool IsCollection(this Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type);
        }
    }
}