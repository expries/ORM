using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ORM.Core.Models.Extensions
{
    /// <summary>
    /// Extension methods for type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Creates a table model for a given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static EntityTable ToTable(this Type type)
        {
            return new EntityTable(type);
        }
        
        /// <summary>
        /// Returns if a given type is internal
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsValueType(this Type type)
        {
            return type.IsValueType || type == typeof(string);
        }

        /// <summary>
        /// Returns if a given type is external
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsExternalType(this Type type)
        {
            return !type.IsValueType();
        }

        /// <summary>
        /// Gets a property on type 'type' that itself has the underlying type of 'other'
        /// </summary>
        /// <param name="type"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static PropertyInfo? GetNavigatedProperty(this Type type, Type other)
        {
            var navigatedProperty = type
                .GetProperties()
                .FirstOrDefault(p => p.PropertyType.GetUnderlyingType() == other.GetProxiedType() );

            return navigatedProperty;
        }

        /// <summary>
        /// Returns if the type is a proxy type created by ProxyFactory
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsProxy(this Type type)
        {
            return type.BaseType is not null && type.Name == $"{type.BaseType.Name}Proxy";
        }
        
        /// <summary>
        /// If the type is a proxy, returns the type that is being proxied. Otherwise returns null
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetProxiedType(this Type type)
        {
            return type.IsProxy() && type.BaseType is not null ? type.BaseType : type;
        }
        
        /// <summary>
        /// Returns if type is a collection of another type (e.g. a list of objects)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

            var arguments = type.GetGenericArguments();
            return arguments.Length == 1;
        }
        
        /// <summary>
        /// If a type is a collection of another type, return the type of the collection.
        /// For example, if the type is a collection of teachers it returns a teacher
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetUnderlyingType(this Type type)
        {
            var underlyingType = type;
            
            if (underlyingType.IsCollectionOfOneType())
            {
                underlyingType = type.GetGenericArguments().First();
            }

            return underlyingType;
        }
        
        /// <summary>
        /// Returns if the type can be assignable to a collection
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsCollection(this Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sequenceType"></param>
        /// <returns></returns>
        public static Type GetElementTypeCustom(this Type sequenceType)
        {
            var enumType = FindIEnumerable(sequenceType);
            
            if (enumType is null)
            {
                return sequenceType;
            }
            
            return enumType.GetGenericArguments().First();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sequenceType"></param>
        /// <returns></returns>
        private static Type? FindIEnumerable(this Type? sequenceType) 
        {
            if (sequenceType == null || sequenceType == typeof(string))
            {
                return null;
            }

            if (sequenceType.IsArray)
            {
                var elementType = sequenceType.GetElementType();
                
                if (elementType is null)
                {
                    throw new ArgumentException($"Cannot get element type for sequence type {sequenceType.Name}");
                }
                
                return typeof(IEnumerable<>).MakeGenericType(elementType);
            }

            if (sequenceType.IsGenericType)
            {
                foreach (var arg in sequenceType.GetGenericArguments())
                {
                    var enumType = typeof(IEnumerable<>).MakeGenericType(arg);
                    
                    if (enumType.IsAssignableFrom(sequenceType))
                    {
                        return enumType;
                    }
                }
            }

            var interfaceTypes = sequenceType.GetInterfaces();

            if (interfaceTypes.Length > 0)
            {
                foreach (var interfaceType in interfaceTypes) 
                {
                    var enumType = FindIEnumerable(interfaceType);

                    if (enumType is not null)
                    {
                        return enumType;
                    }
                }
            }

            if (sequenceType.BaseType is not null && sequenceType.BaseType != typeof(object))
            {
                return FindIEnumerable(sequenceType.BaseType);
            }

            return null;
        }
    }
}