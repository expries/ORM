using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Database;

namespace ORM.Core.Helpers
{
    internal static class AssemblyScanner
    {
        public static IEnumerable<Type> GetEntityTypes(Assembly assembly)
        {
            var dbContexts = GetDbContextTypes(assembly);
            IEnumerable<Type?> entities = new List<Type>();
            
            foreach (var context in dbContexts)
            {
                var properties = context.GetProperties();
                var propertyTypes = properties.Select(p => p.PropertyType).Distinct();
                var genericProperties = propertyTypes.Where(t => t.IsGenericType);
                var dbSetProperties = genericProperties.Where(t => t.GetGenericTypeDefinition() == typeof(DbSet<>));
                var entityTypes = dbSetProperties.Select(t => t.GetGenericArguments().FirstOrDefault());
                entities = entities.Union(entityTypes);
            }

            return entities;
        }
        
        private static IEnumerable<Type> GetDbContextTypes(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            IEnumerable<Type> contexts = types.Where(t => t.IsSubclassOf(typeof(DbContext)));
            return contexts;
        }
    }
}