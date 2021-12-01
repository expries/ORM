using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ORM.Core.Models.Extensions
{
    public static class TypeExtensions
    {
        public static EntityTable ToTable(this Type type)
        {
            return new EntityTable(type);
        }
        
        public static bool IsInternalType(this Type type)
        {
            return type.IsValueType || type == typeof(string);
        }

        public static bool IsExternalType(this Type type)
        {
            return !type.IsInternalType();
        }

        public static PropertyInfo? GetNavigatedProperty(this Type type, Type other)
        {
            var navigatedProperty = type
                .GetProperties()
                .FirstOrDefault(p => 
                    p.PropertyType == other || 
                    p.PropertyType.IsCollectionOfOneType() && 
                    p.PropertyType.GetGenericArguments().First() == other);

            return navigatedProperty;
        }

        public static Type? GetFirstGenericArgument(this Type type)
        {
            return !type.IsCollectionOfOneType() ? null : type.GetGenericArguments().First();
        }

        public static bool IsCollectionOfOneType(this Type type)
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