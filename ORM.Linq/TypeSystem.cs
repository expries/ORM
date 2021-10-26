using System;
using System.Collections.Generic;

namespace ORM.Linq
{
    internal static class TypeSystem 
    {
        internal static Type GetElementType(Type sequenceType)
        {
            var enumType = FindIEnumerable(sequenceType);
            
            if (enumType is null)
            {
                return sequenceType;
            }
            
            return enumType.GetGenericArguments()[0];
        }

        private static Type? FindIEnumerable(Type? sequenceType) 
        {
            if (sequenceType == null || sequenceType == typeof(string))
            {
                return null;
            }

            if (sequenceType.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(sequenceType.GetElementType());
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